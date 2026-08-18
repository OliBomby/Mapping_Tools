using Mapping_Tools.Application.Sliderator;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mapping_Tools.Views.Sliderator;

/// <summary>Adapts the legacy Sliderator completion event to the shared interaction port.</summary>
internal sealed class WpfSlideratorInteraction : ISlideratorInteraction
{
    private readonly SlideratorView view;

    internal WpfSlideratorInteraction(SlideratorView view)
    {
        this.view = view;
    }

    /// <inheritdoc/>
    public Task<bool> RunFastAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        CancellationTokenRegistration registration = default;
        int completed = 0;

        void Complete(bool success)
        {
            if (Interlocked.Exchange(ref completed, 1) != 0)
            {
                return;
            }

            view.RunFinished -= handler;
            registration.Dispose();
            completion.TrySetResult(success);
        }

        handler = (_, args) =>
            Complete(args is RunToolCompletedEventArgs { Success: true });
        view.RunFinished += handler;
        registration = cancellationToken.Register(() =>
        {
            if (Interlocked.Exchange(ref completed, 1) != 0)
            {
                return;
            }

            view.RunFinished -= handler;
            completion.TrySetCanceled(cancellationToken);
        });

        if (Volatile.Read(ref completed) != 0)
        {
            registration.Dispose();
        }

        try
        {
            view.RunFast();
        }
        catch
        {
            Complete(false);
        }

        if (view.CanRun)
        {
            Complete(false);
        }

        return completion.Task;
    }
}
