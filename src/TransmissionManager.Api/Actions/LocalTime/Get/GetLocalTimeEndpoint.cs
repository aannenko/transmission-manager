using Microsoft.AspNetCore.Http.HttpResults;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.Actions.LocalTime.Get;

internal static class GetLocalTimeEndpoint
{
    public static IEndpointRouteBuilder MapGetLocalTimeEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", GetLocalTime).WithName(EndpointNames.GetLocalTime);
        return builder;
    }

    private static Ok<GetLocalTimeResponse> GetLocalTime() =>
        TypedResults.Ok(new GetLocalTimeResponse(DateTimeOffset.Now));
}
