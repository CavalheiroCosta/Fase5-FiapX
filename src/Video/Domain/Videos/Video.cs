namespace Video.Domain.Videos;

public sealed class Video
{
    private Video()
    {
        Login = "";
        Email = "";
        Status = "";
        CaminhoOriginal = "";
    }

    public Guid Id { get; private set; }

    public string Login { get; private set; }

    public string Email { get; private set; }

    public string Status { get; private set; }

    public string CaminhoOriginal { get; private set; }

    public string? CaminhoZip { get; private set; }

    public EfeitoStatus Aplicar(string? momento, string? caminho)
    {
        if (Status is StatusVideo.Concluido or StatusVideo.Erro)
            return EfeitoStatus.Ignorado;

        if (momento == MomentoStatus.Comecou)
        {
            if (Status == StatusVideo.EmProcessamento)
                return EfeitoStatus.Ignorado;

            Status = StatusVideo.EmProcessamento;
            return EfeitoStatus.Alterado;
        }

        if (momento == MomentoStatus.Sucesso)
        {
            if (string.IsNullOrWhiteSpace(caminho))
                return EfeitoStatus.Ignorado;

            Status = StatusVideo.Concluido;
            CaminhoZip = caminho.Trim();
            return EfeitoStatus.Alterado;
        }

        if (momento == MomentoStatus.Erro)
        {
            Status = StatusVideo.Erro;
            return EfeitoStatus.Alterado;
        }

        return EfeitoStatus.Ignorado;
    }

    public static Resultado<Video> Registrar(Guid id, string? login, string? email, string? caminho)
    {
        if (id == Guid.Empty)
            return Resultado<Video>.Erro(CodigosFalha.Validacao, "O identificador do vídeo é obrigatório.");

        if (string.IsNullOrWhiteSpace(login))
            return Resultado<Video>.Erro(CodigosFalha.Validacao, "Login é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            return Resultado<Video>.Erro(CodigosFalha.Validacao, "E-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(caminho))
            return Resultado<Video>.Erro(CodigosFalha.Validacao, "O caminho do arquivo é obrigatório.");

        return Resultado<Video>.Ok(new Video
        {
            Id = id,
            Login = login.Trim(),
            Email = email.Trim(),
            Status = StatusVideo.AguardandoProcessamento,
            CaminhoOriginal = caminho.Trim()
        });
    }
}
