namespace Auth.Domain.Usuarios;

public sealed record Falha(string Codigo, string Mensagem);

public sealed class Resultado<T>
{
    private Resultado(T valor)
    {
        Valor = valor;
    }

    private Resultado(Falha falha)
    {
        Falha = falha;
    }

    public T? Valor { get; }

    public Falha? Falha { get; }

    public bool Sucesso => Falha is null;

    public static Resultado<T> Ok(T valor) => new(valor);

    public static Resultado<T> Erro(string codigo, string mensagem) => new(new Falha(codigo, mensagem));
}
