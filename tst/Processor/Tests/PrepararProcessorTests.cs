using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Processor.Application;
using Processor.Domain.Processamento;
using Processor.Infra;
using Processor.Infra.Ffmpeg;
using Processor.Infra.Filas;
using Processor.Infra.Redis;
using Processor.Infra.Storage;

namespace Processor.Tests;

public class PrepararProcessorTests
{
    [Fact]
    public async Task Prepara_o_bucket_e_para()
    {
        var cliente = new ClienteFalso();
        var preparo = new PrepararProcessorHostedService(cliente, CaminhoZip.BucketPadrao);

        await preparo.StartAsync(CancellationToken.None);
        await preparo.StopAsync(CancellationToken.None);

        Assert.Equal(CaminhoZip.BucketPadrao, cliente.Bucket);
    }

    [Fact]
    public async Task Memoria_sobe_sem_fila_externa()
    {
        var servicos = new ServiceCollection();
        servicos.AddInfrastructure(Configuracao(new Dictionary<string, string?>
        {
            ["Processor:Provider"] = "Memory"
        }));
        await using var provedor = servicos.BuildServiceProvider();

        Assert.IsType<MarcaMemoria>(provedor.GetRequiredService<IMarcaVideo>());
        Assert.IsType<ArmazenamentoProcessamentoMemoria>(provedor.GetRequiredService<IArmazenamentoProcessamento>());
        Assert.IsType<FilaStatusMemoria>(provedor.GetRequiredService<IFilaStatus>());
        Assert.IsType<QuebraVideoFixa>(provedor.GetRequiredService<IQuebraVideo>());
        Assert.DoesNotContain(servicos, descritor => descritor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public async Task Registra_storage_fila_redis_e_ffmpeg_sem_abrir_conexao()
    {
        var servicos = new ServiceCollection();
        servicos.AddApplication();
        servicos.AddInfrastructure(Configuracao(Real()));
        await using var provedor = servicos.BuildServiceProvider();

        Assert.IsType<ArmazenamentoProcessamentoS3>(provedor.GetRequiredService<IArmazenamentoProcessamento>());
        Assert.IsType<FilaStatus>(provedor.GetRequiredService<IFilaStatus>());
        Assert.IsType<MarcaProcessamento>(provedor.GetRequiredService<IMarcaVideo>());
        Assert.IsType<QuebraVideoFfmpeg>(provedor.GetRequiredService<IQuebraVideo>());
        Assert.Equal(2, servicos.Count(descritor => descritor.ServiceType == typeof(IHostedService)));
        Assert.IsType<ClienteObjetoS3>(provedor.GetRequiredService<IClienteObjeto>());

        var fabrica = provedor.GetRequiredService<Func<ISessaoConsumo>>();
        var sessao = fabrica();

        Assert.IsType<SessaoConsumo>(sessao);
    }

    [Fact]
    public async Task Fila_cancelada_nao_abre_conexao()
    {
        var publicador = new PublicadorFilaRabbit("amqp://127.0.0.1:1");
        var token = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publicador.DeclararAsync("status", token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            publicador.PublicarAsync("status", new byte[] { 1 }, token));

        await using var canal = new CanalConsumoRabbit(new OpcoesFila("amqp://127.0.0.1:1", "processamento", "status"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canal.PrepararAsync(token));
    }

    [Fact]
    public void Consumidor_nasce_com_a_espera_padrao()
    {
        var servico = new ConsumidorHostedService(
            () => new SessaoScript(_ => Task.CompletedTask),
            NullLogger<ConsumidorHostedService>.Instance);

        Assert.IsType<ConsumidorHostedService>(servico);
    }

    [Fact]
    public void Completa_bucket_e_nomes_de_fila_quando_vem_em_branco()
    {
        var servicos = new ServiceCollection();
        var valores = Real();
        valores["Storage:Bucket"] = " ";
        valores["Queue:Processamento"] = " ";
        valores["Queue:Status"] = "";

        servicos.AddInfrastructure(Configuracao(valores));

        Assert.Contains(servicos, descritor => descritor.ServiceType == typeof(IHostedService));
    }

    [Theory]
    [InlineData("Storage:ServiceUrl")]
    [InlineData("Storage:AccessKey")]
    [InlineData("Storage:SecretKey")]
    [InlineData("Queue:Uri")]
    [InlineData("Redis:Conexao")]
    public void Recusa_configuracao_incompleta(string chave)
    {
        var valores = Real();
        valores.Remove(chave);
        var servicos = new ServiceCollection();

        var erro = Assert.Throws<InvalidOperationException>(() => servicos.AddInfrastructure(Configuracao(valores)));

        Assert.Contains(chave, erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Canal_sem_conexao_confirma_o_cancelamento_e_recusa_ack()
    {
        await using var canal = new CanalConsumoRabbit(new OpcoesFila("amqp://localhost", "processamento", "status"));
        using var fonte = new CancellationTokenSource();
        fonte.Cancel();

        var pacote = await canal.ReceberAsync(fonte.Token);

        Assert.Null(pacote);
        await Assert.ThrowsAsync<InvalidOperationException>(() => canal.ConfirmarAsync(1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => canal.RecolocarAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task Consumidor_tenta_de_novo_e_para_no_cancelamento()
    {
        var chamadas = 0;
        var segunda = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var servico = new ConsumidorHostedService(() => new SessaoScript(async cancellationToken =>
        {
            var atual = Interlocked.Increment(ref chamadas);
            if (atual == 1)
                throw new IOException("caiu");

            segunda.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }), TimeSpan.Zero, NullLogger<ConsumidorHostedService>.Instance);

        await servico.StartAsync(CancellationToken.None);
        await segunda.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await servico.StopAsync(CancellationToken.None);

        Assert.True(chamadas >= 2);
    }

    [Fact]
    public async Task Consumidor_nao_tenta_de_novo_quando_o_host_para_na_espera()
    {
        var chamadas = 0;
        var primeira = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var servico = new ConsumidorHostedService(() => new SessaoScript(_ =>
        {
            Interlocked.Increment(ref chamadas);
            primeira.TrySetResult();
            throw new IOException("caiu");
        }), TimeSpan.FromMinutes(1), NullLogger<ConsumidorHostedService>.Instance);

        await servico.StartAsync(CancellationToken.None);
        await primeira.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await servico.StopAsync(CancellationToken.None);

        Assert.Equal(1, chamadas);
    }

    private static Dictionary<string, string?> Real() => new()
    {
        ["Storage:ServiceUrl"] = "http://localhost:9000",
        ["Storage:AccessKey"] = "fiapx",
        ["Storage:SecretKey"] = "fiapxfiapx",
        ["Storage:Bucket"] = "videos",
        ["Queue:Uri"] = "amqp://fiapx:fiapx@localhost:5672/",
        ["Queue:Processamento"] = "processamento",
        ["Queue:Status"] = "status",
        ["Redis:Conexao"] = "localhost:6379,password=fiapx"
    };

    private static IConfiguration Configuracao(Dictionary<string, string?> valores) =>
        new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

    private sealed class ClienteFalso : IClienteObjeto
    {
        public string? Bucket { get; private set; }

        public Task GarantirBucketAsync(string bucket, CancellationToken cancellationToken)
        {
            Bucket = bucket;
            return Task.CompletedTask;
        }

        public Task GravarAsync(string bucket, string chave, Stream conteudo, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<Stream> BaixarAsync(string bucket, string chave, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream());
    }

    private sealed class SessaoScript(Func<CancellationToken, Task> executar) : ISessaoConsumo
    {
        public Task ExecutarAsync(CancellationToken cancellationToken) => executar(cancellationToken);
    }
}

public class MetricasProcessorTests
{
    [Fact]
    public void Conta_o_momento_e_o_andamento()
    {
        var nome = "FiapX.Processor.Teste." + Guid.NewGuid().ToString("N");
        using var metricas = new MetricasProcessor(nome);
        long resultados = 0;
        var emAndamento = -1;
        using var ouvinte = new MeterListener();
        ouvinte.InstrumentPublished = (instrumento, listener) =>
        {
            if (instrumento.Meter.Name == nome)
                listener.EnableMeasurementEvents(instrumento);
        };
        ouvinte.SetMeasurementEventCallback<long>((instrumento, valor, _, _) =>
        {
            if (instrumento.Name == MetricasProcessor.Resultados)
                resultados += valor;
        });
        ouvinte.SetMeasurementEventCallback<int>((instrumento, valor, _, _) =>
        {
            if (instrumento.Name == MetricasProcessor.EmAndamento)
                emAndamento = valor;
        });
        ouvinte.Start();

        metricas.Registrar(MomentoStatus.Sucesso);
        metricas.Entrar();
        ouvinte.RecordObservableInstruments();
        metricas.Sair();

        Assert.Equal(1, resultados);
        Assert.Equal(1, emAndamento);
    }
}
