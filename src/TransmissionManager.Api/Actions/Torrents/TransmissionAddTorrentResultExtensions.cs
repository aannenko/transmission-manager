using TransmissionManager.Api.Common.Dto.Transmission;
using TransmissionManager.Api.Services.Transmission;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class TransmissionAddTorrentResultExtensions
{
    public static TransmissionAddResult ToTransmissionAddResult(this TransmissionAddTorrentResult result)
    {
        return result switch
        {
            TransmissionAddTorrentResult.Added => TransmissionAddResult.Added,
            TransmissionAddTorrentResult.Duplicate => TransmissionAddResult.Duplicate,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
        };
    }
}
