using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace PlanningPoker.UnitTests.Api;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "pp-content-" + Guid.NewGuid());
    private string WebRoot => Path.Combine(_contentRoot, "wwwroot");

    public ApiFactory()
    {
        Directory.CreateDirectory(WebRoot);
        File.WriteAllText(Path.Combine(WebRoot, "index.html"), "<html><body>spa-index</body></html>");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Outside Development the static web assets manifest is ignored, so a wwwroot left in the API project by a
        // client build cannot shadow the test web root.
        builder.UseEnvironment(Environments.Production);
        builder.UseSetting(WebHostDefaults.WebRootKey, WebRoot);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, true);
        }
    }
}
