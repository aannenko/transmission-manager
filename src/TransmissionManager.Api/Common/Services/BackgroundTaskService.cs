namespace TransmissionManager.Api.Common.Services;

internal sealed class BackgroundTaskService(IServiceProvider serviceProvider)
{
    public async Task RunScopedAsync<TArg>(
        Func<IServiceProvider, TArg, CancellationToken, Task> func,
        TArg argument,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);

        using var scope = serviceProvider.CreateScope();
        await func(scope.ServiceProvider, argument, cancellationToken).ConfigureAwait(false);
    }
}
