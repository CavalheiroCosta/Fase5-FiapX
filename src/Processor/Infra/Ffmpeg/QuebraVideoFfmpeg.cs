using System.IO.Compression;
using Processor.Domain.Processamento;

namespace Processor.Infra.Ffmpeg;

public sealed class QuebraVideoFfmpeg(IExecutorProcesso executor) : IQuebraVideo
{
    public async Task<Stream> QuebrarAsync(Stream video, CancellationToken cancellationToken)
    {
        var raiz = Directory.CreateTempSubdirectory("fiapx-");
        try
        {
            var entrada = Path.Combine(raiz.FullName, "entrada");
            await using (var arquivo = File.Create(entrada))
                await video.CopyToAsync(arquivo, cancellationToken);

            var frames = Path.Combine(raiz.FullName, "frames");
            Directory.CreateDirectory(frames);
            var padrao = Path.Combine(frames, "frame_%04d.jpg");
            var codigo = await executor.ExecutarAsync(entrada, padrao, cancellationToken);
            if (codigo != 0)
                throw new InvalidOperationException("O ffmpeg não extraiu os frames.");

            var imagens = Directory.GetFiles(frames, "*.jpg");
            if (imagens.Length == 0)
                throw new InvalidOperationException("O vídeo não gerou frames.");

            var zip = new MemoryStream();
            using (var arquivoZip = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var imagem in imagens.OrderBy(Path.GetFileName, StringComparer.Ordinal))
                {
                    var item = arquivoZip.CreateEntry(Path.GetFileName(imagem), CompressionLevel.Optimal);
                    await using var destino = item.Open();
                    var bytes = await File.ReadAllBytesAsync(imagem, cancellationToken);
                    await destino.WriteAsync(bytes, cancellationToken);
                }
            }

            zip.Position = 0;
            return zip;
        }
        finally
        {
            try
            {
                raiz.Delete(recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _ = ex;
            }
        }
    }
}
