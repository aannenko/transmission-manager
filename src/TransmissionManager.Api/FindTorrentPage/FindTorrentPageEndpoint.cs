using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.FindTorrentPage;

public static class FindTorrentPageEndpoint
{
    public static IEndpointRouteBuilder MapFindTorrentPageEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", FindTorrentPageAsync)
            .WithParameterValidation()
            .WithName(EndpointNames.FindTorrentPage);

        return builder;
    }

    private static async Task<Results<Ok<FindTorrentPageResponse>, ValidationProblem>> FindTorrentPageAsync(
        [FromServices] TorrentQueryService service,
        [AsParameters] FindTorrentPageParameters parameters,
        CancellationToken cancellationToken)
    {
        var torrents = await service
            .FindPageAsync(parameters.ToPageDescriptor(), parameters.ToTorrentFilter(), cancellationToken)
            .ConfigureAwait(false);

        var nextPage = parameters.ToNextPageParameters(torrents)?.ToPathAndQueryString();
        return TypedResults.Ok(new FindTorrentPageResponse(torrents, nextPage));
    }
}
