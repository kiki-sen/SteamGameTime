using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Steam_API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationAndSteam(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection("Jwt");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!));

        services.AddSingleton(signingKey);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme; // for external providers (Steam)
            })
            .AddJwtBearer(options =>
            {
                options.IncludeErrorDetails = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = signingKey
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        var pd = new ProblemDetails
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Title = "Unauthorized",
                            Detail = "A valid Bearer token is required.",
                            Type = "about:blank",
                        };

                        pd.Extensions["code"] = "auth.unauthorized";
                        pd.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                        pd.Extensions["path"] = context.Request.Path.Value ?? string.Empty;
                        pd.Extensions["method"] = context.Request.Method;

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(pd);
                    },
                    OnForbidden = async context =>
                    {
                        var pd = new ProblemDetails
                        {
                            Status = StatusCodes.Status403Forbidden,
                            Title = "Forbidden",
                            Detail = "You do not have access to this resource.",
                            Type = "about:blank",
                        };

                        pd.Extensions["code"] = "auth.forbidden";
                        pd.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                        pd.Extensions["path"] = context.Request.Path.Value ?? string.Empty;
                        pd.Extensions["method"] = context.Request.Method;

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(pd);
                    },
                    OnMessageReceived = ctx =>
                    {
                        var auth = ctx.Request.Headers.Authorization.ToString();
                        var raw = string.Empty;

                        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            raw = auth[7..].Trim();
                            // normalize any accidental quoting/encoding
                            raw = raw.Trim('"');
                            raw = System.Net.WebUtility.UrlDecode(raw);
                            ctx.Token = raw;
                        }

                        Console.WriteLine(
                            $"[OnMessageReceived] scheme={ctx.Scheme?.Name} path={ctx.Request.Path} " +
                            $"bearer={auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)} " +
                            $"tokenLen={raw?.Length ?? 0} dots={(raw ?? string.Empty).Count(c => c == '.')}");

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine(
                        $"[OnAuthenticationFailed] scheme={ctx.Scheme?.Name} path={ctx.Request.Path} ex={ctx.Exception}");
                        return Task.CompletedTask;
                    }
                };
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "SteamGameTimeAuth";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.HttpOnly = true;
                options.Cookie.Path = "/";
            })
            .AddSteam(o =>
            {
                var apiKey = config["Steam:ApiKey"];
                Console.WriteLine($"[Steam Config] ApiKey loaded: {(string.IsNullOrEmpty(apiKey) ? "EMPTY/NULL" : $"{apiKey.Substring(0, Math.Min(8, apiKey.Length))}...")}");
                o.ApplicationKey = apiKey;
                o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.CallbackPath = "/signin-steam"; // explicit default callback
            });

        services.AddAuthorization();
        return services;
    }
}