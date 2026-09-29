using Video.Domain.Videos;
using Video.Infra.Redis;

namespace Video.Tests;

public class ListaVideosTests
{
    [Fact]
    public async Task Miss_nao_grava_parcial_e_o_hit_atualiza_a_entrada()
    {
        var comando = new ComandoFalso();
        var lista = new ListaVideos(comando);
        var id = Guid.NewGuid();
        var item = new ItemLista(id, StatusVideo.AguardandoProcessamento, null);

        await lista.AtualizarAsync("ana", item, CancellationToken.None);
        Assert.Null(await lista.ObterAsync("ana", CancellationToken.None));
        Assert.Empty(comando.Gravadas);

        await lista.GuardarAsync("ana", [item], CancellationToken.None);
        var zip = new ItemLista(id, StatusVideo.Concluido, "videos/z.zip");
        await lista.AtualizarAsync("ana", zip, CancellationToken.None);

        var lido = Assert.Single(await lista.ObterAsync("ana", CancellationToken.None) ?? []);
        Assert.Equal(StatusVideo.Concluido, lido.Status);
        Assert.Equal("videos/z.zip", lido.CaminhoZip);
        Assert.Equal("lista:ana", comando.Gravadas[0].Chave);
        Assert.DoesNotContain("caminhoZip", comando.Gravadas[0].Valor);
        Assert.Contains("caminhoZip", comando.Gravadas[1].Valor);
    }

    [Fact]
    public async Task Json_vazio_e_ilegivel_sao_miss_e_lista_vazia_e_hit()
    {
        var comando = new ComandoFalso();
        var lista = new ListaVideos(comando);
        comando.Definir("lista:ana", " ");
        Assert.Null(await lista.ObterAsync("ana", CancellationToken.None));

        comando.Definir("lista:ana", "{");
        Assert.Null(await lista.ObterAsync("ana", CancellationToken.None));

        comando.Definir("lista:ana", "[]");
        var vazia = await lista.ObterAsync("ana", CancellationToken.None);
        Assert.NotNull(vazia);
        Assert.Empty(vazia);
    }

    [Fact]
    public async Task Memoria_repete_a_lista_e_ignora_chave_ausente()
    {
        var memoria = new ListaVideosMemoria();
        var id = Guid.NewGuid();
        var item = new ItemLista(id, StatusVideo.EmProcessamento, null);

        await memoria.AtualizarAsync("ana", item, CancellationToken.None);
        Assert.Null(await memoria.ObterAsync("ana", CancellationToken.None));

        await memoria.GuardarAsync("ana", [], CancellationToken.None);
        await memoria.AtualizarAsync("ana", item, CancellationToken.None);

        var lido = Assert.Single(await memoria.ObterAsync("ana", CancellationToken.None) ?? []);
        Assert.Equal(StatusVideo.EmProcessamento, lido.Status);
        Assert.Null(lido.CaminhoZip);
    }

    [Fact]
    public void De_so_leva_o_zip_quando_concluido()
    {
        var id = Guid.NewGuid();
        var video = global::Video.Domain.Videos.Video.Registrar(id, "ana", "ana@email.com", "videos/a.mp4").Valor!;
        Assert.Null(ItemLista.De(video).CaminhoZip);

        video.Aplicar(MomentoStatus.Sucesso, "videos/z.zip");
        Assert.Equal("videos/z.zip", ItemLista.De(video).CaminhoZip);

        var erro = global::Video.Domain.Videos.Video.Registrar(Guid.NewGuid(), "ana", "ana@email.com", "videos/b.mp4").Valor!;
        erro.Aplicar(MomentoStatus.Erro, null);
        Assert.Null(ItemLista.De(erro).CaminhoZip);
    }

    private sealed class ComandoFalso : IComandoLista
    {
        private readonly Dictionary<string, string> _chaves = [];

        public List<(string Chave, string Valor)> Gravadas { get; } = [];

        public void Definir(string chave, string valor) => _chaves[chave] = valor;

        public Task<string?> LerAsync(string chave, CancellationToken cancellationToken) =>
            Task.FromResult(_chaves.TryGetValue(chave, out var valor) ? valor : null);

        public Task GravarAsync(string chave, string valor, CancellationToken cancellationToken)
        {
            _chaves[chave] = valor;
            Gravadas.Add((chave, valor));
            return Task.CompletedTask;
        }
    }
}
