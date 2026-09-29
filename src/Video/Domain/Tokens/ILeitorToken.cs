namespace Video.Domain.Tokens;

public interface ILeitorToken
{
    IdentidadeAutenticada? Ler(string? token);
}
