namespace Processor.Infra.Ffmpeg;

public interface IExecutorProcesso
{
    Task<int> ExecutarAsync(string entrada, string padraoSaida, CancellationToken cancellationToken);
}
