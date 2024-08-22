using TransmissionManager.Api.Database.Dto;
using TransmissionManager.Api.Database.Models;

namespace TransmissionManager.Api.Database.Abstractions;

public interface ITorrentService
{
    Task<Torrent[]> FindPageAsync(TorrentPageDescriptor dto);

    Task<Torrent?> FindOneByIdAsync(long id);

    Task<long> AddOneAsync(TorrentAddDto dto);

    Task<bool> TryUpdateOneByIdAsync(long id, TorrentUpdateDto dto);

    Task<bool> TryDeleteOneByIdAsync(long id);
}
