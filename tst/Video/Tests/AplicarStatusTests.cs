using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Video.Application;
using Video.Application.Status;
using Video.Domain.Videos;
using Video.Infra.Filas;

namespace Video.Tests;

public class AplicarStatusTests
{
    [Theory]
    [InlineData("")]
    [InlineData("nao-json")]
    [InlineData("{}")]
    [InlineData("{\"id\":\"00000000-0000-0000-0000-000000000000\",\"momento\":\"comecou\"}")]
    public void Recusa_corpo_sem_id(string texto)
    {
        var mensagem = MensagemStatus.Ler(Encoding.UTF8.GetBytes(texto));

        Assert.False(mensagem.Legivel);
    }

    [Fact]
    public async Task Comecou_sucesso_e_erro_gravam_e_a_repeticao_nao_reabre()
    {
        var id = Guid.NewGuid();
        var repositorio = new RepositorioFalso();
        var video = global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/x/a.mp4").Valor!;
        repositorio.Guardar(video);
        var metricas = new MetricasVideo();
        var lista = new ListaFalsa();
        lista.Guardar("ana", [new ItemLista(id, StatusVideo.AguardandoProcessamento, null)]);
        var caso = new AplicarStatusUseCase(repositorio, lista, metricas);

        Assert.Equal(Confirmacao.Confirmar, await caso.ExecutarAsync(Corpo(id, MomentoStatus.Comecou, null), CancellationToken.None));
        Assert.Equal(StatusVideo.EmProcessamento, video.Status);

        var zip = $"videos/{id:D}/{id:D}.zip";
        Assert.Equal(Confirmacao.Confirmar, await caso.ExecutarAsync(Corpo(id, MomentoStatus.Sucesso, zip), CancellationToken.None));
        Assert.Equal(StatusVideo.Concluido, video.Status);
        Assert.Equal(zip, video.CaminhoZip);
        Assert.Equal(2, repositorio.Atualizacoes);

        await caso.ExecutarAsync(Corpo(id, MomentoStatus.Erro, null), CancellationToken.None);
        Assert.Equal(StatusVideo.Concluido, video.Status);
        Assert.Equal(2, repositorio.Atualizacoes);
        Assert.Equal(StatusVideo.Concluido, lista.Itens("ana")!.Single().Status);
        Assert.Equal(zip, lista.Itens("ana")!.Single().CaminhoZip);
    }

    [Fact]
    public async Task Erro_e_sucesso_fecham_sem_comecou()
    {
        var erro = Guid.NewGuid();
        var sucesso = Guid.NewGuid();
        var repositorio = new RepositorioFalso();
        var videoErro = global::Video.Domain.Videos.Video.Registrar(erro, "ana", "ana@email.com", "videos/x/a.mp4").Valor!;
        var videoSucesso = global::Video.Domain.Videos.Video.Registrar(sucesso, "ana", "ana@email.com", "videos/x/b.mp4").Valor!;
        repositorio.Guardar(videoErro);
        repositorio.Guardar(videoSucesso);
        var lista = new ListaFalsa();
        lista.Guardar("ana",
        [
            new ItemLista(erro, StatusVideo.AguardandoProcessamento, null),
            new ItemLista(sucesso, StatusVideo.AguardandoProcessamento, null)
        ]);
        var caso = new AplicarStatusUseCase(repositorio, lista, new MetricasVideo());

        await caso.ExecutarAsync(Corpo(erro, MomentoStatus.Erro, null), CancellationToken.None);
        await caso.ExecutarAsync(Corpo(sucesso, MomentoStatus.Sucesso, $"videos/{sucesso:D}/{sucesso:D}.zip"), CancellationToken.None);

        Assert.Equal(StatusVideo.Erro, videoErro.Status);
        Assert.Null(videoErro.CaminhoZip);
        Assert.Equal(StatusVideo.Concluido, videoSucesso.Status);
        Assert.NotNull(videoSucesso.CaminhoZip);
        Assert.Equal(StatusVideo.Erro, lista.Itens("ana")!.Single(item => item.Id == erro).Status);
        Assert.Null(lista.Itens("ana")!.Single(item => item.Id == erro).CaminhoZip);
        Assert.Equal(StatusVideo.Concluido, lista.Itens("ana")!.Single(item => item.Id == sucesso).Status);
        Assert.Equal($"videos/{sucesso:D}/{sucesso:D}.zip", lista.Itens("ana")!.Single(item => item.Id == sucesso).CaminhoZip);
    }

    [Fact]
    public async Task Ilegivel_ausente_e_sucesso_sem_caminho_confirmam_sem_alterar()
    {
        var id = Guid.NewGuid();
        var repositorio = new RepositorioFalso();
        var video = global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/x/a.mp4").Valor!;
        repositorio.Guardar(video);
        var caso = new AplicarStatusUseCase(repositorio, new ListaFalsa(), new MetricasVideo());

        Assert.Equal(Confirmacao.Confirmar, await caso.ExecutarAsync("{"u8.ToArray(), CancellationToken.None));
        Assert.Equal(Confirmacao.Confirmar, await caso.ExecutarAsync(Corpo(Guid.NewGuid(), MomentoStatus.Comecou, null), CancellationToken.None));
        Assert.Equal(Confirmacao.Confirmar, await caso.ExecutarAsync(Corpo(id, MomentoStatus.Sucesso, null), CancellationToken.None));

        Assert.Equal(StatusVideo.AguardandoProcessamento, video.Status);
        Assert.Equal(0, repositorio.Atualizacoes);
    }

    [Fact]
    public async Task Falha_ao_gravar_confirma_e_cancelamento_recoloca()
    {
        var id = Guid.NewGuid();
        var repositorio = new RepositorioFalso { FalhaAoAtualizar = true };
        repositorio.Guardar(global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/x/a.mp4").Valor!);
        var caso = new AplicarStatusUseCase(repositorio, new ListaFalsa(), new MetricasVideo());

        Assert.Equal(Confirmacao.Confirmar, await caso.ExecutarAsync(Corpo(id, MomentoStatus.Comecou, null), CancellationToken.None));

        repositorio.FalhaAoAtualizar = false;
        repositorio.Cancelar = true;
        Assert.Equal(Confirmacao.Recolocar, await caso.ExecutarAsync(Corpo(id, MomentoStatus.Erro, null), CancellationToken.None));
    }

    [Fact]
    public async Task Falha_da_lista_confirma_e_cancelamento_recoloca()
    {
        var id = Guid.NewGuid();
        var repositorio = new RepositorioFalso();
        var video = global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/x/a.mp4").Valor!;
        repositorio.Guardar(video);
        var lista = new ListaFalsa { Falhar = true };
        lista.Guardar("ana", [new ItemLista(id, StatusVideo.AguardandoProcessamento, null)]);
        var caso = new AplicarStatusUseCase(repositorio, lista, new MetricasVideo());

        Assert.Equal(Confirmacao.Confirmar, await caso.ExecutarAsync(Corpo(id, MomentoStatus.Comecou, null), CancellationToken.None));
        Assert.Equal(StatusVideo.EmProcessamento, video.Status);

        lista.Falhar = false;
        lista.Cancelar = true;
        Assert.Equal(Confirmacao.Recolocar, await caso.ExecutarAsync(Corpo(id, MomentoStatus.Erro, null), CancellationToken.None));
    }

    [Fact]
    public async Task Sessao_confirma_o_aplicado_recoloca_o_cancelado_e_descarta()
    {
        var id = Guid.NewGuid();
        var repositorio = new RepositorioFalso();
        repositorio.Guardar(global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/x/a.mp4").Valor!);
        var servicos = new ServiceCollection();
        servicos.AddSingleton<IVideoRepository>(repositorio);
        servicos.AddSingleton<IListaVideos>(new ListaFalsa());
        servicos.AddSingleton<MetricasVideo>();
        servicos.AddScoped<AplicarStatusUseCase>();
        await using var provedor = servicos.BuildServiceProvider();
        var canal = new CanalFalso();
        canal.Pacotes.Enqueue(new PacoteStatus(1, Encoding.UTF8.GetBytes("sem-json")));
        canal.Pacotes.Enqueue(new PacoteStatus(2, Corpo(id, MomentoStatus.Comecou, null)));
        var sessao = new SessaoStatus(canal, provedor.GetRequiredService<IServiceScopeFactory>());

        await sessao.ExecutarAsync(CancellationToken.None);

        Assert.True(canal.Preparado);
        Assert.True(canal.Descartado);
        Assert.Equal(new ulong[] { 1, 2 }, canal.Confirmadas);
        Assert.Equal(StatusVideo.EmProcessamento, (await repositorio.ObterAsync(id, CancellationToken.None))!.Status);
    }

    [Fact]
    public async Task Sessao_recoloca_quando_o_caso_e_cancelado()
    {
        var id = Guid.NewGuid();
        var repositorio = new RepositorioFalso { Cancelar = true };
        repositorio.Guardar(global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/x/a.mp4").Valor!);
        var servicos = new ServiceCollection();
        servicos.AddSingleton<IVideoRepository>(repositorio);
        servicos.AddSingleton<IListaVideos>(new ListaFalsa());
        servicos.AddSingleton<MetricasVideo>();
        servicos.AddScoped<AplicarStatusUseCase>();
        await using var provedor = servicos.BuildServiceProvider();
        var canal = new CanalFalso();
        canal.Pacotes.Enqueue(new PacoteStatus(7, Corpo(id, MomentoStatus.Erro, null)));
        var sessao = new SessaoStatus(canal, provedor.GetRequiredService<IServiceScopeFactory>());

        await sessao.ExecutarAsync(CancellationToken.None);

        Assert.Equal(new ulong[] { 7 }, canal.Recolocadas);
        Assert.Empty(canal.Confirmadas);
    }

    [Fact]
    public async Task Sessao_para_quando_o_token_ja_esta_cancelado()
    {
        var servicos = new ServiceCollection();
        servicos.AddSingleton<MetricasVideo>();
        servicos.AddSingleton<IListaVideos>(new ListaFalsa());
        servicos.AddScoped<AplicarStatusUseCase>();
        servicos.AddSingleton<IVideoRepository>(new RepositorioFalso());
        await using var provedor = servicos.BuildServiceProvider();
        var canal = new CanalFalso();
        var sessao = new SessaoStatus(canal, provedor.GetRequiredService<IServiceScopeFactory>());

        await sessao.ExecutarAsync(new CancellationToken(canceled: true));

        Assert.True(canal.Preparado);
        Assert.Equal(0, canal.Recebidas);
        Assert.True(canal.Descartado);
    }

    [Fact]
    public async Task Canal_cancelado_nao_abre_a_fila()
    {
        await using var canal = new CanalStatusRabbit(new OpcoesStatus("amqp://127.0.0.1:1", "status"));
        using var fonte = new CancellationTokenSource();
        fonte.Cancel();

        var pacote = await canal.ReceberAsync(fonte.Token);
        await canal.DisposeAsync();

        Assert.Null(pacote);
        await Assert.ThrowsAsync<InvalidOperationException>(() => canal.ConfirmarAsync(1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => canal.RecolocarAsync(1, CancellationToken.None));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canal.PrepararAsync(fonte.Token));
    }

    [Fact]
    public async Task Consumidor_tenta_de_novo_e_para_no_cancelamento()
    {
        var chamadas = 0;
        var segunda = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var servico = new ConsumidorStatusHostedService(() => new SessaoScript(async cancellationToken =>
        {
            var atual = Interlocked.Increment(ref chamadas);
            if (atual == 1)
                throw new IOException("caiu");

            segunda.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }), TimeSpan.Zero, NullLogger<ConsumidorStatusHostedService>.Instance);

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
        var servico = new ConsumidorStatusHostedService(() => new SessaoScript(_ =>
        {
            Interlocked.Increment(ref chamadas);
            primeira.TrySetResult();
            throw new IOException("caiu");
        }), TimeSpan.FromMinutes(1), NullLogger<ConsumidorStatusHostedService>.Instance);

        await servico.StartAsync(CancellationToken.None);
        await primeira.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await servico.StopAsync(CancellationToken.None);

        Assert.Equal(1, chamadas);
    }

    [Fact]
    public void Consumidor_nasce_com_a_espera_padrao()
    {
        var servico = new ConsumidorStatusHostedService(
            () => new SessaoScript(_ => Task.CompletedTask),
            NullLogger<ConsumidorStatusHostedService>.Instance);

        Assert.IsType<ConsumidorStatusHostedService>(servico);
    }

    [Fact]
    public void Conta_o_momento()
    {
        var nome = "FiapX.Video.Teste." + Guid.NewGuid().ToString("N");
        using var metricas = new MetricasVideo(nome);
        long total = 0;
        using var ouvinte = new MeterListener();
        ouvinte.InstrumentPublished = (instrumento, listener) =>
        {
            if (instrumento.Meter.Name == nome)
                listener.EnableMeasurementEvents(instrumento);
        };
        ouvinte.SetMeasurementEventCallback<long>((_, valor, _, _) => total += valor);
        ouvinte.Start();

        metricas.Registrar(MomentoStatus.Comecou);

        Assert.Equal(1, total);
    }

    private static byte[] Corpo(Guid id, string momento, string? caminho)
    {
        var json = caminho is null
            ? $"{{\"id\":\"{id:D}\",\"momento\":\"{momento}\"}}"
            : $"{{\"id\":\"{id:D}\",\"momento\":\"{momento}\",\"caminho\":\"{caminho}\"}}";
        return Encoding.UTF8.GetBytes(json);
    }

    private sealed class RepositorioFalso : IVideoRepository
    {
        private readonly Dictionary<Guid, global::Video.Domain.Videos.Video> _videos = [];

        public bool FalhaAoAtualizar { get; set; }

        public bool Cancelar { get; set; }

        public int Atualizacoes { get; private set; }

        public void Guardar(global::Video.Domain.Videos.Video video) => _videos[video.Id] = video;

        public Task AdicionarAsync(global::Video.Domain.Videos.Video video, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<global::Video.Domain.Videos.Video?> ObterAsync(Guid id, CancellationToken cancellationToken)
        {
            if (Cancelar)
                throw new OperationCanceledException();

            return Task.FromResult(_videos.GetValueOrDefault(id));
        }

        public Task<IReadOnlyList<global::Video.Domain.Videos.Video>> ListarPorLoginAsync(string login, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<global::Video.Domain.Videos.Video>>(_videos.Values.Where(video => video.Login == login).ToArray());

        public Task AtualizarAsync(global::Video.Domain.Videos.Video video, CancellationToken cancellationToken)
        {
            if (FalhaAoAtualizar)
                throw new IOException("banco");

            Atualizacoes++;
            return Task.CompletedTask;
        }
    }

    private sealed class ListaFalsa : IListaVideos
    {
        private readonly Dictionary<string, List<ItemLista>> _listas = [];

        public bool Falhar { get; set; }

        public bool Cancelar { get; set; }

        public void Guardar(string login, IReadOnlyList<ItemLista> itens) => _listas[login] = itens.ToList();

        public IReadOnlyList<ItemLista>? Itens(string login) =>
            _listas.TryGetValue(login, out var itens) ? itens : null;

        public Task<IReadOnlyList<ItemLista>?> ObterAsync(string login, CancellationToken cancellationToken) =>
            Task.FromResult(Itens(login));

        public Task GuardarAsync(string login, IReadOnlyList<ItemLista> itens, CancellationToken cancellationToken)
        {
            Guardar(login, itens);
            return Task.CompletedTask;
        }

        public Task AtualizarAsync(string login, ItemLista item, CancellationToken cancellationToken)
        {
            if (Cancelar)
                throw new OperationCanceledException();
            if (Falhar)
                throw new IOException("redis");
            if (!_listas.TryGetValue(login, out var itens))
                return Task.CompletedTask;

            _listas[login] = ItemLista.Incluir(itens, item).ToList();
            return Task.CompletedTask;
        }
    }

    private sealed class CanalFalso : ICanalStatus, IAsyncDisposable
    {
        public Queue<PacoteStatus> Pacotes { get; } = new();
        public List<ulong> Confirmadas { get; } = [];
        public List<ulong> Recolocadas { get; } = [];
        public bool Preparado { get; private set; }
        public bool Descartado { get; private set; }
        public int Recebidas { get; private set; }

        public Task PrepararAsync(CancellationToken cancellationToken)
        {
            Preparado = true;
            return Task.CompletedTask;
        }

        public Task<PacoteStatus?> ReceberAsync(CancellationToken cancellationToken)
        {
            Recebidas++;
            if (Pacotes.Count == 0)
                return Task.FromResult<PacoteStatus?>(null);

            return Task.FromResult<PacoteStatus?>(Pacotes.Dequeue());
        }

        public Task ConfirmarAsync(ulong etiqueta, CancellationToken cancellationToken)
        {
            Confirmadas.Add(etiqueta);
            return Task.CompletedTask;
        }

        public Task RecolocarAsync(ulong etiqueta, CancellationToken cancellationToken)
        {
            Recolocadas.Add(etiqueta);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Descartado = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SessaoScript(Func<CancellationToken, Task> executar) : ISessaoStatus
    {
        public Task ExecutarAsync(CancellationToken cancellationToken) => executar(cancellationToken);
    }
}
