using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Video.Infra.Filas;

public sealed record OpcoesStatus(string Uri, string Fila);

public sealed record PacoteStatus(ulong Etiqueta, byte[] Corpo);

public interface ICanalStatus
{
    Task PrepararAsync(CancellationToken cancellationToken);

    Task<PacoteStatus?> ReceberAsync(CancellationToken cancellationToken);

    Task ConfirmarAsync(ulong etiqueta, CancellationToken cancellationToken);

    Task RecolocarAsync(ulong etiqueta, CancellationToken cancellationToken);
}

public sealed class CanalStatusRabbit : ICanalStatus, IAsyncDisposable
{
    private readonly string _uri;
    private readonly string _fila;
    private readonly Channel<PacoteStatus> _pacotes = Channel.CreateUnbounded<PacoteStatus>();
    private IConnection? _conexao;
    private IChannel? _canal;

    public CanalStatusRabbit(OpcoesStatus opcoes)
    {
        _uri = opcoes.Uri;
        _fila = opcoes.Fila;
    }

    public async Task PrepararAsync(CancellationToken cancellationToken)
    {
        var fabrica = new ConnectionFactory { Uri = new Uri(_uri) };
        _conexao = await fabrica.CreateConnectionAsync(cancellationToken);
        _canal = await _conexao.CreateChannelAsync(cancellationToken: cancellationToken);
        _conexao.ConnectionShutdownAsync += AoCairAsync;

        await _canal.QueueDeclareAsync(
            queue: _fila,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        await _canal.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken);

        var consumidor = new AsyncEventingBasicConsumer(_canal);
        consumidor.ReceivedAsync += (_, entrega) =>
        {
            _pacotes.Writer.TryWrite(new PacoteStatus(entrega.DeliveryTag, entrega.Body.ToArray()));
            return Task.CompletedTask;
        };

        await _canal.BasicConsumeAsync(_fila, autoAck: false, consumidor, cancellationToken);
    }

    public Task<PacoteStatus?> ReceberAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult<PacoteStatus?>(null);

        return LerAsync(cancellationToken);
    }

    public async Task ConfirmarAsync(ulong etiqueta, CancellationToken cancellationToken) =>
        await ExigirCanal().BasicAckAsync(etiqueta, multiple: false, cancellationToken);

    public async Task RecolocarAsync(ulong etiqueta, CancellationToken cancellationToken) =>
        await ExigirCanal().BasicNackAsync(etiqueta, multiple: false, requeue: true, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        _pacotes.Writer.TryComplete();
        if (_conexao is not null)
            _conexao.ConnectionShutdownAsync -= AoCairAsync;

        if (_canal is not null)
            await _canal.DisposeAsync();

        if (_conexao is not null)
            await _conexao.DisposeAsync();
    }

    private async Task<PacoteStatus?> LerAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _pacotes.Reader.ReadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private IChannel ExigirCanal() =>
        _canal ?? throw new InvalidOperationException("A fila de status não está preparada.");

    private Task AoCairAsync(object? remetente, ShutdownEventArgs argumentos)
    {
        _ = remetente;
        _ = argumentos;
        _pacotes.Writer.TryComplete(new IOException("A fila de status caiu."));
        return Task.CompletedTask;
    }
}
