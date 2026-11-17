using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace PlanningPoker.UnitTests.Fakes;

public sealed class FakeHostEnvironment(string environmentName = "Production") : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "Test";

    public string ContentRootPath { get; set; } = "";

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
