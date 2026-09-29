using Processor.Domain.Processamento;

namespace Processor.Infra.Storage;

public sealed class ArmazenamentoProcessamentoMemoria(string bucket) : IArmazenamentoProcessamento
{
    private readonly Lock _trava = new();
    private readonly Dictionary<string, byte[]> _objetos = [];

    public Task<Stream> BaixarAsync(string caminho, CancellationToken cancellationToken)
    {
        var chave = CaminhoZip.Chave(bucket, caminho);
        lock (_trava)
        {
            if (!_objetos.TryGetValue(chave, out var bytes))
                throw new InvalidOperationException("O arquivo não está no storage.");

            return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
        }
    }

    public async Task<string> SalvarZipAsync(Guid id, Stream conteudo, CancellationToken cancellationToken)
    {
        var caminho = CaminhoZip.Montar(bucket, id);
        using var memoria = new MemoryStream();
        await conteudo.CopyToAsync(memoria, cancellationToken);
        lock (_trava)
            _objetos[CaminhoZip.Chave(bucket, caminho)] = memoria.ToArray();

        return caminho;
    }

    public void Guardar(string caminho, byte[] bytes)
    {
        lock (_trava)
            _objetos[CaminhoZip.Chave(bucket, caminho)] = bytes;
    }

    public bool Contem(string caminho)
    {
        lock (_trava)
            return _objetos.ContainsKey(CaminhoZip.Chave(bucket, caminho));
    }
}
