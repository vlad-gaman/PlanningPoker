using PlanningPoker.Controller.Extensions;
using PlanningPoker.Repository.Extensions;
using PlanningPoker.Service.Extensions;
using PlanningPoker.Service.Services;

if (args.Contains(PasswordHashCommand.Switch))
{
    return PasswordHashCommand.Run(new PasswordHashingService(), Console.In, Console.Out, Console.Error);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRepositoryLayer();
builder.Services.AddServiceLayer();
builder.Services.AddControllerLayer();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.Services.RestoreRooms();

app.UseControllerLayer();
app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.MapControllerLayer();

if (app.Environment.WebRootFileProvider.GetFileInfo("index.html").Exists)
{
    app.MapFallbackToFile("index.html");
}

app.Run();
return 0;

public partial class Program;
