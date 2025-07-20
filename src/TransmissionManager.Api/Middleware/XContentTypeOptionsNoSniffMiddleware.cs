namespace TransmissionManager.Api.Middleware;

internal sealed class XContentTypeOptionsNoSniffMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        return next(context);
    }
}
