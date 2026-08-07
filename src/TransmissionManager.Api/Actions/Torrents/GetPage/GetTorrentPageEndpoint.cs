using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TransmissionManager.Api.Common.Dto.Torrents;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Services;
using Direction = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageDirection;
using Order = TransmissionManager.Api.Common.Dto.Torrents.GetTorrentPageOrder;

namespace TransmissionManager.Api.Actions.Torrents.GetPage;

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
        [EnumDataType(typeof(Order))] Order orderBy = Order.Id,
        [Range(1, 10000)] int take = 20,
        long? anchorId = null,
        string? anchorValue = null,
        [EnumDataType(typeof(Direction))] Direction direction = Direction.Forward,
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

        var parsed = parameters.Parse(out var errors);
        if (errors is not null && errors.Length != 0)
            return TypedResults.ValidationProblem(errors);

        var page = await service.GetPageAsync(parameters, parsed, cancellationToken).ConfigureAwait(false);
        var count = await service.GetCountAsync(parameters.ToTorrentFilter(), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(ToTorrentPageResponse(page, parameters, count));
    }

    private static GetTorrentPageResponse ToTorrentPageResponse(
        TorrentPage page,
        in GetTorrentPageParameters parameters,
        long count)
    {
        var dtos = new TorrentDto[page.Torrents.Count];
        for (var i = 0; i < page.Torrents.Count; i++)
            dtos[i] = page.Torrents[i].ToDto();

        bool emitNext, emitPrevious;
        if (parameters.Direction is Direction.Forward)
        {
            emitNext = page.HasMore;
            emitPrevious = parameters.AnchorId is not null;
        }
        else
        {
            emitPrevious = page.HasMore;
            emitNext = parameters.AnchorId is not null;
        }

        var nextParams = emitNext ? parameters.ToNextPageParameters(dtos) : null;
        var prevParams = emitPrevious ? parameters.ToPreviousPageParameters(dtos) : null;

        return new GetTorrentPageResponse(
            dtos,
            nextParams?.ToPathAndQueryString(),
            prevParams?.ToPathAndQueryString(),
            count);
    }
}
