using System.Text;
using Processor.Application;
using Processor.Domain.Processamento;

namespace Processor.Tests;

public class ProcessarVideoUseCaseTests
{
    [Fact]
    public async Task Sucesso_marca_publica_comecou_grava_o_zip_e_tira_a_marca()
    {
        var id = Guid.NewGuid();
        var cenario = new Cenario();
        cenario.Armazenamento.Guardar($"videos/{id:D}/aula.mp4", [1, 2, 3]);

        var momento = await cenario.Caso.ExecutarAsync(id, $"videos/{id:D}/aula.mp4", CancellationToken.None);

        Assert.Equal(MomentoStatus.Sucesso, momento);
        Assert.Equal(
            ["marca", "comecou", "baixar", "quebrar", "zip", "sucesso", "remover"],
            cenario.Passos);
        Assert.Equal($"videos/{id:D}/{id:D}.zip", cenario.Fila.Mensagens.Last().Caminho);
        Assert.Equal(MomentoStatus.Comecou, cenario.Fila.Mensagens[0].Momento);
        Assert.Null(cenario.Fila.Mensagens[0].Caminho);
        Assert.False(cenario.Marca.Contem(id));
        Assert.True(cenario.Armazenamento.Contem($"videos/{id:D}/{id:D}.zip"));
    }

    [Fact]
    public async Task Marca_existente_nao_processa_de_novo()
    {
        var id = Guid.NewGuid();
        var cenario = new Cenario();
        await cenario.Marca.TentarAsync(id, CancellationToken.None);
        cenario.Passos.Clear();

        var momento = await cenario.Caso.ExecutarAsync(id, $"videos/{id:D}/aula.mp4", CancellationToken.None);

        Assert.Equal(MomentoStatus.Ignorado, momento);
        Assert.Equal(["marca"], cenario.Passos);
        Assert.Empty(cenario.Fila.Mensagens);
        Assert.True(cenario.Marca.Contem(id));
    }

    [Fact]
    public async Task Falha_na_extracao_publica_erro_sem_sucesso()
    {
        var id = Guid.NewGuid();
        var cenario = new Cenario { QuebraFalha = true };
        cenario.Armazenamento.Guardar($"videos/{id:D}/aula.mp4", [1]);

        var momento = await cenario.Caso.ExecutarAsync(id, $"videos/{id:D}/aula.mp4", CancellationToken.None);

        Assert.Equal(MomentoStatus.Erro, momento);
        Assert.Equal(MomentoStatus.Comecou, cenario.Fila.Mensagens[0].Momento);
        Assert.Equal(MomentoStatus.Erro, cenario.Fila.Mensagens[1].Momento);
        Assert.Null(cenario.Fila.Mensagens[1].Caminho);
        Assert.DoesNotContain(cenario.Fila.Mensagens, mensagem => mensagem.Momento == MomentoStatus.Sucesso);
        Assert.False(cenario.Armazenamento.Contem($"videos/{id:D}/{id:D}.zip"));
        Assert.False(cenario.Marca.Contem(id));
    }

    [Fact]
    public async Task Falha_ao_gravar_o_zip_publica_erro()
    {
        var id = Guid.NewGuid();
        var cenario = new Cenario { ZipFalha = true };
        cenario.Armazenamento.Guardar($"videos/{id:D}/aula.mp4", [1]);

        var momento = await cenario.Caso.ExecutarAsync(id, $"videos/{id:D}/aula.mp4", CancellationToken.None);

        Assert.Equal(MomentoStatus.Erro, momento);
        Assert.DoesNotContain(cenario.Fila.Mensagens, mensagem => mensagem.Momento == MomentoStatus.Sucesso);
        Assert.False(cenario.Marca.Contem(id));
    }

    [Fact]
    public async Task Caminho_nulo_publica_erro()
    {
        var cenario = new Cenario();

        var momento = await cenario.Caso.ExecutarAsync(Guid.NewGuid(), null, CancellationToken.None);

        Assert.Equal(MomentoStatus.Erro, momento);
    }

    [Fact]
    public async Task Cancelamento_ao_publicar_erro_sobe()
    {
        var id = Guid.NewGuid();
        var cenario = new Cenario { QuebraFalha = true, CancelarNoErro = true };
        cenario.Armazenamento.Guardar($"videos/{id:D}/aula.mp4", [1]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cenario.Caso.ExecutarAsync(id, $"videos/{id:D}/aula.mp4", CancellationToken.None));

        Assert.False(cenario.Marca.Contem(id));
    }

    [Fact]
    public async Task Caminho_invalido_publica_erro()
    {
        var cenario = new Cenario();

        var momento = await cenario.Caso.ExecutarAsync(Guid.NewGuid(), "sem-bucket", CancellationToken.None);

        Assert.Equal(MomentoStatus.Erro, momento);
        Assert.Equal(MomentoStatus.Erro, cenario.Fila.Mensagens.Last().Momento);
    }

    [Fact]
    public async Task Falha_ao_publicar_sucesso_publica_erro_com_o_zip_ja_salvo()
    {
        var id = Guid.NewGuid();
        var cenario = new Cenario { FalhaNoSucesso = true };
        cenario.Armazenamento.Guardar($"videos/{id:D}/aula.mp4", [1]);

        var momento = await cenario.Caso.ExecutarAsync(id, $"videos/{id:D}/aula.mp4", CancellationToken.None);

        Assert.Equal(MomentoStatus.Erro, momento);
        Assert.True(cenario.Armazenamento.Contem($"videos/{id:D}/{id:D}.zip"));
        Assert.Contains(cenario.Fila.Mensagens, mensagem => mensagem.Momento == MomentoStatus.Erro);
    }

    [Fact]
    public async Task Falha_ao_publicar_comecou_publica_erro_sem_baixar()
    {
        var cenario = new Cenario { FalhaNoComecou = true };

        var momento = await cenario.Caso.ExecutarAsync(Guid.NewGuid(), "videos/x/aula.mp4", CancellationToken.None);

        Assert.Equal(MomentoStatus.Erro, momento);
        Assert.DoesNotContain("baixar", cenario.Passos);
    }

    [Fact]
    public async Task Falha_ao_publicar_erro_ainda_encerra_como_erro()
    {
        var cenario = new Cenario { QuebraFalha = true, FalhaNoErro = true };
        var id = Guid.NewGuid();
        cenario.Armazenamento.Guardar($"videos/{id:D}/aula.mp4", [1]);

        var momento = await cenario.Caso.ExecutarAsync(id, $"videos/{id:D}/aula.mp4", CancellationToken.None);

        Assert.Equal(MomentoStatus.Erro, momento);
        Assert.False(cenario.Marca.Contem(id));
    }

    [Fact]
    public async Task Cancelamento_tira_a_marca_e_nao_publica_erro()
    {
        var id = Guid.NewGuid();
        var cenario = new Cenario { CancelarNoDownload = true };
        cenario.Armazenamento.Guardar($"videos/{id:D}/aula.mp4", [1]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cenario.Caso.ExecutarAsync(id, $"videos/{id:D}/aula.mp4", new CancellationToken(canceled: true)));

        Assert.DoesNotContain(cenario.Fila.Mensagens, mensagem => mensagem.Momento == MomentoStatus.Erro);
        Assert.False(cenario.Marca.Contem(id));
    }

    [Fact]
    public async Task Falha_ao_remover_a_marca_nao_esconde_o_sucesso()
    {
        var id = Guid.NewGuid();
        var cenario = new Cenario { FalhaAoRemover = true };
        cenario.Armazenamento.Guardar($"videos/{id:D}/aula.mp4", [1]);

        var momento = await cenario.Caso.ExecutarAsync(id, $"videos/{id:D}/aula.mp4", CancellationToken.None);

        Assert.Equal(MomentoStatus.Sucesso, momento);
    }

    private sealed class Cenario
    {
        public List<string> Passos { get; } = [];
        public MarcaFalsa Marca { get; }
        public FilaFalsa Fila { get; }
        public ArmazenamentoFalso Armazenamento { get; }
        public ProcessarVideoUseCase Caso { get; }
        public bool QuebraFalha { get; init; }
        public bool ZipFalha { get; init; }
        public bool FalhaNoSucesso { get; init; }
        public bool FalhaNoComecou { get; init; }
        public bool FalhaNoErro { get; init; }
        public bool CancelarNoErro { get; init; }
        public bool CancelarNoDownload { get; init; }
        public bool FalhaAoRemover { get; init; }

        public Cenario()
        {
            Marca = new MarcaFalsa(this);
            Fila = new FilaFalsa(this);
            Armazenamento = new ArmazenamentoFalso(this);
            Caso = new ProcessarVideoUseCase(Marca, Fila, Armazenamento, new QuebraFalsa(this), new MetricasProcessor());
        }
    }

    private sealed class MarcaFalsa(Cenario cenario) : IMarcaVideo
    {
        private readonly HashSet<Guid> _marcas = [];

        public bool Contem(Guid id) => _marcas.Contains(id);

        public Task<bool> TentarAsync(Guid id, CancellationToken cancellationToken)
        {
            cenario.Passos.Add("marca");
            return Task.FromResult(_marcas.Add(id));
        }

        public Task RemoverAsync(Guid id, CancellationToken cancellationToken)
        {
            cenario.Passos.Add("remover");
            if (cenario.FalhaAoRemover)
                throw new InvalidOperationException("redis");

            _marcas.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class FilaFalsa(Cenario cenario) : IFilaStatus
    {
        public List<StatusItem> Mensagens { get; } = [];

        public Task PublicarAsync(Guid id, string momento, string? caminho, CancellationToken cancellationToken)
        {
            cenario.Passos.Add(momento);
            if (momento == MomentoStatus.Comecou && cenario.FalhaNoComecou)
                throw new InvalidOperationException("fila");
            if (momento == MomentoStatus.Sucesso && cenario.FalhaNoSucesso)
                throw new InvalidOperationException("fila");
            if (momento == MomentoStatus.Erro && cenario.CancelarNoErro)
                throw new OperationCanceledException();
            if (momento == MomentoStatus.Erro && cenario.FalhaNoErro)
                throw new InvalidOperationException("fila");

            Mensagens.Add(new StatusItem(id, momento, caminho));
            return Task.CompletedTask;
        }
    }

    private sealed record StatusItem(Guid Id, string Momento, string? Caminho);

    private sealed class ArmazenamentoFalso(Cenario cenario) : IArmazenamentoProcessamento
    {
        private readonly Dictionary<string, byte[]> _objetos = [];

        public void Guardar(string caminho, byte[] bytes) => _objetos[caminho] = bytes;

        public bool Contem(string caminho) => _objetos.ContainsKey(caminho);

        public Task<Stream> BaixarAsync(string caminho, CancellationToken cancellationToken)
        {
            cenario.Passos.Add("baixar");
            cancellationToken.ThrowIfCancellationRequested();
            if (cenario.CancelarNoDownload)
                throw new OperationCanceledException();
            if (!_objetos.TryGetValue(caminho, out var bytes))
                throw new InvalidOperationException("ausente");

            return Task.FromResult<Stream>(new MemoryStream(bytes));
        }

        public Task<string> SalvarZipAsync(Guid id, Stream conteudo, CancellationToken cancellationToken)
        {
            cenario.Passos.Add("zip");
            if (cenario.ZipFalha)
                throw new InvalidOperationException("storage");

            var caminho = $"videos/{id:D}/{id:D}.zip";
            _objetos[caminho] = [9];
            return Task.FromResult(caminho);
        }
    }

    private sealed class QuebraFalsa(Cenario cenario) : IQuebraVideo
    {
        public Task<Stream> QuebrarAsync(Stream video, CancellationToken cancellationToken)
        {
            cenario.Passos.Add("quebrar");
            if (cenario.QuebraFalha)
                throw new InvalidOperationException("ffmpeg");

            return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("zip")));
        }
    }
}
