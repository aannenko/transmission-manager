using Microsoft.AspNetCore.Components;
using System.Net;
using TransmissionManager.Web.Dto;
using TransmissionManager.Web.Extensions;

namespace TransmissionManager.Web.Components;

/// <summary>
/// Base for components that call the API, giving them a busy state and one message to show.
/// </summary>
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
        $"Connection cannot be established: '{exception.Message}'.";

    private protected virtual string GetGenericErrorMessage(HttpRequestException exception) =>
        $"An error occurred: '{exception.Message}'.";

    /// <summary>
    /// Runs an API call, holding the busy state and describing anything that went wrong in
    /// <see cref="Message"/>.
    /// </summary>
    /// <param name="arg">What the call needs, passed in so the callback can stay static.</param>
    /// <param name="func">The call to run.</param>
    /// <returns>
    /// What the API answered, or <c>default</c> - a failure carrying nothing - if the call never got
    /// an answer.
    /// </returns>
    private protected async Task<ApiResult<TValue>> CallNetworkService<TArg, TValue>(
        TArg arg,
        Func<TArg, Task<ApiResult<TValue>>> func)
        where TValue : class
    {
        Message = BusyMessage;
        IsBusy = true;
        try
        {
            var result = await func(arg).ConfigureAwait(false);
            if (result.Status is not ApiResultStatus.Success)
                Message = GetProblemDetailsMessage(result.ProblemDetails, result.StatusCode);

            return result;
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

    /// <inheritdoc cref="CallNetworkService{TArg, TValue}(TArg, Func{TArg, Task{ApiResult{TValue}}})"/>
    private protected async Task<ApiResult> CallNetworkService<TArg>(TArg arg, Func<TArg, Task<ApiResult>> func)
    {
        Message = BusyMessage;
        IsBusy = true;
        try
        {
            var result = await func(arg).ConfigureAwait(false);
            if (result.Status is not ApiResultStatus.Success)
                Message = GetProblemDetailsMessage(result.ProblemDetails, result.StatusCode);

            return result;
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

    /// <summary>
    /// Describes a request the API answered and refused.
    /// </summary>
    /// <remarks>
    /// Override to name the operation that failed or the fields at fault, and call the base to get
    /// what the API said. A refusal carrying no message of its own is described by its status code,
    /// which is all that is left to report - and a success code there means the address answered
    /// with something this application could not read, not that the request was refused.
    /// </remarks>
    private protected virtual string GetProblemDetailsMessage(
        ApiProblemDetails? problemDetails,
        HttpStatusCode? statusCode)
    {
        var message = problemDetails?.JoinErrorMessages();
        if (!string.IsNullOrEmpty(message))
            return message;

        if (statusCode is null)
            return "The request could not be completed.";

        return statusCode.Value.IsSuccessCode()
            ? $"The address answered with status {(int)statusCode} ({statusCode}), but not with data this application understands."
            : $"The request failed with status {(int)statusCode} ({statusCode}).";
    }
}
