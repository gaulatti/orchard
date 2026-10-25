using System.Numerics;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerAlertDialog
{
    private OverlayPanel? overlay;
    private Grid? backdrop;
    private Border? blurHost, tint, icon, footer;
    private FocusSurface? focusSurface;
    private BleeckerOverlaySurface? paint;
    private ScrollView? viewport;
    private BodyPanel? body;
    private PositionPanel? header, footerContent;
    private BleeckerContentText? titleText, descriptionText, cancelText, confirmText;
    private BleeckerButton? cancelButton, confirmButton;
    private Grid? confirmContent;
    private Viewbox? loader;
    private Microsoft.UI.Xaml.Shapes.Path? loaderPath;
    private readonly CompositeTransform surfaceTransform = new();
    private Storyboard? spinner;
    private RotateTransform? spinnerRotation;
    private BleeckerAlertDialogVariant? renderedVariant;
    private ResourceDictionary? palette;
    private Style? defaultSurfaceStyle;
    private BleeckerAlertDialogModel.Layout layout;
    private readonly List<IDisposable> effects = [];
    private SpriteVisual? blurVisual;

    private void MountPresentation()
    {
        if (registration is { IsAttached: true }) { if (!registration.Layer.Active) registration.Host.Activate(registration); return; }
        ClearPresentation(); palette = new() { Source = new Uri("ms-appx:///Orchard/Themes/AlertDialog.xaml") }; defaultSurfaceStyle = (Style)palette["BleeckerAlertDialogSurfaceStyle"];
        titleText = new("AlertDialogTitle", "BleeckerPrimaryMediumFontFamily", 20, 25, "BleeckerTextPrimaryBrush", medium: true) { ConstrainLayoutWidth = true };
        descriptionText = new("AlertDialogDescription", "BleeckerSecondaryFontFamily", 14, 22.75, "BleeckerTextSecondaryBrush") { ConstrainLayoutWidth = true };
        cancelText = new("AlertDialogCancelLabel", "BleeckerPrimaryMediumFontFamily", 13, 19.5, "BleeckerTextPrimaryBrush", medium: true) { NoWrap = true };
        confirmText = new("AlertDialogConfirmLabel", "BleeckerPrimaryMediumFontFamily", 13, 19.5, "BleeckerTextPrimaryBrush", medium: true) { NoWrap = true };
        cancelText.Text.ClearValue(TextBlock.ForegroundProperty); confirmText.Text.ClearValue(TextBlock.ForegroundProperty);
        header = new(this, true) { Name = "AlertDialogHeader", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        header.Children.Add(titleText); header.Children.Add(descriptionText);
        cancelButton = new() { Name = "AlertDialogCancel", Size = BleeckerButtonSize.Small, Variant = BleeckerButtonVariant.Secondary, Content = cancelText, UseLayoutRounding = false };
        confirmContent = new() { UseLayoutRounding = false }; confirmContent.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); confirmContent.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        loader = (Viewbox)XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='16' Height='16' AutomationProperties.AccessibilityView='Raw'><Canvas Width='24' Height='24'><Path x:Name='AlertDialogLoaderPath' Data='M21 12a9 9 0 1 1-6.219-8.56' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'><Path.RenderTransform><RotateTransform CenterX='12' CenterY='12'/></Path.RenderTransform></Path></Canvas></Viewbox>");
        loaderPath = (Microsoft.UI.Xaml.Shapes.Path)((Canvas)loader.Child).Children[0]; BleeckerButtonGlyphForeground.Attach(loaderPath); spinnerRotation = (RotateTransform)loaderPath.RenderTransform;
        confirmContent.Children.Add(loader); confirmContent.Children.Add(confirmText); Grid.SetColumn(confirmText, 1);
        confirmButton = new() { Name = "AlertDialogConfirm", Size = BleeckerButtonSize.Small, Content = confirmContent, UseLayoutRounding = false };
        cancelButton.Click += CancelClick; confirmButton.Click += ConfirmClick;
        footerContent = new(this, false) { UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight }; footerContent.Children.Add(cancelButton); footerContent.Children.Add(confirmButton);
        footer = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' BorderBrush='{ThemeResource AlertDialogFooterBorderBrush}' BorderThickness='0,1,0,0' UseLayoutRounding='False'/>"); footer.Name = "AlertDialogFooter"; footer.Child = footerContent;
        body = new(this) { Name = "AlertDialogInner", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight }; body.Children.Add(header); body.Children.Add(footer);
        viewport = new() { Name = "AlertDialogViewport", IsTabStop = false, UseLayoutRounding = false, Content = body, ContentOrientation = ScrollingContentOrientation.Vertical, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Top, HorizontalScrollMode = ScrollingScrollMode.Disabled, HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden, VerticalScrollMode = ScrollingScrollMode.Enabled, VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Auto, ZoomMode = ScrollingZoomMode.Disabled, Style = CreateViewportStyle() };
        paint = new() { Name = "AlertDialogPaintSurface", Content = viewport, Style = SurfaceStyle ?? defaultSurfaceStyle, UseLayoutRounding = false };
        focusSurface = new(this) { Name = "AlertDialogSurface", Content = paint, IsTabStop = true, UseSystemFocusVisuals = false, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch, UseLayoutRounding = false, RenderTransform = surfaceTransform, RenderTransformOrigin = new Point(.5, .5), TabFocusNavigation = KeyboardNavigationMode.Cycle };
        AutomationProperties.SetIsDialog(focusSurface, true); AutomationProperties.SetLabeledBy(focusSurface, titleText.Text);
        backdrop = new() { Name = "AlertDialogBackdrop", Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), UseLayoutRounding = false };
        blurHost = new(); backdrop.Children.Add(blurHost);
        tint = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource AlertDialogBackdropBrush}'/>"); backdrop.Children.Add(tint); backdrop.Tapped += BackdropTapped;
        overlay = new(this) { Name = "AlertDialogOverlay", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight }; overlay.Children.Add(backdrop); overlay.Children.Add(focusSurface);
        RenderContent(); var host = BleeckerModalHost.Require(this); registration = host.Attach(this, focusSurface, overlay, Cancel); host.Activate(registration); UpdateSpinner();
    }
    private void CancelClick(object sender, RoutedEventArgs args) => Cancel();
    private void ConfirmClick(object sender, RoutedEventArgs args) => Confirm();
    private void BackdropTapped(object sender, TappedRoutedEventArgs args) { Cancel(); args.Handled = true; }
    private void RenderContent()
    {
        if (paint is null || titleText is null || descriptionText is null || cancelText is null || confirmText is null || cancelButton is null || confirmButton is null || loader is null || confirmContent is null || header is null || focusSurface is null || viewport is null) return;
        if (!ReferenceEquals(paint.Style, SurfaceStyle ?? defaultSurfaceStyle)) paint.Style = SurfaceStyle ?? defaultSurfaceStyle;
        titleText.Set(Title, null); descriptionText.Set(Description, null); cancelText.Set(CancelLabel, null); confirmText.Set(ConfirmLabel, null);
        descriptionText.Visibility = string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;
        foreach (var slot in new[] { titleText, descriptionText, cancelText, confirmText }) { slot.FlowDirection = slot.Text.FlowDirection = FlowDirection; slot.Text.Language = Language; slot.Text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; }
        viewport.FlowDirection = paint.FlowDirection = cancelButton.FlowDirection = confirmButton.FlowDirection = confirmContent.FlowDirection = FlowDirection;
        focusSurface.IsEnabled = IsEnabled;
        confirmButton.Variant = Variant == BleeckerAlertDialogVariant.Destructive ? BleeckerButtonVariant.Destructive : BleeckerButtonVariant.Primary;
        cancelButton.IsEnabled = IsEnabled && !ConfirmLoading && CancelCommand?.CanExecute(CancelCommandParameter) != false;
        confirmButton.IsEnabled = IsEnabled && !ConfirmLoading && ConfirmCommand?.CanExecute(ConfirmCommandParameter) != false;
        loader.Visibility = ConfirmLoading ? Visibility.Visible : Visibility.Collapsed; confirmContent.ColumnSpacing = ConfirmLoading && ConfirmLabel.Length > 0 ? 8 : 0;
        AutomationProperties.SetName(cancelButton, CancelLabel); AutomationProperties.SetName(confirmButton, ConfirmLabel); AutomationProperties.SetName(focusSurface, Title ?? "");
        AutomationProperties.SetFullDescription(focusSurface, Description ?? ""); var described = AutomationProperties.GetDescribedBy(focusSurface); described.Clear(); if (!string.IsNullOrEmpty(Description)) described.Add(descriptionText.Text);
        if (renderedVariant != Variant)
        {
            if (icon is not null) header.Children.Remove(icon); var prefix = Variant == BleeckerAlertDialogVariant.Destructive ? "Destructive" : "Default";
            icon = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource AlertDialog" + prefix + "IconBackgroundBrush}' BorderBrush='{ThemeResource AlertDialog" + prefix + "IconBorderBrush}' BorderThickness='1' CornerRadius='9' AutomationProperties.AccessibilityView='Raw'><Viewbox Width='20' Height='20'><Canvas Width='24' Height='24'><Path Data='M21.73 18a2 2 0 0 1-1.73 3H4a2 2 0 0 1-1.73-3l8-14a2 2 0 0 1 3.46 0z M12 9v4 M12 17h.01' Stroke='{ThemeResource AlertDialog" + prefix + "IconForegroundBrush}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox></Border>"); icon.Name = "AlertDialogIcon"; header.Children.Add(icon); renderedVariant = Variant;
        }
        UpdateSpinner(); overlay?.InvalidateMeasure();
    }
    private Size MeasurePresentation(Size available)
    {
        if (titleText is null || descriptionText is null || cancelText is null || confirmText is null || paint is null || focusSurface is null || body is null) return available;
        cancelText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); confirmText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var cancelWidth = cancelText.NaturalWidth + 30; var confirmWidth = confirmText.NaturalWidth + 30 + (ConfirmLoading ? 16 + (ConfirmLabel.Length > 0 ? 8 : 0) : 0);
        var maximumWidth = Math.Min(paint.MaxWidth, Math.Max(2, available.Width - 32));
        var next = BleeckerAlertDialogModel.Measure(available.Width, available.Height, width => { titleText.Measure(new Size(width, double.PositiveInfinity)); descriptionText.Measure(new Size(width, double.PositiveInfinity)); return new(titleText.NaturalHeight, descriptionText.NaturalHeight); }, cancelWidth, confirmWidth, !string.IsNullOrEmpty(Description), FlowDirection == FlowDirection.RightToLeft, maximumWidth);
        if (layout != next) { layout = next; body.InvalidateMeasure(); header?.InvalidateMeasure(); footerContent?.InvalidateMeasure(); }
        focusSurface.Measure(new Size(layout.Surface.Width, layout.Surface.Height)); UpdateBlur(available); return available;
    }
    private void ApplyMotion(BleeckerAlertDialogModel.Presentation presentation)
    {
        if (focusSurface is null || backdrop is null) return; focusSurface.Opacity = presentation.Opacity; backdrop.Opacity = presentation.BackdropOpacity; surfaceTransform.ScaleX = surfaceTransform.ScaleY = presentation.Scale; surfaceTransform.TranslateY = presentation.TranslateY;
    }
    private void UpdateSpinner()
    {
        if (spinnerRotation is null) return;
        if (!IsLoaded || !ConfirmLoading || !settings.AnimationsEnabled) { spinner?.Stop(); spinner = null; spinnerRotation.Angle = 0; return; }
        if (spinner is not null) return; var turn = new DoubleAnimation { From = 0, To = 360, Duration = TimeSpan.FromSeconds(1), RepeatBehavior = RepeatBehavior.Forever }; Storyboard.SetTarget(turn, spinnerRotation); Storyboard.SetTargetProperty(turn, "Angle"); spinner = new(); spinner.Children.Add(turn); spinner.Begin();
    }
    private void UpdateBlur(Size size)
    {
        if (blurHost is null) return;
        if (blurVisual is null)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(blurHost).Compositor; var source = compositor.CreateBackdropBrush();
            using var blur = new GaussianBlurEffect { BlurAmount = 2, BorderMode = EffectBorderMode.Hard, Optimization = EffectOptimization.Quality, Source = new CompositionEffectSourceParameter("backdrop") };
            var factory = compositor.CreateEffectFactory(blur); var brush = factory.CreateBrush(); brush.SetSourceParameter("backdrop", source); blurVisual = compositor.CreateSpriteVisual(); blurVisual.Brush = brush; effects.Add(source); effects.Add(factory); effects.Add(brush); effects.Add(blurVisual); ElementCompositionPreview.SetElementChildVisual(blurHost, blurVisual);
        }
        blurVisual.Size = new Vector2((float)size.Width, (float)size.Height);
    }
    private void ClearPresentation()
    {
        spinner?.Stop(); spinner = null; spinnerRotation = null;
        if (backdrop is not null) backdrop.Tapped -= BackdropTapped; if (blurHost is not null) ElementCompositionPreview.SetElementChildVisual(blurHost, null);
        if (cancelButton is not null) cancelButton.Click -= CancelClick; if (confirmButton is not null) confirmButton.Click -= ConfirmClick;
        for (var i = effects.Count - 1; i >= 0; i--) effects[i].Dispose(); effects.Clear(); blurVisual = null;
        if (viewport is not null) viewport.Content = null; if (paint is not null) paint.Content = null; if (focusSurface is not null) focusSurface.Content = null;
        overlay?.Children.Clear(); overlay = null; backdrop = null; blurHost = null; tint = null; focusSurface = null; paint = null; viewport = null; body = null; header = null; footer = null; footerContent = null; icon = null; titleText = descriptionText = cancelText = confirmText = null; cancelButton = confirmButton = null; confirmContent = null; loader = null; loaderPath = null; palette = null; defaultSurfaceStyle = null; renderedVariant = null;
    }
    private sealed partial class OverlayPanel(BleeckerAlertDialog owner) : Panel
    {
        protected override Size MeasureOverride(Size availableSize) => owner.MeasurePresentation(availableSize);
        protected override Size ArrangeOverride(Size finalSize)
        {
            owner.backdrop?.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height)); var box = owner.layout.Surface; owner.focusSurface?.Arrange(new Rect(box.X, box.Y, box.Width, box.Height)); return finalSize;
        }
    }
    private sealed partial class BodyPanel(BleeckerAlertDialog owner) : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            var l = owner.layout; owner.header?.Measure(new Size(l.Header.Width, l.Header.Height)); owner.footer?.Measure(new Size(l.Footer.Width, l.Footer.Height)); return new(l.ViewportWidth, l.ExtentHeight);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var l = owner.layout;
            static void Place(FrameworkElement? element, BleeckerAlertDialogModel.Rect box, double x, double y) { if (element is null) return; element.Measure(new Size(box.Width, box.Height)); element.Arrange(new Rect(box.X - x, box.Y - y, box.Width, box.Height)); }
            Place(owner.header, l.Header, l.Inner.X, l.Inner.Y); Place(owner.footer, l.Footer, l.Inner.X, l.Inner.Y);
            return finalSize;
        }
    }
    private sealed partial class PositionPanel(BleeckerAlertDialog owner, bool isHeader) : Panel
    {
        private IEnumerable<(FrameworkElement? Element, BleeckerAlertDialogModel.Rect Box)> Boxes()
        {
            var l = owner.layout;
            if (isHeader) { yield return (owner.icon, l.Icon); yield return (owner.titleText, l.Title); if (l.Description is { } description) yield return (owner.descriptionText, description); }
            else { yield return (owner.cancelButton, l.Cancel); yield return (owner.confirmButton, l.Confirm); }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var (element, box) in Boxes()) element?.Measure(new Size(box.Width, box.Height)); var l = owner.layout; return new(isHeader ? l.Header.Width : l.Footer.Width, isHeader ? l.Header.Height : l.Footer.Height - 1);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var l = owner.layout; var x = isHeader ? l.Header.X : l.Footer.X; var y = isHeader ? l.Header.Y : l.Footer.Y + 1;
            foreach (var (element, box) in Boxes()) element?.Arrange(new Rect(box.X - x, box.Y - y, box.Width, box.Height)); return finalSize;
        }
    }
    private sealed class FocusSurface(BleeckerAlertDialog owner) : ContentControl
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new DialogPeer(this, owner);
    }
    private sealed class DialogPeer(FocusSurface surface, BleeckerAlertDialog owner) : FrameworkElementAutomationPeer(surface), IWindowProvider
    {
        protected override string GetClassNameCore() => nameof(BleeckerAlertDialog);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;
        protected override bool IsDialogCore() => true;
        protected override string GetNameCore() => owner.Title ?? "";
        protected override object GetPatternCore(PatternInterface patternInterface) => patternInterface == PatternInterface.Window ? this : base.GetPatternCore(patternInterface);
        public bool IsModal => true;
        public bool IsTopmost => ReferenceEquals(owner.registration?.Host.ActiveSurface, surface);
        public bool Maximizable => false;
        public bool Minimizable => false;
        public WindowInteractionState InteractionState => owner.IsOpen ? WindowInteractionState.ReadyForUserInteraction : WindowInteractionState.Closing;
        public WindowVisualState VisualState => WindowVisualState.Normal;
        public void Close() => owner.Cancel();
        public void SetVisualState(WindowVisualState state) { if (state != WindowVisualState.Normal) throw new InvalidOperationException("AlertDialog cannot be minimized or maximized."); }
        public bool WaitForInputIdle(int milliseconds) { if (milliseconds < 0) throw new ArgumentOutOfRangeException(nameof(milliseconds)); return owner.IsOpen; }
    }
    private static Style CreateViewportStyle() => BleeckerModalViewport.CreateStyle();

}

