using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Processor.Application;
using Processor.Domain.Processamento;
using Processor.Infra.Ffmpeg;
using Processor.Infra.Filas;
using Processor.Infra.Redis;
using Processor.Infra.Storage;

namespace Processor.Infra;

public static class DependencyInjection
{
    public const string ProvedorMemoria = "Memory";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provedor = configuration["Processor:Provider"];
        if (string.Equals(provedor, ProvedorMemoria, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<MarcaMemoria>();
            services.AddSingleton<IMarcaVideo>(servico => servico.GetRequiredService<MarcaMemoria>());
            services.AddSingleton(_ => new ArmazenamentoProcessamentoMemoria(CaminhoZip.BucketPadrao));
            services.AddSingleton<IArmazenamentoProcessamento>(servico => servico.GetRequiredService<ArmazenamentoProcessamentoMemoria>());
            services.AddSingleton<FilaStatusMemoria>();
            services.AddSingleton<IFilaStatus>(servico => servico.GetRequiredService<FilaStatusMemoria>());
            services.AddSingleton<IQuebraVideo, QuebraVideoFixa>();
            return services;
        }

        var serviceUrl = configuration["Storage:ServiceUrl"]
            ?? throw new InvalidOperationException("Storage:ServiceUrl não está configurada.");
        var accessKey = configuration["Storage:AccessKey"]
            ?? throw new InvalidOperationException("Storage:AccessKey não está configurada.");
        var secretKey = configuration["Storage:SecretKey"]
            ?? throw new InvalidOperationException("Storage:SecretKey não está configurada.");
        var bucket = configuration["Storage:Bucket"];
        if (string.IsNullOrWhiteSpace(bucket))
            bucket = CaminhoZip.BucketPadrao;

        var uriFila = configuration["Queue:Uri"]
            ?? throw new InvalidOperationException("Queue:Uri não está configurada.");
        var processamento = configuration["Queue:Processamento"];
        if (string.IsNullOrWhiteSpace(processamento))
            processamento = "processamento";
        var status = configuration["Queue:Status"];
        if (string.IsNullOrWhiteSpace(status))
            status = "status";

        var redis = configuration["Redis:Conexao"]
            ?? throw new InvalidOperationException("Redis:Conexao não está configurada.");

        services.AddSingleton<IClienteObjeto>(_ => new ClienteObjetoS3(serviceUrl, accessKey, secretKey));
        services.AddSingleton(servico => new ArmazenamentoProcessamentoS3(servico.GetRequiredService<IClienteObjeto>(), bucket));
        services.AddSingleton<IArmazenamentoProcessamento>(servico => servico.GetRequiredService<ArmazenamentoProcessamentoS3>());
        services.AddHostedService(servico => new PrepararProcessorHostedService(servico.GetRequiredService<IClienteObjeto>(), bucket));

        services.AddSingleton<IPublicadorFila>(_ => new PublicadorFilaRabbit(uriFila));
        services.AddSingleton<IFilaStatus>(servico => new FilaStatus(servico.GetRequiredService<IPublicadorFila>(), status));

        services.AddSingleton(_ => new ComandoMarcaRedis(redis));
        services.AddSingleton<IMarcaVideo>(servico => new MarcaProcessamento(servico.GetRequiredService<ComandoMarcaRedis>()));

        var caminhoFfmpeg = configuration["Ffmpeg:Caminho"];
        if (string.IsNullOrWhiteSpace(caminhoFfmpeg))
            caminhoFfmpeg = ExecutorFfmpeg.CaminhoPadrao;

        services.AddSingleton<IExecutorProcesso>(_ => new ExecutorFfmpeg(caminhoFfmpeg));
        services.AddSingleton<IQuebraVideo, QuebraVideoFfmpeg>();

        services.AddSingleton(new OpcoesFila(uriFila, processamento, status));
        services.AddSingleton<Func<ISessaoConsumo>>(servico =>
        {
            var entrega = servico.GetRequiredService<EntregaMensagem>();
            var opcoes = servico.GetRequiredService<OpcoesFila>();
            return () => new SessaoConsumo(new CanalConsumoRabbit(opcoes), entrega);
        });
        services.AddHostedService<ConsumidorHostedService>();
        return services;
    }
}
