using System.Diagnostics;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerSonner
{
    private sealed class ToastEntry : IDisposable
    {
        private readonly BleeckerSonner owner;
        private readonly BleeckerSonnerToast original;
        private readonly BleeckerSonnerLifetime state = new();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly DispatcherQueueTimer timeout, removal;
        private readonly TranslateTransform shift = new() { Y = 8 };
        private readonly ToastBody body;
        private readonly EntryCommand dismissCommand, actionCommand;
        private readonly double? duration;
        private Storyboard? motion;
        private bool disposed;
        internal string Id { get; }
        internal BleeckerOverlaySurface Surface { get; }
        private double Now => clock.Elapsed.TotalMilliseconds;

        internal ToastEntry(BleeckerSonner owner, string id, BleeckerSonnerToast original, double? duration)
        {
            this.owner = owner; this.original = original; this.duration = duration; Id = id;
            dismissCommand = new(this, false); actionCommand = new(this, true);
            timeout = owner.DispatcherQueue.CreateTimer(); timeout.IsRepeating = false;
            removal = owner.DispatcherQueue.CreateTimer(); removal.IsRepeating = false;
            timeout.Tick += Timeout; removal.Tick += Removal;
            Surface = new BleeckerOverlaySurface { Name = "SonnerToastSurface", Tag = original, Opacity = 0, RenderTransform = shift, FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
            AutomationProperties.SetAutomationId(Surface, id); AutomationProperties.SetAccessibilityView(Surface, AccessibilityView.Raw);
            body = new ToastBody(this); Surface.Content = body;
            if (original.Action is not null) original.Action.Command.CanExecuteChanged += OriginalCommandChanged;
            Surface.Loaded += Loaded;
            Refresh();
        }
        private void Loaded(object sender, RoutedEventArgs args) { body.ConfigureAction(); Notify(); }
        internal void Start()
        {
            if (duration is not double value || !(value > 0)) return;
            // HTML's setTimeout takes a signed 32-bit delay. Non-finite positive
            // values coerce to zero, while the source's earlier truthiness/
            // <=0 test makes null, NaN, zero and negative values persistent.
            var delay = double.IsFinite(value) ? Math.Truncate(value) % 4294967296d : 0;
            if (delay >= 2147483648d) delay -= 4294967296d;
            timeout.Interval = TimeSpan.FromMilliseconds(Math.Max(0, delay)); timeout.Start();
        }
        internal void Frame()
        {
            if (disposed) return; state.Frame(Now);
            if (!state.IsMounted) { owner.RemoveEntry(this); return; }
            Animate(); FrameworkElementAutomationPeer.CreatePeerForElement(body)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
        private void Timeout(DispatcherQueueTimer sender, object args) => BeginDismiss();
        private void BeginDismiss()
        { if (disposed) return; state.Dismiss(Now); if (!state.IsMounted) { owner.RemoveEntry(this); return; } Animate(); ScheduleRemoval(); }
        private void ScheduleRemoval()
        { if (state.RemoveAt is not double deadline || disposed) return; removal.Stop(); removal.Interval = TimeSpan.FromMilliseconds(Math.Max(0, deadline - Now)); removal.Start(); }
        private void Removal(DispatcherQueueTimer sender, object args)
        { if (disposed) return; state.AdvanceTo(Now); if (!state.IsMounted) owner.RemoveEntry(this); else ScheduleRemoval(); }
        private void Animate()
        {
            var opacity = Surface.Opacity; var y = shift.Y; motion?.Stop(); motion = null;
            var targetOpacity = state.IsVisible ? 1d : 0; var targetY = state.IsVisible ? 0d : 8 * owner.RemScale;
            Surface.Opacity = opacity; shift.Y = y;
            if (!owner.MotionEnabled) { Surface.Opacity = targetOpacity; shift.Y = targetY; return; }
            motion = new Storyboard();
            Add(Surface, "Opacity", opacity, targetOpacity); Add(shift, "Y", y, targetY); motion.Begin();
            void Add(DependencyObject target, string property, double initial, double final)
            {
                var animation = new DoubleAnimationUsingKeyFrames { EnableDependentAnimation = true };
                animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = initial });
                animation.KeyFrames.Add(new SplineDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)), Value = final, KeySpline = new KeySpline { ControlPoint1 = new Point(.4, 0), ControlPoint2 = new Point(.2, 1) } });
                Storyboard.SetTarget(animation, target); Storyboard.SetTargetProperty(animation, property); motion.Children.Add(animation);
            }
        }
        internal void Refresh()
        {
            Surface.Style = owner.SurfaceStyle; Surface.RequestedTheme = owner.ActualTheme; Surface.Padding = new Thickness(16 * owner.RemScale);
            body.Refresh(); Surface.InvalidateMeasure();
        }
        internal void Paint() => body.Paint();
        private bool Allowed => !disposed && state.CanInvokeAction && Surface.IsLoaded && owner.InputAllowed;
        internal void Notify() { dismissCommand.Notify(); actionCommand.Notify(); }
        private void OriginalCommandChanged(object? sender, EventArgs args)
        {
            if (owner.DispatcherQueue.HasThreadAccess) { if (!disposed) Notify(); }
            else if (!owner.DispatcherQueue.TryEnqueue(() => { if (!disposed) Notify(); })) throw new InvalidOperationException("The Sonner action dispatcher rejected a command update.");
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true; state.Remove(); timeout.Stop(); removal.Stop(); timeout.Tick -= Timeout; removal.Tick -= Removal; motion?.Stop(); motion = null;
            if (original.Action is not null) original.Action.Command.CanExecuteChanged -= OriginalCommandChanged;
            Surface.Loaded -= Loaded; Notify();
        }
        private sealed class EntryCommand(ToastEntry entry, bool action) : ICommand
        {
            public event EventHandler? CanExecuteChanged;
            public bool CanExecute(object? parameter) => entry.Allowed && (!action || entry.original.Action is { } value && (entry.body.ActionElement is not BleeckerButton wrapper || wrapper.IsEnabled) && value.Command.CanExecute(value.CommandParameter));
            public void Execute(object? parameter)
            {
                if (!CanExecute(parameter)) return;
                var native = action ? entry.body.NativeAction : entry.body.Close;
                if (native is not null && !native.IsEnabled) return;
                if (action) { var value = entry.original.Action!; value.Command.Execute(value.CommandParameter); }
                else entry.BeginDismiss();
            }
            internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        private sealed class ToastBody : Panel
        {
            private readonly ToastEntry entry;
            private readonly BleeckerContentText title = new("SonnerTitle", "BleeckerPrimaryMediumFontFamily", 14, 20, "BleeckerTextPrimaryBrush", true), description = new("SonnerDescription", "BleeckerPrimaryFontFamily", 12, 16, "BleeckerTextSecondaryBrush");
            private readonly FrameworkElement icon;
            private readonly BleeckerContentText? actionText;
            private readonly FrameworkElement? action;
            private double titleHeight, descriptionHeight, actionHeight, textWidth, textX;
            internal Button Close { get; }
            internal Control? ActionElement => action as Control;
            internal Button? NativeAction { get; private set; }
            internal ToastBody(ToastEntry entry)
            {
                this.entry = entry; Name = "SonnerToastBody"; Tag = entry.original; FlowDirection = FlowDirection.LeftToRight; UseLayoutRounding = false;
                icon = Icon(entry.original.Variant, false); icon.Name = "SonnerVariantIcon";
                Close = new Button { Name = "SonnerDismiss", Content = Icon(BleeckerSonnerVariant.Default, true), Command = entry.dismissCommand, UseLayoutRounding = false };
                AutomationProperties.SetName(Close, "Dismiss");
                Children.Add(icon); Children.Add(title); Children.Add(description); Children.Add(Close);
                title.Set(entry.original.Title, null); description.Set(entry.original.Description, null);
                if (entry.original.Action is { } original)
                {
                    actionText = new("SonnerActionContent", "BleeckerPrimaryMediumFontFamily", 12, 16, "BleeckerTextPrimaryBrush", true); actionText.Set(original.Label, null);
                    if (original.Variant == BleeckerButtonVariant.Link) action = new Button { Name = "SonnerLinkAction", Content = actionText, Command = entry.actionCommand, Tag = original, UseLayoutRounding = false };
                    else action = new BleeckerButton { Name = "SonnerButtonAction", Content = actionText, Variant = original.Variant, Size = BleeckerButtonSize.Small, Command = entry.actionCommand, Tag = original, UseLayoutRounding = false };
                    AutomationProperties.SetName(action, original.Label); Children.Add(action);
                    action.Loaded += (_, _) => { ConfigureAction(); entry.Notify(); };
                }
                AutomationProperties.SetName(this, entry.original.Title + (string.IsNullOrEmpty(entry.original.Description) ? "" : " " + entry.original.Description));
                AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Assertive);
                Close.Loaded += (_, _) => { Close.ApplyTemplate(); var presenter = Descendants(Close).OfType<ContentPresenter>().FirstOrDefault(); if (presenter is not null) BindInk((FrameworkElement)Close.Content, presenter); };
            }
            private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
            { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); yield return child; foreach (var descendant in Descendants(child)) yield return descendant; } }
            private static void BindInk(FrameworkElement root, object source)
            { foreach (var path in Descendants(root).OfType<Microsoft.UI.Xaml.Shapes.Shape>()) path.SetBinding(Microsoft.UI.Xaml.Shapes.Shape.StrokeProperty, new Binding { Source = source, Path = new PropertyPath("Foreground") }); }
            internal void ConfigureAction()
            {
                if (action is null || actionText is null || !action.IsLoaded) return;
                if (action is BleeckerButton button)
                {
                    button.ApplyTemplate(); NativeAction = Descendants(button).OfType<Button>().FirstOrDefault();
                    if (NativeAction is not null) { NativeAction.MinWidth = 0; NativeAction.MinHeight = 36 * entry.owner.RemScale; NativeAction.Padding = new Thickness(14 * entry.owner.RemScale, 4 * entry.owner.RemScale, 14 * entry.owner.RemScale, 4 * entry.owner.RemScale); NativeAction.FontSize = 12 * entry.owner.RemScale; }
                }
                else NativeAction = (Button)action;
                if (NativeAction is not null) { var presenter = Descendants(NativeAction).OfType<ContentPresenter>().FirstOrDefault(); if (presenter is not null) actionText.Text.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = presenter, Path = new PropertyPath("Foreground") }); }
            }
            internal void Refresh()
            {
                var owner = entry.owner; var factor = owner.RemScale;
                Close.Style = owner.DismissButtonStyle; Close.Padding = new Thickness(2 * factor); Close.CornerRadius = new CornerRadius(6 * factor);
                if (action is Button link) link.Style = owner.LinkActionStyle; else if (action is BleeckerButton button) button.Style = owner.ButtonActionStyle;
                foreach (var text in new[] { title, description, actionText }.OfType<BleeckerContentText>()) { text.Language = owner.Language; text.FlowDirection = owner.FlowDirection; text.Text.FlowDirection = owner.FlowDirection; text.Text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; }
                title.Text.FontSize = 14 * factor; title.Text.LineHeight = 20 * factor; description.Text.FontSize = 12 * factor; description.Text.LineHeight = 16 * factor;
                if (actionText is not null) { actionText.Text.FontSize = 12 * factor; actionText.Text.LineHeight = 16 * factor; }
                Paint(); ConfigureAction(); InvalidateMeasure();
            }
            internal void Paint()
            { title.Text.Foreground = entry.owner.Ink("BleeckerTextPrimaryBrush"); description.Text.Foreground = entry.owner.Ink("BleeckerTextSecondaryBrush"); }
            protected override Size MeasureOverride(Size available)
            {
                var factor = entry.owner.RemScale; Close.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); icon.Measure(new Size(18, 18));
                textWidth = Math.Max(0, available.Width - 18 - Close.DesiredSize.Width - 24 * factor);
                title.Measure(new Size(textWidth, double.PositiveInfinity)); titleHeight = title.DesiredSize.Height;
                descriptionHeight = 0; if (!string.IsNullOrEmpty(entry.original.Description)) { description.Measure(new Size(textWidth, double.PositiveInfinity)); descriptionHeight = description.DesiredSize.Height; }
                actionHeight = 0; if (action is not null) { action.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); actionHeight = action.DesiredSize.Height; }
                var textHeight = titleHeight + (descriptionHeight > 0 ? 2 * factor + descriptionHeight : 0) + (action is not null ? 8 * factor + actionHeight + (entry.original.Action?.Variant == BleeckerButtonVariant.Link ? 2 * factor : 0) : 0);
                return new Size(available.Width, Math.Max(Math.Max(18 + 2 * factor, Close.DesiredSize.Height), textHeight));
            }
            protected override Size ArrangeOverride(Size final)
            {
                var factor = entry.owner.RemScale; var rtl = entry.owner.FlowDirection == FlowDirection.RightToLeft;
                icon.Arrange(new Rect(rtl ? final.Width - 18 : 0, 2 * factor, 18, 18)); Close.Arrange(new Rect(rtl ? 0 : final.Width - Close.DesiredSize.Width, 0, Close.DesiredSize.Width, Close.DesiredSize.Height));
                textX = rtl ? Close.DesiredSize.Width + 12 * factor : 18 + 12 * factor;
                title.Arrange(new Rect(textX, 0, textWidth, titleHeight)); var y = titleHeight;
                if (descriptionHeight > 0) { y += 2 * factor; description.Arrange(new Rect(textX, y, textWidth, descriptionHeight)); y += descriptionHeight; } else description.Arrange(new Rect(textX, y, textWidth, 0));
                if (action is not null) action.Arrange(new Rect(textX + (rtl ? textWidth - action.DesiredSize.Width : 0), y + 8 * factor, action.DesiredSize.Width, actionHeight));
                return final;
            }
            protected override AutomationPeer OnCreateAutomationPeer() => new ToastPeer(this);
            private sealed class ToastPeer(ToastBody owner) : FrameworkElementAutomationPeer(owner)
            { protected override string GetClassNameCore() => "BleeckerSonnerToast"; protected override string GetLocalizedControlTypeCore() => "alert"; protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group; }
            private static FrameworkElement Icon(BleeckerSonnerVariant variant, bool close)
            {
                var brush = variant switch { BleeckerSonnerVariant.Default => "BleeckerTextSecondaryBrush", BleeckerSonnerVariant.Success => "BleeckerAlertSuccessIconBrush", BleeckerSonnerVariant.Error => "BleeckerAlertErrorIconBrush", BleeckerSonnerVariant.Warning => "BleeckerAlertWarningIconBrush", _ => "BleeckerAlertInfoIconBrush" };
                var data = close ? "M18 6 6 18 M6 6 18 18" : variant switch
                {
                    BleeckerSonnerVariant.Success => "M21.801 10A10 10 0 1 1 17 3.335 M9 11 12 14 22 4",
                    BleeckerSonnerVariant.Warning => "M21.73 18 13.73 4A2 2 0 0 0 10.25 4L2.25 18A2 2 0 0 0 4 21H20A2 2 0 0 0 21.73 18 M12 9V13 M12 17H12.01",
                    BleeckerSonnerVariant.Error => "M12 2A10 10 0 1 1 12 22A10 10 0 1 1 12 2 M12 8V12 M12 16H12.01",
                    _ => "M12 2A10 10 0 1 1 12 22A10 10 0 1 1 12 2 M12 16V12 M12 8H12.01"
                };
                return (FrameworkElement)XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='" + (close ? "14" : "18") + "' Height='" + (close ? "14" : "18") + "' AutomationProperties.AccessibilityView='Raw'><Canvas Width='24' Height='24'><Path Data='" + data + "' Stroke='{ThemeResource " + brush + "}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox>");
            }
        }
    }
}
