namespace Video.Domain.Videos;

public static class CaminhoVideo
{
    public const string BucketPadrao = "videos";

    public static string? NomeSeguro(string? nomeArquivo)
    {
        if (string.IsNullOrWhiteSpace(nomeArquivo))
            return null;

        var nome = Path.GetFileName(nomeArquivo.Trim());
        if (string.IsNullOrWhiteSpace(nome) || nome is "." or "..")
            return null;

        return nome;
    }

    public static string Montar(string bucket, Guid id, string nomeArquivo)
    {
        var nome = NomeSeguro(nomeArquivo);
        if (string.IsNullOrWhiteSpace(bucket) || id == Guid.Empty || nome is null)
            throw new ArgumentException("O caminho do vídeo é obrigatório.");

        return $"{bucket}/{id:D}/{nome}";
    }

    public static string Chave(string bucket, string caminho)
    {
        var prefixo = bucket + "/";
        if (string.IsNullOrWhiteSpace(caminho) || !caminho.StartsWith(prefixo, StringComparison.Ordinal) || caminho.Length == prefixo.Length)
            throw new ArgumentException("Caminho inválido.", nameof(caminho));

        return caminho[prefixo.Length..];
    }
}
