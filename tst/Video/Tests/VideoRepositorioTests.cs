using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Video.Infra.Persistence;
using VideoEnviado = Video.Domain.Videos.Video;

namespace Video.Tests;

public class VideoRepositorioTests : IDisposable
{
    private readonly SqliteConnection _conexao;
    private readonly VideoDbContext _db;
    private readonly VideoRepository _repositorio;

    public VideoRepositorioTests()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();
        var opcoes = new DbContextOptionsBuilder<VideoDbContext>().UseSqlite(_conexao).Options;
        _db = new VideoDbContext(opcoes);
        _db.Database.EnsureCreated();
        _repositorio = new VideoRepository(_db);
    }

    [Fact]
    public async Task Grava_o_guid_que_ja_nasceu_no_envio()
    {
        var id = Guid.NewGuid();
        var criado = VideoEnviado.Registrar(id, "ana", "ana@email.com", "videos/x/aula.mp4");

        await _repositorio.AdicionarAsync(criado.Valor!, CancellationToken.None);

        var obtido = await _db.Videos.SingleAsync();
        Assert.Equal(id, obtido.Id);
        Assert.Equal("ana", obtido.Login);
        Assert.Equal("aguardando_processamento", obtido.Status);
        Assert.Null(obtido.CaminhoZip);
    }

    [Fact]
    public async Task Recusa_guid_vazio()
    {
        var criado = VideoEnviado.Registrar(Guid.NewGuid(), "ana", "ana@email.com", "videos/x/aula.mp4");
        typeof(VideoEnviado).GetProperty(nameof(VideoEnviado.Id))!.SetValue(criado.Valor, Guid.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repositorio.AdicionarAsync(criado.Valor!, CancellationToken.None));
    }

    [Fact]
    public async Task Memoria_lista_o_video_gravado()
    {
        var memoria = new VideoRepositorioMemoria();
        var criado = VideoEnviado.Registrar(Guid.NewGuid(), "ana", "ana@email.com", "videos/x/aula.mp4");

        await memoria.AdicionarAsync(criado.Valor!, CancellationToken.None);

        Assert.Equal(criado.Valor!.Id, Assert.Single(memoria.Listar()).Id);
    }

    [Fact]
    public async Task Memoria_recusa_guid_vazio()
    {
        var memoria = new VideoRepositorioMemoria();
        var criado = VideoEnviado.Registrar(Guid.NewGuid(), "ana", "ana@email.com", "videos/x/aula.mp4");
        typeof(VideoEnviado).GetProperty(nameof(VideoEnviado.Id))!.SetValue(criado.Valor, Guid.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            memoria.AdicionarAsync(criado.Valor!, CancellationToken.None));
    }

    public void Dispose()
    {
        _db.Dispose();
        _conexao.Dispose();
    }
}
