namespace Community_Event_Finder.Middleware
{
    /// <summary>
    /// Middleware to redirect unauthenticated users to the login page when accessing protected routes.
    /// </summary>
    public class AuthenticationRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationRedirectMiddleware> _logger;

        private static readonly string[] ProtectedPaths = new[]
        {
            "/api/favorites",
            "/api/user"
        };

        private static readonly string[] PublicPaths = new[]
        {
            "/start.html",
            "/api/events",
            "/api/geo",
            "/Identity",
            "/lib",
            "/css",
            "/js"
        };

        public AuthenticationRedirectMiddleware(RequestDelegate next, ILogger<AuthenticationRedirectMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "/";

            // Check if path is public
            if (IsPublicPath(path))
            {
                await _next(context);
                return;
            }

            // Check if path is protected and user is not authenticated
            if (IsProtectedPath(path) && context.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogInformation("Unauthenticated user accessing protected path: {Path}", path);

                // For API requests, return 401 Unauthorized
                if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { error = "Authentication required" });
                    return;
                }

                // For web requests, redirect to login
                context.Response.Redirect("/Identity/Account/Login");
                return;
            }

            await _next(context);
        }

        private bool IsPublicPath(string path)
        {
            return PublicPaths.Any(publicPath =>
                path.Equals(publicPath, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(publicPath + "/", StringComparison.OrdinalIgnoreCase));
        }

        private bool IsProtectedPath(string path)
        {
            return ProtectedPaths.Any(protectedPath =>
                path.Equals(protectedPath, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(protectedPath + "/", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Extension methods for the AuthenticationRedirectMiddleware.
    /// </summary>
    public static class AuthenticationRedirectMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationRedirect(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationRedirectMiddleware>();
        }
    }
}
