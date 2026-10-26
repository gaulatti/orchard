using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Orchard;

/// <summary>A plain-text multiline Bleecker editor with native selection, undo and vertical resizing.</summary>
public sealed partial class BleeckerTextarea : RichEditBox
{
    private bool pointerOver;
    private string? appearanceState;
    private bool applyingAppearance;
    private bool synchronizingText;
    private bool editorReady => IsLoaded && GetTemplateChild("ContentElement") is FrameworkElement { IsLoaded: true };
    private int documentEditDepth;
    private int contentVersion;
    private Task pasteOperation = Task.CompletedTask;
    private Control? resizeGrip;
    private Grid? resizeHost;

    public BleeckerTextarea()
    {
        DefaultStyleKey = typeof(BleeckerTextarea);
        AcceptsReturn = true;
        TextWrapping = TextWrapping.Wrap;
        ClipboardCopyFormat = RichEditClipboardFormat.PlainText;
        InitializePlainTextCommands();
        InitializeShadow();
        InitializeMotion();
        InitializePointer();
        TextChanged += (_, _) => ReadNativeText();
        Paste += OnPlainTextPaste;
        GotFocus += (_, _) => UpdateAppearance();
        LostFocus += (_, _) => UpdateAppearance();
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { UpdatePointerObservation(); UpdateAppearance(); });
        RegisterPropertyChangedCallback(FocusStateProperty, (_, _) => UpdateAppearance());
        foreach (var property in new[] { FontFamilyProperty, FontSizeProperty, FontWeightProperty, FontStyleProperty, ForegroundProperty, FlowDirectionProperty, TextAlignmentProperty })
            RegisterPropertyChangedCallback(property, (_, _) => ApplyParagraphFormat());
        RegisterPropertyChangedCallback(FlowDirectionProperty, (_, _) => UpdateResizeGripDirection());
        Loaded += (_, _) => { contentVersion++; DispatcherQueue.TryEnqueue(() => { if (!IsLoaded) return; UpdateGeometry(); WriteNativeText(); UpdateAppearance(false); }); };
        Unloaded += (_, _) => { if (IsLoaded) return; contentVersion++; CancelResize(); };
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(BleeckerTextarea),
        new PropertyMetadata("", (owner, _) => ((BleeckerTextarea)owner).WriteNativeText()));
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public new static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(BleeckerTextarea),
        new PropertyMetadata(false, (owner, _) => ((BleeckerTextarea)owner).UpdateNativeReadOnly()));
    public new bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
    public static readonly DependencyProperty InputSizeProperty = DependencyProperty.Register(nameof(InputSize), typeof(BleeckerInputSize), typeof(BleeckerTextarea),
        new PropertyMetadata(BleeckerInputSize.Medium, OnGeometryChanged));
    public BleeckerInputSize InputSize { get => (BleeckerInputSize)GetValue(InputSizeProperty); set => SetValue(InputSizeProperty, value); }
    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(nameof(Rows), typeof(int), typeof(BleeckerTextarea), new PropertyMetadata(4, OnGeometryChanged));
    public int Rows { get => (int)GetValue(RowsProperty); set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); SetValue(RowsProperty, value); } }
    public static readonly DependencyProperty LineHeightProperty = DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(BleeckerTextarea), new PropertyMetadata(20d));
    public double LineHeight { get => (double)GetValue(LineHeightProperty); private set => SetValue(LineHeightProperty, value); }
    public static readonly DependencyProperty IsErrorProperty = DependencyProperty.Register(nameof(IsError), typeof(bool), typeof(BleeckerTextarea),
        new PropertyMetadata(false, (owner, _) => { var editor = (BleeckerTextarea)owner; editor.UpdateAccessibility(); editor.UpdateAppearance(); }));
    public bool IsError { get => (bool)GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }
    public static readonly DependencyProperty IsInvalidProperty = DependencyProperty.Register(nameof(IsInvalid), typeof(object), typeof(BleeckerTextarea),
        new PropertyMetadata(null, (owner, _) => ((BleeckerTextarea)owner).UpdateAccessibility()));
    /// <summary>Overrides error-derived accessibility validity without changing the error paint.</summary>
    public bool? IsInvalid { get => GetValue(IsInvalidProperty) is bool value ? value : null; set => SetValue(IsInvalidProperty, value); }
    public static readonly DependencyProperty PlaceholderForegroundProperty = DependencyProperty.Register(nameof(PlaceholderForeground), typeof(Brush), typeof(BleeckerTextarea), new PropertyMetadata(null));
    public Brush PlaceholderForeground { get => (Brush)GetValue(PlaceholderForegroundProperty); set => SetValue(PlaceholderForegroundProperty, value); }

    protected override void OnApplyTemplate()
    {
        plainTextMenu?.Hide();
        DetachResizeGrip();
        contentVersion++;
        base.OnApplyTemplate();
        ApplyShadowTemplate();
        ApplyMotionTemplate();
        resizeHost = GetTemplateChild("Editor") as Grid;
        if (resizeHost is not null)
        {
            resizeGrip = new TextareaResizeGrip { Name = "TextareaResizeGrip", Width = 12, Height = 12, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, IsTabStop = true,
                Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid Background='Transparent'><Path Stroke='{ThemeResource BleeckerTextSecondaryBrush}' StrokeThickness='1' Data='M 3,10 L 10,3 M 7,10 L 10,7' /></Grid></ControlTemplate>") };
            AutomationProperties.SetName(resizeGrip, "Resize text area");
            AutomationProperties.SetHelpText(resizeGrip, "Drag vertically or use Up and Down arrows to resize.");
            resizeGrip.KeyDown += OnResizeKey;
            resizeHost.Children.Add(resizeGrip);
            UpdateResizeGripDirection();
        }
        appearanceState = null;
        UpdateGeometry();
        WriteNativeText();
        UpdateAccessibility();
        UpdateAppearance(false);
    }

    private (double Minimum, double Horizontal, double Vertical, double Font, double Line) Geometry => InputSize switch
    {
        BleeckerInputSize.Small => (96, 12, 8, 14, 20),
        BleeckerInputSize.Medium => (112, 16, 12, 14, 20),
        BleeckerInputSize.Large => (128, 16, 12, 16, 24),
        _ => throw new ArgumentOutOfRangeException(nameof(InputSize))
    };
    private static void OnGeometryChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerTextarea)owner).UpdateGeometry();
    private void UpdateGeometry()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Rows);
        var recipe = Geometry;
        MinHeight = recipe.Minimum;
        FontSize = recipe.Font;
        LineHeight = recipe.Line;
        Padding = new Thickness(recipe.Horizontal, recipe.Vertical, recipe.Horizontal, recipe.Vertical);
        ApplyParagraphFormat();
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var preferred = Math.Max(MinHeight, Rows * LineHeight + Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom);
        var height = Math.Min(availableSize.Height, double.IsNaN(Height) ? preferred : Height);
        var measured = base.MeasureOverride(new Size(availableSize.Width, height));
        return new Size(measured.Width, height);
    }
    private void ApplyParagraphFormat()
    {
        if (!editorReady) return;
        var previous = synchronizingText;
        synchronizingText = true;
        try
        {
            MutateDocument(() =>
            {
                var format = TextDocument.GetDefaultParagraphFormat();
                format.SetLineSpacing(LineSpacingRule.Exactly, (float)(LineHeight * .75));
                format.SpaceBefore = format.SpaceAfter = 0;
                TextDocument.SetDefaultParagraphFormat(format);
                var font = TextDocument.GetDefaultCharacterFormat();
                var name = FontFamily.Source;
                // Preserve XAML's private font-face binding for an asset URI.
                // Reassigning only its family name loses the custom collection.
                if (!name.Contains('#')) font.Name = name;
                font.Size = (float)(FontSize * .75);
                font.Bold = FontWeight.Weight >= 600 ? FormatEffect.On : FormatEffect.Off;
                font.Italic = FontStyle == Windows.UI.Text.FontStyle.Normal ? FormatEffect.Off : FormatEffect.On;
                font.Underline = UnderlineType.None;
                if (Foreground is SolidColorBrush foreground) font.ForegroundColor = foreground.Color;
                TextDocument.SetDefaultCharacterFormat(font);
                var selection = TextDocument.Selection;
                var start = selection.StartPosition; var end = selection.EndPosition;
                var range = TextDocument.GetRange(0, int.MaxValue);
                range.ParagraphFormat = format;
                range.CharacterFormat = font;
                selection.SetRange(start, end);
            });
        }
        finally { synchronizingText = previous; }
    }
    private void WriteNativeText()
    {
        if (!editorReady || synchronizingText || ReadDocument() == Text) return;
        contentVersion++;
        synchronizingText = true;
        try { MutateDocument(() => { TextDocument.SetText(TextSetOptions.None, Text ?? ""); ApplyParagraphFormat(); }); }
        finally { synchronizingText = false; }
    }
    private string ReadDocument()
    {
        // Request the immutable final paragraph explicitly so only that
        // sentinel is removed; a consumer's trailing newline must survive.
        TextDocument.GetText(TextGetOptions.AllowFinalEop | TextGetOptions.UseLf, out var value);
        return value.EndsWith('\n') ? value[..^1] : value;
    }
    private void ReadNativeText()
    {
        if (!editorReady || synchronizingText) return;
        var value = ReadDocument();
        if (value == Text) return;
        contentVersion++;
        synchronizingText = true;
        try { SetValue(TextProperty, value); }
        finally { synchronizingText = false; }
    }
    private async void OnPlainTextPaste(object sender, TextControlPasteEventArgs args)
    {
        args.Handled = true;
        await StartPlainTextPasteAsync();
    }
    private Task StartPlainTextPasteAsync() => pasteOperation = PastePlainTextAsync();
    private async Task PastePlainTextAsync()
    {
        if (!editorReady || !IsLoaded || IsReadOnly || !IsEnabled) return;
        var data = Clipboard.GetContent();
        if (!data.Contains(StandardDataFormats.Text)) return;
        var version = contentVersion;
        var root = XamlRoot;
        var start = TextDocument.Selection.StartPosition;
        var end = TextDocument.Selection.EndPosition;
        var text = await data.GetTextAsync();
        // A delayed clipboard provider must not write into replacement content,
        // a remounted view, a new selection or a newly read-only editor.
        if (!IsLoaded || IsReadOnly || !IsEnabled || version != contentVersion || !ReferenceEquals(root, XamlRoot) ||
            TextDocument.Selection.StartPosition != start || TextDocument.Selection.EndPosition != end) return;
        var insertion = BleeckerTextareaInsertion.Prepare(text, Text, start, end, MaxLength);
        if (insertion.Length == 0 && start == end) return;
        TextDocument.Selection.SetText(TextSetOptions.None, insertion);
        var caret = TextDocument.Selection.EndPosition;
        TextDocument.Selection.SetRange(caret, caret);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new TextareaPeer(this);
    private sealed class TextareaPeer(BleeckerTextarea editor) : RichEditBoxAutomationPeer(editor)
    {
        protected override bool IsDataValidForFormCore() => base.IsDataValidForFormCore() && !(editor.IsInvalid ?? editor.IsError);
    }
    private void UpdateAccessibility()
    {
        AutomationProperties.SetIsDataValidForForm(this, !(IsInvalid ?? IsError));
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void UpdateNativeReadOnly()
    {
        if (documentEditDepth == 0) base.IsReadOnly = IsReadOnly;
    }
    private void MutateDocument(Action action)
    {
        // The consumer's dependency property remains stable. RichEdit's native
        // document also locks programmatic formatting while read-only, so only
        // its internal guard is released for this synchronous UI-thread edit.
        documentEditDepth++;
        if (documentEditDepth == 1) base.IsReadOnly = false;
        try { action(); }
        finally { documentEditDepth--; UpdateNativeReadOnly(); }
    }
    private void UpdateAppearance(bool animate = true)
    {
        var state = !IsEnabled ? pointerOver ? "InputDisabledHovered" : "InputDisabled" : FocusState != FocusState.Unfocused ? "InputFocused" : pointerOver ? "InputHovered" : "InputNormal";
        if (IsError) state += "Error";
        var changed = appearanceState != state; appearanceState = state;
        applyingAppearance = true;
        try { VisualStateManager.GoToState(this, state, false); }
        finally { applyingAppearance = false; }
        if (changed || !animate) RequestMotion(animate);
        UpdateShadow(animate);
    }
    private void OnResizeKey(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key is not (Windows.System.VirtualKey.Up or Windows.System.VirtualKey.Down)) return;
        Height = Math.Clamp(ActualHeight + (args.Key == Windows.System.VirtualKey.Up ? -LineHeight : LineHeight), MinHeight, MaxHeight);
        args.Handled = true;
    }
    private void DetachResizeGrip()
    {
        if (resizeGrip is null) return;
        CancelResize();
        resizeGrip.KeyDown -= OnResizeKey;
        resizeHost?.Children.Remove(resizeGrip);
        resizeGrip = null; resizeHost = null;
    }
}
