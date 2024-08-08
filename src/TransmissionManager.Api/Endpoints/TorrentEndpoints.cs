using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.Composite.Services;
using TransmissionManager.Api.Database.Models;
using TransmissionManager.Api.Database.Services;
using TransmissionManager.Api.Endpoints.Dto;
using TransmissionManager.Api.Endpoints.Extensions;

namespace TransmissionManager.Api.Endpoints;

public static class TorrentEndpoints
{
    public static IEndpointRouteBuilder MapTorrentEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/v1/torrents");

        group.MapGet("/", FindPage);
        group.MapGet("/{id}", FindOneById);
        group.MapPost("/", TryAddOrUpdateOneAsync);
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

    private static async Task<Results<Ok<bool>, ValidationProblem>> TryAddOrUpdateOneAsync(
        [FromServices] CompositeService<SchedulableTorrentService> service,
        TorrentPostRequest dto,
        CancellationToken cancellationToken = default)
    {
        if (!MiniValidator.TryValidate(dto, out var errors))
            return TypedResults.ValidationProblem(errors);

        var isSuccess = await service.TryAddOrUpdateTorrentAsync(dto, cancellationToken);
        return TypedResults.Ok(isSuccess);
    }

    private static Results<Ok, NotFound, ValidationProblem> UpdateOneById(
        [FromServices] SchedulableTorrentService service,
        long id,
        TorrentPutRequest dto)
    {
        if (!MiniValidator.TryValidate(dto, out var errors))
            return TypedResults.ValidationProblem(errors);

        return service.TryUpdateOneById(id, dto.ToTorrentUpdateDto()) ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static Results<Ok, NotFound> RemoveOneById(
        [FromServices] SchedulableTorrentService service,
        long id)
    {
        return service.TryDeleteOneById(id) ? TypedResults.Ok() : TypedResults.NotFound();
    }
}
