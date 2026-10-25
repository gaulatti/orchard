using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Orchard;

internal sealed partial class BleeckerSelectSurface
{
    private ScrollViewer? popupScroll;
    private Control? popupFocus;
    private Border? scrollUp, scrollDown;
    private DispatcherQueueTimer? scrollTimer;
    private bool scrollInitialized;
    private double lastScrollOffset;
    private int scrollDirection;
    private Border? scrollingIndicator;
    private Point scrollPointer;
    private double? controlledScrollOffset;
    private int optionsFocusVersion;

    internal Action? PreserveOptionsFocus(Func<BleeckerSelectItem, string> key)
    {
        // Radix keeps keyed option focus and the existing scroll position on
        // controlled updates, even when reordering moves that row offscreen.
        var version = ++optionsFocusVersion;
        if (!IsDropDownOpen || !CanTakeFocus || popupFocus is null || popupScroll is null) return null;
        var focused = FocusManager.GetFocusedElement(XamlRoot);
        var options = Items.OfType<BleeckerSelectItem>().ToArray();
        if (!ReferenceEquals(focused, popupFocus) && (focused is not BleeckerSelectItem item || !options.Contains(item))) return null;
        var original = focused as BleeckerSelectItem;
        var value = original is null ? null : key(original);
        var occurrence = original is null ? 0 : options.TakeWhile(item => !ReferenceEquals(item, original)).Count(item => key(item) == value);
        var state = ((Control)focused).FocusState;
        var offset = popupScroll.VerticalOffset;
        return () =>
        {
            if (!IsDropDownOpen || !IsLoaded || version != optionsFocusVersion) return;
            var next = original is null ? null : Items.OfType<BleeckerSelectItem>().Where(item => key(item) == value).Skip(occurrence).FirstOrDefault();
            var target = next is { IsEnabled: true } ? (Control)next : popupFocus;
            void Restore()
            {
                if (!IsDropDownOpen || !CanTakeFocus || version != optionsFocusVersion) return;
                if (next is not null && !Items.Contains(next)) return;
                popupRoot!.UpdateLayout();
                controlledScrollOffset = offset;
                if (!target.Focus(state)) throw new InvalidOperationException("The controlled options update could not retain its logical focus target.");
                QueueOpenPosition();
            }
            popupRoot!.UpdateLayout();
            if (target.IsLoaded) Restore();
            else if (!DispatcherQueue.TryEnqueue(Restore)) throw new InvalidOperationException("The controlled options update could not schedule its logical focus target.");
        };
    }

    internal void ClearControlledFocus()
    {
        optionsFocusVersion++;
        controlledScrollOffset = null;
    }

    internal Action? PreserveViewportFocus()
    {
        if (!IsDropDownOpen || !CanTakeFocus || popupFocus is null || !ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),popupFocus)) return null;
        var viewport = popupFocus; var state = viewport.FocusState;
        return () => { if (IsDropDownOpen && CanTakeFocus && viewport.IsLoaded && !ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),viewport) && !viewport.Focus(state)) throw new InvalidOperationException("The controlled options update could not preserve viewport focus."); };
    }


    private void AttachScrollChrome(BleeckerOverlaySurface surface, ScrollViewer scroll, Control focusHost)
    {
        popupScroll = scroll; popupFocus = focusHost; scrollInitialized = false;
        scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scroll.VerticalSnapPointsType = SnapPointsType.None;
        var column = new Grid { UseLayoutRounding = false };
        for (var row = 0; row < 3; row++) column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        surface.Content = null;
        scrollUp = ScrollIndicator("PART_ScrollUp", true); scrollDown = ScrollIndicator("PART_ScrollDown", false);
        Grid.SetRow(focusHost, 1); Grid.SetRow(scrollDown, 2);
        column.Children.Add(scrollUp); column.Children.Add(focusHost); column.Children.Add(scrollDown); surface.Content = column;
        scroll.ViewChanged += PopupViewChanged;
        scrollTimer = DispatcherQueue.CreateTimer(); scrollTimer.Interval = TimeSpan.FromMilliseconds(50); scrollTimer.Tick += ScrollTick;
    }

    private Border ScrollIndicator(string name, bool up)
    {
        var indicator = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Height='28' Background='{ThemeResource BleeckerCardBrush}' BorderBrush='{ThemeResource BleeckerPaginationScrollSeparatorBrush}' BorderThickness='" + (up ? "0,0,0,1" : "0,1,0,0") + "' UseLayoutRounding='False'><Viewbox Width='15' Height='15' HorizontalAlignment='Center' VerticalAlignment='Center' IsHitTestVisible='False'><Canvas Width='24' Height='24'><Path Data='" + (up ? "M18 15L12 9L6 15" : "M6 9L12 15L18 9") + "' Stroke='{ThemeResource BleeckerTextSecondaryBrush}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox></Border>");
        indicator.Name = name; indicator.Visibility = Visibility.Collapsed; AutomationProperties.SetAccessibilityView(indicator, AccessibilityView.Raw);
        indicator.PointerMoved += (_, args) => { if (!args.IsGenerated) BeginScroll(indicator, args, up ? -1 : 1); };
        indicator.PointerPressed += (_, args) => BeginScroll(indicator, args, up ? -1 : 1);
        indicator.PointerExited += (_, _) => StopScroll();
        return indicator;
    }

    private void InitializeScrollIndicators()
    {
        if (scrollInitialized || popupScroll is null || !IsDropDownOpen || (Items.Count > 0 && (popupScroll.ExtentHeight <= 0 || popupScroll.ViewportHeight <= 0))) return;
        popupScroll.UpdateLayout(); scrollInitialized = true; UpdateScrollIndicators();
    }

    private void PopupViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        // Radix updates these flags on actual viewport scroll events. An
        // extent-only options change preserves the currently mounted flags.
        if (!scrollInitialized) InitializeScrollIndicators();
        else if (popupScroll is not null && popupScroll.VerticalOffset != lastScrollOffset) UpdateScrollIndicators();
    }

    private void UpdateScrollIndicators()
    {
        var scroll = popupScroll ?? throw new InvalidOperationException("The opened chooser requires its viewport.");
        lastScrollOffset = scroll.VerticalOffset;
        scrollUp!.Visibility = scroll.VerticalOffset > 0 ? Visibility.Visible : Visibility.Collapsed;
        scrollDown!.Visibility = Math.Ceiling(scroll.VerticalOffset) < scroll.ExtentHeight - scroll.ViewportHeight ? Visibility.Visible : Visibility.Collapsed;
        popupRoot?.UpdateLayout();
        if ((scrollDirection < 0 && scrollUp.Visibility == Visibility.Collapsed) || (scrollDirection > 0 && scrollDown.Visibility == Visibility.Collapsed)) StopScroll();
        QueueOpenPosition();
    }

    private void BeginScroll(Border indicator, PointerRoutedEventArgs args, int direction)
    {
        if (!IsDropDownOpen || !IsEnabled || !CanTakeFocus || popupScroll is null) return;
        ClearControlledFocus();
        scrollingIndicator = indicator; scrollPointer = args.GetCurrentPoint(XamlRoot.Content).Position;
        if (!popupFocus!.Focus(FocusState.Pointer)) throw new InvalidOperationException("The source scroll control requires focus on its open viewport.");
        scrollDirection = direction;
        if (!scrollTimer!.IsRunning) scrollTimer.Start();
    }

    private void ScrollTick(DispatcherQueueTimer sender, object args)
    {
        if (!IsDropDownOpen || !IsEnabled || !InteractionAllowed || popupScroll is null || !popupScroll.IsLoaded) { StopScroll(); return; }
        // The browser sends pointer-leave when layout moves an indicator
        // away from a stationary pointer. Enforce the same real-event point
        // against the indicator's current painted allocation on Windows.
        if (scrollingIndicator is not { Visibility: Visibility.Visible } indicator || !indicator.TransformToVisual(XamlRoot.Content).TransformBounds(new Rect(0,0,indicator.ActualWidth,indicator.ActualHeight)).Contains(scrollPointer)) { StopScroll(); return; }
        // The source uses the selected item's actual height, or its first
        // enabled item when the controlled value has no matching option.
        var selected = SelectedItem as BleeckerSelectItem ?? Items.OfType<BleeckerSelectItem>().FirstOrDefault();
        if (selected is not null) popupScroll.ChangeView(null, Math.Clamp(popupScroll.VerticalOffset + scrollDirection * selected.ActualHeight, 0, popupScroll.ScrollableHeight), null, true);
    }

    private void StopScroll() { scrollTimer?.Stop(); scrollDirection = 0; scrollingIndicator = null; }
    private void DetachScrollChrome()
    {
        StopScroll();
        if (scrollTimer is not null) scrollTimer.Tick -= ScrollTick;
        if (popupScroll is not null) popupScroll.ViewChanged -= PopupViewChanged;
        scrollTimer = null; popupScroll = null; popupFocus = null; scrollUp = scrollDown = null; scrollInitialized = false;
    }
}
