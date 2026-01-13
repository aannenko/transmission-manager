using Microsoft.AspNetCore.Http.HttpResults;

namespace TransmissionManager.Api.Actions.AppVersion;

internal static class GetAppVersionEndpoint
{
    private static readonly Version _assemblyVersion = typeof(Program).Assembly.GetName().Version!;

    public static IEndpointRouteBuilder MapGetAppVersionEndpoint(this IEndpointRouteBuilder builder)
    {
        _ = builder.MapGet("/", GetAppVersion).WithName(EndpointNames.GetAppVersion);
        return builder;
    }

    private static Ok<Version> GetAppVersion() =>
        TypedResults.Ok(_assemblyVersion);
}
