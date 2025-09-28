
namespace TransmissionManager.Api.Middleware;

internal sealed class CacheControlHeaderMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.Headers.Append("Cache-Control", "no-cache");
        return next(context);
    }
}
