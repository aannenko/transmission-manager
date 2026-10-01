namespace TransmissionManager.Api.Common.Dto.Torrents;

/// <summary>Defines supported torrent-page sort orders.</summary>
public enum GetTorrentPageOrder
{
    /// <summary>Orders by ID ascending.</summary>
    Id,
    /// <summary>Orders by ID descending.</summary>
    IdDesc,
    /// <summary>Orders by refresh date ascending.</summary>
    RefreshDate,
    /// <summary>Orders by refresh date descending.</summary>
    RefreshDateDesc,
    /// <summary>Orders by name ascending.</summary>
    Name,
    /// <summary>Orders by name descending.</summary>
    NameDesc,
    /// <summary>Orders by source URI ascending.</summary>
    Uri,
    /// <summary>Orders by source URI descending.</summary>
    UriDesc,
    /// <summary>Orders by download directory ascending.</summary>
    DownloadDir,
    /// <summary>Orders by download directory descending.</summary>
    DownloadDirDesc,
}
