using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Common.Extensions;
using TransmissionManager.Api.Constants;
using TransmissionManager.Api.Extensions;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class FindTorrentPageEndpoint
{
    public static IEndpointRouteBuilder MapFindTorrentPageEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", FindTorrentPageAsync)
            //.WithParameterValidation() // Commented out until the bugs described below are fixed
            .WithName(EndpointNames.FindTorrentPage);

        return builder;
    }

    // Using [AsParameters] class or struct has these bugs:
    // - a class cannot have nullable reference type constructor parameters https://github.com/dotnet/aspnetcore/issues/58953
    // - default values of a struct's constructor parameters are ignored https://github.com/dotnet/aspnetcore/issues/56396
    private static async Task<Results<Ok<FindTorrentPageResponse>, ValidationProblem>> FindTorrentPageAsync(
        [FromServices] TorrentService service,
        //[AsParameters] FindTorrentPageParameters parameters,
        FindTorrentPageOrder orderBy = FindTorrentPageOrder.Id,
        int take = 20,
        long? anchorId = null,
        string? anchorValue = null,
        FindTorrentPageDirection direction = FindTorrentPageDirection.Forward,
        string? propertyStartsWith = null,
        bool? cronExists = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new FindTorrentPageParameters(
            orderBy,
            anchorId,
            anchorValue,
            take,
            direction,
            propertyStartsWith,
            cronExists);

        if (!MiniValidator.TryValidate(parameters, out var errors))
            return TypedResults.ValidationProblem(errors);

        var pageDescriptor = parameters.ToTorrentPageDescriptor();
        var filter = parameters.ToTorrentFilter();

        var torrents = await service.FindPageAsync(pageDescriptor, filter, cancellationToken).ConfigureAwait(false);
        var dtos = torrents.Select(static torrent => torrent.ToDto()).ToArray();

        var nextPage = parameters.ToNextPageParameters(dtos)?.ToPathAndQueryString();
        var previousPage = parameters.ToPreviousPageParameters(dtos)?.ToPathAndQueryString();

        return TypedResults.Ok(new FindTorrentPageResponse(
            [.. torrents.Select(static torrent => torrent.ToDto())],
            nextPage,
            previousPage));
    }
}
