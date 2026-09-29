using System.Diagnostics;

namespace Processor.Infra.Ffmpeg;

public sealed class ExecutorFfmpeg : IExecutorProcesso
{
    public async Task<int> ExecutarAsync(string entrada, string padraoSaida, CancellationToken cancellationToken)
    {
        using var processo = new Process();
        processo.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        processo.StartInfo.ArgumentList.Add("-y");
        processo.StartInfo.ArgumentList.Add("-i");
        processo.StartInfo.ArgumentList.Add(entrada);
        processo.StartInfo.ArgumentList.Add("-vf");
        processo.StartInfo.ArgumentList.Add("fps=1");
        processo.StartInfo.ArgumentList.Add(padraoSaida);

        if (!processo.Start())
            throw new InvalidOperationException("Não foi possível iniciar o ffmpeg.");

        var erros = processo.StandardError.ReadToEndAsync(cancellationToken);
        var saida = processo.StandardOutput.ReadToEndAsync(cancellationToken);
        await processo.WaitForExitAsync(cancellationToken);
        _ = await erros;
        _ = await saida;
        return processo.ExitCode;
    }
}
