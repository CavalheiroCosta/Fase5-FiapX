using System.Diagnostics.Metrics;

namespace Video.Application;

public sealed class MetricasVideo : IDisposable
{
    public const string NomeMedidor = "FiapX.Video";
    public const string StatusAplicado = "fiapx_video_status";

    private readonly Meter _medidor;
    private readonly Counter<long> _status;

    public MetricasVideo()
        : this(NomeMedidor)
    {
    }

    public MetricasVideo(string nomeMedidor)
    {
        _medidor = new Meter(nomeMedidor);
        _status = _medidor.CreateCounter<long>(StatusAplicado);
    }

    public void Registrar(string momento) =>
        _status.Add(1, new KeyValuePair<string, object?>("momento", momento));

    public void Dispose() => _medidor.Dispose();
}
