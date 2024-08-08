using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;

namespace TransmissionManager.Api.Database.Abstractions;

public interface ITorrentService
{
    Torrent[] FindPage(TorrentPageDescriptor dto);

    Torrent? FindOneById(long id);

    long AddOne(TorrentAddDto dto);

    bool TryUpdateOneById(long id, TorrentUpdateDto dto);

    bool TryDeleteOneById(long id);
}
