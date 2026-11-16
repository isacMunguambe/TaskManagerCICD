using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskManagerCICD.Data;

namespace TaskManagerCICD.HealthChecks;

// Verifica se a API consegue ligar à base de dados; usado pelo endpoint /health
public class DatabaseHealthCheck(TaskManagerDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Não foi possível ligar à base de dados.");
    }
}
