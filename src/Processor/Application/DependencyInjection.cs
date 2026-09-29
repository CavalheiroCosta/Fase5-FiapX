using Microsoft.Extensions.DependencyInjection;
using Processor.Domain.Processamento;

namespace Processor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<MetricasProcessor>();
        services.AddSingleton(provedor => new ProcessarVideoUseCase(
            provedor.GetRequiredService<IMarcaVideo>(),
            provedor.GetRequiredService<IFilaStatus>(),
            provedor.GetRequiredService<IArmazenamentoProcessamento>(),
            provedor.GetRequiredService<IQuebraVideo>(),
            provedor.GetRequiredService<MetricasProcessor>()));
        services.AddSingleton<EntregaMensagem>();
        return services;
    }
}
