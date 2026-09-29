using Microsoft.Extensions.Hosting;
using Processor.Infra.Storage;

namespace Processor.Infra.Filas;

public sealed class PrepararProcessorHostedService(IClienteObjeto cliente, string bucket) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        cliente.GarantirBucketAsync(bucket, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
