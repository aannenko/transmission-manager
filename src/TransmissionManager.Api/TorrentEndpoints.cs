using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.Dto;
using TransmissionManager.Api.Extensions;
using TransmissionManager.Api.Services;
using TransmissionManager.Database.Models;
using TransmissionManager.Database.Services;
using AddOrUpdateResult = TransmissionManager.Api.Dto.AddOrUpdateTorrentResult.ResultType;
using RefreshResult = TransmissionManager.Api.Dto.RefreshTorrentResult.ResultType;

namespace TransmissionManager.Api;

public static class TorrentEndpoints
{
    private const string _torrentsApiAddress = "/api/v1/torrents";
    private const string _idNotFoundMessage = "Torrent with id {0} was not found.";

    public static IEndpointRouteBuilder MapTorrentEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup(_torrentsApiAddress);

        group.MapGet("/", FindPageAsync);
        group.MapGet("/{id}", FindOneByIdAsync);
        group.MapPost("/", AddOrUpdateOneAsync);
        group.MapPost("/{id}", RefreshOneByIdAsync);
        group.MapPatch("/{id}", UpdateOneByIdAsync);
        group.MapDelete("/{id}", RemoveOneByIdAsync);

        return builder;
    }

    private static async Task<Torrent[]> FindPageAsync(
        [FromServices] TorrentService service,
        [AsParameters] TorrentFindPageParameters parameters,
        CancellationToken cancellationToken)
    {
        return await service
            .FindPageAsync(parameters.ToPageDescriptor(), parameters.ToTorrentFilter(), cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<Results<Ok<Torrent>, NotFound<string>>> FindOneByIdAsync(
        [FromServices] TorrentService service,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await service.FindOneByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.NotFound(string.Format(_idNotFoundMessage, id));
    }

    private static async Task<Results<Created, NoContent, BadRequest<string>, ValidationProblem>> AddOrUpdateOneAsync(
        [FromServices] CompositeAddOrUpdateTorrentService service,
        TorrentPostRequest dto,
        CancellationToken cancellationToken)
    {
        if (!MiniValidator.TryValidate(dto, out var errors))
            return TypedResults.ValidationProblem(errors);

        var (resultType, id, errorMessage) =
            await service.AddOrUpdateTorrentAsync(dto, cancellationToken).ConfigureAwait(false);

        return resultType switch
        {
            AddOrUpdateResult.Add => TypedResults.Created($"{_torrentsApiAddress}/{id}"),
            AddOrUpdateResult.Update => TypedResults.NoContent(),
            _ => TypedResults.BadRequest(errorMessage)
        };
    }

    private static async Task<Results<NoContent, NotFound<string>, BadRequest<string>>> RefreshOneByIdAsync(
        [FromServices] CompositeRefreshTorrentService service,
        long id,
        CancellationToken cancellationToken)
    {
        var (resultType, errorMessage) =
            await service.RefreshTorrentAsync(id, cancellationToken).ConfigureAwait(false);

        return resultType switch
        {
            RefreshResult.Success => TypedResults.NoContent(),
            RefreshResult.NotFound => TypedResults.NotFound(string.Format(_idNotFoundMessage, id)),
            _ => TypedResults.BadRequest(errorMessage)
        };
    }

    private static async Task<Results<NoContent, NotFound<string>, ValidationProblem>> UpdateOneByIdAsync(
        [FromServices] SchedulableTorrentService service,
        long id,
        TorrentPatchRequest dto,
        CancellationToken cancellationToken)
    {
        if (!MiniValidator.TryValidate(dto, out var errors))
            return TypedResults.ValidationProblem(errors);

        var updateDto = dto.ToTorrentUpdateDto();
        return await service.TryUpdateOneByIdAsync(id, updateDto, cancellationToken).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.NotFound(string.Format(_idNotFoundMessage, id));
    }

    private static async Task<Results<NoContent, NotFound<string>>> RemoveOneByIdAsync(
        [FromServices] SchedulableTorrentService service,
        long id,
        CancellationToken cancellationToken)
    {
        return await service.TryDeleteOneByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.NotFound(string.Format(_idNotFoundMessage, id));
    }
}
