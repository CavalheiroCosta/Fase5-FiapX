using Auth.Application.Senhas;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infra.Senhas;

public sealed class SenhaHasher : ISenhaHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string GerarHash(string senha) => _hasher.HashPassword(new object(), senha);

    public bool Conferir(string senha, string senhaHash) =>
        _hasher.VerifyHashedPassword(new object(), senhaHash, senha) != PasswordVerificationResult.Failed;
}
