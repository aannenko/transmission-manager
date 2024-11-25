﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using TransmissionManager.Api.Common.Constants;
using TransmissionManager.Database.Services;

namespace TransmissionManager.Api.Actions.FindTorrentPage;

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
        int Take = 20,
        long AfterId = 0,
        string? PropertyStartsWith = null,
        bool? CronExists = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new FindTorrentPageParameters(Take, AfterId, PropertyStartsWith, CronExists);
        if (!MiniValidator.TryValidate(parameters, out var errors))
            return TypedResults.ValidationProblem(errors);

        var torrents = await service
            .FindPageAsync(parameters.ToPageDescriptor(), parameters.ToTorrentFilter(), cancellationToken)
            .ConfigureAwait(false);

        var nextPage = parameters.ToNextPageParameters(torrents)?.ToPathAndQueryString();
        return TypedResults.Ok(new FindTorrentPageResponse(torrents, nextPage));
    }
}
