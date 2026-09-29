using System.Diagnostics.Metrics;
using Video.Application;
using Video.Application.Listagem;
using Video.Domain.Tokens;
using Video.Domain.Videos;

namespace Video.Tests;

public class ListarVideosUseCaseTests
{
    [Fact]
    public async Task Sem_token_nao_le_lista_nem_repositorio()
    {
        var lista = new ListaFalsa();
        var repositorio = new RepositorioFalso();
        var caso = Caso(new LeitorFalso { Identidade = null }, lista, repositorio);

        var resultado = await caso.ExecutarAsync("token", CancellationToken.None);

        Assert.Equal(CodigosFalha.AcessoRecusado, resultado.Falha!.Codigo);
        Assert.Equal(0, lista.Leituras);
        Assert.Equal(0, repositorio.Listagens);
    }

    [Fact]
    public async Task Hit_nao_le_o_postgres()
    {
        var id = Guid.NewGuid();
        var lista = new ListaFalsa();
        lista.Guardar("ana", [new ItemLista(id, StatusVideo.Concluido, "videos/z.zip")]);
        var repositorio = new RepositorioFalso();
        var metricas = Medidor(out var total, out var origem);
        var caso = Caso(new LeitorFalso(), lista, repositorio, metricas);

        var resultado = await caso.ExecutarAsync("token", CancellationToken.None);

        Assert.Equal(0, repositorio.Listagens);
        Assert.Equal(id, Assert.Single(resultado.Valor!).Id);
        Assert.Equal("videos/z.zip", resultado.Valor![0].CaminhoZip);
        Assert.Equal(1, total());
        Assert.Equal(MetricasVideo.OrigemRedis, origem());
    }

    [Fact]
    public async Task Miss_le_o_postgres_e_preenche_o_redis()
    {
        var ana = Guid.NewGuid();
        var bia = Guid.NewGuid();
        var repositorio = new RepositorioFalso();
        repositorio.Guardar(global::Video.Domain.Videos.Video.Registrar(ana, "ana", "ana@email.com", "videos/a.mp4").Valor!);
        repositorio.Guardar(global::Video.Domain.Videos.Video.Registrar(bia, "bia", "bia@email.com", "videos/b.mp4").Valor!);
        var lista = new ListaFalsa();
        var metricas = Medidor(out var total, out var origem);
        var caso = Caso(new LeitorFalso(), lista, repositorio, metricas);

        var resultado = await caso.ExecutarAsync("token", CancellationToken.None);

        Assert.Equal(1, repositorio.Listagens);
        Assert.Equal(ana, Assert.Single(resultado.Valor!).Id);
        Assert.Null(resultado.Valor![0].CaminhoZip);
        Assert.Equal(ana, Assert.Single(lista.Itens("ana")!).Id);
        Assert.Equal(1, total());
        Assert.Equal(MetricasVideo.OrigemPostgres, origem());
    }

    [Fact]
    public async Task Lista_vazia_gravada_nao_volta_ao_postgres()
    {
        var lista = new ListaFalsa();
        lista.Guardar("ana", []);
        var repositorio = new RepositorioFalso();
        var caso = Caso(new LeitorFalso(), lista, repositorio);

        var resultado = await caso.ExecutarAsync("token", CancellationToken.None);

        Assert.Empty(resultado.Valor!);
        Assert.Equal(0, repositorio.Listagens);
    }

    [Fact]
    public async Task Zip_fora_de_concluido_nao_sai_na_resposta()
    {
        var lista = new ListaFalsa();
        lista.Guardar("ana", [new ItemLista(Guid.NewGuid(), StatusVideo.Erro, "videos/z.zip")]);
        var caso = Caso(new LeitorFalso(), lista, new RepositorioFalso());

        var resultado = await caso.ExecutarAsync("token", CancellationToken.None);

        Assert.Null(Assert.Single(resultado.Valor!).CaminhoZip);
    }

    [Fact]
    public async Task Falha_ao_ler_o_redis_cai_no_postgres()
    {
        var id = Guid.NewGuid();
        var repositorio = new RepositorioFalso();
        repositorio.Guardar(global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/a.mp4").Valor!);
        var lista = new ListaFalsa { FalharLeitura = true };
        var caso = Caso(new LeitorFalso(), lista, repositorio);

        var resultado = await caso.ExecutarAsync("token", CancellationToken.None);

        Assert.Equal(id, Assert.Single(resultado.Valor!).Id);
        Assert.Equal(1, repositorio.Listagens);
    }

    [Fact]
    public async Task Falha_ao_gravar_o_redis_ainda_devolve_o_postgres()
    {
        var id = Guid.NewGuid();
        var repositorio = new RepositorioFalso();
        repositorio.Guardar(global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/a.mp4").Valor!);
        var lista = new ListaFalsa { FalharGravacao = true };
        var caso = Caso(new LeitorFalso(), lista, repositorio);

        var resultado = await caso.ExecutarAsync("token", CancellationToken.None);

        Assert.Equal(id, Assert.Single(resultado.Valor!).Id);
        Assert.Null(lista.Itens("ana"));
    }

    [Fact]
    public async Task Cancelamento_nao_vira_lista_vazia()
    {
        var lista = new ListaFalsa { Cancelar = true };
        var caso = Caso(new LeitorFalso(), lista, new RepositorioFalso());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            caso.ExecutarAsync("token", CancellationToken.None));
    }

    [Fact]
    public void Inclui_item_novo_e_substitui_o_existente()
    {
        var id = Guid.NewGuid();
        var outro = Guid.NewGuid();
        var atual = new ItemLista(id, StatusVideo.AguardandoProcessamento, null);
        var junto = ItemLista.Incluir([atual], new ItemLista(outro, StatusVideo.EmProcessamento, null));
        var trocado = ItemLista.Incluir(junto, new ItemLista(id, StatusVideo.Concluido, "videos/z.zip"));

        Assert.Equal(2, trocado.Count);
        Assert.Equal(StatusVideo.Concluido, trocado.Single(item => item.Id == id).Status);
        Assert.Equal("videos/z.zip", trocado.Single(item => item.Id == id).CaminhoZip);
        Assert.Equal(outro, trocado[1].Id);
    }

    private static ListarVideosUseCase Caso(ILeitorToken leitor, IListaVideos lista, IVideoRepository repositorio, MetricasVideo? metricas = null) =>
        new(leitor, lista, repositorio, metricas ?? new MetricasVideo());

    private static MetricasVideo Medidor(out Func<long> total, out Func<string?> origem)
    {
        var nome = "FiapX.Video.Teste." + Guid.NewGuid().ToString("N");
        var metricas = new MetricasVideo(nome);
        long soma = 0;
        string? rotulo = null;
        var ouvinte = new MeterListener();
        ouvinte.InstrumentPublished = (instrumento, listener) =>
        {
            if (instrumento.Meter.Name == nome && instrumento.Name == MetricasVideo.Listagem)
                listener.EnableMeasurementEvents(instrumento);
        };
        ouvinte.SetMeasurementEventCallback<long>((_, valor, tags, _) =>
        {
            soma += valor;
            foreach (var tag in tags)
            {
                if (tag.Key == "origem")
                    rotulo = tag.Value?.ToString();
            }
        });
        ouvinte.Start();
        total = () => soma;
        origem = () => rotulo;
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
        private readonly List<global::Video.Domain.Videos.Video> _videos = [];

        public int Listagens { get; private set; }

        public void Guardar(global::Video.Domain.Videos.Video video) => _videos.Add(video);

        public Task AdicionarAsync(global::Video.Domain.Videos.Video video, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<global::Video.Domain.Videos.Video?> ObterAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_videos.FirstOrDefault(video => video.Id == id));

        public Task<IReadOnlyList<global::Video.Domain.Videos.Video>> ListarPorLoginAsync(string login, CancellationToken cancellationToken)
        {
            Listagens++;
            return Task.FromResult<IReadOnlyList<global::Video.Domain.Videos.Video>>(
                _videos.Where(video => video.Login == login).ToArray());
        }

        public Task AtualizarAsync(global::Video.Domain.Videos.Video video, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class ListaFalsa : IListaVideos
    {
        private readonly Dictionary<string, ItemLista[]> _listas = [];

        public int Leituras { get; private set; }

        public bool FalharLeitura { get; set; }

        public bool FalharGravacao { get; set; }

        public bool Cancelar { get; set; }

        public void Guardar(string login, IReadOnlyList<ItemLista> itens) => _listas[login] = itens.ToArray();

        public IReadOnlyList<ItemLista>? Itens(string login) =>
            _listas.TryGetValue(login, out var itens) ? itens : null;

        public Task<IReadOnlyList<ItemLista>?> ObterAsync(string login, CancellationToken cancellationToken)
        {
            Leituras++;
            if (Cancelar)
                throw new OperationCanceledException();
            if (FalharLeitura)
                throw new IOException("redis");

            return Task.FromResult(Itens(login));
        }

        public Task GuardarAsync(string login, IReadOnlyList<ItemLista> itens, CancellationToken cancellationToken)
        {
            if (FalharGravacao)
                throw new IOException("redis");

            Guardar(login, itens);
            return Task.CompletedTask;
        }

        public Task AtualizarAsync(string login, ItemLista item, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
