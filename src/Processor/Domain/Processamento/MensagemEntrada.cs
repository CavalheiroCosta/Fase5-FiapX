using System.Text.Json;

namespace Processor.Domain.Processamento;

public readonly record struct MensagemEntrada(bool Legivel, Guid Id, string? Caminho)
{
    public static MensagemEntrada Ler(ReadOnlySpan<byte> corpo)
    {
        if (corpo.IsEmpty)
            return new(false, Guid.Empty, null);

        try
        {
            using var json = JsonDocument.Parse(corpo.ToArray());
            if (!json.RootElement.TryGetProperty("id", out var idElemento) || !idElemento.TryGetGuid(out var id) || id == Guid.Empty)
                return new(false, Guid.Empty, null);

            string? caminho = null;
            if (json.RootElement.TryGetProperty("caminho", out var caminhoElemento) && caminhoElemento.ValueKind == JsonValueKind.String)
                caminho = caminhoElemento.GetString();

            return new(true, id, caminho);
        }
        catch (JsonException)
        {
            return new(false, Guid.Empty, null);
        }
    }
}
