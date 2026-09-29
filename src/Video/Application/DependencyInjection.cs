using Video.Application.Envio;
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
            provedor.GetRequiredService<MetricasVideo>()));
        services.AddScoped(provedor => new EnviarVideoUseCase(
            provedor.GetRequiredService<ILeitorToken>(),
            provedor.GetRequiredService<IArmazenamentoVideo>(),
            provedor.GetRequiredService<IVideoRepository>(),
            provedor.GetRequiredService<IFilaProcessamento>(),
            limiteBytes));
        return services;
    }
}
