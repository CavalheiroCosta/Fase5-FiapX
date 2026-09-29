namespace Processor.Domain.Processamento;

public static class CaminhoZip
{
    public const string BucketPadrao = "videos";

    public static string Montar(string bucket, Guid id)
    {
        if (string.IsNullOrWhiteSpace(bucket) || id == Guid.Empty)
            throw new ArgumentException("O caminho do ZIP é obrigatório.");

        return $"{bucket}/{id:D}/{id:D}.zip";
    }

    public static string Chave(string bucket, string caminho)
    {
        var prefixo = bucket + "/";
        if (string.IsNullOrWhiteSpace(caminho) || !caminho.StartsWith(prefixo, StringComparison.Ordinal) || caminho.Length == prefixo.Length)
            throw new ArgumentException("Caminho inválido.", nameof(caminho));

        return caminho[prefixo.Length..];
    }
}
