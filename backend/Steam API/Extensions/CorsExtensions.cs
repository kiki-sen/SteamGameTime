namespace Steam_API.Extensions;

public static class CorsExtensions
{
    private const string PolicyName = "spa";


    public static IServiceCollection AddSpaCors(this IServiceCollection services, IConfiguration config)
    {
        services.AddCors(o => o.AddPolicy(PolicyName, p =>
        {
            var corsOrigins = config.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"];

            p.WithOrigins(corsOrigins)
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }));

        return services;
    }
}