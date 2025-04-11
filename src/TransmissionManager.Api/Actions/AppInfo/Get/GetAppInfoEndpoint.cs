using Microsoft.AspNetCore.Http.HttpResults;
using TransmissionManager.Api.Constants;
using TransmissionManager.Api.Shared.Dto.AppInfo.Get;

namespace TransmissionManager.Api.Actions.AppInfo.Get;

internal static class GetAppInfoEndpoint
{
    public static IEndpointRouteBuilder MapGetAppInfoEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", GetAppInfo).WithName(EndpointNames.GetAppInfo);
        return builder;
    }

    private static Ok<GetAppInfoResponse> GetAppInfo()
    {
        var appInfo = new GetAppInfoResponse(
            typeof(Program).Assembly.GetName().Version!,
            DateTimeOffset.Now);

        return TypedResults.Ok(appInfo);
    }
}
