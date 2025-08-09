using TransmissionManager.Web.Services;

namespace TransmissionManager.Web.Extensions;

internal static class ConnectionServiceExtensions
{
    public static async Task InitializeAsync(
        this ConnectionService connectionService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await connectionService
                .ConnectAsync(connectionService.BaseAddress, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
        }
    }
}
