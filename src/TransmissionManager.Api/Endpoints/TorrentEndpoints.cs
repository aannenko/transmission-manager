using Microsoft.AspNetCore.Mvc;
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

        group.MapGet("/", FindMany);
        group.MapGet("/{id}", FindOneById);
        group.MapPost("/", TryAddOrUpdateOneAsync);
        group.MapPut("/{id}", UpdateOne);
        group.MapDelete("/{id}", RemoveOne);

        return builder;
    }

    private static Torrent[] FindMany(
        [FromServices] TorrentService torrentService,
        int take = 20,
        long afterId = 0,
        string? webPageUri = null,
        string? nameStartsWith = null,
        bool? cronExists = null)
    {
        return torrentService.FindPage(new()
        {
            Take = take,
            AfterId = afterId,
            WebPageUri = webPageUri,
            NameStartsWith = nameStartsWith,
            CronExists = cronExists
        });
    }

    private static Torrent? FindOneById([FromServices] TorrentService service, long id)
    {
        return service.FindOneById(id);
    }

    private static Task<bool> TryAddOrUpdateOneAsync(
        [FromServices] CompositeService<SchedulableTorrentService> compositeService,
        TorrentPostRequest dto,
        CancellationToken cancellationToken = default)
    {
        return compositeService.TryAddOrUpdateTorrentAsync(dto, cancellationToken);
    }

    private static void UpdateOne(
        [FromServices] SchedulableTorrentService torrentService,
        long id,
        TorrentPutRequest dto)
    {
        torrentService.UpdateOne(id, dto.ToTorrentUpdateDto());
    }

    private static void RemoveOne(
        [FromServices] SchedulableTorrentService torrentService,
        long id)
    {
        torrentService.RemoveOne(id);
    }
}
