using Auth.Infra;
using Auth.Infra.Persistence;
using Auth.Infra.Senhas;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Tests;

public class PrepararAuthTests
{
    [Fact]
    public async Task Sobe_o_schema_e_grava_o_administrador_uma_vez()
    {
        await using var conexao = new SqliteConnection("Data Source=:memory:");
        await conexao.OpenAsync();

        var servicos = new ServiceCollection();
        servicos.AddDbContext<AuthDbContext>(opcoes => opcoes.UseSqlite(conexao));
        servicos.AddScoped<Auth.Domain.Usuarios.IUsuarioRepository, UsuarioRepository>();
        servicos.AddSingleton<Auth.Application.Senhas.ISenhaHasher, SenhaHasher>();
        servicos.AddScoped<AdministradorSeed>();
        await using var provedor = servicos.BuildServiceProvider();

        var preparo = new PrepararAuthHostedService(provedor.GetRequiredService<IServiceScopeFactory>());
        await preparo.StartAsync(CancellationToken.None);
        await preparo.StartAsync(CancellationToken.None);
        await preparo.StopAsync(CancellationToken.None);

        using var escopo = provedor.CreateScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<Auth.Domain.Usuarios.IUsuarioRepository>();
        var administrador = await repositorio.ObterPorLoginAsync(AdministradorSeed.Login, CancellationToken.None);
        var lista = await repositorio.ListarAsync(CancellationToken.None);

        Assert.NotNull(administrador);
        Assert.Equal(AdministradorSeed.Nome, administrador!.Nome);
        Assert.Equal(AdministradorSeed.Email, administrador.Email);
        Assert.NotEqual(AdministradorSeed.Senha, administrador.SenhaHash);
        Assert.True(administrador.Administrador);
        Assert.Single(lista);
    }

    [Fact]
    public void Registra_o_postgres_sem_abrir_conexao()
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "Postgres",
            ["ConnectionStrings:Usuarios"] = "Host=localhost;Database=fiapx_usuarios;Username=fiapx;Password=fiapx"
        }).Build();

        var servicos = new ServiceCollection();
        servicos.AddInfrastructure(configuracao);
        using var provedor = servicos.BuildServiceProvider();

        var db = provedor.GetRequiredService<AuthDbContext>();
        Assert.Contains("Npgsql", db.Database.ProviderName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Memoria_semeia_o_administrador_sem_banco()
    {
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "Memory"
        }).Build();

        var servicos = new ServiceCollection();
        servicos.AddInfrastructure(configuracao);
        await using var provedor = servicos.BuildServiceProvider();
        var preparo = new PrepararAuthHostedService(provedor.GetRequiredService<IServiceScopeFactory>());

        await preparo.StartAsync(CancellationToken.None);

        var administrador = await provedor
            .GetRequiredService<Auth.Domain.Usuarios.IUsuarioRepository>()
            .ObterPorLoginAsync(AdministradorSeed.Login, CancellationToken.None);

        Assert.Equal(AdministradorSeed.Email, administrador!.Email);
    }
}
