using System.Text.Json;
using System.Text.Json.Serialization;
using Video.Domain.Videos;

namespace Video.Infra.Redis;

public sealed class ListaVideos(IComandoLista comando) : IListaVideos
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Chave(string login) => $"lista:{login}";

    public async Task<IReadOnlyList<ItemLista>?> ObterAsync(string login, CancellationToken cancellationToken)
    {
        var texto = await comando.LerAsync(Chave(login), cancellationToken);
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<ItemLista>>(texto, Opcoes) ?? [];
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public Task GuardarAsync(string login, IReadOnlyList<ItemLista> itens, CancellationToken cancellationToken) =>
        comando.GravarAsync(Chave(login), JsonSerializer.Serialize(itens, Opcoes), cancellationToken);

    public async Task AtualizarAsync(string login, ItemLista item, CancellationToken cancellationToken)
    {
        var atuais = await ObterAsync(login, cancellationToken);
        if (atuais is null)
            return;

        await GuardarAsync(login, ItemLista.Incluir(atuais, item), cancellationToken);
    }
}
