using Microsoft.AspNetCore.Mvc;

namespace Steam_API.Extensions;

public static class MvcExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiControllers()
        {
            services.AddControllers()
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
                    problem.Extensions["path"] = context.HttpContext.Request.Path.Value ?? string.Empty;
                    problem.Extensions["method"] = context.HttpContext.Request.Method;

                    return new BadRequestObjectResult(problem)
                    {
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });

            services.AddEndpointsApiExplorer();
            return services;
        }
    }
}