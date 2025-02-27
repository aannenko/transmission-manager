using Microsoft.AspNetCore.Http.HttpResults;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.Actions.AppVersion.Get;

internal static class GetAppVersionEndpoint
{
    public static IEndpointRouteBuilder MapGetAppVersionEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", GetAppVersion).WithName(EndpointNames.GetAppVersion);
        return builder;
    }

    private static Ok<GetAppVersionResponse> GetAppVersion() =>
        TypedResults.Ok(new GetAppVersionResponse(typeof(Program).Assembly.GetName().Version!));
}
