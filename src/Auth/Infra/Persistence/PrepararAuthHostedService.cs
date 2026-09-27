using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Auth.Infra.Persistence;

public sealed class PrepararAuthHostedService(IServiceScopeFactory escopos) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetService<AuthDbContext>();
        if (db is not null)
            db.Database.EnsureCreated();

        var semente = escopo.ServiceProvider.GetRequiredService<AdministradorSeed>();
        await semente.GarantirAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
