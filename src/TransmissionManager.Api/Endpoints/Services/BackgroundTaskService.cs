namespace TransmissionManager.Api.Endpoints.Services;

public sealed class BackgroundTaskService(IServiceProvider serviceProvider)
{
    public async Task RunScopedAsync<TArg>(
        Func<IServiceProvider, TArg, CancellationToken, Task> func,
        TArg argument,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        await func(scope.ServiceProvider, argument, cancellationToken).ConfigureAwait(false);
    }
}
