using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TransmissionManager.Api.Constants;
using TransmissionManager.Api.Common.Dto.Torrents.Add;

namespace TransmissionManager.Api.Actions.Torrents.Add;

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
                TypedResults.Created(GetTorrentUri(linker, id), new AddTorrentResponse(transmissionResult!.Value)),
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

    private static string? GetTorrentUri(LinkGenerator linker, long? id) =>
        linker.GetPathByName(EndpointNames.FindTorrentById, new() { [nameof(id)] = id });
}
