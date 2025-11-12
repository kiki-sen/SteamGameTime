using Steam_API.Infrastructure;
using Steam_API.Infrastructure.YourAppNamespace.Extensions;
using Steam_API.Services;

namespace Steam_API.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddMemoryCache();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
        services.AddScoped<SteamApiClient>();
        services.AddScoped<SteamGameService>();
        services.AddScoped<IFriendsService, FriendsService>();
        services.AddScoped<ISteamProfileService, SteamProfileService>();

        services.AddTransient<GlobalExceptionMiddleware>();
        services.AddHtmlSanitizer();

        return services;
    }
}