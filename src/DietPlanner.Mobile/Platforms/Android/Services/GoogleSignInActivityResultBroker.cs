using Android.App;
using Android.Content;

namespace DietPlanner.Mobile.Services;

internal static class GoogleSignInActivityResultBroker
{
    private const int RequestCode = 9247;

    private static TaskCompletionSource<Intent?>? _pendingRequest;

    public static Task<Intent?> StartAsync(Activity activity, Intent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(intent);

        if (_pendingRequest is not null)
        {
            throw new InvalidOperationException("Another Google sign-in request is already in progress.");
        }

        var pendingRequest = new TaskCompletionSource<Intent?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequest = pendingRequest;

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                if (ReferenceEquals(_pendingRequest, pendingRequest))
                {
                    _pendingRequest = null;
                    pendingRequest.TrySetCanceled(cancellationToken);
                }
            });
        }

        activity.StartActivityForResult(intent, RequestCode);
        return pendingRequest.Task;
    }

    public static bool TryHandleResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode != RequestCode || _pendingRequest is null)
        {
            return false;
        }

        var pendingRequest = _pendingRequest;
        _pendingRequest = null;

        if (GoogleSignInResultPolicy.ShouldTreatAsCancellation(
                wasCanceled: resultCode == Result.Canceled,
                hasIntentData: data is not null))
        {
            pendingRequest.TrySetException(new OperationCanceledException("Google sign-in was canceled."));
            return true;
        }

        pendingRequest.TrySetResult(data);
        return true;
    }
}
