using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Video.Domain.Tokens;
using Video.Domain.Videos;
using Video.Infra.Filas;
using Video.Infra.Persistence;
using Video.Infra.Storage;
using Video.Infra.Tokens;

namespace Video.Infra;

public static class DependencyInjection
{
    public const string ProvedorMemoria = "Memory";
    public const string ProvedorPostgres = "Postgres";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        var chave = configuration["Token:Chave"]
            ?? throw new InvalidOperationException("Token:Chave não está configurada.");
        services.AddSingleton<ILeitorToken>(provedor =>
            new LeitorTokenHmac(chave, provedor.GetRequiredService<TimeProvider>()));

        var provedorPersistencia = configuration["Persistence:Provider"] ?? ProvedorPostgres;
        if (provedorPersistencia.Equals(ProvedorMemoria, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<VideoRepositorioMemoria>();
            services.AddSingleton<IVideoRepository>(provedor => provedor.GetRequiredService<VideoRepositorioMemoria>());
            services.AddSingleton<ArmazenamentoMemoria>(_ => new ArmazenamentoMemoria(CaminhoVideo.BucketPadrao));
            services.AddSingleton<IArmazenamentoVideo>(provedor => provedor.GetRequiredService<ArmazenamentoMemoria>());
            services.AddSingleton<FilaProcessamentoMemoria>();
            services.AddSingleton<IFilaProcessamento>(provedor => provedor.GetRequiredService<FilaProcessamentoMemoria>());
            return services;
        }

        var conexao = configuration.GetConnectionString("Videos");
        services.AddDbContext<VideoDbContext>(opcoes => opcoes.UseNpgsql(conexao));
        services.AddScoped<IVideoRepository, VideoRepository>();

        var bucket = configuration["Storage:Bucket"];
        if (string.IsNullOrWhiteSpace(bucket))
            bucket = CaminhoVideo.BucketPadrao;

        var serviceUrl = configuration["Storage:ServiceUrl"]
            ?? throw new InvalidOperationException("Storage:ServiceUrl não está configurada.");
        var accessKey = configuration["Storage:AccessKey"]
            ?? throw new InvalidOperationException("Storage:AccessKey não está configurada.");
        var secretKey = configuration["Storage:SecretKey"]
            ?? throw new InvalidOperationException("Storage:SecretKey não está configurada.");

        services.AddSingleton<IClienteObjeto>(_ => new ClienteObjetoS3(serviceUrl, accessKey, secretKey));
        services.AddSingleton(provedor => new ArmazenamentoS3(provedor.GetRequiredService<IClienteObjeto>(), bucket));
        services.AddSingleton<IArmazenamentoVideo>(provedor => provedor.GetRequiredService<ArmazenamentoS3>());
        services.AddSingleton<IPreparacaoExterna>(provedor => provedor.GetRequiredService<ArmazenamentoS3>());

        var uriFila = configuration["Queue:Uri"]
            ?? throw new InvalidOperationException("Queue:Uri não está configurada.");
        var nomeFila = configuration["Queue:Nome"];
        if (string.IsNullOrWhiteSpace(nomeFila))
            nomeFila = "processamento";

        services.AddSingleton<IPublicadorFila>(_ => new PublicadorFilaRabbit(uriFila));
        services.AddSingleton(provedor => new FilaProcessamento(provedor.GetRequiredService<IPublicadorFila>(), nomeFila));
        services.AddSingleton<IFilaProcessamento>(provedor => provedor.GetRequiredService<FilaProcessamento>());
        services.AddSingleton<IPreparacaoExterna>(provedor => provedor.GetRequiredService<FilaProcessamento>());
        return services;
    }
}
