using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Orchard;

/// <summary>A native file chooser with Bleecker's file-input recipe. No file content is read or uploaded.</summary>
public sealed class BleeckerFileInput : Button, INotifyPropertyChanged
{
    private readonly BleeckerFileInputSelection<StorageFile> selection = new();
    private readonly BleeckerDisabledPointer pointer;
    private readonly InputSystemCursor arrow = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    private readonly InputSystemCursor unavailable = InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);
    private TextBlock? chooseText, selectionText;
    private Border? choose, focusRing;
    private bool pointerOver, updating;
    private bool updatingDefaults;
    private Binding defaultFontBinding = new();
    private Binding defaultPaddingBinding = new();
    private Exception? notifiedError;
    private long generation;

    public BleeckerFileInput()
    {
        defaultFontBinding.Source=this;defaultFontBinding.Path=new(nameof(InputFontSize));
        defaultPaddingBinding.Source=this;defaultPaddingBinding.Path=new(nameof(InputPadding));
        DefaultStyleKey = typeof(BleeckerFileInput);
        pointer = new(this, () => this, SetPointerOver, observeEnabled: true);
        Click += OnChoose;
        GotFocus += (_, _) => UpdateAppearance(); LostFocus += (_, _) => UpdateAppearance();
        Loaded += (_, _) => { pointer.Update(); pointer.ReconcileLifecycle(); UpdateParts(); };
        Unloaded += (_, _) =>
        {
            pointer.Detach(); pointerOver = false;
            // A native remove/add may deliver Unloaded after the new Loaded.
            // Only a truly detached input retires its selection and callbacks.
            if (!IsLoaded) { generation++; ClearSelection(); }
            pointer.ReconcileLifecycle();
        };
        foreach (var property in new[] { FontFamilyProperty, FontSizeProperty, FontStyleProperty, FontStretchProperty, IsTextScaleFactorEnabledProperty, FlowDirectionProperty, CornerRadiusProperty, LanguageProperty })
            RegisterPropertyChangedCallback(property, (_, _) => UpdateParts());
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { pointer.Update(); Changed(nameof(IsSelectionMissing)); UpdateAppearance(); });
        RegisterPropertyChangedCallback(FocusStateProperty, (_, _) => UpdateAppearance());
        RegisterPropertyChangedCallback(StyleProperty, (_, _) => UpdateParts());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<IReadOnlyList<StorageFile>>? SelectionChanged;
    public event EventHandler? SelectionCanceled;
    public event EventHandler<Exception>? PickerFailed;

    /// <summary>Native Windows extension filters, such as .png or .m3u. Empty means all file types. MIME types are not translated.</summary>
    public Collection<string> FileTypeFilter { get; } = new();
    /// <summary>Optional host-native selection integration. Receives the already owner-associated, extension-filtered picker.</summary>
    public Func<FileOpenPicker, bool, Task<IReadOnlyList<StorageFile>?>>? Picker { get; set; }
    public IReadOnlyList<StorageFile> SelectedFiles => selection.Items;
    public bool IsPicking { get; private set; }
    public Exception? LastPickerError { get; private set; }
    public bool IsSelectionMissing => IsEnabled && IsRequired && SelectedFiles.Count == 0;
    public static readonly DependencyProperty InputFontSizeProperty = DependencyProperty.Register(nameof(InputFontSize),typeof(double),typeof(BleeckerFileInput),new PropertyMetadata(14d));
    public double InputFontSize => (double)GetValue(InputFontSizeProperty);
    public static readonly DependencyProperty InputPaddingProperty = DependencyProperty.Register(nameof(InputPadding),typeof(Thickness),typeof(BleeckerFileInput),new PropertyMetadata(new Thickness(8)));
    public Thickness InputPadding => (Thickness)GetValue(InputPaddingProperty);
    public string ChooseButtonText => IsMultiple ? ChooseFilesText : ChooseFileText;
    public string SelectionText => SelectedFiles.Count switch { 0 => EmptyText, 1 => SelectedFiles[0].Name, _ => string.Format(CultureInfo.CurrentCulture, MultipleFilesTextFormat, SelectedFiles.Count) };
    internal string AutomationValue => string.Join(", ", SelectedFiles.Select(file => file.Name));

    public static readonly DependencyProperty InputSizeProperty = DependencyProperty.Register(nameof(InputSize), typeof(BleeckerInputSize), typeof(BleeckerFileInput), new PropertyMetadata(BleeckerInputSize.Medium, RecipeChanged));
    public BleeckerInputSize InputSize { get => (BleeckerInputSize)GetValue(InputSizeProperty); set => SetValue(InputSizeProperty, value); }
    /// <summary>Scale the source rem recipe independently of a caller's explicit FontSize. Does not change Windows settings.</summary>
    public static readonly DependencyProperty FontScaleProperty = DependencyProperty.Register(nameof(FontScale), typeof(double), typeof(BleeckerFileInput), new PropertyMetadata(1d, RecipeChanged));
    public double FontScale { get => (double)GetValue(FontScaleProperty); set => SetValue(FontScaleProperty, value); }
    public static readonly DependencyProperty IsErrorProperty = DependencyProperty.Register(nameof(IsError), typeof(bool), typeof(BleeckerFileInput), new PropertyMetadata(false, RecipeChanged));
    public bool IsError { get => (bool)GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }
    public static readonly DependencyProperty IsMultipleProperty = DependencyProperty.Register(nameof(IsMultiple), typeof(bool), typeof(BleeckerFileInput), new PropertyMetadata(false, RecipeChanged));
    public bool IsMultiple { get => (bool)GetValue(IsMultipleProperty); set => SetValue(IsMultipleProperty, value); }
    public static readonly DependencyProperty IsRequiredProperty = DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(BleeckerFileInput), new PropertyMetadata(false, RecipeChanged));
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }
    public static readonly DependencyProperty ChooseFileTextProperty = DependencyProperty.Register(nameof(ChooseFileText), typeof(string), typeof(BleeckerFileInput), new PropertyMetadata("Choose File", RecipeChanged));
    public string ChooseFileText { get => (string)GetValue(ChooseFileTextProperty); set => SetValue(ChooseFileTextProperty, value); }
    public static readonly DependencyProperty ChooseFilesTextProperty = DependencyProperty.Register(nameof(ChooseFilesText), typeof(string), typeof(BleeckerFileInput), new PropertyMetadata("Choose Files", RecipeChanged));
    public string ChooseFilesText { get => (string)GetValue(ChooseFilesTextProperty); set => SetValue(ChooseFilesTextProperty, value); }
    public static readonly DependencyProperty EmptyTextProperty = DependencyProperty.Register(nameof(EmptyText), typeof(string), typeof(BleeckerFileInput), new PropertyMetadata("No file chosen", RecipeChanged));
    public string EmptyText { get => (string)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }
    public static readonly DependencyProperty MultipleFilesTextFormatProperty = DependencyProperty.Register(nameof(MultipleFilesTextFormat), typeof(string), typeof(BleeckerFileInput), new PropertyMetadata("{0} files", RecipeChanged));
    public string MultipleFilesTextFormat { get => (string)GetValue(MultipleFilesTextFormatProperty); set => SetValue(MultipleFilesTextFormatProperty, value); }

    private static void RecipeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var owner = (BleeckerFileInput)sender;
        if (!Enum.IsDefined(owner.InputSize)) throw new ArgumentOutOfRangeException(nameof(InputSize));
        if (!double.IsFinite(owner.FontScale) || owner.FontScale <= 0) throw new ArgumentOutOfRangeException(nameof(FontScale));
        owner.SetValue(InputFontSizeProperty,(owner.InputSize==BleeckerInputSize.Large?16d:14d)*owner.FontScale);
        owner.SetValue(InputPaddingProperty,new Thickness(8*owner.FontScale));
        owner.Changed(nameof(ChooseButtonText)); owner.Changed(nameof(SelectionText)); owner.Changed(nameof(IsSelectionMissing));
        owner.UpdateParts();
    }

    /// <summary>Supply original native files without input/change notification, like assigning the native files/value property.</summary>
    public void SetFiles(IEnumerable<StorageFile> files)
    {
        VerifyThread(); var previous = AutomationValue; selection.Replace(files); SelectionUpdated(previous);
    }
    /// <summary>Clear the selection without a SelectionChanged event, matching native programmatic clearing/form reset.</summary>
    public void ClearSelection()
    {
        VerifyThread(); var previous = AutomationValue; selection.Clear(); SelectionUpdated(previous);
    }
    private void SelectionUpdated(string previous)
    {
        Changed(nameof(SelectedFiles)); Changed(nameof(SelectionText)); Changed(nameof(IsSelectionMissing)); UpdateParts();
        if (FrameworkElementAutomationPeer.FromElement(this) is FileInputPeer peer && previous != AutomationValue)
            peer.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, previous, AutomationValue);
    }

    /// <summary>Open the owner-associated Windows picker. A null result is cancellation and retains the previous selection.</summary>
    public async Task<IReadOnlyList<StorageFile>?> PickFilesAsync()
    {
        VerifyThread();
        if (!IsLoaded || XamlRoot?.ContentIslandEnvironment is null) throw new InvalidOperationException("File Input must be attached to its owning window.");
        if (!IsEnabled) throw new InvalidOperationException("A disabled File Input cannot open its picker.");
        if (IsPicking) throw new InvalidOperationException("This File Input already has an active picker.");
        var current = generation; IsPicking = true; LastPickerError = null; Changed(nameof(IsPicking)); Changed(nameof(LastPickerError));
        try
        {
            var picker = new FileOpenPicker();
            foreach (var filter in FileTypeFilter)
            {
                if (string.IsNullOrWhiteSpace(filter) || filter != "*" && (!filter.StartsWith('.') || filter.Contains('/') || filter.Contains('\\')))
                    throw new ArgumentException("Windows picker filters must be explicit extensions such as .png, or *; MIME types are not translated.", nameof(FileTypeFilter));
                picker.FileTypeFilter.Add(filter);
            }
            if (FileTypeFilter.Count == 0) picker.FileTypeFilter.Add("*");
            var window = Microsoft.UI.Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
            if (window == 0) throw new InvalidOperationException("File Input has no native owner window.");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, window);
            var integration = Picker;
            var result = integration is null ? await PickFilesCoreAsync(picker, IsMultiple) : await integration(picker, IsMultiple);
            // Do not notify or repopulate a detached input when an old dialog
            // completes. The awaiting caller still receives the original result.
            if (generation == current && IsLoaded)
            {
                if (result is null || result.Count == 0) SelectionCanceled?.Invoke(this, EventArgs.Empty);
                else { SetFiles(result); SelectionChanged?.Invoke(this, SelectedFiles); }
            }
            return result;
        }
        catch (Exception error)
        {
            if (generation == current && IsLoaded) PublishFailure(error);
            throw;
        }
        finally { IsPicking = false; Changed(nameof(IsPicking)); }
    }

    private static async Task<IReadOnlyList<StorageFile>?> PickFilesCoreAsync(FileOpenPicker picker, bool multiple)
    {
        if (multiple) return await picker.PickMultipleFilesAsync();
        var file = await picker.PickSingleFileAsync(); return file is null ? null : new[] { file };
    }
    private async void OnChoose(object sender, RoutedEventArgs args)
    {
        var current = generation;
        try { await PickFilesAsync(); }
        catch (Exception error)
        {
            if (generation != current || !IsLoaded) return;
            if (!ReferenceEquals(LastPickerError, error)) PublishFailure(error);
            if (!ReferenceEquals(notifiedError, error)) throw;
        }
    }
    private void PublishFailure(Exception error)
    {
        LastPickerError = error; Changed(nameof(LastPickerError)); var callback = PickerFailed;
        notifiedError = callback is null ? null : error; callback?.Invoke(this, error);
    }
    private void VerifyThread() { if (!DispatcherQueue.HasThreadAccess) throw new InvalidOperationException("File Input must be used on its owning dispatcher."); }
    private void Changed(string name) => PropertyChanged?.Invoke(this, new(name));

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate(); choose = GetTemplateChild("FileChooseChrome") as Border; focusRing = GetTemplateChild("FileFocusRing") as Border;
        chooseText = GetTemplateChild("FileChooseText") as TextBlock; selectionText = GetTemplateChild("FileSelectionText") as TextBlock;
        UpdateParts();
    }
    private void SetPointerOver(bool value) { if (pointerOver == value) return; pointerOver = value; UpdateAppearance(); }
    private void EnsureDefaultBindings()
    {
        if(updatingDefaults)return;updatingDefaults=true;
        try
        {
            var standard=(Style)Application.Current.Resources["BleeckerFileInputStyle"];
            bool CallerStyleSets(DependencyProperty property)
            {
                bool HasSetter(Style? style)
                {
                    for(;style is not null&&!ReferenceEquals(style,standard);style=style.BasedOn)
                        if(style.Setters.OfType<Setter>().Any(setter=>setter.Property==property))return true;
                    return false;
                }
                if(Style is {} explicitStyle)return HasSetter(explicitStyle);
                // Implicit styles leave Style null. Respect the nearest declaration
                // while retaining the standard style's size-dependent defaults.
                for(DependencyObject? scope=this;scope is not null;scope=VisualTreeHelper.GetParent(scope))
                    if(scope is FrameworkElement element&&element.Resources.TryGetValue(typeof(BleeckerFileInput),out var resource)&&resource is Style implicitStyle)
                        return HasSetter(implicitStyle);
                if(Application.Current.Resources.TryGetValue(typeof(BleeckerFileInput),out var applicationStyle)&&applicationStyle is Style style)
                    return HasSetter(style);
                return false;
            }
            void Default(DependencyProperty property,ref Binding binding,string sourceProperty)
            {
                var owns=ReferenceEquals(GetBindingExpression(property)?.ParentBinding,binding);
                if(CallerStyleSets(property)){if(owns)ClearValue(property);return;}
                if(ReadLocalValue(property)==DependencyProperty.UnsetValue)
                {
                    // Reattach a fresh expression after a caller clears a local
                    // override; a retired native Binding expression is not reused.
                    binding=new Binding{Source=this,Path=new(sourceProperty)};SetBinding(property,binding);
                }
            }
            // WinUI does not evaluate Binding in Setter.Value. Bind actual
            // element properties, while preserving caller locals and styles.
            Default(FontSizeProperty,ref defaultFontBinding,nameof(InputFontSize));Default(PaddingProperty,ref defaultPaddingBinding,nameof(InputPadding));
        }
        finally{updatingDefaults=false;}
    }
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        EnsureDefaultBindings();return base.MeasureOverride(availableSize);
    }
    private void UpdateParts()
    {
        EnsureDefaultBindings();
        if (updating) return; updating = true;
        try
        {
            var lineHeight = FontSize * (InputSize == BleeckerInputSize.Large ? 1.5 : 20d / 14);
            if (choose is not null)
            {
                var padding = InputSize switch { BleeckerInputSize.Small => new Thickness(12,8,12,8), BleeckerInputSize.Large => new Thickness(16,12,16,12), _ => new Thickness(16,10,16,10) };
                choose.Padding = new(padding.Left*FontScale,padding.Top*FontScale,padding.Right*FontScale,padding.Bottom*FontScale);
                choose.CornerRadius = new(6*FontScale); choose.Margin = new(FlowDirection == FlowDirection.RightToLeft ? 4*FontScale : 0,0,16*FontScale,0);
            }
            if (focusRing is not null) focusRing.CornerRadius = new(CornerRadius.TopLeft+2,CornerRadius.TopRight+2,CornerRadius.BottomRight+2,CornerRadius.BottomLeft+2);
            foreach (var text in new[] { chooseText, selectionText }) if (text is not null)
            {
                text.FontFamily = FontFamily; text.FontSize = FontSize; text.FontStyle = FontStyle; text.FontStretch = FontStretch;
                text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; text.LineHeight = lineHeight;
                // Chromium lays out the source line strut in 1/64 CSS-pixel units.
                text.Height = Math.Floor(lineHeight * 64) / 64;
                text.Language = Language;
            }
            if (chooseText is not null)
            {
                chooseText.Text = ChooseButtonText;
                if (FontFamily.Source == ((FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"]).Source)
                    chooseText.FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"];
            }
            if (selectionText is not null) selectionText.Text = SelectionText;
            UpdateAppearance();
        }
        finally { updating = false; }
    }
    private void UpdateAppearance()
    {
        ProtectedCursor = IsEnabled ? arrow : unavailable;
        VisualStateManager.GoToState(this, IsError ? "FileError" : "FileNormal", false);
        VisualStateManager.GoToState(this, FocusState == FocusState.Unfocused ? "FileUnfocused" : "FileFocused", false);
        VisualStateManager.GoToState(this, IsEnabled ? pointerOver ? "FileHovered" : "FileEnabled" : pointerOver ? "FileDisabledHovered" : "FileDisabled", false);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new FileInputPeer(this);
    private sealed class FileInputPeer(BleeckerFileInput owner) : ButtonAutomationPeer(owner), IValueProvider
    {
        protected override string GetClassNameCore() => nameof(BleeckerFileInput);
        protected override bool IsRequiredForFormCore() => owner.IsRequired;
        protected override bool IsDataValidForFormCore() => !owner.IsSelectionMissing;
        protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Value ? this : base.GetPatternCore(pattern);
        public bool IsReadOnly => true;
        public string Value => owner.AutomationValue;
        public void SetValue(string value) => throw new InvalidOperationException("Choose files through the native picker; a path cannot be assigned as a value.");
    }
}
