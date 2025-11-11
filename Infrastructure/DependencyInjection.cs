namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string choice)
    {

        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<ITopologyService, TopologyService>();
        services.AddSingleton<ISnapshotService, SnapshotService>();
        services.AddSingleton<IRipProtocolService, RipProtocolService>();
        services.AddSingleton<IFaultInjectionService, FaultInjectionService>();

        // Register background service
        //services.AddHostedService<SimulationService>();
        switch (choice)
        {
            case "A":
                services.AddHostedService<ScenarioABackgroundService>();
                break;
            case "B":
                services.AddHostedService<ScenarioBBackgroundService>();
                break;
            case "C":
                services.AddHostedService<ScenarioCBackgroundService>();
                break;
            case "D":
                services.AddHostedService<ScenarioDBackgroundService>();
                break;
            case "E":
                services.AddHostedService<ScenarioEBackgroundService>();
                break;

        }

        return services;
    }
}
