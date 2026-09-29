namespace Video.Domain.Videos;

public interface IArmazenamentoVideo
{
    Task<string> SalvarAsync(Guid id, string nomeArquivo, Stream conteudo, CancellationToken cancellationToken);

    Task<Stream> AbrirAsync(string caminho, CancellationToken cancellationToken);

    Task RemoverAsync(string caminho, CancellationToken cancellationToken);
}
