using System.Diagnostics.Metrics;

namespace Processor.Application;

public sealed class MetricasProcessor : IDisposable
{
    public const string NomeMedidor = "FiapX.Processor";
    public const string Resultados = "fiapx_processor_resultados";
    public const string EmAndamento = "fiapx_processor_em_andamento";

    private readonly Meter _medidor;
    private readonly Counter<long> _resultados;
    private int _emAndamento;

    public MetricasProcessor()
        : this(NomeMedidor)
    {
    }

    public MetricasProcessor(string nomeMedidor)
    {
        _medidor = new Meter(nomeMedidor);
        _resultados = _medidor.CreateCounter<long>(Resultados);
        _medidor.CreateObservableGauge(EmAndamento, () => Volatile.Read(ref _emAndamento));
    }

    public void Registrar(string momento) =>
        _resultados.Add(1, new KeyValuePair<string, object?>("momento", momento));

    public void Entrar() => Interlocked.Increment(ref _emAndamento);

    public void Sair() => Interlocked.Decrement(ref _emAndamento);

    public void Dispose() => _medidor.Dispose();
}
