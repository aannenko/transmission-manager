using Microsoft.AspNetCore.Http.HttpResults;
using TransmissionManager.Api.Constants;
using TransmissionManager.Api.Common.Dto.AppInfo;

namespace TransmissionManager.Api.Actions.AppInfo;

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
