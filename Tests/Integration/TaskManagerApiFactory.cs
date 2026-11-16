using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManagerCICD.Services;

namespace Tests.Integration;

// Levanta a API inteira em memória (sem IIS nem SQL Server) para a testar por HTTP
public class TaskManagerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // O Program.cs exige uma connection string ao arrancar; esta nunca chega a ser usada
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=not-used;Database=not-used");

        builder.ConfigureTestServices(services =>
        {
            // Troca o serviço de SQL Server pela versão em memória
            services.RemoveAll<ITaskService>();
            services.AddSingleton<ITaskService, InMemoryTaskService>();
        });
    }
}
