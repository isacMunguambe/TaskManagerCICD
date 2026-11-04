using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace TaskManagerCICD.Controllers;

[ApiController]
[Route("api/version")]
public class VersionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var version = typeof(VersionController).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        return Ok(new { version });
    }
}
