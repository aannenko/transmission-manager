namespace TransmissionManager.Api.Services.Background;

internal sealed class BackgroundTaskService(IServiceScopeFactory serviceScopeFactory)
{
    public async Task RunScopedAsync<TArg>(
        Func<IServiceProvider, TArg, CancellationToken, Task> func,
        TArg argument,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        await func(scope.ServiceProvider, argument, cancellationToken).ConfigureAwait(false);
    }
}
