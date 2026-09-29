namespace Processor.Infra.Filas;

public sealed record OpcoesFila(string Uri, string Processamento, string Status);

public sealed record Pacote(ulong Etiqueta, byte[] Corpo);

public interface ICanalConsumo
{
    Task PrepararAsync(CancellationToken cancellationToken);

    Task<Pacote?> ReceberAsync(CancellationToken cancellationToken);

    Task ConfirmarAsync(ulong etiqueta, CancellationToken cancellationToken);

    Task RecolocarAsync(ulong etiqueta, CancellationToken cancellationToken);
}
