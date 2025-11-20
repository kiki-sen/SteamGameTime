using Microsoft.AspNetCore.HttpOverrides;
using Steam_API.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

builder.Services
    .AddSpaCors(builder.Configuration)
    .AddApiControllers()
    .AddAppServices()
    .AddAuthenticationAndSteam(builder.Configuration)
    .AddSwaggerDocs();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors("spa");
app.UseGlobalExceptionHandling();
app.UseSwaggerWithUi();
app.UseAuthentication();

if (app.Environment.IsDevelopment())
{
    app.UseRequestDebugLogging();
}

app.UseAuthorization();

app.MapControllers();
app.Run();