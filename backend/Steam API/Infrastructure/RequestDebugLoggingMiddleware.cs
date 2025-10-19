namespace Steam_API.Infrastructure;

public class RequestDebugLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestDebugLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext ctx)
    {
        var hasBearer = ctx
            .Request
            .Headers["Authorization"]
            .ToString()
            .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        var userAuth = ctx.User?.Identity?.AuthenticationType ?? "(none)";
        var cookies = string.Join(", ", ctx.Request.Cookies.Select(c => $"{c.Key}={c.Value[..Math.Min(10, c.Value.Length)]}"));
        Console.WriteLine($"[{ctx.Request.Path}] AuthHeaderBearer={hasBearer}, UserAuthType={userAuth}, IsAuth={ctx.User?.Identity?.IsAuthenticated}, Cookies={cookies}");

        var auth = ctx.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(auth))
        {
            Console.WriteLine($"Authorization header (first 60): {auth[..Math.Min(60, auth.Length)]}");
            var parts = auth.Split(' ', 2);
            var bearer = parts.Length == 2 ? parts[1] : string.Empty;
            Console.WriteLine($"Bearer parts: {bearer.Split('.').Length - 1} dots");
        }

        await _next(ctx);
    }
}