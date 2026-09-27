using Auth.Domain.Usuarios;
using Auth.Infra.Persistence;
using Auth.Infra.Senhas;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests;

public class UsuarioRepositorioTests : IDisposable
{
    private readonly SqliteConnection _conexao;
    private readonly AuthDbContext _db;
    private readonly UsuarioRepository _repositorio;

    public UsuarioRepositorioTests()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();
        var opcoes = new DbContextOptionsBuilder<AuthDbContext>().UseSqlite(_conexao).Options;
        _db = new AuthDbContext(opcoes);
        _db.Database.EnsureCreated();
        _repositorio = new UsuarioRepository(_db);
    }

    [Fact]
    public async Task Grava_o_guid_na_insercao_e_nao_devolve_a_senha()
    {
        var criado = Usuario.Criar("ana", "Ana", "ana@email.com", "hash-ana");
        Assert.Equal(Guid.Empty, criado.Valor!.Id);

        await _repositorio.AdicionarAsync(criado.Valor, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, criado.Valor.Id);
        var obtido = await _repositorio.ObterPorIdAsync(criado.Valor.Id, CancellationToken.None);
        Assert.Equal("ana", obtido!.Login);
        Assert.Equal("hash-ana", obtido.SenhaHash);
        Assert.True(obtido.Acesso);

        obtido.Alterar("Ana Costa", "ana.costa@email.com", "hash-novo");
        await _repositorio.AtualizarAsync(obtido, CancellationToken.None);

        var lista = await _repositorio.ListarAsync(CancellationToken.None);
        Assert.Contains(lista, item => item.Email == "ana.costa@email.com" && item.SenhaHash == "hash-novo");

        await _repositorio.RemoverAsync(obtido, CancellationToken.None);
        Assert.Null(await _repositorio.ObterPorLoginAsync("ana", CancellationToken.None));
        Assert.Null(await _repositorio.ObterPorEmailAsync("ana.costa@email.com", CancellationToken.None));
    }

    public void Dispose()
    {
        _db.Dispose();
        _conexao.Dispose();
    }
}

public class SenhaHasherTests
{
    [Fact]
    public void Guarda_hash_e_confere_a_senha()
    {
        var hasher = new SenhaHasher();
        var hash = hasher.GerarHash("Adm");

        Assert.NotEqual("Adm", hash);
        Assert.True(hasher.Conferir("Adm", hash));
        Assert.False(hasher.Conferir("outra", hash));
    }
}
