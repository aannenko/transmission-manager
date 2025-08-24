using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Constants;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class AddTorrentEndpoint
{
    public static IEndpointRouteBuilder MapAddTorrentEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/", AddTorrentAsync)
            .WithParameterValidation()
            .WithName(EndpointNames.AddTorrent);

        return builder;
    }

    private static async Task<Results<Created<AddTorrentResponse>, ProblemHttpResult, ValidationProblem>>
        AddTorrentAsync(
            [FromServices] LinkGenerator linker,
            [FromServices] AddTorrentHandler handler,
            AddTorrentRequest dto,
            CancellationToken cancellationToken)
    {
        var (result, id, transmissionResult, error) = await handler
            .AddTorrentAsync(dto, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            AddTorrentResult.Added =>
                TypedResults.Created(
                    linker.GetPathByName(EndpointNames.GetTorrentById, new() { [nameof(id)] = id!.Value }),
                    new AddTorrentResponse(id!.Value, transmissionResult!.Value)),
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
