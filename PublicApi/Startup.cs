using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Consul;

namespace PublicApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var logger = loggerFactory.CreateLogger("RequestHandling");

            app.Run(context => HandleRequest(context, logger));
        }

        private async Task HandleRequest(HttpContext context, ILogger logger)
        {
            logger.LogInformation("Handling request");

            var serviceResponse = await GetNameFromBackend(logger);

            await context.Response.WriteAsync($"Hello {serviceResponse ?? "whoever you are"}!");

            logger.LogInformation("Request handling complete");
        }

        private async Task<string> GetNameFromBackend(ILogger logger)
        {
            string serviceName = "backend";
            string consulHost = Environment.GetEnvironmentVariable("CONSUL_HOST") ?? "localhost";
            logger.LogInformation($"Consul host is {consulHost}");

            Uri backendServiceUri = null;

            try
            {
                using (var c = new ConsulClient(cnfg => cnfg.Address = new Uri($"http://{consulHost}:8500")))
                {
                    if (!await IsServiceHealthy(serviceName, c, logger)) return null;

                    backendServiceUri = await GetServiceUri(serviceName, c, logger);  
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to get host and port from Consul: {ex.ToString()}");
                return null;
            }

            if (backendServiceUri == null) return null;

            return await CallBackend(backendServiceUri, logger);
        }

        private async Task<bool> IsServiceHealthy(string serviceName, ConsulClient client, ILogger logger)
        {
            var healthResult = await client.Health.Service(serviceName);

            return ProcessQueryResult(
                healthResult,
                logger,
                serviceEntry =>
                {
                    if (serviceEntry.Checks.Any(chk => string.Compare(chk.Status, "critical", true) == 0))
                    {
                        logger.LogError("Backend service in critical state");
                        return false;
                    }

                    return true;
                });
        }

        private async Task<Uri> GetServiceUri(string serviceName, ConsulClient client, ILogger logger)
        {
            var catalogResult = await client.Catalog.Service(serviceName);

            return ProcessQueryResult(
                catalogResult,
                logger,
                agentService =>
                {
                    var backendPort = agentService.ServicePort.ToString();
                    var backendHost = agentService.ServiceAddress;
                    logger.LogInformation($"Found host {backendHost} and port {backendPort} for backend");

                    if (backendHost == Environment.MachineName) backendHost = "localhost";

                    return new Uri($"http://{backendHost}:{backendPort}"); ;
                });
        }

        private TRes ProcessQueryResult<T, TRes>(
            QueryResult<T[]> consulQueryResult,
            ILogger logger,
            Func<T, TRes> processResult)
        {
            if (consulQueryResult.StatusCode == HttpStatusCode.OK)
            {
                var coreResult = consulQueryResult.Response.FirstOrDefault();
                if (coreResult != null)
                {
                    return processResult(coreResult);
                }
                else
                {
                    logger.LogError("Service was not found");
                    return default(TRes);
                }
            }
            else
            {
                logger.LogError($"Failed to get OK response from Consul: {consulQueryResult.StatusCode.ToString()}");
                return default(TRes);
            }
        }

        private async Task<string> CallBackend(Uri serviceUri, ILogger logger)
        {
            using (var c = new System.Net.Http.HttpClient())
            {
                c.BaseAddress = serviceUri;

                logger.LogInformation($"Calling backend at {c.BaseAddress}");

                try
                {
                    var serviceResponse = await c.GetStringAsync(string.Empty);

                    logger.LogInformation($"Backend call completed with '{serviceResponse}'");

                    return serviceResponse;
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to call backend: {ex.ToString()}");
                    return null;
                }
            }
        }
    }
}
