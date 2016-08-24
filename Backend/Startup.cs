using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Backend
{
    public class Startup
    {
        private const string HealthPath = "/health";

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            RegisterService(app, loggerFactory); //Fire and forget

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //Specify content type, because why not!
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add(
                    "Content-Type",
                    "text/plain");
                await next();
            });

            //Routing for Consul checks
            app.Map(HealthPath, app2 =>
            {
                var logger = loggerFactory.CreateLogger("Health");
                app2.Run(async (context) =>
                {
                    logger.LogInformation("Health check performed");
                    await context.Response.WriteAsync("I'm alive");
                });
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Fred");
            });
        }

        public async void RegisterService(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Consul");
            try
            {
                logger.LogInformation("Registering service with Consul");
                logger.LogInformation($"MachineName is {Environment.MachineName}");

                var addresses = app.ServerFeatures.Get<IServerAddressesFeature>();
                var serviceUri = new Uri(addresses.Addresses.Single());
                logger.LogInformation($"Service URI is {serviceUri.ToString()}");

                var checkAddress = $"{serviceUri.Scheme}://localhost:{serviceUri.Port.ToString()}{HealthPath}";

                using (var c = new Consul.ConsulClient())
                {
                    var writeResult = await c.Agent.ServiceRegister(
                        new Consul.AgentServiceRegistration()
                        {
                            Name = "backend",
                            Port = serviceUri.Port,
                            Address = Environment.MachineName,
                            Check = new Consul.AgentServiceCheck
                            {
                                HTTP = checkAddress,
                                Interval = TimeSpan.FromSeconds(2.0)
                            }
                        });

                    logger.LogInformation($"Completed registration with Consul with response code {writeResult.StatusCode.ToString()}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error registering service with Consul: {ex.ToString()}");
            }

        }
    }
}
