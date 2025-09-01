using Microsoft.AspNetCore.Components;

namespace TransmissionManager.Web.Components;

#pragma warning disable CA1515 // Consider making public types internal - Blazor components must be public
public abstract class CommonComponentBase : ComponentBase
#pragma warning restore CA1515 // Consider making public types internal
{
    private protected bool IsBusy { get; set; }

    private protected string Message { get; set; } = string.Empty;

    private protected abstract string BusyMessage { get; }

    private protected virtual string GetOperationCanceledMessage(OperationCanceledException exception) =>
        "Operation was canceled.";

    private protected virtual string GetDisconnectedMessage(HttpRequestException exception) =>
        $"Connection to Transmission Manager cannot be established: '{exception.Message}'.";

    private protected virtual string GetGenericErrorMessage(HttpRequestException exception) =>
        $"An error occurred: '{exception.Message}'.";

    private protected async Task<TReturn?> CallService<TArg, TReturn>(
        TArg arg,
        Func<TArg, Task<TReturn>> func)
    {
        Message = BusyMessage;
        IsBusy = true;
        try
        {
            return await func(arg).ConfigureAwait(false);
        }
        catch (OperationCanceledException e)
        {
            Message = GetOperationCanceledMessage(e);
            return default;
        }
        catch (HttpRequestException e)
        {
            Message = e.StatusCode is null ? GetDisconnectedMessage(e) : GetGenericErrorMessage(e);
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private protected async Task<bool> CallService<TArg>(TArg arg, Func<TArg, Task> func)
    {
        Message = BusyMessage;
        IsBusy = true;
        try
        {
            await func(arg).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException e)
        {
            Message = GetOperationCanceledMessage(e);
            return false;
        }
        catch (HttpRequestException e)
        {
            Message = e.StatusCode is null ? GetDisconnectedMessage(e) : GetGenericErrorMessage(e);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
