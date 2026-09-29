using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Video.Infra.Filas;

public sealed class ConsumidorStatusHostedService(
    Func<ISessaoStatus> fabrica,
    TimeSpan espera,
    ILogger<ConsumidorStatusHostedService> logger) : BackgroundService
{
    public ConsumidorStatusHostedService(Func<ISessaoStatus> fabrica, ILogger<ConsumidorStatusHostedService> logger)
        : this(fabrica, TimeSpan.FromSeconds(2), logger)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await RodarAsync(stoppingToken))
        {
            try
            {
                await Task.Delay(espera, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task<bool> RodarAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return false;

        try
        {
            await fabrica().ExecutarAsync(stoppingToken);
            return !stoppingToken.IsCancellationRequested;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "A conexão com a fila de status caiu. Nova tentativa em seguida.");
            return !stoppingToken.IsCancellationRequested;
        }
    }
}
