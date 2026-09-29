using System.Text;
using System.Text.Json;
using Processor.Application;
using Processor.Domain.Processamento;
using Processor.Infra.Filas;

namespace Processor.Tests;

public class EntregaMensagemTests
{
    [Fact]
    public async Task Mensagem_ilegivel_confirma_sem_processar()
    {
        var marca = new MarcaQueFalha();
        var caso = new ProcessarVideoUseCase(marca, new FilaMuda(), new ArmazenamentoMudo(), new QuebraMuda(), new MetricasProcessor());
        var entrega = new EntregaMensagem(caso);

        var confirmacao = await entrega.TratarAsync("{"u8.ToArray(), CancellationToken.None);

        Assert.Equal(Confirmacao.Confirmar, confirmacao);
        Assert.Equal(0, marca.Chamadas);
    }

    [Fact]
    public async Task Mensagem_valida_confirma()
    {
        var id = Guid.NewGuid();
        var marca = new MarcaQueFalha();
        var fila = new FilaMuda();
        var armazenamento = new ArmazenamentoMudo();
        armazenamento.Guardar($"videos/{id:D}/aula.mp4");
        var caso = new ProcessarVideoUseCase(marca, fila, armazenamento, new QuebraMuda(), new MetricasProcessor());
        var entrega = new EntregaMensagem(caso);
        var corpo = Encoding.UTF8.GetBytes($"{{\"id\":\"{id:D}\",\"caminho\":\"videos/{id:D}/aula.mp4\"}}");

        var confirmacao = await entrega.TratarAsync(corpo, CancellationToken.None);

        Assert.Equal(Confirmacao.Confirmar, confirmacao);
        Assert.Contains(fila.Momentos, momento => momento == MomentoStatus.Sucesso);
    }

    [Fact]
    public async Task Cancelamento_recoloca_a_mensagem()
    {
        var id = Guid.NewGuid();
        var caso = new ProcessarVideoUseCase(
            new MarcaQueFalha(),
            new FilaMuda(),
            new ArmazenamentoMudo(),
            new QuebraMuda(),
            new MetricasProcessor());
        var entrega = new EntregaMensagem(caso);
        var corpo = Encoding.UTF8.GetBytes($"{{\"id\":\"{id:D}\",\"caminho\":\"videos/{id:D}/aula.mp4\"}}");

        var confirmacao = await entrega.TratarAsync(corpo, new CancellationToken(canceled: true));

        Assert.Equal(Confirmacao.Recolocar, confirmacao);
    }

    [Fact]
    public async Task Sessao_confirma_o_processado_e_recoloca_o_cancelado()
    {
        var id = Guid.NewGuid();
        var canal = new CanalFalso();
        canal.Pacotes.Enqueue(new Pacote(1, Encoding.UTF8.GetBytes("sem-json")));
        canal.Pacotes.Enqueue(new Pacote(2, Encoding.UTF8.GetBytes($"{{\"id\":\"{id:D}\",\"caminho\":\"videos/{id:D}/aula.mp4\"}}")));
        var armazenamento = new ArmazenamentoMudo();
        armazenamento.Guardar($"videos/{id:D}/aula.mp4");
        var caso = new ProcessarVideoUseCase(new MarcaQueFalha(), new FilaMuda(), armazenamento, new QuebraMuda(), new MetricasProcessor());
        var sessao = new SessaoConsumo(canal, new EntregaMensagem(caso));

        await sessao.ExecutarAsync(CancellationToken.None);

        Assert.True(canal.Preparado);
        Assert.Equal(new ulong[] { 1, 2 }, canal.Confirmadas);
        Assert.Empty(canal.Recolocadas);
    }

    [Fact]
    public async Task Sessao_recoloca_quando_o_caso_e_cancelado()
    {
        var id = Guid.NewGuid();
        var canal = new CanalFalso();
        canal.Pacotes.Enqueue(new Pacote(7, Encoding.UTF8.GetBytes($"{{\"id\":\"{id:D}\",\"caminho\":\"videos/{id:D}/aula.mp4\"}}")));
        var caso = new ProcessarVideoUseCase(new MarcaQueFalha(), new FilaMuda(), new ArmazenamentoMudo(), new QuebraMuda(), new MetricasProcessor());
        var sessao = new SessaoConsumo(canal, new EntregaMensagem(caso));
        using var fonte = new CancellationTokenSource();
        canal.AoReceber = () => fonte.Cancel();

        await sessao.ExecutarAsync(fonte.Token);

        Assert.Equal(new ulong[] { 7 }, canal.Recolocadas);
        Assert.Empty(canal.Confirmadas);
    }

    [Fact]
    public async Task Sessao_para_quando_o_token_ja_esta_cancelado()
    {
        var canal = new CanalFalso();
        var caso = new ProcessarVideoUseCase(new MarcaQueFalha(), new FilaMuda(), new ArmazenamentoMudo(), new QuebraMuda(), new MetricasProcessor());
        var sessao = new SessaoConsumo(canal, new EntregaMensagem(caso));

        await sessao.ExecutarAsync(new CancellationToken(canceled: true));

        Assert.True(canal.Preparado);
        Assert.Equal(0, canal.Recebidas);
    }

    private sealed class MarcaQueFalha : IMarcaVideo
    {
        public int Chamadas { get; private set; }

        public Task<bool> TentarAsync(Guid id, CancellationToken cancellationToken)
        {
            Chamadas++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public Task RemoverAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FilaMuda : IFilaStatus
    {
        public List<string> Momentos { get; } = [];

        public Task PublicarAsync(Guid id, string momento, string? caminho, CancellationToken cancellationToken)
        {
            Momentos.Add(momento);
            return Task.CompletedTask;
        }
    }

    private sealed class ArmazenamentoMudo : IArmazenamentoProcessamento
    {
        private readonly HashSet<string> _caminhos = [];

        public void Guardar(string caminho) => _caminhos.Add(caminho);

        public Task<Stream> BaixarAsync(string caminho, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_caminhos.Contains(caminho))
                throw new InvalidOperationException("ausente");

            return Task.FromResult<Stream>(new MemoryStream([1]));
        }

        public Task<string> SalvarZipAsync(Guid id, Stream conteudo, CancellationToken cancellationToken) =>
            Task.FromResult($"videos/{id:D}/{id:D}.zip");
    }

    private sealed class QuebraMuda : IQuebraVideo
    {
        public Task<Stream> QuebrarAsync(Stream video, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream([2]));
    }

    private sealed class CanalFalso : ICanalConsumo
    {
        public Queue<Pacote> Pacotes { get; } = new();
        public List<ulong> Confirmadas { get; } = [];
        public List<ulong> Recolocadas { get; } = [];
        public bool Preparado { get; private set; }
        public int Recebidas { get; private set; }
        public Action? AoReceber { get; set; }

        public Task PrepararAsync(CancellationToken cancellationToken)
        {
            Preparado = true;
            return Task.CompletedTask;
        }

        public Task<Pacote?> ReceberAsync(CancellationToken cancellationToken)
        {
            Recebidas++;
            if (Pacotes.Count == 0)
                return Task.FromResult<Pacote?>(null);

            var pacote = Pacotes.Dequeue();
            AoReceber?.Invoke();
            return Task.FromResult<Pacote?>(pacote);
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
    }
}

public class MensagemEntradaTests
{
    [Fact]
    public void Le_id_e_caminho()
    {
        var id = Guid.NewGuid();
        var corpo = Encoding.UTF8.GetBytes($"{{\"id\":\"{id:D}\",\"caminho\":\"videos/{id:D}/aula.mp4\"}}");

        var mensagem = MensagemEntrada.Ler(corpo);

        Assert.True(mensagem.Legivel);
        Assert.Equal(id, mensagem.Id);
        Assert.Equal($"videos/{id:D}/aula.mp4", mensagem.Caminho);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nao-json")]
    [InlineData("{}")]
    [InlineData("{\"id\":\"00000000-0000-0000-0000-000000000000\",\"caminho\":\"videos/x\"}")]
    public void Recusa_corpo_sem_id(string texto)
    {
        var mensagem = MensagemEntrada.Ler(Encoding.UTF8.GetBytes(texto));

        Assert.False(mensagem.Legivel);
    }

    [Fact]
    public void Id_valido_sem_caminho_continua_legivel()
    {
        var id = Guid.NewGuid();
        var mensagem = MensagemEntrada.Ler(Encoding.UTF8.GetBytes($"{{\"id\":\"{id:D}\"}}"));

        Assert.True(mensagem.Legivel);
        Assert.Null(mensagem.Caminho);
    }
}

public class CaminhoZipTests
{
    [Fact]
    public void Monta_a_chave_dentro_do_bucket()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var caminho = CaminhoZip.Montar(CaminhoZip.BucketPadrao, id);

        Assert.Equal($"videos/{id:D}/{id:D}.zip", caminho);
        Assert.Equal($"{id:D}/{id:D}.zip", CaminhoZip.Chave(CaminhoZip.BucketPadrao, caminho));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Recusa_bucket_vazio(string bucket)
    {
        Assert.Throws<ArgumentException>(() => CaminhoZip.Montar(bucket, Guid.NewGuid()));
    }

    [Fact]
    public void Recusa_guid_vazio_e_caminho_fora_do_bucket()
    {
        Assert.Throws<ArgumentException>(() => CaminhoZip.Montar("videos", Guid.Empty));
        Assert.Throws<ArgumentException>(() => CaminhoZip.Chave("videos", "videos"));
        Assert.Throws<ArgumentException>(() => CaminhoZip.Chave("videos", "outro/arquivo.zip"));
    }
}

public class FilaStatusTests
{
    [Fact]
    public async Task Comecou_e_erro_nao_levam_caminho_e_sucesso_leva()
    {
        var publicador = new PublicadorFalso();
        var fila = new FilaStatus(publicador, "status");
        var id = Guid.NewGuid();

        await fila.PublicarAsync(id, MomentoStatus.Comecou, null, CancellationToken.None);
        await fila.PublicarAsync(id, MomentoStatus.Sucesso, $"videos/{id:D}/{id:D}.zip", CancellationToken.None);
        await fila.PublicarAsync(id, MomentoStatus.Erro, null, CancellationToken.None);

        Assert.Equal(["status", "status", "status"], publicador.Filas);
        Assert.False(Json(publicador.Corpos[0]).RootElement.TryGetProperty("caminho", out _));
        Assert.Equal(MomentoStatus.Comecou, Json(publicador.Corpos[0]).RootElement.GetProperty("momento").GetString());
        Assert.Equal($"videos/{id:D}/{id:D}.zip", Json(publicador.Corpos[1]).RootElement.GetProperty("caminho").GetString());
        Assert.False(Json(publicador.Corpos[2]).RootElement.TryGetProperty("caminho", out _));
    }

    [Fact]
    public async Task Memoria_guarda_o_momento()
    {
        var fila = new FilaStatusMemoria();
        var id = Guid.NewGuid();

        await fila.PublicarAsync(id, MomentoStatus.Erro, null, CancellationToken.None);

        var mensagem = Assert.Single(fila.Mensagens);
        Assert.Equal(id, mensagem.Id);
        Assert.Equal(MomentoStatus.Erro, mensagem.Momento);
        Assert.Null(mensagem.Caminho);
    }

    private static JsonDocument Json(byte[] corpo) => JsonDocument.Parse(Encoding.UTF8.GetString(corpo));

    private sealed class PublicadorFalso : IPublicadorFila
    {
        public List<string> Filas { get; } = [];
        public List<byte[]> Corpos { get; } = [];

        public Task DeclararAsync(string fila, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task PublicarAsync(string fila, ReadOnlyMemory<byte> corpo, CancellationToken cancellationToken)
        {
            Filas.Add(fila);
            Corpos.Add(corpo.ToArray());
            return Task.CompletedTask;
        }
    }
}
