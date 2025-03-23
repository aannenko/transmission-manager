using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.Constants;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents.FindPage;

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
        TorrentOrder orderBy = TorrentOrder.Id,
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

        var nextPage = parameters.ToNextPageParameters(torrents)?.ToPathAndQueryString();
        var previousPage = parameters.ToPreviousPageParameters(torrents)?.ToPathAndQueryString();

        return TypedResults.Ok(new FindTorrentPageResponse(torrents, nextPage, previousPage));
    }
}
