using PlanningPoker.Controller.Extensions;
using PlanningPoker.Repository.Extensions;
using PlanningPoker.Service.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRepositoryLayer();
builder.Services.AddServiceLayer();
builder.Services.AddControllerLayer();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

public partial class Program;
