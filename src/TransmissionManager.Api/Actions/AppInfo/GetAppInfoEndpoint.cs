using Microsoft.AspNetCore.Http.HttpResults;
using TransmissionManager.Api.Common.Dto.AppInfo;
using TransmissionManager.Api.Constants;

namespace TransmissionManager.Api.Actions.AppInfo;

internal static class GetAppInfoEndpoint
{
    private static readonly Version _assemblyVersion = typeof(Program).Assembly.GetName().Version!;

    public static IEndpointRouteBuilder MapGetAppInfoEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", GetAppInfo).WithName(EndpointNames.GetAppInfo);
        return builder;
    }

    private static Ok<GetAppInfoResponse> GetAppInfo() =>
        TypedResults.Ok(new GetAppInfoResponse(_assemblyVersion, DateTimeOffset.Now));
}
