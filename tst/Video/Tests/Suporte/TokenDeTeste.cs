using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Video.Tests.Suporte;

public static class TokenDeTeste
{
    public static string Emitir(
        string chave,
        string? login,
        string? email,
        DateTimeOffset emitidoEm,
        DateTimeOffset expiraEm)
    {
        var handler = new JwtSecurityTokenHandler();
        handler.OutboundClaimTypeMap.Clear();
        var claims = new Dictionary<string, object>();
        if (login is not null)
            claims["login"] = login;
        if (email is not null)
            claims["email"] = email;

        var descritor = new SecurityTokenDescriptor
        {
            Claims = claims,
            NotBefore = emitidoEm.UtcDateTime,
            IssuedAt = emitidoEm.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
                SecurityAlgorithms.HmacSha256)
        };

        return handler.WriteToken(handler.CreateToken(descritor));
    }
}
