using System.Reflection;
using Microsoft.OpenApi.Models;

namespace Steam_API.Extensions;

public static class SwaggerExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSwaggerDocs()
        {
            services.AddSwaggerGen(c =>
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

            return services;
        }
    }

    extension(IApplicationBuilder application)
    {
        public IApplicationBuilder UseSwaggerWithUi()
        {
            application.UseSwagger(options => options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0);
            application.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Steam Game Time API v1");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "Steam Game Time API";
            });

            return application;
        }
    }
}