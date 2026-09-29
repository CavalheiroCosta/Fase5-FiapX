using System.Diagnostics.Metrics;

namespace Video.Application;

public sealed class MetricasVideo : IDisposable
{
    public const string NomeMedidor = "FiapX.Video";
    public const string StatusAplicado = "fiapx_video_status";
    public const string Listagem = "fiapx_video_listagem";
    public const string OrigemRedis = "redis";
    public const string OrigemPostgres = "postgres";

    private readonly Meter _medidor;
    private readonly Counter<long> _status;
    private readonly Counter<long> _listagem;

    public MetricasVideo()
        : this(NomeMedidor)
    {
    }

    public MetricasVideo(string nomeMedidor)
    {
        _medidor = new Meter(nomeMedidor);
        _status = _medidor.CreateCounter<long>(StatusAplicado);
        _listagem = _medidor.CreateCounter<long>(Listagem);
    }

    public void Registrar(string momento) =>
        _status.Add(1, new KeyValuePair<string, object?>("momento", momento));

    public void RegistrarListagem(string origem) =>
        _listagem.Add(1, new KeyValuePair<string, object?>("origem", origem));

    public void Dispose() => _medidor.Dispose();
}
