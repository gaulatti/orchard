using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

/// <summary>Subscribes to the shared alert stream for its native XAML root.</summary>
public sealed class BleeckerAlertContainer : Panel
{
    private sealed record AlertMessage(Guid Id, string Message, BleeckerAlertType Type);
    private sealed class RootState
    {
        internal readonly List<AlertMessage> Messages = [];
        internal readonly List<WeakReference<BleeckerAlertContainer>> Subscribers = [];
        internal void Notify()
        {
            foreach (var subscriber in Subscribers.ToArray())
                if (subscriber.TryGetTarget(out var host)) host.Render(Messages.ToArray());
                else Subscribers.Remove(subscriber);
        }
        internal void Remove(Guid id) { Messages.RemoveAll(message => message.Id == id); Notify(); }
    }
    private static readonly ConditionalWeakTable<XamlRoot, RootState> roots = new();
    private RootState? state;
    private WeakReference<BleeckerAlertContainer>? subscription;
    private readonly Dictionary<Guid, BleeckerAlert> rendered = [];

    public BleeckerAlertContainer()
    {
        Loaded += (_, _) =>
        {
            state = roots.GetOrCreateValue(XamlRoot);
            subscription = new(this);
            state.Subscribers.Add(subscription);
            // As in the pinned source, subscription alone does not replay the
            // stream. The next ShowAlert/removal publishes its current snapshot.
        };
        Unloaded += (_, _) =>
        {
            if (subscription is not null) state?.Subscribers.Remove(subscription);
            subscription = null; state = null;
            Children.Clear(); rendered.Clear();
        };
    }

    /// <summary>Publishes an alert to containers subscribed in the specified native window.</summary>
    public static void ShowAlert(XamlRoot owner, string message, BleeckerAlertType type = BleeckerAlertType.Info)
    {
        ArgumentNullException.ThrowIfNull(owner); ArgumentNullException.ThrowIfNull(message);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (!owner.Content.DispatcherQueue.HasThreadAccess) throw new InvalidOperationException("ShowAlert must run on its owner's UI thread.");
        var state = roots.GetOrCreateValue(owner);
        state.Messages.Add(new(Guid.NewGuid(), message, type));
        state.Notify();
    }
    private void Render(IReadOnlyList<AlertMessage> messages)
    {
        foreach (var id in rendered.Keys.Except(messages.Select(message => message.Id)).ToArray())
        {
            Children.Remove(rendered[id]); rendered.Remove(id);
        }
        foreach (var item in messages)
        {
            if (rendered.ContainsKey(item.Id)) continue;
            var alert = new BleeckerAlert { Message = item.Message, Type = item.Type };
            alert.Closed += (_, _) => state?.Remove(item.Id);
            rendered.Add(item.Id, alert); Children.Add(alert);
        }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(new Size(0, 0));
        return new Size(0, 0);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children) child.Arrange(new Rect(0, 0, 0, 0));
        return finalSize;
    }
}
