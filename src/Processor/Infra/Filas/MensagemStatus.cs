using System.Text.Json;
using Processor.Domain.Processamento;

namespace Processor.Infra.Filas;

public static class MensagemStatus
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);

    public static byte[] Serializar(Guid id, string momento, string? caminho)
    {
        if (momento == MomentoStatus.Sucesso)
            return JsonSerializer.SerializeToUtf8Bytes(new ComCaminho(id, momento, caminho ?? ""), Opcoes);

        return JsonSerializer.SerializeToUtf8Bytes(new SemCaminho(id, momento), Opcoes);
    }

    private sealed record SemCaminho(Guid Id, string Momento);

    private sealed record ComCaminho(Guid Id, string Momento, string Caminho);
}
