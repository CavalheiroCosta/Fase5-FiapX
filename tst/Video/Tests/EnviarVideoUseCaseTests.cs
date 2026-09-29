using Video.Application.Envio;
using Video.Domain.Tokens;
using Video.Domain.Videos;

namespace Video.Tests;

public class EnviarVideoUseCaseTests
{
    private const long Limite = 10;

    [Fact]
    public async Task Sem_token_nao_grava_nem_publica()
    {
        var armazenamento = new ArmazenamentoFalso();
        var repositorio = new RepositorioFalso();
        var fila = new FilaFalsa();
        var caso = Caso(new LeitorFalso(), armazenamento, repositorio, fila);

        var resultado = await caso.ExecutarAsync(null, "aula.mp4", new MemoryStream([1]), 1, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(CodigosFalha.AcessoRecusado, resultado.Falha!.Codigo);
        Assert.Empty(armazenamento.Passos);
        Assert.Empty(repositorio.Videos);
        Assert.Empty(fila.Mensagens);
    }

    [Fact]
    public async Task Token_invalido_recusa_antes_do_storage()
    {
        var armazenamento = new ArmazenamentoFalso();
        var caso = Caso(new LeitorFalso { Identidade = null }, armazenamento, new RepositorioFalso(), new FilaFalsa());

        var resultado = await caso.ExecutarAsync("token", "aula.mp4", new MemoryStream([1]), 1, CancellationToken.None);

        Assert.Equal(CodigosFalha.AcessoRecusado, resultado.Falha!.Codigo);
        Assert.Empty(armazenamento.Passos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Arquivo_vazio_responde_validacao(long tamanho)
    {
        var armazenamento = new ArmazenamentoFalso();
        var caso = Caso(new LeitorFalso(), armazenamento, new RepositorioFalso(), new FilaFalsa());

        var resultado = await caso.ExecutarAsync("token", "aula.mp4", new MemoryStream(), tamanho, CancellationToken.None);

        Assert.Equal(CodigosFalha.Validacao, resultado.Falha!.Codigo);
        Assert.Empty(armazenamento.Passos);
    }

    [Fact]
    public async Task Stream_nulo_ou_nome_invalido_responde_validacao()
    {
        var caso = Caso(new LeitorFalso(), new ArmazenamentoFalso(), new RepositorioFalso(), new FilaFalsa());

        var semStream = await caso.ExecutarAsync("token", "aula.mp4", null, 1, CancellationToken.None);
        var semNome = await caso.ExecutarAsync("token", "  ", new MemoryStream([1]), 1, CancellationToken.None);
        var nomeRuim = await caso.ExecutarAsync("token", "..", new MemoryStream([1]), 1, CancellationToken.None);

        Assert.Equal(CodigosFalha.Validacao, semStream.Falha!.Codigo);
        Assert.Equal(CodigosFalha.Validacao, semNome.Falha!.Codigo);
        Assert.Equal(CodigosFalha.Validacao, nomeRuim.Falha!.Codigo);
    }

    [Fact]
    public async Task Arquivo_acima_do_limite_nao_grava()
    {
        var armazenamento = new ArmazenamentoFalso();
        var caso = Caso(new LeitorFalso(), armazenamento, new RepositorioFalso(), new FilaFalsa());

        var resultado = await caso.ExecutarAsync("token", "aula.mp4", new MemoryStream([1]), Limite + 1, CancellationToken.None);

        Assert.Equal(CodigosFalha.Validacao, resultado.Falha!.Codigo);
        Assert.Empty(armazenamento.Passos);
    }

    [Fact]
    public async Task Grava_registra_e_depois_publica()
    {
        var ordem = new List<string>();
        var armazenamento = new ArmazenamentoFalso { Ordem = ordem };
        var repositorio = new RepositorioFalso { Ordem = ordem };
        var fila = new FilaFalsa { Ordem = ordem };
        var caso = Caso(new LeitorFalso(), armazenamento, repositorio, fila);

        var resultado = await caso.ExecutarAsync("token", "pasta/aula.mp4", new MemoryStream([1, 2]), Limite, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(["storage", "repositorio", "fila"], ordem);
        Assert.Equal("aula.mp4", armazenamento.Nomes.Single());
        Assert.Equal(StatusVideo.AguardandoProcessamento, resultado.Valor!.Status);
        Assert.Equal(resultado.Valor.Caminho, repositorio.Videos.Single().CaminhoOriginal);
        Assert.Equal("ana", repositorio.Videos.Single().Login);
        Assert.Equal("ana@email.com", repositorio.Videos.Single().Email);
        Assert.Null(repositorio.Videos.Single().CaminhoZip);
        Assert.Equal(resultado.Valor.Id, fila.Mensagens.Single().Id);
        Assert.Equal(resultado.Valor.Caminho, fila.Mensagens.Single().Caminho);
    }

    [Fact]
    public async Task Falha_de_storage_nao_registra()
    {
        var armazenamento = new ArmazenamentoFalso { Falhar = true };
        var repositorio = new RepositorioFalso();
        var fila = new FilaFalsa();
        var caso = Caso(new LeitorFalso(), armazenamento, repositorio, fila);

        var resultado = await caso.ExecutarAsync("token", "aula.mp4", new MemoryStream([1]), 1, CancellationToken.None);

        Assert.Equal(CodigosFalha.Storage, resultado.Falha!.Codigo);
        Assert.Empty(repositorio.Videos);
        Assert.Empty(fila.Mensagens);
    }

    [Fact]
    public async Task Falha_de_registro_apaga_o_objeto()
    {
        var armazenamento = new ArmazenamentoFalso();
        var repositorio = new RepositorioFalso { Falhar = true };
        var fila = new FilaFalsa();
        var caso = Caso(new LeitorFalso(), armazenamento, repositorio, fila);

        var resultado = await caso.ExecutarAsync("token", "aula.mp4", new MemoryStream([1]), 1, CancellationToken.None);

        Assert.Equal(CodigosFalha.Persistencia, resultado.Falha!.Codigo);
        Assert.Single(armazenamento.Removidos);
        Assert.Empty(fila.Mensagens);
    }

    [Fact]
    public async Task Falha_ao_apagar_depois_do_registro_ainda_devolve_erro()
    {
        var armazenamento = new ArmazenamentoFalso { FalharRemocao = true };
        var repositorio = new RepositorioFalso { Falhar = true };
        var caso = Caso(new LeitorFalso(), armazenamento, repositorio, new FilaFalsa());

        var resultado = await caso.ExecutarAsync("token", "aula.mp4", new MemoryStream([1]), 1, CancellationToken.None);

        Assert.Equal(CodigosFalha.Persistencia, resultado.Falha!.Codigo);
    }

    [Fact]
    public async Task Caminho_vazio_apagado_devolve_validacao()
    {
        var armazenamento = new ArmazenamentoFalso { Caminho = "" };
        var repositorio = new RepositorioFalso();
        var caso = Caso(new LeitorFalso(), armazenamento, repositorio, new FilaFalsa());

        var resultado = await caso.ExecutarAsync("token", "aula.mp4", new MemoryStream([1]), 1, CancellationToken.None);

        Assert.Equal(CodigosFalha.Validacao, resultado.Falha!.Codigo);
        Assert.Empty(repositorio.Videos);
        Assert.Single(armazenamento.Removidos);
    }

    [Fact]
    public async Task Falha_da_fila_mantem_o_video_aguardando()
    {
        var armazenamento = new ArmazenamentoFalso();
        var repositorio = new RepositorioFalso();
        var fila = new FilaFalsa { Falhar = true };
        var caso = Caso(new LeitorFalso(), armazenamento, repositorio, fila);

        var resultado = await caso.ExecutarAsync("token", "aula.mp4", new MemoryStream([1]), 1, CancellationToken.None);

        Assert.Equal(CodigosFalha.Fila, resultado.Falha!.Codigo);
        Assert.Contains("aguardando processamento", resultado.Falha.Mensagem, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(armazenamento.Removidos);
        var video = Assert.Single(repositorio.Videos);
        Assert.Equal(StatusVideo.AguardandoProcessamento, video.Status);
    }

    [Fact]
    public async Task Cancelamento_nao_vira_erro_de_storage()
    {
        var armazenamento = new ArmazenamentoFalso { Cancelar = true };
        var caso = Caso(new LeitorFalso(), armazenamento, new RepositorioFalso(), new FilaFalsa());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            caso.ExecutarAsync("token", "aula.mp4", new MemoryStream([1]), 1, CancellationToken.None));
    }

    private static EnviarVideoUseCase Caso(
        ILeitorToken leitor,
        IArmazenamentoVideo armazenamento,
        IVideoRepository repositorio,
        IFilaProcessamento fila) =>
        new(leitor, armazenamento, repositorio, fila, Limite);

    private sealed class LeitorFalso : ILeitorToken
    {
        public IdentidadeAutenticada? Identidade { get; set; } = new("ana", "ana@email.com");

        public IdentidadeAutenticada? Ler(string? token) =>
            string.IsNullOrWhiteSpace(token) ? null : Identidade;
    }

    private sealed class ArmazenamentoFalso : IArmazenamentoVideo
    {
        public List<string> Passos { get; } = [];
        public List<string> Ordem { get; set; } = [];
        public List<string> Nomes { get; } = [];
        public List<string> Removidos { get; } = [];
        public bool Falhar { get; set; }
        public bool FalharRemocao { get; set; }
        public bool Cancelar { get; set; }
        public string? Caminho { get; set; }

        public Task<string> SalvarAsync(Guid id, string nomeArquivo, Stream conteudo, CancellationToken cancellationToken)
        {
            Passos.Add("storage");
            Ordem.Add("storage");
            Nomes.Add(nomeArquivo);
            if (Cancelar)
                throw new OperationCanceledException();
            if (Falhar)
                throw new IOException("minio");

            return Task.FromResult(Caminho ?? $"videos/{id:D}/{nomeArquivo}");
        }

        public Task RemoverAsync(string caminho, CancellationToken cancellationToken)
        {
            Removidos.Add(caminho);
            if (FalharRemocao)
                throw new IOException("apagar");

            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioFalso : IVideoRepository
    {
        public List<string> Passos { get; } = [];
        public List<string> Ordem { get; set; } = [];
        public List<Video.Domain.Videos.Video> Videos { get; } = [];
        public bool Falhar { get; set; }

        public Task AdicionarAsync(Video.Domain.Videos.Video video, CancellationToken cancellationToken)
        {
            Passos.Add("repositorio");
            Ordem.Add("repositorio");
            if (Falhar)
                throw new IOException("postgres");

            Videos.Add(video);
            return Task.CompletedTask;
        }
    }

    private sealed class FilaFalsa : IFilaProcessamento
    {
        public List<string> Passos { get; } = [];
        public List<string> Ordem { get; set; } = [];
        public List<(Guid Id, string Caminho)> Mensagens { get; } = [];
        public bool Falhar { get; set; }

        public Task PublicarAsync(Guid id, string caminho, CancellationToken cancellationToken)
        {
            Passos.Add("fila");
            Ordem.Add("fila");
            if (Falhar)
                throw new IOException("rabbit");

            Mensagens.Add((id, caminho));
            return Task.CompletedTask;
        }
    }
}
