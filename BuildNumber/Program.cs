using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;

IHost host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddTransient<BuildNumberService>();
        services.AddSingleton(_ => new DatabaseConnectionInfo(Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING")!));
    })
    .Build();

host.Run();
