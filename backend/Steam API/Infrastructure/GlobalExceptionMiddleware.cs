using System.Text.Json;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Steam_API.Infrastructure;

public sealed class GlobalExceptionMiddleware : IMiddleware
{
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(IWebHostEnvironment env) => _env = env;

    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            var problem = MapToProblemDetails(ctx, ex, _env);
            await WriteProblem(ctx, problem, _env);
        }
    }

    private static async Task WriteProblem(HttpContext ctx, ProblemDetails problem, IWebHostEnvironment env)
    {
        problem.Extensions["traceId"] = ctx.TraceIdentifier;
        problem.Extensions["path"] = ctx.Request.Path.Value ?? "";
        problem.Extensions["method"] = ctx.Request.Method;

        // Route data: controller/action if using controllers
        var rd = ctx.GetRouteData();
        if (rd.Values.TryGetValue("controller", out var ctrl)) problem.Extensions["controller"] = ctrl;
        if (rd.Values.TryGetValue("action", out var act)) problem.Extensions["action"] = act;

        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = env.IsDevelopment()
        });

        await ctx.Response.WriteAsync(json);
    }

    private static ProblemDetails MapToProblemDetails(HttpContext ctx, Exception ex, IWebHostEnvironment env)
    {
        // default
        var pd = New(500, "An unexpected error occurred.", env, ex);
        SetCode(pd, "error.unexpected");

        // ---- Steam / Flurl
        if (ex is FlurlHttpTimeoutException or TaskCanceledException)
        {
            pd = New(504, "Upstream timeout", env, ex,
                detail: "Steam did not respond in time.");
            SetCode(pd, "steam.timeout");
            return pd;
        }

        if (ex is FlurlHttpException fhex)
        {
            var status = (int?)fhex.Call?.Response?.StatusCode ?? 502;
            string? upstreamBody = null;
            try { upstreamBody = fhex.GetResponseStringAsync().GetAwaiter().GetResult(); } catch { }

            (string title, string detail, string code) = status switch
            {
                400 => ("Bad request to Steam", "The request to Steam was invalid (check SteamID or parameters).", "steam.bad_request"),
                401 or 403 => ("Steam data not accessible", "Your Steam profile’s Game details may be private.", "steam.forbidden"),
                404 => ("Not found at Steam", "The requested Steam resource was not found.", "steam.not_found"),
                429 => ("Rate limited by Steam", "Too many requests to Steam API. Please retry later.", "steam.rate_limited"),
                >= 500 and < 600 => ("Steam service error", "Steam returned an error.", "steam.server_error"),
                _ => ("Upstream error from Steam", "Steam returned a non-success status code.", "steam.upstream_error")
            };

            pd = New(status, title, env, fhex, detail);
            SetCode(pd, code);

            if (fhex.Call is not null)
            {
                pd.Extensions["upstreamUrl"] = fhex.Call.Request.Url.ToString();
                pd.Extensions["upstreamStatus"] = status;
            }

            if (env.IsDevelopment() && !string.IsNullOrWhiteSpace(upstreamBody))
                pd.Extensions["upstreamBody"] = Truncate(upstreamBody, 800);

            return pd;
        }

        // ---- JWT / security (when thrown)
        if (ex is SecurityTokenException)
        {
            pd = New(401, "Invalid token", env, ex, "Your access token is invalid.");
            SetCode(pd, "auth.invalid_token");
            return pd;
        }

        // ---- Typical app semantics
        if (ex is KeyNotFoundException)
        {
            pd = New(404, "Not found", env, ex, "The requested resource was not found.");
            SetCode(pd, "error.not_found");
            return pd;
        }

        if (ex is InvalidOperationException)
        {
            pd = New(409, "Invalid operation", env, ex, "The operation could not be completed.");
            SetCode(pd, "error.conflict");
            return pd;
        }

        return pd;
    }

    private static ProblemDetails New(int status, string title, IWebHostEnvironment env, Exception ex, string? detail = null)
    {
        var pd = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = "about:blank",
            Detail = detail ?? (env.IsDevelopment() ? ex.Message : "Please contact support if the problem persists.")
        };

        if (env.IsDevelopment())
        {
            pd.Extensions["exception"] = ex.GetType().FullName;
            pd.Extensions["stackTrace"] = ex.StackTrace;
        }
        return pd;
    }

    private static void SetCode(ProblemDetails pd, string code) => pd.Extensions["code"] = code;

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max] + "…");
}
