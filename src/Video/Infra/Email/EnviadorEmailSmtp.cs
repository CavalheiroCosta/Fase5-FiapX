using System.Net.Mail;
using Video.Domain.Videos;

namespace Video.Infra.Email;

#pragma warning disable SYSLIB0014

public sealed class EnviadorEmailSmtp : IEnviadorEmail
{
    private readonly string _host;
    private readonly int _porta;
    private readonly string _remetente;

    public EnviadorEmailSmtp(string host, int porta, string remetente)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("O host do e-mail é obrigatório.", nameof(host));
        if (porta <= 0)
            throw new ArgumentOutOfRangeException(nameof(porta), "A porta do e-mail é obrigatória.");
        if (string.IsNullOrWhiteSpace(remetente))
            throw new ArgumentException("O remetente do e-mail é obrigatório.", nameof(remetente));

        _host = host;
        _porta = porta;
        _remetente = remetente;
    }

    public async Task EnviarErroAsync(string destinatario, Guid id, CancellationToken cancellationToken)
    {
        using var mensagem = new MailMessage(_remetente, destinatario, AvisoErro.Assunto, AvisoErro.Corpo(id));
        using var cliente = new SmtpClient(_host, _porta) { EnableSsl = false }; // NOSONAR SMTP só do Mailpit local, sem TLS
        await cliente.SendMailAsync(mensagem, cancellationToken);
    }
}

#pragma warning restore SYSLIB0014
