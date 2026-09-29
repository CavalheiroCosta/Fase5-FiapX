using System.Diagnostics;

namespace Processor.Infra.Ffmpeg;

public sealed class ExecutorFfmpeg : IExecutorProcesso
{
    public const string CaminhoPadrao = "/usr/bin/ffmpeg";

    private readonly string _caminho;

    public ExecutorFfmpeg(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho) || !Path.IsPathRooted(caminho))
            throw new ArgumentException("O caminho do ffmpeg precisa ser absoluto.", nameof(caminho));

        _caminho = caminho;
    }

    public async Task<int> ExecutarAsync(string entrada, string padraoSaida, CancellationToken cancellationToken)
    {
        using var processo = new Process();
        processo.StartInfo = new ProcessStartInfo
        {
            FileName = _caminho,
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
