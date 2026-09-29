using System.Text.Json;
using Video.Domain.Videos;
using Video.Infra;

namespace Video.Infra.Filas;

public static class MensagemProcessamento
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);

    public static byte[] Serializar(Guid id, string caminho) =>
        JsonSerializer.SerializeToUtf8Bytes(new Corpo(id, caminho), Opcoes);

    private sealed record Corpo(Guid Id, string Caminho);
}

public sealed class FilaProcessamento(IPublicadorFila publicador, string nome) : IFilaProcessamento, IPreparacaoExterna
{
    public Task PublicarAsync(Guid id, string caminho, CancellationToken cancellationToken) =>
        publicador.PublicarAsync(nome, MensagemProcessamento.Serializar(id, caminho), cancellationToken);

    public Task PrepararAsync(CancellationToken cancellationToken) =>
        publicador.DeclararAsync(nome, cancellationToken);
}
