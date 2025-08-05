namespace TransmissionManager.Api.Middleware;

internal sealed class AllowPrivateNetworkHeaderMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Method == HttpMethods.Options)
            context.Response.Headers.Append("Access-Control-Allow-Private-Network", "true");

        return next(context);
    }
}
