namespace Video.Domain.Videos;

public static class AvisoErro
{
    public const string Assunto = "FIAP X: o vídeo não foi processado";

    public const string RemetentePadrao = "fiapx@fiapx.local";

    public static string Corpo(Guid id) => $"O vídeo {id:D} não foi processado.";
}
