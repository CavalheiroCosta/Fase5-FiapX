using Video.Domain.Videos;

namespace Video.Infra.Redis;

public sealed class ListaVideosMemoria : IListaVideos
{
    private readonly Lock _trava = new();
    private readonly Dictionary<string, ItemLista[]> _listas = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<ItemLista>?> ObterAsync(string login, CancellationToken cancellationToken)
    {
        lock (_trava)
        {
            if (!_listas.TryGetValue(login, out var itens))
                return Task.FromResult<IReadOnlyList<ItemLista>?>(null);

            return Task.FromResult<IReadOnlyList<ItemLista>?>(itens.ToArray());
        }
    }

    public Task GuardarAsync(string login, IReadOnlyList<ItemLista> itens, CancellationToken cancellationToken)
    {
        lock (_trava)
            _listas[login] = itens.ToArray();

        return Task.CompletedTask;
    }

    public async Task AtualizarAsync(string login, ItemLista item, CancellationToken cancellationToken)
    {
        var atuais = await ObterAsync(login, cancellationToken);
        if (atuais is null)
            return;

        await GuardarAsync(login, ItemLista.Incluir(atuais, item), cancellationToken);
    }
}
