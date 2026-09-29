using Video.Domain.Videos;

namespace Video.Infra.Email;

public sealed class EnviadorEmailMemoria : IEnviadorEmail
{
    private readonly Lock _trava = new();
    private readonly List<Aviso> _enviados = [];

    public IReadOnlyList<Aviso> Enviados
    {
        get
        {
            lock (_trava)
                return _enviados.ToArray();
        }
    }

    public Task EnviarErroAsync(string destinatario, Guid id, CancellationToken cancellationToken)
    {
        lock (_trava)
            _enviados.Add(new Aviso(destinatario, AvisoErro.Assunto, AvisoErro.Corpo(id)));

        return Task.CompletedTask;
    }

    public sealed record Aviso(string Destinatario, string Assunto, string Corpo);
}
