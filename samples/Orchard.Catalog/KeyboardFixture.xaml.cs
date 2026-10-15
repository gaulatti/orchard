using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class KeyboardFixture : UserControl
{
    private readonly ObservableCollection<string> keys = ["⌘", "K"];
    public KeyboardFixture()
    {
        InitializeComponent();
        Combo.Keys = keys;
        ThreeKeys.Keys = new[] { "Ctrl", "Shift", "P" };
    }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        UpdateLayout();
        await MainWindow.AwaitPresentationAsync(this);
        Require(Single.ActualHeight == 20 && Single.ActualWidth >= 20 && Empty.ActualWidth == 20 && Single.Padding.Left == 6,
            "Keycap geometry differs from the pinned contract.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Combo);
        Require(peer.GetAutomationControlType() == AutomationControlType.Group && peer.GetName() == "⌘+K" && !Combo.IsTabStop,
            "Shortcut group lost its accessible name, role or display-only contract.");
        var group = Find<StackPanel>(Combo, "PART_Combination");
        Require(group.Children.Count == 3 && group.Spacing == 2 && ((TextBlock)group.Children[1]).Text == "+", "Shortcut separators differ.");
        Require(Single.FontFamily.Source == "Consolas" && Single.FontSize == 11 && Single.FontWeight.Weight == 500, "Keycap typography differs.");
        var expected = BleeckerTokens.Colors(scheme)[scheme == BleeckerColorScheme.Dark ? "textPrimary" : "textSecondary"];
        Require(Hex(Single.Foreground) == expected, "Keycap foreground differs from the theme.");
        keys.Add("K");
        Require(group.Children.Count == 5 && peer.GetName() == "⌘+K+K", "Observable key insertion or duplicate keys were lost.");
        keys.RemoveAt(2);
        Combo.Keys = new[] { "Alt" };
        keys.Add("detached");
        Require(peer.GetName() == "Alt" && group.Children.Count == 1, "Replaced key collection retained an observer or lost one-key group semantics.");
        Combo.Keys = Array.Empty<string>();
        Require(peer.GetName() == "Esc" && peer.GetAutomationControlType() == AutomationControlType.Text && Find<Grid>(Combo, "PART_Single").Visibility == Visibility.Visible,
            "Empty keys did not restore consumer content.");
        AutomationProperties.SetName(Combo, "Explicit shortcut");
        Combo.Keys = keys;
        Require(peer.GetName() == "Explicit shortcut", "Keys replaced the consumer accessibility name.");
        Combo.ClearValue(AutomationProperties.NameProperty);
        keys.Remove("detached");
        await VerifyCollectionLifecycleAsync();
        UpdateLayout();
        var measurements = new[] { Single, Combo, ThreeKeys, Empty, Rich }.Select(control => new
        {
            control.Name, control.ActualWidth, control.ActualHeight,
            font = control.FontFamily.Source, control.FontSize, weight = control.FontWeight.Weight,
            foreground = Hex(control.Foreground), background = Hex(control.Background),
            backgroundOpacity = control.Background.Opacity, border = Hex(control.BorderBrush), borderOpacity = control.BorderBrush.Opacity,
            children = Find<StackPanel>(control, "PART_Combination").Children.OfType<FrameworkElement>().Select(child => new
            {
                kind = child.GetType().Name, child.ActualWidth, child.ActualHeight,
                origin = child.TransformToVisual(control).TransformPoint(new Windows.Foundation.Point()),
                text = child is BleeckerKbd cap ? cap.Content as string : (child as TextBlock)?.Text
            })
        });
        await File.WriteAllTextAsync(Path.Combine(folder, $"keyboard-measurements-{scheme}.json"),
            System.Text.Json.JsonSerializer.Serialize(new { scheme, displayScale = XamlRoot.RasterizationScale,
                textScale = new Windows.UI.ViewManagement.UISettings().TextScaleFactor, measurements },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"keyboard-{scheme}.png"));
    }

    private async Task VerifyCollectionLifecycleAsync()
    {
        var panel = (StackPanel)Content;
        var position = panel.Children.IndexOf(Combo);
        var collection = new ObservedKeys("Ctrl", "S");
        try
        {
            Combo.Keys = collection;
            Require(collection.Subscribers == 1, "Loaded keyboard display did not subscribe exactly once.");
            var unloaded = false;
            void OnUnloaded(object sender, RoutedEventArgs args) => unloaded = true;
            Combo.Unloaded += OnUnloaded;
            try
            {
                panel.Children.Remove(Combo);
                // IsLoaded can clear before the queued Unloaded notification.
                // Observe the lifecycle event before checking its cleanup.
                await AwaitAsync(() => unloaded, "Keyboard display did not raise Unloaded.");
                Require(!Combo.IsLoaded && collection.Subscribers == 0,
                    "Unloaded keyboard display retained its collection subscription.");
            }
            finally { Combo.Unloaded -= OnUnloaded; }
            collection.Add("Shift");
            panel.Children.Insert(position, Combo);
            UpdateLayout();
            await AwaitAsync(() => Combo.IsLoaded && collection.Subscribers == 1, "Reloaded keyboard display did not restore one collection subscription.");
            Require(Find<StackPanel>(Combo, "PART_Combination").Children.Count == 5 &&
                FrameworkElementAutomationPeer.CreatePeerForElement(Combo).GetName() == "Ctrl+S+Shift",
                "Reloaded keyboard display did not render changes made while detached.");
            Combo.Keys = keys;
            Require(collection.Subscribers == 0, "Collection replacement retained the previous subscription.");
        }
        finally
        {
            if (!panel.Children.Contains(Combo)) panel.Children.Insert(position, Combo);
            Combo.Keys = keys;
        }
    }

    private async Task AwaitAsync(Func<bool> condition, string message)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(1)) { UpdateLayout(); await Task.Delay(16); }
        Require(condition(), message);
    }

    private sealed class ObservedKeys(params string[] initial) : IEnumerable<string>, System.Collections.Specialized.INotifyCollectionChanged
    {
        private readonly List<string> values = [.. initial];
        private System.Collections.Specialized.NotifyCollectionChangedEventHandler? changed;
        public int Subscribers { get; private set; }
        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add { changed += value; Subscribers++; }
            remove { changed -= value; Subscribers--; }
        }
        public void Add(string value)
        {
            values.Add(value);
            changed?.Invoke(this, new(System.Collections.Specialized.NotifyCollectionChangedAction.Add, value, values.Count - 1));
        }
        public IEnumerator<string> GetEnumerator() => values.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static string Hex(Brush brush)
    {
        var color = ((SolidColorBrush)brush).Color;
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
    private static T Find<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent is T match && match.Name == name) return match;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            try { return Find<T>(VisualTreeHelper.GetChild(parent, index), name); }
            catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException($"Missing keyboard template part {name}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
