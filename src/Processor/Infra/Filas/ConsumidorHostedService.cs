using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Processor.Infra.Filas;

public sealed class ConsumidorHostedService : BackgroundService
{
    private readonly Func<ISessaoConsumo> _fabrica;
    private readonly TimeSpan _espera;
    private readonly ILogger<ConsumidorHostedService> _logger;

    public ConsumidorHostedService(Func<ISessaoConsumo> fabrica, ILogger<ConsumidorHostedService> logger)
        : this(fabrica, TimeSpan.FromSeconds(2), logger)
    {
    }

    public ConsumidorHostedService(Func<ISessaoConsumo> fabrica, TimeSpan espera, ILogger<ConsumidorHostedService> logger)
    {
        _fabrica = fabrica;
        _espera = espera;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sessao = _fabrica();
                await sessao.ExecutarAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "A conexão com a fila caiu. Nova tentativa em seguida.");
                try
                {
                    await Task.Delay(_espera, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}
