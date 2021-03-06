using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlanningPokerUi.Middleware;
using PlanningPokerUi.Services;
using System;

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
            services.AddTransient<SessionMiddleware>()
                    .AddSingleton<PeopleManagerService>()
                    .AddSingleton<WebSocketManagerService>()
                    .AddSingleton<WebSocketHandlerService, RoomsMessageService>()
                    .AddSingleton<WebSocketMiddleware>()
                    .AddSingleton<RoomsManagerService>();

            services.AddRazorPages();
            services.AddSession();
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
            }

            app.UseSession();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
            };
            app.UseWebSockets(webSocketOptions);
            app.UseMiddleware<SessionMiddleware>();
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
