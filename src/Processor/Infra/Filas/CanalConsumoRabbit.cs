using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Processor.Infra.Filas;

public sealed class CanalConsumoRabbit : ICanalConsumo, IAsyncDisposable
{
    private readonly string _uri;
    private readonly string _processamento;
    private readonly string _status;
    private readonly Channel<Pacote> _pacotes = Channel.CreateUnbounded<Pacote>();
    private IConnection? _conexao;
    private IChannel? _canal;

    public CanalConsumoRabbit(OpcoesFila opcoes)
    {
        _uri = opcoes.Uri;
        _processamento = opcoes.Processamento;
        _status = opcoes.Status;
    }

    public async Task PrepararAsync(CancellationToken cancellationToken)
    {
        var fabrica = new ConnectionFactory { Uri = new Uri(_uri) };
        _conexao = await fabrica.CreateConnectionAsync(cancellationToken);
        _canal = await _conexao.CreateChannelAsync(cancellationToken: cancellationToken);
        _conexao.ConnectionShutdownAsync += AoCairAsync;

        await DeclararAsync(_canal, _processamento, cancellationToken);
        await DeclararAsync(_canal, _status, cancellationToken);
        await _canal.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken);

        var consumidor = new AsyncEventingBasicConsumer(_canal);
        consumidor.ReceivedAsync += (_, entrega) =>
        {
            _pacotes.Writer.TryWrite(new Pacote(entrega.DeliveryTag, entrega.Body.ToArray()));
            return Task.CompletedTask;
        };

        await _canal.BasicConsumeAsync(_processamento, autoAck: false, consumidor, cancellationToken);
    }

    public async Task<Pacote?> ReceberAsync(CancellationToken cancellationToken)
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

    public async Task ConfirmarAsync(ulong etiqueta, CancellationToken cancellationToken)
    {
        var canal = _canal ?? throw new InvalidOperationException("A fila não está preparada.");
        await canal.BasicAckAsync(etiqueta, multiple: false, cancellationToken);
    }

    public async Task RecolocarAsync(ulong etiqueta, CancellationToken cancellationToken)
    {
        var canal = _canal ?? throw new InvalidOperationException("A fila não está preparada.");
        await canal.BasicNackAsync(etiqueta, multiple: false, requeue: true, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _pacotes.Writer.TryComplete();
        if (_canal is not null)
            await _canal.DisposeAsync();
        if (_conexao is not null)
        {
            _conexao.ConnectionShutdownAsync -= AoCairAsync;
            await _conexao.DisposeAsync();
        }
    }

    private Task AoCairAsync(object? remetente, ShutdownEventArgs argumentos)
    {
        _ = remetente;
        _ = argumentos;
        _pacotes.Writer.TryComplete(new IOException("A conexão com a fila caiu."));
        return Task.CompletedTask;
    }

    private static Task DeclararAsync(IChannel canal, string fila, CancellationToken cancellationToken) =>
        canal.QueueDeclareAsync(
            queue: fila,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
}
