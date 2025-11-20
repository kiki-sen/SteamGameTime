namespace Steam_API.Extensions;

public static class CorsExtensions
{
    private const string PolicyName = "spa";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddSpaCors(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            services.AddCors(o => o.AddPolicy(PolicyName, p =>
            {
                var corsOrigins =
                    configuration.GetSection("Cors:Origins").Get<string[]>()
                    ?? ["http://localhost:4200"];

                p.WithOrigins(corsOrigins)
                 .AllowCredentials()
                 .AllowAnyHeader()
                 .AllowAnyMethod();
            }));

            return services;
        }
    }
}
