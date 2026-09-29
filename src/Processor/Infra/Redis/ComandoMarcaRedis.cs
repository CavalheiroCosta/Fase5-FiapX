using StackExchange.Redis;

namespace Processor.Infra.Redis;

public sealed class ComandoMarcaRedis : IComandoMarca, IDisposable
{
    private readonly Lazy<IConnectionMultiplexer> _conexao;

    public ComandoMarcaRedis(string conexao)
    {
        if (string.IsNullOrWhiteSpace(conexao))
            throw new ArgumentException("A conexão do Redis é obrigatória.", nameof(conexao));

        _conexao = new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(conexao));
    }

    public Task<bool> DefinirSeAusenteAsync(string chave, CancellationToken cancellationToken) =>
        Banco().StringSetAsync(chave, "1", when: When.NotExists);

    public async Task RemoverAsync(string chave, CancellationToken cancellationToken) =>
        await Banco().KeyDeleteAsync(chave);

    public void Dispose()
    {
        if (_conexao.IsValueCreated)
            _conexao.Value.Dispose();
    }

    private IDatabase Banco() => _conexao.Value.GetDatabase();
}
