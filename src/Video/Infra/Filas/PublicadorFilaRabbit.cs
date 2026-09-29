using RabbitMQ.Client;

namespace Video.Infra.Filas;

public sealed class PublicadorFilaRabbit(string uri) : IPublicadorFila
{
    public async Task DeclararAsync(string fila, CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var canal = await conexao.CreateChannelAsync(cancellationToken: cancellationToken);
        await DeclararCanalAsync(canal, fila, cancellationToken);
    }

    public async Task PublicarAsync(string fila, ReadOnlyMemory<byte> corpo, CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var canal = await conexao.CreateChannelAsync(cancellationToken: cancellationToken);
        await DeclararCanalAsync(canal, fila, cancellationToken);
        await canal.BasicPublishAsync(
            exchange: "",
            routingKey: fila,
            mandatory: false,
            basicProperties: new BasicProperties { Persistent = true },
            body: corpo,
            cancellationToken: cancellationToken);
    }

    private async Task<IConnection> AbrirAsync(CancellationToken cancellationToken)
    {
        var fabrica = new ConnectionFactory { Uri = new Uri(uri) };
        return await fabrica.CreateConnectionAsync(cancellationToken);
    }

    private static Task DeclararCanalAsync(IChannel canal, string fila, CancellationToken cancellationToken) =>
        canal.QueueDeclareAsync(
            queue: fila,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
}
