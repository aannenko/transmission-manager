using TransmissionManager.Api.Actions.Torrents.AddOne;
using TransmissionManager.Api.Actions.Torrents.DeleteById;
using TransmissionManager.Api.Actions.Torrents.GetById;
using TransmissionManager.Api.Actions.Torrents.GetPage;
using TransmissionManager.Api.Actions.Torrents.RefreshById;
using TransmissionManager.Api.Actions.Torrents.UpdateById;
using TransmissionManager.Api.Common.Constants;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTorrentEndpoints(this IEndpointRouteBuilder builder)
    {
        _ = builder
            .MapGroup(EndpointAddresses.Torrents)
            .MapGetTorrentByIdEndpoint()
            .MapGetTorrentPageEndpoint()
            .MapAddTorrentEndpoint()
            .MapRefreshTorrentByIdEndpoint()
            .MapUpdateTorrentByIdEndpoint()
            .MapDeleteTorrentByIdEndpoint();

        return builder;
    }
}
