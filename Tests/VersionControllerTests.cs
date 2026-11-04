using Microsoft.AspNetCore.Mvc;
using TaskManagerCICD.Controllers;

namespace Tests;

public class VersionControllerTests
{
    [Fact]
    public void Get_ReturnsOkWithVersion()
    {
        var controller = new VersionController();

        var result = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
