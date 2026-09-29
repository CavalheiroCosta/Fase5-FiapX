using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Video.Infra.Filas;

public sealed class ConsumidorStatusHostedService : BackgroundService
{
    private readonly Func<ISessaoStatus> _fabrica;
    private readonly TimeSpan _espera;
    private readonly ILogger<ConsumidorStatusHostedService> _logger;

    public ConsumidorStatusHostedService(Func<ISessaoStatus> fabrica, ILogger<ConsumidorStatusHostedService> logger)
        : this(fabrica, TimeSpan.FromSeconds(2), logger)
    {
    }

    public ConsumidorStatusHostedService(
        Func<ISessaoStatus> fabrica,
        TimeSpan espera,
        ILogger<ConsumidorStatusHostedService> logger)
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
                _logger.LogWarning(ex, "A conexão com a fila de status caiu. Nova tentativa em seguida.");
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
