using StackExchange.Redis;

namespace Video.Infra.Redis;

public sealed class ComandoListaRedis : IComandoLista, IDisposable
{
    private readonly Lazy<IConnectionMultiplexer> _conexao;

    public ComandoListaRedis(string conexao)
    {
        if (string.IsNullOrWhiteSpace(conexao))
            throw new ArgumentException("A conexão do Redis é obrigatória.", nameof(conexao));

        _conexao = new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(conexao));
    }

    public async Task<string?> LerAsync(string chave, CancellationToken cancellationToken)
    {
        var valor = await Banco().StringGetAsync(chave);
        if (valor.IsNull)
            return null;

        return valor.ToString();
    }

    public async Task GravarAsync(string chave, string valor, CancellationToken cancellationToken) =>
        await Banco().StringSetAsync(chave, valor);

    public void Dispose()
    {
        if (_conexao.IsValueCreated)
            _conexao.Value.Dispose();
    }

    private IDatabase Banco() => _conexao.Value.GetDatabase();
}
