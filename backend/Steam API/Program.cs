using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Steam_API.Infrastructure.YourAppNamespace.Extensions;
using System.Reflection;
using System.Text;
using Steam_API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddPolicy("spa", p =>
{
    var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() 
        ?? new[] { "http://localhost:4200" };
    
    p.WithOrigins(corsOrigins)
     .AllowCredentials()
     .AllowAnyHeader()
     .AllowAnyMethod();
}));

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!));

builder.Services.AddAuthentication(options =>
    {
        // APIs use JWT by default
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

        // external providers (Steam) will sign into this cookie
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
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
                pd.Extensions["path"] = context.Request.Path.Value ?? "";
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
                pd.Extensions["path"] = context.Request.Path.Value ?? "";
                pd.Extensions["method"] = context.Request.Method;

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(pd);
            },
            OnMessageReceived = ctx =>
            {
                var auth = ctx.Request.Headers.Authorization.ToString();
                var raw = "";

                if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    raw = auth.Substring(7).Trim();
                    // normalize any accidental quoting/encoding
                    raw = raw.Trim('"');
                    raw = System.Net.WebUtility.UrlDecode(raw);
                    ctx.Token = raw;
                }

                Console.WriteLine(
                    $"[OnMessageReceived] scheme={ctx.Scheme?.Name} path={ctx.Request.Path} " +
                    $"bearer={auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)} " +
                    $"tokenLen={raw?.Length ?? 0} dots={(raw ?? "").Count(c => c == '.')}");

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
      // Explicitly use default Steam callback path
      o.ApplicationKey = builder.Configuration["Steam:ApiKey"];
      o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
      o.CallbackPath = "/signin-steam";
  });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "about:blank"
            };

            problem.Extensions["code"] = "validation.failed";
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            problem.Extensions["path"] = context.HttpContext.Request.Path.Value ?? "";
            problem.Extensions["method"] = context.HttpContext.Request.Method;

            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<SteamApiClient>();
builder.Services.AddScoped<SteamGameService>();
builder.Services.AddScoped<IFriendsService, FriendsService>();
builder.Services.AddScoped<ISteamProfileService, SteamProfileService>();
builder.Services.AddSingleton<SymmetricSecurityKey>(sp => signingKey);
builder.Services.AddTransient<Steam_API.Infrastructure.GlobalExceptionMiddleware>();

builder.Services.AddHtmlSanitizer();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Steam Game Time API",
        Version = "v1",
        Description = "ASP.NET Core Web API for Steam OpenID login and owned games."
    });

    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
app.UseForwardedHeaders();
app.UseCors("spa");
app.UseMiddleware<Steam_API.Infrastructure.GlobalExceptionMiddleware>();
app.UseSwagger(options => options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0);
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Steam Game Time API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Steam Game Time API";
});
app.UseAuthentication();
app.Use(async (ctx, next) =>
{
    var hasBearer = ctx.Request.Headers["Authorization"].ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    var userAuth = ctx.User?.Identity?.AuthenticationType ?? "(none)";
    var cookies = string.Join(", ", ctx.Request.Cookies.Select(c => $"{c.Key}={c.Value[..Math.Min(10, c.Value.Length)]}"));
    Console.WriteLine($"[{ctx.Request.Path}] AuthHeaderBearer={hasBearer}, UserAuthType={userAuth}, IsAuth={ctx.User?.Identity?.IsAuthenticated}, Cookies={cookies}");
    await next();
});
app.Use(async (ctx, next) =>
{
    var auth = ctx.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrEmpty(auth))
    {
        // Print the first 60 chars to avoid dumping full token
        Console.WriteLine($"Authorization header: {auth.Substring(0, Math.Min(60, auth.Length))}");
        var parts = auth.Split(' ', 2);
        var bearer = parts.Length == 2 ? parts[1] : "";
        Console.WriteLine($"Bearer parts: {(bearer.Split('.').Length - 1)} dots");
    }
    await next();
});
app.UseAuthorization();

app.MapControllers();
app.Run();
