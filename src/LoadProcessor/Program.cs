using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LoadProcessor
{
    public class Program
    {
        private static string HOST_ENVIRONMENT = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
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
                    services.AddSingleton<ServiceBusClient>(provider =>
                    {
                        var connectionStr = Configuration.GetValue<string>("AzServiceBusConnectionString");
                        return new ServiceBusClient(connectionStr);
                    });
                    
                    services.AddHostedService<SbQueueProcessorWorker>();
                });
    }
}