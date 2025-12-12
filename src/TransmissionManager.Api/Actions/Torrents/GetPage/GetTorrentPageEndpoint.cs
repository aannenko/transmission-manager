using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Api.Constants;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class GetTorrentPageEndpoint
{
    public static IEndpointRouteBuilder MapGetTorrentPageEndpoint(this IEndpointRouteBuilder builder)
    {
        _ = builder.MapGet("/", GetTorrentPageAsync).WithName(EndpointNames.GetTorrentPage);
        return builder;
    }

    // Using [AsParameters] class or struct has these bugs:
    // - a class cannot have nullable reference type constructor parameters https://github.com/dotnet/aspnetcore/issues/58953
    // - default values of a struct's constructor parameters are ignored https://github.com/dotnet/aspnetcore/issues/56396
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tested after trimming")]
    private static async Task<Results<Ok<GetTorrentPageResponse>, ValidationProblem>> GetTorrentPageAsync(
        [FromServices] TorrentService service,
        //[AsParameters] GetTorrentPageParameters parameters,
        [EnumDataType(typeof(GetTorrentPageOrder))] GetTorrentPageOrder orderBy = GetTorrentPageOrder.Id,
        [Range(1, 1000)] int take = 20,
        long? anchorId = null,
        string? anchorValue = null,
        [EnumDataType(typeof(GetTorrentPageDirection))] GetTorrentPageDirection direction = GetTorrentPageDirection.Forward,
        [MinLength(1)] string? propertyStartsWith = null,
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

        var errors = parameters.Validate();
        if (errors is not null && errors.Length != 0)
            return TypedResults.ValidationProblem(errors);

        var torrents = await service.GetPageAsync(parameters, cancellationToken).ConfigureAwait(false);

        var dtos = torrents.Select(static torrent => torrent.ToDto()).ToArray();
        return TypedResults.Ok(new GetTorrentPageResponse(
            dtos,
            parameters.ToNextPageParameters(dtos)?.ToPathAndQueryString(),
            parameters.ToPreviousPageParameters(dtos)?.ToPathAndQueryString()));
    }
}
