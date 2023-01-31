using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlanningPokerUi.Middleware;
using PlanningPokerUi.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlanningPokerUi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                    .AddScoped<WebSocketMiddleware>()

                    .AddSingleton<WebSocketHandlerService, RoomsMessageService>()
                    .AddSingleton<RoomsMessageService>(s => s.GetRequiredService<WebSocketHandlerService>() as RoomsMessageService)
                    .AddSingleton<PeopleManagerService>()
                    .AddSingleton<WebSocketManagerService>()
                    .AddSingleton<RoomsManagerService>();
            ReadCsvInto("AgeAdjectives", RoomNameGenerator.AgeAdjectives);
            ReadCsvInto("ColourAdjectives", RoomNameGenerator.ColourAdjectives);
            ReadCsvInto("MaterialAdjectives", RoomNameGenerator.MaterialAdjectives);
            ReadCsvInto("OpinionAdjectives", RoomNameGenerator.OpinionAdjectives);
            ReadCsvInto("OriginAdjectives", RoomNameGenerator.OriginAdjectives);
            ReadCsvInto("SizeAdjectives", RoomNameGenerator.SizeAdjectives);
            ReadCsvInto("ShapeAdjectives", RoomNameGenerator.ShapeAdjectives);
            ReadCsvInto("Nouns", RoomNameGenerator.Nouns);

            services.AddRazorPages();
            services.AddSession();
        }

        private static void ReadCsvInto(string name, List<string> list)
        {
            using (var reader = new StreamReader(@$".\Services\Csvs\{name}.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    foreach (var val in values)
                    {
                        var toAdd = val.ToLower().Trim();
                        if (!string.IsNullOrWhiteSpace(toAdd))
                        {
                            list.Add(toAdd);
                        }
                    }
                }
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseSession();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
            };
            app.UseWebSockets(webSocketOptions);
            app.Map("/ws", _app => _app.UseMiddleware<WebSocketMiddleware>());

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute("Room", "Room/{guid}", new { controller = "Room", action = "Room" });
                endpoints.MapControllers();
            });
        }
    }
}