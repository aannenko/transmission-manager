using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Dto.Torrents;

namespace TransmissionManager.Api.Actions.Torrents.AddOne;

internal static class AddTorrentEndpoint
{
    public static IEndpointRouteBuilder MapAddTorrentEndpoint(this IEndpointRouteBuilder builder)
    {
        _ = builder.MapPost("/", AddTorrentAsync).WithName(EndpointNames.AddTorrent);
        return builder;
    }

    private static async Task<Results<Created<AddTorrentResponse>, ProblemHttpResult, ValidationProblem>>
        AddTorrentAsync(
            [FromServices] LinkGenerator linker,
            [FromServices] AddTorrentHandler handler,
            AddTorrentRequest request,
            CancellationToken cancellationToken)
    {
        var (result, torrent, transmissionResult, error) = await handler
            .AddTorrentAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            AddTorrentResult.Added =>
                TypedResults.Created(
                    linker.GetPathByName(EndpointNames.GetTorrentById, new() { ["id"] = torrent!.Id }),
                    new AddTorrentResponse(torrent!, transmissionResult!.Value)),
            AddTorrentResult.Exists =>
                TypedResults.Problem(
                    error,
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: [new(nameof(transmissionResult), transmissionResult)]),
            AddTorrentResult.DependencyFailed =>
                TypedResults.Problem(error, statusCode: StatusCodes.Status424FailedDependency),
            _ => throw new NotImplementedException()
        };
    }
}
