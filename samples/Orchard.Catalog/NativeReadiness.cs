using Microsoft.UI.Dispatching;

namespace Orchard.Catalog;

// Readiness conditions still own their original deadlines and layout checks.
// Give pending binding/callback chains two turns before the existing poll.
// This is not a presentation, pointer or final-capture Rendering gate.
internal sealed class NativeReadiness(DispatcherQueue dispatcher)
{
    private int queuedTurns;

    internal Task WaitAsync()
    {
        if (queuedTurns++ >= 2) return Task.Delay(16);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () => completed.TrySetResult()))
            throw new InvalidOperationException("Catalog readiness dispatcher is unavailable.");
        // A busy dispatcher must never delay the caller beyond its original
        // poll. Either wake-up only schedules the next real condition check.
        return Task.WhenAny(completed.Task, Task.Delay(16));
    }
}
