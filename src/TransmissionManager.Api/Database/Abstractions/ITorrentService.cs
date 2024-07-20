using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;

namespace TransmissionManager.Api.Database.Abstractions;

public interface ITorrentService
{
    Torrent[] FindPage(TorrentGetPageDescriptor dto);

    Torrent? FindOneById(long id);

    long AddOne(TorrentAddDto dto);

    void UpdateOne(long id, TorrentUpdateDto dto);

    void RemoveOne(long id);
}
