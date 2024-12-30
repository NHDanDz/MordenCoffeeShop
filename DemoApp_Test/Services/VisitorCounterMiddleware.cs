using Microsoft.AspNetCore.Http;

namespace DemoApp_Test.Services
{
    public class VisitorCounterMiddleware
    {
        private readonly RequestDelegate _next;

        public VisitorCounterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (!IsStaticFile(context.Request))
                {
                    VisitorCounter.IncrementVisitorCount(context);
                }

                await _next(context);

                if (context.Response.StatusCode == StatusCodes.Status401Unauthorized ||
                    context.Response.StatusCode == StatusCodes.Status403Forbidden)
                {
                    VisitorCounter.DecrementOnlineUser(context);
                }
            }
            catch
            {
                VisitorCounter.DecrementOnlineUser(context);
                throw;
            }
        }

        private bool IsStaticFile(HttpRequest request)
        {
            return request.Path.Value?.StartsWith("/css/") == true ||
                   request.Path.Value?.StartsWith("/js/") == true ||
                   request.Path.Value?.StartsWith("/images/") == true ||
                   request.Path.Value?.StartsWith("/lib/") == true;
        }
    }

    // Extension method
    public static class VisitorCounterMiddlewareExtensions
    {
        public static IApplicationBuilder UseVisitorCounter(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<VisitorCounterMiddleware>();
        }
    }
}