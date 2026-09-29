using System.IO.Compression;
using Processor.Domain.Processamento;

namespace Processor.Infra.Ffmpeg;

public sealed class QuebraVideoFixa : IQuebraVideo
{
    public Task<Stream> QuebrarAsync(Stream video, CancellationToken cancellationToken)
    {
        var zip = new MemoryStream();
        using (var arquivo = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var destino = arquivo.CreateEntry("frame_0001.jpg").Open();
            destino.WriteByte(1);
        }

        zip.Position = 0;
        return Task.FromResult<Stream>(zip);
    }
}
