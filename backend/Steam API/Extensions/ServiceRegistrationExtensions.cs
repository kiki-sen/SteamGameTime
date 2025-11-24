using Flurl.Http.Configuration;
using Steam_API.Infrastructure;
using Steam_API.Infrastructure.YourAppNamespace.Extensions;
using Steam_API.Services;

namespace Steam_API.Extensions;

public static class ServiceRegistrationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAppServices()
        {
            services.AddSingleton<IFlurlClientCache>(_ => new FlurlClientCache()
                .Add("steam-store", "https://store.steampowered.com")
                .Add("steam-api", "https://api.steampowered.com"));
            services.AddMemoryCache();

            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
            services.AddScoped<SteamApiClient>();
            services.AddScoped<SteamGameService>();
            services.AddScoped<IFriendsService, FriendsService>();
            services.AddScoped<ISteamProfileService, SteamProfileService>();
            services.AddSingleton<ISteamStoreFrontService, SteamStoreFrontService>();

            services.AddTransient<GlobalExceptionMiddleware>();
            services.AddHtmlSanitizer();

            return services;
        }
    }
}