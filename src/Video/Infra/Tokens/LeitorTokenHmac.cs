using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Video.Domain.Tokens;

namespace Video.Infra.Tokens;

public sealed class LeitorTokenHmac : ILeitorToken
{
    public const string ClaimLogin = "login";
    public const string ClaimEmail = "email";

    private readonly SymmetricSecurityKey _chave;
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly TimeProvider _relogio;

    public LeitorTokenHmac(string chave, TimeProvider relogio)
    {
        if (string.IsNullOrWhiteSpace(chave) || Encoding.UTF8.GetByteCount(chave) < 32)
            throw new ArgumentException("A chave do token precisa ter pelo menos 32 bytes.", nameof(chave));

        _chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave));
        _relogio = relogio;
        _handler.InboundClaimTypeMap.Clear();
        _handler.OutboundClaimTypeMap.Clear();
    }

    public IdentidadeAutenticada? Ler(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var agora = _relogio.GetUtcNow();
            var principal = _handler.ValidateToken(token.Trim(), Parametros(agora), out _);
            var login = principal.FindFirst(ClaimLogin)?.Value;
            var email = principal.FindFirst(ClaimEmail)?.Value;
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(email))
                return null;

            return new IdentidadeAutenticada(login, email);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException)
        {
            return null;
        }
    }

    private TokenValidationParameters Parametros(DateTimeOffset agora) => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _chave,
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        LifetimeValidator = (_, expires, _, _) =>
            expires is not null && expires.Value > agora.UtcDateTime
    };
}
