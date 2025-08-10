using TransmissionManager.Api.Common.Dto.AppInfo;
using TransmissionManager.Web.Services;

namespace TransmissionManager.Web.Extensions;

internal static class ConnectionServiceExtensions
{
    public static async Task<GetAppInfoResponse> TryConnectAsync(
        this ConnectionService connectionService,
        Uri? baseAddress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await connectionService
                .ConnectAsync(baseAddress ?? connectionService.BaseAddress, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            return default;
        }
    }
}
