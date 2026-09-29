using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Video.Domain.Videos;
using Video.Infra.Filas;
using Video.Infra.Persistence;
using Video.Infra.Redis;
using Video.Infra.Storage;

namespace Video.Tests.Suporte;

public class VideoApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(SubstituirInfraestrutura);
    }

    public static void SubstituirInfraestrutura(IServiceCollection servicos)
    {
        Remover(servicos, typeof(IVideoRepository));
        Remover(servicos, typeof(VideoRepositorioMemoria));
        Remover(servicos, typeof(VideoDbContext));
        Remover(servicos, typeof(DbContextOptions<VideoDbContext>));
        Remover(servicos, typeof(IArmazenamentoVideo));
        Remover(servicos, typeof(ArmazenamentoS3));
        Remover(servicos, typeof(ArmazenamentoMemoria));
        Remover(servicos, typeof(IClienteObjeto));
        Remover(servicos, typeof(IFilaProcessamento));
        Remover(servicos, typeof(FilaProcessamento));
        Remover(servicos, typeof(FilaProcessamentoMemoria));
        Remover(servicos, typeof(IPublicadorFila));
        Remover(servicos, typeof(Video.Infra.IPreparacaoExterna));
        Remover(servicos, typeof(ConsumidorStatusHostedService));
        Remover(servicos, typeof(IListaVideos));
        Remover(servicos, typeof(ListaVideos));
        Remover(servicos, typeof(ListaVideosMemoria));
        Remover(servicos, typeof(ComandoListaRedis));

        servicos.AddSingleton<VideoRepositorioMemoria>();
        servicos.AddSingleton<IVideoRepository>(provedor => provedor.GetRequiredService<VideoRepositorioMemoria>());
        servicos.AddSingleton(_ => new ArmazenamentoMemoria(CaminhoVideo.BucketPadrao));
        servicos.AddSingleton<IArmazenamentoVideo>(provedor => provedor.GetRequiredService<ArmazenamentoMemoria>());
        servicos.AddSingleton<FilaProcessamentoMemoria>();
        servicos.AddSingleton<IFilaProcessamento>(provedor => provedor.GetRequiredService<FilaProcessamentoMemoria>());
        servicos.AddSingleton<ListaVideosMemoria>();
        servicos.AddSingleton<IListaVideos>(provedor => provedor.GetRequiredService<ListaVideosMemoria>());
    }

    protected static void Remover(IServiceCollection servicos, Type tipo)
    {
        var alvos = servicos.Where(descritor =>
            descritor.ServiceType == tipo || descritor.ImplementationType == tipo).ToList();
        foreach (var descritor in alvos)
            servicos.Remove(descritor);
    }
}
