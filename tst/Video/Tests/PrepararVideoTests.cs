using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Video.Domain.Videos;
using Video.Infra;
using Video.Infra.Filas;
using Video.Infra.Persistence;
using Video.Infra.Storage;

namespace Video.Tests;

public class PrepararVideoTests
{
    [Fact]
    public async Task Sobe_o_schema_e_prepara_storage_e_fila()
    {
        await using var conexao = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await conexao.OpenAsync();

        var servicos = new ServiceCollection();
        servicos.AddDbContext<VideoDbContext>(opcoes => opcoes.UseSqlite(conexao));
        servicos.AddSingleton<PreparacaoFalsa>();
        servicos.AddSingleton<IPreparacaoExterna>(provedor => provedor.GetRequiredService<PreparacaoFalsa>());
        await using var provedor = servicos.BuildServiceProvider();

        var preparo = new PrepararVideoHostedService(provedor.GetRequiredService<IServiceScopeFactory>());
        await preparo.StartAsync(CancellationToken.None);
        await preparo.StartAsync(CancellationToken.None);
        await preparo.StopAsync(CancellationToken.None);

        Assert.Equal(2, provedor.GetRequiredService<PreparacaoFalsa>().Chamadas);
        using var escopo = provedor.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<VideoDbContext>();
        Assert.True(await db.Videos.AnyAsync() == false);
    }

    [Fact]
    public async Task Memoria_sobe_sem_banco_nem_fila_externa()
    {
        var servicos = new ServiceCollection();
        servicos.AddInfrastructure(Configuracao(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "Memory",
            ["Token:Chave"] = "0123456789abcdef0123456789abcdef"
        }));
        await using var provedor = servicos.BuildServiceProvider();
        var preparo = new PrepararVideoHostedService(provedor.GetRequiredService<IServiceScopeFactory>());

        await preparo.StartAsync(CancellationToken.None);
        await preparo.StopAsync(CancellationToken.None);

        Assert.IsType<ArmazenamentoMemoria>(provedor.GetRequiredService<IArmazenamentoVideo>());
        Assert.IsType<FilaProcessamentoMemoria>(provedor.GetRequiredService<IFilaProcessamento>());
        Assert.Empty(provedor.GetServices<IPreparacaoExterna>());
    }

    [Fact]
    public void Registra_postgres_storage_e_fila_sem_abrir_conexao()
    {
        var servicos = new ServiceCollection();
        servicos.AddInfrastructure(Configuracao(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "Postgres",
            ["ConnectionStrings:Videos"] = "Host=localhost;Port=5433;Database=fiapx_videos;Username=fiapx;Password=fiapx",
            ["Token:Chave"] = "0123456789abcdef0123456789abcdef",
            ["Storage:ServiceUrl"] = "http://localhost:9000",
            ["Storage:AccessKey"] = "fiapx",
            ["Storage:SecretKey"] = "fiapxfiapx",
            ["Storage:Bucket"] = "",
            ["Queue:Uri"] = "amqp://fiapx:fiapx@localhost:5672/",
            ["Queue:Nome"] = " "
        }));
        using var provedor = servicos.BuildServiceProvider();

        var db = provedor.GetRequiredService<VideoDbContext>();
        Assert.Contains("Npgsql", db.Database.ProviderName, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<ArmazenamentoS3>(provedor.GetRequiredService<IArmazenamentoVideo>());
        Assert.IsType<FilaProcessamento>(provedor.GetRequiredService<IFilaProcessamento>());
        Assert.Equal(2, provedor.GetServices<IPreparacaoExterna>().Count());
        (provedor.GetRequiredService<IClienteObjeto>() as IDisposable)?.Dispose();
    }

    [Fact]
    public void Exige_chave_url_e_credencial()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(Configuracao(new Dictionary<string, string?>())));

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(Configuracao(new Dictionary<string, string?>
            {
                ["Token:Chave"] = "0123456789abcdef0123456789abcdef"
            })));

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(Configuracao(new Dictionary<string, string?>
            {
                ["Token:Chave"] = "0123456789abcdef0123456789abcdef",
                ["Storage:ServiceUrl"] = "http://localhost:9000"
            })));

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(Configuracao(new Dictionary<string, string?>
            {
                ["Token:Chave"] = "0123456789abcdef0123456789abcdef",
                ["Storage:ServiceUrl"] = "http://localhost:9000",
                ["Storage:AccessKey"] = "fiapx"
            })));

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(Configuracao(new Dictionary<string, string?>
            {
                ["Token:Chave"] = "0123456789abcdef0123456789abcdef",
                ["Storage:ServiceUrl"] = "http://localhost:9000",
                ["Storage:AccessKey"] = "fiapx",
                ["Storage:SecretKey"] = "fiapxfiapx"
            })));
    }

    [Fact]
    public void Cliente_s3_exige_url()
    {
        Assert.Throws<ArgumentException>(() => new ClienteObjetoS3(" ", "fiapx", "fiapxfiapx"));
        using var cliente = new ClienteObjetoS3("http://localhost:9000", "fiapx", "fiapxfiapx");
        Assert.NotNull(cliente);
    }

    private static IConfiguration Configuracao(Dictionary<string, string?> valores) =>
        new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

    private sealed class PreparacaoFalsa : IPreparacaoExterna
    {
        public int Chamadas { get; private set; }

        public Task PrepararAsync(CancellationToken cancellationToken)
        {
            Chamadas++;
            return Task.CompletedTask;
        }
    }
}
