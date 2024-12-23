using System;
using System.Threading.Tasks;
using AwakenServer.Silo;
using AwakenServer.Silo.Extensions;
using AwakenServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace AwakenServer;
public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
#if DEBUG
            .WriteTo.Async(c => c.Console())
#endif
            .CreateLogger();
        try
        {
            Log.Information("Starting AwakenServer.Silo.");
            await CreateHostBuilder(args).RunConsoleAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    internal static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostcontext, services) =>
        {
            services.AddApplication<AwakenServerServerOrleansSiloModule>();
        })
        .ConfigureAppConfiguration((h, c) => c.AddJsonFile("apollosettings.json"))
        .UseApollo()
        .UseOrleansSnapshot()
        .UseAutofac()
        .UseSerilog();
    
}