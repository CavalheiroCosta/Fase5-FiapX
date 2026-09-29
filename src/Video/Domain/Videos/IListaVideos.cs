namespace Video.Domain.Videos;

public interface IListaVideos
{
    Task<IReadOnlyList<ItemLista>?> ObterAsync(string login, CancellationToken cancellationToken);

    Task GuardarAsync(string login, IReadOnlyList<ItemLista> itens, CancellationToken cancellationToken);

    Task AtualizarAsync(string login, ItemLista item, CancellationToken cancellationToken);
}
