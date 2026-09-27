namespace Auth.Application.Senhas;

public interface ISenhaHasher
{
    string GerarHash(string senha);

    bool Conferir(string senha, string senhaHash);
}
