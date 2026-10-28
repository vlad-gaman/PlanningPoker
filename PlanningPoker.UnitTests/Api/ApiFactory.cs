using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PlanningPoker.UnitTests.Api;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), "pp-webroot-" + Guid.NewGuid());

    public ApiFactory()
    {
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), "<html><body>spa-index</body></html>");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.WebRootKey, _webRoot);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, true);
        }
    }
}
