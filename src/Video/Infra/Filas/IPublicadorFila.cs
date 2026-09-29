namespace Video.Infra.Filas;

public interface IPublicadorFila
{
    Task DeclararAsync(string fila, CancellationToken cancellationToken);

    Task PublicarAsync(string fila, ReadOnlyMemory<byte> corpo, CancellationToken cancellationToken);
}
