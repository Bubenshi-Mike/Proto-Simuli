namespace ProtoSimuli.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddProtoSimuliService(this IServiceCollection services, IConfiguration configuration, string choice)
    {
        //services.AddHostedService<Worker>();
        services.AddInfrastructure(configuration, choice);

        return services;
    }
}
