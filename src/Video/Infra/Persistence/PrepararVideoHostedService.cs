using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Video.Infra.Persistence;

public sealed class PrepararVideoHostedService(IServiceScopeFactory escopos) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetService<VideoDbContext>();
        if (db is not null)
            await db.Database.EnsureCreatedAsync(cancellationToken);

        foreach (var preparo in escopo.ServiceProvider.GetServices<IPreparacaoExterna>())
            await preparo.PrepararAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
