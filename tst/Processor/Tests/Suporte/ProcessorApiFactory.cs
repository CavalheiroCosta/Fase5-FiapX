using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Processor.Tests.Suporte;

public class ProcessorApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting vira argumento do host. O Program lê Processor:Provider desse argumento.
        builder.UseSetting("Processor:Provider", "Memory");
    }
}
