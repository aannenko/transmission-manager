﻿using System.Net;
using TransmissionManager.Api.Shared.Constants;
using TransmissionManager.Database.Dto;
using TransmissionManager.Database.Models;

namespace TransmissionManager.Api.Actions.FindTorrentPage;

public static class FindTorrentPageParametersExtensions
{
    public static PageDescriptor ToPageDescriptor(this FindTorrentPageParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return new(
            Take: parameters.Take,
            AfterId: parameters.AfterId);
    }

    public static TorrentFilter ToTorrentFilter(this FindTorrentPageParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return new(
            HashString: parameters.HashString,
            WebPageUri: parameters.WebPageUri,
            NameStartsWith: parameters.NameStartsWith,
            CronExists: parameters.CronExists);
    }

    public static string ToPathAndQueryString(this FindTorrentPageParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var (take, afterId, hashString, webPageUri, nameStartsWith, cronExists) = parameters;
        nameStartsWith = WebUtility.UrlEncode(nameStartsWith);
        return $"{EndpointAddresses.TorrentsApi}?{nameof(take)}={take}&{nameof(afterId)}={afterId}" +
            $"{(hashString is null ? string.Empty : $"&{nameof(hashString)}={hashString}")}" +
            $"{(webPageUri is null ? string.Empty : $"&{nameof(webPageUri)}={webPageUri}")}" +
            $"{(string.IsNullOrEmpty(nameStartsWith) ? string.Empty : $"&{nameof(nameStartsWith)}={nameStartsWith}")}" +
            $"{(cronExists is null ? string.Empty : $"&{nameof(cronExists)}={cronExists}")}";
    }

    public static FindTorrentPageParameters? ToNextPageParameters(
        this FindTorrentPageParameters parameters,
        IReadOnlyList<Torrent> currentPage)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(currentPage);

        if (currentPage.Count is 0 || parameters.WebPageUri is not null || parameters.HashString is not null)
            return null;

        return parameters with { AfterId = currentPage[^1].Id };
    }
}