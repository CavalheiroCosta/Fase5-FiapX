using System.Diagnostics.Metrics;
using Video.Application;
using Video.Application.Download;
using Video.Domain.Tokens;
using Video.Domain.Videos;

namespace Video.Tests;

public class BaixarZipUseCaseTests
{
    [Fact]
    public async Task Sem_token_nao_abre_o_arquivo()
    {
        var armazenamento = new ArmazenamentoFalso();
        var caso = Caso(new LeitorFalso { Identidade = null }, new RepositorioFalso(), armazenamento);

        var resultado = await caso.ExecutarAsync(null, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CodigosFalha.AcessoRecusado, resultado.Falha!.Codigo);
        Assert.Empty(armazenamento.Abertos);
    }

    [Fact]
    public async Task Entrega_o_zip_do_dono_concluido()
    {
        var id = Guid.NewGuid();
        var caminho = $"videos/{id:D}/{id:D}.zip";
        var video = global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/a.mp4").Valor!;
        video.Aplicar(MomentoStatus.Sucesso, caminho);
        var repositorio = new RepositorioFalso();
        repositorio.Guardar(video);
        var armazenamento = new ArmazenamentoFalso { Conteudo = [4, 5] };
        var metricas = Medidor(out var total, out var resultadoMetrica);
        var caso = Caso(new LeitorFalso(), repositorio, armazenamento, metricas);

        var resultado = await caso.ExecutarAsync("token", id, CancellationToken.None);

        Assert.Equal(caminho, armazenamento.Abertos.Single());
        Assert.Equal($"{id:D}.zip", resultado.Valor!.NomeArquivo);
        Assert.Equal(4, resultado.Valor.Conteudo.ReadByte());
        Assert.Equal(1, total());
        Assert.Equal(MetricasVideo.ResultadoEntregue, resultadoMetrica());
    }

    [Theory]
    [InlineData("outro")]
    [InlineData("ausente")]
    [InlineData("aguardando")]
    [InlineData("erro")]
    [InlineData("sem-zip")]
    public async Task Nao_entrega_quando_o_video_nao_pode_ser_baixado(string cenario)
    {
        var id = Guid.NewGuid();
        var repositorio = new RepositorioFalso();
        if (cenario != "ausente")
        {
            var login = cenario == "outro" ? "bia" : "ana";
            var video = global::Video.Domain.Videos.Video.Registrar(id, login, "ana@email.com", "videos/a.mp4").Valor!;
            if (cenario == "erro")
                video.Aplicar(MomentoStatus.Erro, null);
            if (cenario == "sem-zip")
            {
                video.Aplicar(MomentoStatus.Sucesso, $"videos/{id:D}/{id:D}.zip");
                typeof(global::Video.Domain.Videos.Video).GetProperty(nameof(global::Video.Domain.Videos.Video.CaminhoZip))!
                    .SetValue(video, " ");
            }
            if (cenario is "outro")
                video.Aplicar(MomentoStatus.Sucesso, $"videos/{id:D}/{id:D}.zip");
            repositorio.Guardar(video);
        }

        var armazenamento = new ArmazenamentoFalso();
        var caso = Caso(new LeitorFalso(), repositorio, armazenamento);

        var resultado = await caso.ExecutarAsync("token", id, CancellationToken.None);

        Assert.Equal(CodigosFalha.NaoEncontrado, resultado.Falha!.Codigo);
        Assert.Equal("Vídeo não encontrado.", resultado.Falha.Mensagem);
        Assert.Empty(armazenamento.Abertos);
    }

    [Fact]
    public async Task Falha_do_storage_responde_sem_arquivo()
    {
        var id = Guid.NewGuid();
        var video = global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/a.mp4").Valor!;
        video.Aplicar(MomentoStatus.Sucesso, $"videos/{id:D}/{id:D}.zip");
        var repositorio = new RepositorioFalso();
        repositorio.Guardar(video);
        var caso = Caso(new LeitorFalso(), repositorio, new ArmazenamentoFalso { Falhar = true });

        var resultado = await caso.ExecutarAsync("token", id, CancellationToken.None);

        Assert.Equal(CodigosFalha.Storage, resultado.Falha!.Codigo);
    }

    [Fact]
    public async Task Cancelamento_nao_vira_erro_de_storage()
    {
        var id = Guid.NewGuid();
        var video = global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/a.mp4").Valor!;
        video.Aplicar(MomentoStatus.Sucesso, $"videos/{id:D}/{id:D}.zip");
        var repositorio = new RepositorioFalso();
        repositorio.Guardar(video);
        var caso = Caso(new LeitorFalso(), repositorio, new ArmazenamentoFalso { Cancelar = true });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            caso.ExecutarAsync("token", id, CancellationToken.None));
    }

    private static BaixarZipUseCase Caso(
        ILeitorToken leitor,
        IVideoRepository repositorio,
        IArmazenamentoVideo armazenamento,
        MetricasVideo? metricas = null) =>
        new(leitor, repositorio, armazenamento, metricas ?? new MetricasVideo());

    private static MetricasVideo Medidor(out Func<long> total, out Func<string?> resultado)
    {
        var nome = "FiapX.Video.Teste." + Guid.NewGuid().ToString("N");
        var metricas = new MetricasVideo(nome);
        long soma = 0;
        string? rotulo = null;
        var ouvinte = new MeterListener();
        ouvinte.InstrumentPublished = (instrumento, listener) =>
        {
            if (instrumento.Meter.Name == nome && instrumento.Name == MetricasVideo.Download)
                listener.EnableMeasurementEvents(instrumento);
        };
        ouvinte.SetMeasurementEventCallback<long>((_, valor, tags, _) =>
        {
            soma += valor;
            foreach (var tag in tags)
            {
                if (tag.Key == "resultado")
                    rotulo = tag.Value?.ToString();
            }
        });
        ouvinte.Start();
        total = () => soma;
        resultado = () => rotulo;
        return metricas;
    }

    private sealed class LeitorFalso : ILeitorToken
    {
        public IdentidadeAutenticada? Identidade { get; set; } = new("ana", "ana@email.com");

        public IdentidadeAutenticada? Ler(string? token) =>
            string.IsNullOrWhiteSpace(token) ? null : Identidade;
    }

    private sealed class RepositorioFalso : IVideoRepository
    {
        private readonly Dictionary<Guid, global::Video.Domain.Videos.Video> _videos = [];

        public void Guardar(global::Video.Domain.Videos.Video video) => _videos[video.Id] = video;

        public Task AdicionarAsync(global::Video.Domain.Videos.Video video, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<global::Video.Domain.Videos.Video?> ObterAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_videos.GetValueOrDefault(id));

        public Task<IReadOnlyList<global::Video.Domain.Videos.Video>> ListarPorLoginAsync(string login, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<global::Video.Domain.Videos.Video>>([]);

        public Task AtualizarAsync(global::Video.Domain.Videos.Video video, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class ArmazenamentoFalso : IArmazenamentoVideo
    {
        public List<string> Abertos { get; } = [];
        public byte[] Conteudo { get; set; } = [];
        public bool Falhar { get; set; }
        public bool Cancelar { get; set; }

        public Task<string> SalvarAsync(Guid id, string nomeArquivo, Stream conteudo, CancellationToken cancellationToken) =>
            Task.FromResult($"videos/{id:D}/{nomeArquivo}");

        public Task<Stream> AbrirAsync(string caminho, CancellationToken cancellationToken)
        {
            Abertos.Add(caminho);
            if (Cancelar)
                throw new OperationCanceledException();
            if (Falhar)
                throw new IOException("minio");

            return Task.FromResult<Stream>(new MemoryStream(Conteudo));
        }

        public Task RemoverAsync(string caminho, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
