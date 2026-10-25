using System.Runtime.ExceptionServices;

namespace Orchard;

// Win2D font validation can dispatch nested XAML layout on the same thread.
// Nested resolution uses the identical asset URI while the outer call completes
// validation; other threads wait, and validation errors remain authoritative.
internal sealed class BleeckerBundledFontAsset(Uri uri, Action<Uri> validate)
{
    private readonly object gate = new();
    private bool validating, validated;
    private ExceptionDispatchInfo? failure;

    internal string Resolve()
    {
        lock (gate)
        {
            failure?.Throw();
            if (validated || validating) return uri.AbsoluteUri;
            validating = true;
            try { validate(uri); validated = true; return uri.AbsoluteUri; }
            catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); throw; }
            finally { validating = false; }
        }
    }
}
