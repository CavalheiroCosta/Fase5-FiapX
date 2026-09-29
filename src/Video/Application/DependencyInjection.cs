using Video.Application.Envio;
using Video.Application.Listagem;
using Video.Application.Status;
using Video.Domain.Tokens;
using Video.Domain.Videos;
using Microsoft.Extensions.DependencyInjection;

namespace Video.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, long limiteBytes)
    {
        services.AddSingleton<MetricasVideo>();
        services.AddScoped(provedor => new AplicarStatusUseCase(
            provedor.GetRequiredService<IVideoRepository>(),
            provedor.GetRequiredService<IListaVideos>(),
            provedor.GetRequiredService<MetricasVideo>()));
        services.AddScoped(provedor => new EnviarVideoUseCase(
            provedor.GetRequiredService<ILeitorToken>(),
            provedor.GetRequiredService<IArmazenamentoVideo>(),
            provedor.GetRequiredService<IVideoRepository>(),
            provedor.GetRequiredService<IFilaProcessamento>(),
            provedor.GetRequiredService<IListaVideos>(),
            limiteBytes));
        services.AddScoped(provedor => new ListarVideosUseCase(
            provedor.GetRequiredService<ILeitorToken>(),
            provedor.GetRequiredService<IListaVideos>(),
            provedor.GetRequiredService<IVideoRepository>(),
            provedor.GetRequiredService<MetricasVideo>()));
        return services;
    }
}
