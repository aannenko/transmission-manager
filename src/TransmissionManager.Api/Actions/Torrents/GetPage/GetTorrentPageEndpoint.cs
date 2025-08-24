using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Constants;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class GetTorrentPageEndpoint
{
    public static IEndpointRouteBuilder MapGetTorrentPageEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", GetTorrentPageAsync)
            //.WithParameterValidation() // Commented out until the bugs described below are fixed
            .WithName(EndpointNames.GetTorrentPage);

        return builder;
    }

    // Using [AsParameters] class or struct has these bugs:
    // - a class cannot have nullable reference type constructor parameters https://github.com/dotnet/aspnetcore/issues/58953
    // - default values of a struct's constructor parameters are ignored https://github.com/dotnet/aspnetcore/issues/56396
    private static async Task<Results<Ok<GetTorrentPageResponse>, ValidationProblem>> GetTorrentPageAsync(
        [FromServices] TorrentService service,
        //[AsParameters] GetTorrentPageParameters parameters,
        GetTorrentPageOrder orderBy = GetTorrentPageOrder.Id,
        int take = 20,
        long? anchorId = null,
        string? anchorValue = null,
        GetTorrentPageDirection direction = GetTorrentPageDirection.Forward,
        string? propertyStartsWith = null,
        bool? cronExists = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetTorrentPageParameters(
            orderBy,
            anchorId,
            anchorValue,
            take,
            direction,
            propertyStartsWith,
            cronExists);

        if (!MiniValidator.TryValidate(parameters, out var errors))
            return TypedResults.ValidationProblem(errors);

        var torrents = await service.GetPageAsync(parameters, cancellationToken).ConfigureAwait(false);

        var dtos = torrents.Select(static torrent => torrent.ToDto()).ToArray();
        return TypedResults.Ok(new GetTorrentPageResponse(
            dtos,
            parameters.ToNextPageParameters(dtos)?.ToPathAndQueryString(),
            parameters.ToPreviousPageParameters(dtos)?.ToPathAndQueryString()));
    }
}
