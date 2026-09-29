using Video.Application.Envio;

namespace Video.Api;

public static class LimiteUpload
{
    public static long Ler(IConfiguration configuration)
    {
        var valor = configuration.GetValue<long?>("Upload:LimiteBytes");
        return valor is null or <= 0 ? EnviarVideoUseCase.LimitePadraoBytes : valor.Value;
    }
}
