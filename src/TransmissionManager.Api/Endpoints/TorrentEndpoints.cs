using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.Composite.Services;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Endpoints.Extensions;
using AddOrUpdateResult = TransmissionManager.Api.Composite.Dto.AddOrUpdateTorrentResult.ResultType;
using RefreshResult = TransmissionManager.Api.Composite.Dto.RefreshTorrentResult.ResultType;

namespace TransmissionManager.Api.Endpoints;

public static class TorrentEndpoints
{
    private const string _torrentsApiAddress = "/api/v1/torrents";

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
        int take = 20,
        long afterId = 0,
        string? webPageUri = null,
        string? nameStartsWith = null,
        bool? cronExists = null)
    {
        return await service.FindPageAsync(new()
        {
            Take = take,
            AfterId = afterId,
            WebPageUri = webPageUri,
            NameStartsWith = nameStartsWith,
            CronExists = cronExists
        }).ConfigureAwait(false);
    }

    private static async Task<Results<Ok<Torrent>, NotFound>> FindOneByIdAsync(
        [FromServices] TorrentService service,
        long id)
    {
        var result = await service.FindOneByIdAsync(id).ConfigureAwait(false);
        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    private static async Task<Results<Created, NoContent, BadRequest<string>, ValidationProblem>> AddOrUpdateOneAsync(
        [FromServices] CompositeAddOrUpdateTorrentService service,
        TorrentPostRequest dto,
        CancellationToken cancellationToken = default)
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

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> RefreshOneByIdAsync(
        [FromServices] CompositeRefreshTorrentService service,
        long id,
        CancellationToken cancellationToken)
    {
        var (resultType, errorMessage) =
            await service.RefreshTorrentAsync(id, cancellationToken).ConfigureAwait(false);

        return resultType switch
        {
            RefreshResult.Success => TypedResults.NoContent(),
            RefreshResult.NotFound => TypedResults.NotFound(),
            _ => TypedResults.BadRequest(errorMessage)
        };
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateOneByIdAsync(
        [FromServices] SchedulableTorrentService service,
        long id,
        TorrentPatchRequest dto)
    {
        if (!MiniValidator.TryValidate(dto, out var errors))
            return TypedResults.ValidationProblem(errors);

        return await service.TryUpdateOneByIdAsync(id, dto.ToTorrentUpdateDto()).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> RemoveOneByIdAsync(
        [FromServices] SchedulableTorrentService service,
        long id)
    {
        return await service.TryDeleteOneByIdAsync(id).ConfigureAwait(false)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
