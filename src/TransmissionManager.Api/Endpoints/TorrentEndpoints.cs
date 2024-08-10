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

        group.MapGet("/", FindPage);
        group.MapGet("/{id}", FindOneById);
        group.MapPost("/", AddOrUpdateOneAsync);
        group.MapPost("/{id}", RefreshOneByIdAsync);
        group.MapPatch("/{id}", UpdateOneById);
        group.MapDelete("/{id}", RemoveOneById);

        return builder;
    }

    private static Torrent[] FindPage(
        [FromServices] TorrentService service,
        int take = 20,
        long afterId = 0,
        string? webPageUri = null,
        string? nameStartsWith = null,
        bool? cronExists = null)
    {
        return service.FindPage(new()
        {
            Take = take,
            AfterId = afterId,
            WebPageUri = webPageUri,
            NameStartsWith = nameStartsWith,
            CronExists = cronExists
        });
    }

    private static Results<Ok<Torrent>, NotFound> FindOneById(
        [FromServices] TorrentService service,
        long id)
    {
        var result = service.FindOneById(id);
        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    private static async Task<Results<Created, NoContent, BadRequest<string>, ValidationProblem>> AddOrUpdateOneAsync(
        [FromServices] CompositeTorrentService<SchedulableTorrentService> service,
        TorrentPostRequest dto,
        CancellationToken cancellationToken = default)
    {
        if (!MiniValidator.TryValidate(dto, out var errors))
            return TypedResults.ValidationProblem(errors);

        var (resultType, id, errorMessage) = await service.AddOrUpdateTorrentAsync(dto, cancellationToken);
        
        return resultType switch
        {
            AddOrUpdateResult.Add => TypedResults.Created($"{_torrentsApiAddress}/{id}"),
            AddOrUpdateResult.Update => TypedResults.NoContent(),
            _ => TypedResults.BadRequest(errorMessage)
        };
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> RefreshOneByIdAsync(
        [FromServices] CompositeTorrentService<TorrentService> service,
        long id,
        CancellationToken cancellationToken)
    {
        var (resultType, errorMessage) = await service.RefreshTorrentAsync(id, cancellationToken);

        return resultType switch
        {
            RefreshResult.Success => TypedResults.NoContent(),
            RefreshResult.NotFound => TypedResults.NotFound(),
            _ => TypedResults.BadRequest(errorMessage)
        };
    }

    private static Results<NoContent, NotFound, ValidationProblem> UpdateOneById(
        [FromServices] SchedulableTorrentService service,
        long id,
        TorrentPatchRequest dto)
    {
        if (!MiniValidator.TryValidate(dto, out var errors))
            return TypedResults.ValidationProblem(errors);

        return service.TryUpdateOneById(id, dto.ToTorrentUpdateDto())
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static Results<NoContent, NotFound> RemoveOneById(
        [FromServices] SchedulableTorrentService service,
        long id)
    {
        return service.TryDeleteOneById(id)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
