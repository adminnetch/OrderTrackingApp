using OrderTrackingApp.Services;

namespace OrderTrackingApp.Middleware;

public class FirstRunMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirstRunMiddleware> _logger;

    public FirstRunMiddleware(RequestDelegate next, ILogger<FirstRunMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IInstallationStateService stateService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        var publicPaths = new[] { "/setup", "/account/login", "/account/register", "/js", "/css", "/lib", "/images", "/api" };
        if (publicPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        if (path == "/" || path.StartsWith("/home") || path.StartsWith("/account") || path.StartsWith("/ordini"))
        {
            try
            {
                var isFirstRunRequired = await stateService.IsFirstRunRequiredAsync();
                
                if (isFirstRunRequired)
                {
                    _logger.LogInformation("First run required, redirecting to /setup");
                    context.Response.Redirect("/setup");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking first run state, allowing access");
            }
        }

        await _next(context);
    }
}

public static class FirstRunMiddlewareExtensions
{
    public static IApplicationBuilder UseFirstRunDetection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FirstRunMiddleware>();
    }
}