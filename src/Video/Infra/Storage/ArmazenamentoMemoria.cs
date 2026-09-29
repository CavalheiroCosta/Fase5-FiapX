using Video.Domain.Videos;

namespace Video.Infra.Storage;

public sealed class ArmazenamentoMemoria : IArmazenamentoVideo
{
    private readonly Lock _trava = new();
    private readonly Dictionary<string, byte[]> _objetos = new(StringComparer.Ordinal);
    private readonly string _bucket;

    public ArmazenamentoMemoria(string bucket)
    {
        _bucket = bucket;
    }

    public int Contagem
    {
        get
        {
            lock (_trava)
                return _objetos.Count;
        }
    }

    public bool Contem(string caminho)
    {
        lock (_trava)
            return _objetos.ContainsKey(caminho);
    }

    public long Tamanho(string caminho)
    {
        lock (_trava)
            return _objetos.TryGetValue(caminho, out var bytes) ? bytes.LongLength : 0;
    }

    public async Task<string> SalvarAsync(Guid id, string nomeArquivo, Stream conteudo, CancellationToken cancellationToken)
    {
        using var memoria = new MemoryStream();
        await conteudo.CopyToAsync(memoria, cancellationToken);
        var caminho = CaminhoVideo.Montar(_bucket, id, nomeArquivo);
        lock (_trava)
            _objetos[caminho] = memoria.ToArray();

        return caminho;
    }

    public Task RemoverAsync(string caminho, CancellationToken cancellationToken)
    {
        lock (_trava)
            _objetos.Remove(caminho);

        return Task.CompletedTask;
    }
}
