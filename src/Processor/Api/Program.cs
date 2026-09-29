using OpenTelemetry.Metrics;
using Processor.Application;
using Processor.Infra;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenTelemetry()
    .WithMetrics(metricas => metricas
        .AddAspNetCoreInstrumentation()
        .AddMeter(MetricasProcessor.NomeMedidor)
        .AddPrometheusExporter());

var app = builder.Build();

_ = app.Services.GetRequiredService<MetricasProcessor>();
app.MapPrometheusScrapingEndpoint();

await app.RunAsync();

public partial class Program;
