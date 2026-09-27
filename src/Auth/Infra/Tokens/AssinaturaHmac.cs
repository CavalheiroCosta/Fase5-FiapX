using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Auth.Domain.Tokens;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infra.Tokens;

public sealed class AssinaturaHmac : IAssinaturaToken
{
    public const string ClaimLogin = "login";
    public const string ClaimEmail = "email";

    private readonly SymmetricSecurityKey _chave;
    private readonly JwtSecurityTokenHandler _handler = new();

    public AssinaturaHmac(string chave)
    {
        if (string.IsNullOrWhiteSpace(chave) || Encoding.UTF8.GetByteCount(chave) < 32)
            throw new ArgumentException("A chave do token precisa ter pelo menos 32 bytes.", nameof(chave));

        _chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave));
        _handler.InboundClaimTypeMap.Clear();
        _handler.OutboundClaimTypeMap.Clear();
    }

    public string Assinar(IdentidadeAutenticada identidade, DateTimeOffset emitidoEm, DateTimeOffset expiraEm)
    {
        var descritor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                [ClaimLogin] = identidade.Login,
                [ClaimEmail] = identidade.Email
            },
            NotBefore = emitidoEm.UtcDateTime,
            IssuedAt = emitidoEm.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            SigningCredentials = new SigningCredentials(_chave, SecurityAlgorithms.HmacSha256)
        };

        return _handler.WriteToken(_handler.CreateToken(descritor));
    }

    public IdentidadeAutenticada? Ler(string token, DateTimeOffset agora)
    {
        try
        {
            var principal = _handler.ValidateToken(token, Parametros(agora), out _);
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
