using Steam_API.Infrastructure;

namespace Steam_API.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();

    public static IApplicationBuilder UseRequestDebugLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestDebugLoggingMiddleware>();
}