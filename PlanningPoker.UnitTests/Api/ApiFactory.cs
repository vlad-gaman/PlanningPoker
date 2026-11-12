using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using PlanningPoker.Common.Constants;

namespace PlanningPoker.UnitTests.Api;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "pp-content-" + Guid.NewGuid());
    private readonly string _ownStorePath = Path.Combine(Path.GetTempPath(), "pp-store-" + Guid.NewGuid() + ".db");
    private string WebRoot => Path.Combine(_contentRoot, "wwwroot");

    public ApiFactory()
    {
        Directory.CreateDirectory(WebRoot);
        File.WriteAllText(Path.Combine(WebRoot, "index.html"), "<html><body>spa-index</body></html>");
    }

    protected virtual string EnvironmentName => Environments.Production;

    /// <summary>The SQLite file the host stores its rooms in. Every factory gets its own, unless a test shares one to simulate a restart.</summary>
    protected virtual string StorePath => _ownStorePath;

    /// <summary>True when the store file outlives the host, so that another host can boot on it.</summary>
    protected virtual bool KeepStore => false;

    /// <summary>The <c>Https:Redirect</c> setting. The tests talk plain HTTP to a Production host, so they switch the redirect off; null leaves it unset.</summary>
    protected virtual bool? HttpsRedirect => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Outside Development the static web assets manifest is ignored, so a wwwroot left in the API project by a
        // client build cannot shadow the test web root.
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting(WebHostDefaults.WebRootKey, WebRoot);
        builder.UseSetting("Persistence:ConnectionString", $"Data Source={StorePath}");
        if (HttpsRedirect is { } redirect)
        {
            builder.UseSetting($"{HttpsConstants.ConfigurationSection}:Redirect", redirect.ToString());
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, true);
        }

        if (disposing && !KeepStore)
        {
            File.Delete(StorePath);
        }
    }
}
