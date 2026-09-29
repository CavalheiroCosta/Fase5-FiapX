using System.Text.Json.Serialization;

namespace Video.Domain.Videos;

public sealed record ItemLista(
    Guid Id,
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CaminhoZip)
{
    public static ItemLista De(Video video) =>
        new(video.Id, video.Status, video.Status == StatusVideo.Concluido ? video.CaminhoZip : null);

    public static IReadOnlyList<ItemLista> Incluir(IReadOnlyList<ItemLista> atuais, ItemLista item)
    {
        var lista = new List<ItemLista>(atuais);
        var indice = lista.FindIndex(existente => existente.Id == item.Id);
        if (indice >= 0)
            lista[indice] = item;
        else
            lista.Add(item);

        return lista;
    }
}
