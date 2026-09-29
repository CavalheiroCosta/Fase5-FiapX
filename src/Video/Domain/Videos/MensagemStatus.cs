using System.Text.Json;

namespace Video.Domain.Videos;

public readonly record struct MensagemStatus(bool Legivel, Guid Id, string? Momento, string? Caminho)
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);

    public static MensagemStatus Ler(ReadOnlySpan<byte> corpo)
    {
        try
        {
            var json = JsonSerializer.Deserialize<Corpo>(corpo, Opcoes);
            if (json is null || json.Id == Guid.Empty || string.IsNullOrWhiteSpace(json.Momento))
                return new(false, Guid.Empty, null, null);

            return new(true, json.Id, json.Momento, json.Caminho);
        }
        catch (JsonException)
        {
            return new(false, Guid.Empty, null, null);
        }
    }

    private sealed record Corpo(Guid Id, string? Momento, string? Caminho);
}
