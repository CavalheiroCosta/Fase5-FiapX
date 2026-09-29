using System.Diagnostics.Metrics;

namespace Video.Application;

public sealed class MetricasVideo : IDisposable
{
    public const string NomeMedidor = "FiapX.Video";
    public const string StatusAplicado = "fiapx_video_status";
    public const string Listagem = "fiapx_video_listagem";
    public const string Download = "fiapx_video_download";
    public const string OrigemRedis = "redis";
    public const string OrigemPostgres = "postgres";
    public const string ResultadoEntregue = "entregue";
    public const string ResultadoRecusado = "recusado";

    private readonly Meter _medidor;
    private readonly Counter<long> _status;
    private readonly Counter<long> _listagem;
    private readonly Counter<long> _download;

    public MetricasVideo()
        : this(NomeMedidor)
    {
    }

    public MetricasVideo(string nomeMedidor)
    {
        _medidor = new Meter(nomeMedidor);
        _status = _medidor.CreateCounter<long>(StatusAplicado);
        _listagem = _medidor.CreateCounter<long>(Listagem);
        _download = _medidor.CreateCounter<long>(Download);
    }

    public void Registrar(string momento) =>
        _status.Add(1, new KeyValuePair<string, object?>("momento", momento));

    public void RegistrarListagem(string origem) =>
        _listagem.Add(1, new KeyValuePair<string, object?>("origem", origem));

    public void RegistrarDownload(string resultado) =>
        _download.Add(1, new KeyValuePair<string, object?>("resultado", resultado));

    public void Dispose() => _medidor.Dispose();
}
