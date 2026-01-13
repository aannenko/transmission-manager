using TransmissionManager.Api.Actions.Torrents.AddOne;
using TransmissionManager.Api.Actions.Torrents.DeleteById;
using TransmissionManager.Api.Actions.Torrents.RefreshById;
using TransmissionManager.Api.Actions.Torrents.UpdateById;

namespace TransmissionManager.Api.Actions.Torrents;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTorrentEndpointHandlers(this IServiceCollection services)
    {
        _ = services
            .AddTransient<AddTorrentHandler>()
            .AddTransient<RefreshTorrentByIdHandler>()
            .AddTransient<UpdateTorrentByIdHandler>()
            .AddTransient<DeleteTorrentByIdHandler>();

        return services;
    }
}
