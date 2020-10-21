using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Polly;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HttpClientItemsWorker
{
    public class Program
    {
        private static string HOST_ENVIRONMENT =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{HOST_ENVIRONMENT}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("Environment", HOST_ENVIRONMENT)
                .ReadFrom.Configuration(Configuration, "Serilog")
                .CreateLogger();

            try
            {
                Log.ForContext<Program>().Information("Starting host");
                Log.ForContext<Program>().Information($"Environment {HOST_ENVIRONMENT}");
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.Information("Host stopped");
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient("ItemsClient", c =>
                        {
                            var apiBase = Configuration.GetValue<string>("ItemsApiBase");
                            c.BaseAddress = new Uri(apiBase);
                            c.DefaultRequestHeaders.Add("Accept", "application/json");
                            c.DefaultRequestHeaders.Add("User-Agent", "ItemsClient-Worker");
                        })
                        .AddTransientHttpErrorPolicy(policy =>
                            policy
                                .OrResult(r => r.StatusCode == (HttpStatusCode) 429)
                                .WaitAndRetryForeverAsync((retry, resp, ctx) =>
                                    {
                                        var retryAfter = resp?.Result?.Headers.GetValues("Retry-After")
                                            ?.FirstOrDefault();

                                        return retryAfter == null
                                            ? TimeSpan.FromSeconds(retry * 2)
                                            : TimeSpan.FromSeconds(int.Parse(retryAfter) * 2);
                                    },
                                    (msg, count, ts, ctx) =>
                                    {
                                        Log.ForContext<Worker>().Warning("Retrying request attempt: {attempt}", count);
                                        return Task.CompletedTask;
                                    }));
                    services.AddHostedService<Worker>();
                });
    }
}