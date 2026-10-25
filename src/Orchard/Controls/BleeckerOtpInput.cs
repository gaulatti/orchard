using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed record BleeckerOtpValueRequestedEventArgs(string Value);

/// <summary>Controlled digit entry using native text and password editors; the caller accepts value requests.</summary>
public sealed class BleeckerOtpInput : Control
{
    private readonly ResourceDictionary resources = new() { Source = new Uri("ms-appx:///Orchard/Themes/OtpInput.xaml") };
    private readonly List<Slot> slots = [];
    private readonly Dictionary<string, TextBlock> palette = [];
    private readonly UISettings settings = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private OtpPanel? panel;
    private bool rendering, painting;
    private int revision;

    public BleeckerOtpInput()
    {
        DefaultStyleKey = typeof(BleeckerOtpInput); IsTabStop = false;
        Loaded += (_, _) => { if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += MotionChanged; Render(false); };
        Unloaded += (_, _) => { revision++; if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= MotionChanged; StopPainting(); };
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { revision++; Render(false); });
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => Render(false));
    }
    private static DependencyProperty Property(string name, Type type, object value) => DependencyProperty.Register(name, type, typeof(BleeckerOtpInput), new PropertyMetadata(value, (owner, _) =>
    {
        var input = (BleeckerOtpInput)owner; BleeckerOtpModel.Validate(input.Value, input.Length);
        if (!double.IsFinite(input.Spacing) || input.Spacing < 0) throw new ArgumentOutOfRangeException(nameof(Spacing));
        input.revision++; input.Render();
    }));
    public static readonly DependencyProperty ValueProperty = Property(nameof(Value), typeof(string), "");
    public static readonly DependencyProperty LengthProperty = Property(nameof(Length), typeof(int), 6);
    public static readonly DependencyProperty IsSecureProperty = Property(nameof(IsSecure), typeof(bool), false);
    public static readonly DependencyProperty SpacingProperty = Property(nameof(Spacing), typeof(double), 8d);
    public string Value { get => (string)GetValue(ValueProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(ValueProperty, value); } }
    public int Length { get => (int)GetValue(LengthProperty); set { ArgumentOutOfRangeException.ThrowIfNegative(value); SetValue(LengthProperty, value); } }
    public bool IsSecure { get => (bool)GetValue(IsSecureProperty); set => SetValue(IsSecureProperty, value); }
    public double Spacing { get => (double)GetValue(SpacingProperty); set { if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(SpacingProperty, value); } }
    public event EventHandler<BleeckerOtpValueRequestedEventArgs>? ValueRequested;
    protected override AutomationPeer OnCreateAutomationPeer() => new GroupPeer(this);
    private sealed class GroupPeer(BleeckerOtpInput owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerOtpInput);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate();
        var host = GetTemplateChild("PART_Items") as Grid ?? throw new InvalidOperationException("OTP Input requires PART_Items.");
        foreach (var name in new[] { "White", "Sand", "Sea", "DeepSea", "Text" }) palette[name] = GetTemplateChild("Otp" + name + "Palette") as TextBlock ?? throw new InvalidOperationException("OTP Input requires its theme palettes.");
        panel = new OtpPanel(this) { UseLayoutRounding = false }; host.Children.Add(panel); Render(false);
    }
    private void Detach()
    {
        revision++; StopPainting(); foreach (var slot in slots) slot.Dispose(); slots.Clear(); panel?.Children.Clear(); panel = null; palette.Clear();
    }
    private static Binding Bind(object source, string property) => new() { Source = source, Path = new PropertyPath(property), Mode = BindingMode.OneWay };
    private bool Current(Slot slot) => IsLoaded && slot.IsLoaded && panel?.Children.Contains(slot) == true && slot.Index < slots.Count && ReferenceEquals(slots[slot.Index], slot);
    private void Render(bool animate = true)
    {
        if (panel is null || rendering) return;
        rendering = true;
        try
        {
            while (slots.Count > Length) { var slot = slots[^1]; slots.RemoveAt(slots.Count - 1); panel.Children.Remove(slot); slot.Dispose(); }
            while (slots.Count < Length) { var slot = new Slot(this, slots.Count); slots.Add(slot); panel.Children.Add(slot); }
            foreach (var slot in slots)
            {
                slot.SetSecure(IsSecure); slot.Editor.IsEnabled = IsEnabled;
                slot.SetText(BleeckerOtpModel.Display(Value, Length, slot.Index));
                slot.Opacity = IsEnabled ? 1 : .5;
                AutomationProperties.SetSizeOfSet(slot.Editor, Length); Paint(slot, animate);
            }
            panel.InvalidateMeasure();
        }
        finally { rendering = false; }
    }
    private void Request(Slot slot, BleeckerOtpModel.Edit edit)
    {
        if (!Current(slot)) return;
        if (!IsEnabled || !slot.Editor.IsEnabled) { RestoreAfterEdit(false); return; }
        revision++;
        try { if (edit.RequestedValue is { } value) ValueRequested?.Invoke(this, new(value)); }
        finally
        {
            RestoreAfterEdit(true);
        }
        if (edit.FocusIndex is { } target && target >= 0 && target < slots.Count && IsLoaded && IsEnabled && XamlRoot?.IsHostVisible == true)
            slots[target].Editor.Focus(FocusState.Keyboard);
    }
    private void RestoreAfterEdit(bool animate)
    {
        Render(animate);
        // Native edit processing can finish after its dependency-property
        // callback, overwriting an in-callback controlled-value restore.
        var current = revision;
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () => { if (IsLoaded && current == revision) Render(animate); });
    }
    private void Edited(Slot slot)
    {
        if (rendering || !Current(slot)) return;
        Request(slot, BleeckerOtpModel.Change(Value, Length, slot.Index, slot.Text));
    }
    private void Key(Slot slot, KeyRoutedEventArgs args)
    {
        if (!Current(slot) || !IsEnabled) return;
        if (args.Key == VirtualKey.Back) { args.Handled = true; Request(slot, BleeckerOtpModel.Backspace(Value, Length, slot.Index)); return; }
        var target = args.Key == VirtualKey.Left ? slot.Index - 1 : args.Key == VirtualKey.Right ? slot.Index + 1 : -1;
        if (target < 0 || target >= slots.Count) return;
        args.Handled = true;
        if (XamlRoot?.IsHostVisible == true) slots[target].Editor.Focus(FocusState.Keyboard);
    }
    private async void Paste(Slot slot, TextControlPasteEventArgs args)
    {
        args.Handled = true; if (!Current(slot) || !IsEnabled) return;
        var pending = ++revision; var editor = slot.Editor; var data = Clipboard.GetContent();
        var text = data.Contains(StandardDataFormats.Text) ? await data.GetTextAsync() : "";
        if (pending != revision || !Current(slot) || !ReferenceEquals(slot.Editor, editor) || !IsEnabled) return;
        Request(slot, BleeckerOtpModel.Paste(text, Length));
    }
    private void MotionChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => Render(false));
    private Color Color(string name) => ((SolidColorBrush)palette[name].Foreground).Color;
    private void Paint(Slot slot, bool animate)
    {
        if (palette.Count == 0) return;
        var dark = ActualTheme == ElementTheme.Dark; var focused = slot.Editor.FocusState != FocusState.Unfocused;
        var filled = BleeckerOtpModel.Display(Value, Length, slot.Index).Length > 0;
        var border = Color(focused || filled ? "Sea" : dark ? "White" : "Sand");
        var alpha = focused ? 1 : filled ? dark ? .65 : .6 : dark ? .15 : .4;
        var now = clock.Elapsed.TotalMilliseconds;
        slot.BorderMotion.Retarget(BleeckerColorMix.Premultiply((border.R, border.G, border.B, alpha)), now, BleeckerTokens.Durations["control"], animate && IsLoaded && settings.AnimationsEnabled);
        slot.RingMotion.Retarget(focused ? 1 : 0, now, BleeckerTokens.Durations["control"], animate && IsLoaded && settings.AnimationsEnabled);
        slot.Chrome.Background = new SolidColorBrush(Color(dark ? "DeepSea" : "White"));
        slot.Editor.Foreground = palette["Text"].Foreground;
        var ring = Color("Sea"); slot.Ring.BorderBrush = new SolidColorBrush(ring) { Opacity = dark ? .15 : .12 };
        slot.CssShadow.Set(new(2, 1, 0, .025f, 26, 55, 77));
        PaintFrame(slot, now);
        if (slot.BorderMotion.IsRunning(now) || slot.RingMotion.IsRunning(now)) StartPainting();
    }
    private void StartPainting() { if (!painting) { painting = true; CompositionTarget.Rendering += Frame; } }
    private void StopPainting() { if (painting) CompositionTarget.Rendering -= Frame; painting = false; }
    private void Frame(object? sender, object args)
    {
        var now = clock.Elapsed.TotalMilliseconds; foreach (var slot in slots) PaintFrame(slot, now);
        if (!IsLoaded || slots.All(slot => !slot.BorderMotion.IsRunning(now) && !slot.RingMotion.IsRunning(now))) StopPainting();
    }
    private static void PaintFrame(Slot slot, double now)
    {
        var color = BleeckerColorMix.EncodePremultiplied(slot.BorderMotion.ValueAt(now));
        ((SolidColorBrush)slot.Chrome.BorderBrush).Color = Windows.UI.Color.FromArgb(255, color.R, color.G, color.B);
        slot.Chrome.BorderBrush.Opacity = color.Alpha; slot.Ring.Opacity = slot.RingMotion.ValueAt(now);
    }
    private sealed class OtpPanel(BleeckerOtpInput owner) : Panel
    {
        protected override Size MeasureOverride(Size available)
        {
            foreach (var child in Children) child.Measure(new Size(40, 48));
            return new Size(double.IsPositiveInfinity(available.Width) ? Children.Count * 40 + Math.Max(0, Children.Count - 1) * owner.Spacing : available.Width, Children.Count == 0 ? 0 : 48);
        }
        protected override Size ArrangeOverride(Size final)
        {
            for (var i = 0; i < Children.Count; i++) Children[i].Arrange(new Rect(i * (40 + owner.Spacing), (final.Height - 48) / 2, 40, 48));
            return final;
        }
    }
    private sealed class Slot : Grid, IDisposable
    {
        private readonly BleeckerOtpInput owner;
        private long textToken;
        private bool secure, hasEditor;
        internal int Index { get; }
        internal Control Editor { get; private set; } = null!;
        internal Border Chrome { get; } = new() { Name = "OtpChrome", BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(), CornerRadius = new CornerRadius(7) };
        internal Border Ring { get; } = new() { Name = "OtpRing", Margin = new Thickness(-2), BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(9), IsHitTestVisible = false, Opacity = 0 };
        internal BleeckerCssTransition<(double L,double A,double B,double Alpha)> BorderMotion { get; } = new(BleeckerColorMix.MixPremultiplied);
        internal BleeckerCssTransition<double> RingMotion { get; } = new((a,b,p)=>a+(b-a)*p);
        internal BleeckerCssShadow CssShadow { get; }
        internal string Text => Editor is TextBox text ? text.Text : ((PasswordBox)Editor).Password;
        internal Slot(BleeckerOtpInput owner, int index)
        {
            this.owner = owner; Index = index; Name = "OtpSlot" + index; UseLayoutRounding = false;
            var shadowHost = new Grid { IsHitTestVisible = false }; var mask = new Microsoft.UI.Xaml.Shapes.Path(); var exterior = new Microsoft.UI.Xaml.Shapes.Path();
            var canvas = new Canvas { Opacity = 0, IsHitTestVisible = false }; canvas.Children.Add(mask); canvas.Children.Add(exterior);
            Children.Add(canvas); Children.Add(shadowHost); Children.Add(Ring); Children.Add(Chrome);
            CssShadow = new(owner, Chrome, shadowHost, mask, exterior);
        }
        internal void SetText(string value)
        {
            if (Editor is TextBox text) { if (text.Text != value) text.Text = value; }
            else if (((PasswordBox)Editor).Password != value) ((PasswordBox)Editor).Password = value;
        }
        internal void SetSecure(bool value)
        {
            if (hasEditor && secure == value) return;
            var restoreFocus = hasEditor && Editor.FocusState != FocusState.Unfocused;
            Disconnect(); secure = value;
            var scope = new InputScope(); scope.Names.Add(new InputScopeName { NameValue = value ? InputScopeNameValue.NumericPin : InputScopeNameValue.Number });
            if (value)
            {
                var password = new PasswordBox { MaxLength = 1, PasswordRevealMode = PasswordRevealMode.Hidden, InputScope = scope, Template = (ControlTemplate)owner.resources["BleeckerOtpPasswordTemplate"] };
                password.Paste += Paste; Editor = password;
                textToken = password.RegisterPropertyChangedCallback(PasswordBox.PasswordProperty, (_, _) => owner.Edited(this));
            }
            else
            {
                var text = new TextBox { MaxLength = 1, TextAlignment = TextAlignment.Center, InputScope = scope, IsSpellCheckEnabled = false, IsTextPredictionEnabled = false, Template = (ControlTemplate)owner.resources["BleeckerOtpTextTemplate"] };
                text.Paste += Paste; Editor = text;
                textToken = text.RegisterPropertyChangedCallback(TextBox.TextProperty, (_, _) => owner.Edited(this));
            }
            hasEditor = true; Editor.Name = "OtpEditor" + Index; Editor.MinWidth = 0; Editor.MinHeight = 0; Editor.Padding = new Thickness(0); Editor.BorderThickness = new Thickness(1); Editor.Margin = new Thickness(0); Editor.UseSystemFocusVisuals = false;
            Editor.HorizontalAlignment = HorizontalAlignment.Stretch; Editor.VerticalAlignment = VerticalAlignment.Stretch;
            Editor.HorizontalContentAlignment = HorizontalAlignment.Center; Editor.VerticalContentAlignment = VerticalAlignment.Center;
            foreach (var (property, name) in new[] { (FontFamilyProperty, nameof(FontFamily)), (FontSizeProperty, nameof(FontSize)), (FontWeightProperty, nameof(FontWeight)), (CharacterSpacingProperty, nameof(CharacterSpacing)), (IsTextScaleFactorEnabledProperty, nameof(IsTextScaleFactorEnabled)) }) Editor.SetBinding(property, Bind(owner, name));
            AutomationProperties.SetName(Editor, "Digit " + (Index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)); AutomationProperties.SetPositionInSet(Editor, Index + 1);
            Editor.PreviewKeyDown += Key; Editor.GotFocus += Focused; Editor.LostFocus += Blurred; Children.Add(Editor);
            if (restoreFocus && owner.XamlRoot?.IsHostVisible == true) Editor.Focus(FocusState.Keyboard);
        }
        private void Key(object sender, KeyRoutedEventArgs args) => owner.Key(this, args);
        private void Paste(object sender, TextControlPasteEventArgs args) => owner.Paste(this, args);
        private void Focused(object sender, RoutedEventArgs args) { if (Editor is TextBox text) text.SelectAll(); else ((PasswordBox)Editor).SelectAll(); owner.Paint(this, true); }
        private void Blurred(object sender, RoutedEventArgs args) => owner.Paint(this, true);
        private void Disconnect()
        {
            if (!hasEditor) return;
            Editor.PreviewKeyDown -= Key; Editor.GotFocus -= Focused; Editor.LostFocus -= Blurred;
            if (Editor is TextBox text) { text.Paste -= Paste; text.UnregisterPropertyChangedCallback(TextBox.TextProperty, textToken); }
            else { var password = (PasswordBox)Editor; password.Paste -= Paste; password.UnregisterPropertyChangedCallback(PasswordBox.PasswordProperty, textToken); }
            Children.Remove(Editor); hasEditor = false;
        }
        public void Dispose() { Disconnect(); CssShadow.Dispose(); }
    }
}
