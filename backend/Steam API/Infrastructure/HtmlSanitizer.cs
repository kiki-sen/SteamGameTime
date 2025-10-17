using Ganss.Xss;
using AngleSharp.Html.Dom;

namespace Steam_API.Infrastructure
{
    namespace YourAppNamespace.Extensions
    {
        public static class ServiceCollectionExtensions
        {
            /// <summary>
            /// Registers a preconfigured HtmlSanitizer singleton for safe HTML output.
            /// </summary>
            public static IServiceCollection AddHtmlSanitizer(this IServiceCollection services)
            {
                services.AddSingleton(sp =>
                {
                    var s = new HtmlSanitizer();

                    // Allowed tags
                    s.AllowedTags.Clear();
                    s.AllowedTags.UnionWith(
                    [
                        "p","br","ul","ol","li","strong","em","b","i","u","blockquote",
                        "h1","h2","h3","h4","h5","h6","code","pre","hr","span","div",
                        "a","img"
                    ]);

                    // Allowed attributes
                    s.AllowedAttributes.Clear();
                    s.AllowedAttributes.UnionWith(["href", "title", "alt", "src", "rel", "target"]);

                    // Allowed schemes
                    s.AllowedSchemes.Clear();
                    s.AllowedSchemes.UnionWith(["https"]);

                    // No inline CSS
                    s.AllowedCssProperties.Clear();
                    s.AllowedClasses.Clear();

                    // Post-process links and images
                    s.PostProcessNode += (o, e) =>
                    {
                        if (e.Node is IHtmlAnchorElement a)
                        {
                            a.SetAttribute("rel", "noopener noreferrer");
                            a.SetAttribute("target", "_blank");
                            if (a.Href?.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                a.Remove();
                            }
                        }
                        else if (e.Node is IHtmlImageElement img)
                        {
                            if (string.IsNullOrWhiteSpace(img.Source) ||
                                !Uri.TryCreate(img.Source, UriKind.Absolute, out var uri) ||
                                uri.Scheme != Uri.UriSchemeHttps)
                            {
                                img.Remove();
                                return;
                            }

                            var allowedHosts = new[]
                            {
                                "cdn.akamai.steamstatic.com",
                                "cdn.cloudflare.steamstatic.com",
                                "steamcdn-a.akamaihd.net"
                            };
                            if (!allowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
                            {
                                img.Remove();
                            }
                        }
                    };

                    s.KeepChildNodes = false;
                    return s;
                });

                return services;
            }
        }
    }
}
