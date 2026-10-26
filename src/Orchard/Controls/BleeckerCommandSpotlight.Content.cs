using Microsoft.UI.Text;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;

namespace Orchard;

public sealed partial class BleeckerCommandSpotlight
{
    private readonly ConditionalWeakTable<TextBlock, TextPresentation> textPresentations = new();
    private readonly ConditionalWeakTable<BleeckerKbd, HintPresentation> hintPresentations = new();
    private sealed class TextPresentation
    {
        internal FontFamily? Family;
        internal Brush? Foreground;
        internal string? Language;
        internal double Size, Line;
        internal FlowDirection Direction;
        internal bool Scaling, Applied;
    }
    private sealed class HintPresentation
    {
        internal string? Language;
        internal FlowDirection Direction;
        internal double AppearanceFactor, DimensionFactor;
        internal bool Scaling, AppearanceApplied, DimensionsApplied;
    }
    private static TextBlock Text(string name, bool medium = false) => new() { Name = name, UseLayoutRounding = false, LineStackingStrategy = LineStackingStrategy.BlockLineHeight, FontWeight = medium ? FontWeights.Medium : FontWeights.Normal, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis };
    private void SetText(TextBlock text, double size, double line, Brush foreground)
    {
        var weight = text.FontWeight.Weight;
        var family = HasCallerFontFamily ? FontFamily : (FontFamily)Application.Current.Resources[weight >= 600 ? "BleeckerPrimarySemiboldFontFamily" : weight >= 500 ? "BleeckerPrimaryMediumFontFamily" : "BleeckerPrimaryFontFamily"];
        var language = Language; var direction = FlowDirection; var scaling = IsTextScaleFactorEnabled;
        var previous = textPresentations.GetValue(text, static _ => new());
        if (previous.Applied && ReferenceEquals(previous.Family, family) && ReferenceEquals(previous.Foreground, foreground) && previous.Size == size && previous.Line == line && previous.Language == language && previous.Direction == direction && previous.Scaling == scaling) return;
        text.FontFamily = family; text.FontSize = size; text.LineHeight = line; text.Foreground = foreground; text.Language = language; text.FlowDirection = direction; text.IsTextScaleFactorEnabled = scaling;
        previous.Family = family; previous.Foreground = foreground; previous.Size = size; previous.Line = line; previous.Language = language; previous.Direction = direction; previous.Scaling = scaling; previous.Applied = true;
    }
    private static Viewbox SearchIcon(double size, Brush brush)
    {
        var canvas = new Canvas { Width = 24, Height = 24 }; var circle = new Ellipse { Width = 16, Height = 16, Stroke = brush, StrokeThickness = 2 }; Canvas.SetLeft(circle, 3); Canvas.SetTop(circle, 3); canvas.Children.Add(circle);
        canvas.Children.Add(new Line { X1 = 16.65, Y1 = 16.65, X2 = 21, Y2 = 21, Stroke = brush, StrokeThickness = 2, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
        return new() { Width = size, Height = size, Child = canvas, IsHitTestVisible = false };
    }
    private BleeckerKbd Hint(string name, IEnumerable<string> keys)
    {
        var hint = new BleeckerKbd { Name = name, IsTabStop = false, UseLayoutRounding = false, Keys = keys.ToArray() };
        // Kbd creates its nested caps when its template loads, after construction.
        hint.Loaded += (_, _) => UpdateHint(hint);
        UpdateHint(hint); return hint;
    }
    private void UpdateHint(BleeckerKbd hint)
    {
        var factor = Rem; var direction = FlowDirection; var scaling = IsTextScaleFactorEnabled; var language = Language;
        var previous = hintPresentations.GetValue(hint, static _ => new());
        if (!previous.AppearanceApplied || previous.AppearanceFactor != factor || previous.Direction != direction || previous.Scaling != scaling || previous.Language != language)
        {
            hint.FlowDirection = direction; hint.IsTextScaleFactorEnabled = scaling; hint.Language = language; hint.Padding = new(6 * factor, 0, 6 * factor, 0); hint.FontSize = 11;
            previous.AppearanceFactor = factor; previous.Direction = direction; previous.Scaling = scaling; previous.Language = language; previous.AppearanceApplied = true;
        }
        Dimensions(hint);
        void Dimensions(BleeckerKbd cap)
        {
            var value = hintPresentations.GetValue(cap, static _ => new());
            if (value.DimensionsApplied && value.DimensionFactor == factor) return;
            cap.Height = cap.MinHeight = 20 * factor; cap.MinWidth = 20 * factor;
            value.DimensionFactor = factor; value.DimensionsApplied = true;
        }
        void Walk(DependencyObject node)
        {
            if (node is BleeckerKbd { Keys: null } cap) Dimensions(cap);
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) Walk(VisualTreeHelper.GetChild(node, i));
        }
        Walk(hint);
    }
    private string[] HotkeyKeys => ["Ctrl", BleeckerCommandSpotlightCasing.Upper(Hotkey)];
    private sealed class Trigger : Button
    {
        private readonly BleeckerCommandSpotlight owner;
        private readonly StackPanel content = new() { Orientation = Orientation.Horizontal, UseLayoutRounding = false, VerticalAlignment = VerticalAlignment.Center };
        private readonly TextBlock label = Text("SpotlightTriggerLabel", true);
        private readonly BleeckerKbd hint;
        private readonly HashSet<DependencyProperty> defaultProperties = [];
        private bool narrow, hover;
        internal Trigger(BleeckerCommandSpotlight owner)
        {
            this.owner = owner; Name = "SpotlightTrigger"; UseLayoutRounding = false; Content = content; hint = owner.Hint("SpotlightTriggerHint", owner.HotkeyKeys);
            AutomationProperties.SetName(this, "Open command spotlight"); Click += (_, _) => owner.Show();
            PointerEntered += (_, _) => { hover = true; Paint(); }; PointerExited += (_, _) => { hover = false; Paint(); };
            GotFocus += (_, _) => Paint(); LostFocus += (_, _) => Paint();
        }
        internal void SetNarrow(bool value) { if (narrow == value) return; narrow = value; Update(); }
        internal void Update()
        {
            Style = owner.TriggerStyle ?? (Style)owner.ResourcesForContent()["BleeckerCommandSpotlightTriggerStyle"]; Visibility = owner.ShowTrigger ? Visibility.Visible : Visibility.Collapsed;
            Default(HeightProperty, 36 * owner.Rem); Default(PaddingProperty, new Thickness(12 * owner.Rem, 6 * owner.Rem, 12 * owner.Rem, 6 * owner.Rem)); content.Spacing = 8 * owner.Rem; content.FlowDirection = owner.FlowDirection;
            label.Text = owner.TriggerLabel; owner.SetText(label, 13, 19.5, owner.Foreground); label.Visibility = hint.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
            if (!ReferenceEquals(hint.Keys, owner.HotkeyKeys) && !(hint.Keys?.SequenceEqual(owner.HotkeyKeys) ?? false)) hint.Keys = owner.HotkeyKeys;
            owner.UpdateHint(hint);
            if (content.Children.Count == 0) { content.Children.Add(SearchIcon(15, owner.Brush("Secondary"))); content.Children.Add(label); content.Children.Add(hint); }
            else { content.Children.RemoveAt(0); content.Children.Insert(0, SearchIcon(15, owner.Brush("Secondary"))); }
            Paint();
        }
        private void Default(DependencyProperty property, object value)
        {
            if (CallerStyleSets(owner.TriggerStyle, property))
            {
                if (defaultProperties.Remove(property)) ClearValue(property);
            }
            else { SetValue(property, value); defaultProperties.Add(property); }
        }
        private void Paint()
        {
            if (owner.palette.Count == 0) return;
            Default(BackgroundProperty, owner.Brush(hover ? "TriggerHover" : "TriggerBackground"));
            Default(BorderBrushProperty, owner.Brush(FocusState == FocusState.Keyboard ? "Focus" : hover ? "TriggerHoverBorder" : "TriggerBorder"));
        }
    }
    private ResourceDictionary? contentStyles;
    private ResourceDictionary ResourcesForContent() => contentStyles ??= new() { Source = new Uri("ms-appx:///Orchard/Themes/CommandSpotlight.xaml") };
    private sealed class Body : Panel, IDisposable
    {
        private readonly BleeckerCommandSpotlight owner;
        internal readonly TextBox Editor = new() { Name = "SpotlightSearch", IsSpellCheckEnabled = false, IsTextPredictionEnabled = false, UseLayoutRounding = false, MinHeight = 0, MinWidth = 0, Padding = new(0), BorderThickness = new(0), Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
        private readonly TextBlock placeholder = Text("SpotlightPlaceholder"), empty = Text("SpotlightEmpty");
        private readonly Header header;
        private readonly Border headerBorder;
        private readonly ScrollView list = new() { Name = "SpotlightList", UseLayoutRounding = false, IsTabStop = false, HorizontalScrollMode = ScrollingScrollMode.Disabled, HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden, VerticalScrollMode = ScrollingScrollMode.Enabled, VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Auto, ContentOrientation = ScrollingContentOrientation.Vertical, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Top, ZoomMode = ScrollingZoomMode.Disabled };
        private readonly StackPanel groups = new() { Name = "SpotlightGroups", UseLayoutRounding = false, HorizontalAlignment = HorizontalAlignment.Stretch };
        private readonly Border listContent;
        private readonly Dictionary<string, StackPanel> sections = new(StringComparer.Ordinal);
        private readonly Dictionary<(string Group, string Id), Row> rows = [];
        private readonly KeyEventHandler keyHandler;
        private bool updating;
        private string? lastSelected;
        internal double MaximumListHeight { get => list.MaxHeight; set => list.MaxHeight = value; }
        internal Body(BleeckerCommandSpotlight owner)
        {
            this.owner = owner; Name = "SpotlightBody"; UseLayoutRounding = false; FlowDirection = FlowDirection.LeftToRight;
            Editor.Style = (Style)owner.ResourcesForContent()["BleeckerCommandSpotlightEditorStyle"]; Editor.TextChanged += EditorChanged; AutomationProperties.SetName(Editor, owner.Placeholder);
            placeholder.IsHitTestVisible = false; AutomationProperties.SetAccessibilityView(placeholder, AccessibilityView.Raw);
            header = new(owner, Editor, placeholder); headerBorder = new() { Child = header, BorderThickness = new(0, 0, 0, 1), UseLayoutRounding = false };
            keyHandler = EditorKeyDown; Editor.AddHandler(KeyDownEvent, keyHandler, true); list.Style = BleeckerModalViewport.CreateStyle(); listContent = new() { Child = groups, UseLayoutRounding = false }; list.Content = listContent;
            Children.Add(headerBorder); Children.Add(list);
        }
        private void EditorChanged(object sender, TextChangedEventArgs args) { if (!updating && Editor.Text != owner.Query) owner.SetQuery(Editor.Text); }
        private void EditorKeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (!owner.SurfaceInputAllowed) return;
            if (args.Key is VirtualKey.Down or VirtualKey.Up) { args.Handled = true; owner.state.Move(args.Key == VirtualKey.Down ? 1 : -1); Update(); }
            else if (args.Key == VirtualKey.Enter) { args.Handled = true; if (owner.state.EnterTarget is { } action) owner.InvokeAction(action); }
        }
        internal void Update()
        {
            var f = owner.Rem; updating = true; try { Editor.Text = owner.Query; Editor.PlaceholderText = owner.Placeholder; } finally { updating = false; }
            Editor.FontFamily = owner.HasCallerFontFamily ? owner.FontFamily : (FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"]; Editor.FontSize = 16 * f; Editor.Foreground = owner.Foreground; Editor.FlowDirection = owner.FlowDirection; Editor.Language = owner.Language; Editor.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; AutomationProperties.SetName(Editor, owner.Placeholder);
            placeholder.Text = owner.Placeholder; placeholder.Visibility = owner.Query.Length == 0 ? Visibility.Visible : Visibility.Collapsed; owner.SetText(placeholder, 16 * f, 24 * f, owner.Brush("Secondary"));
            headerBorder.BorderBrush = owner.Brush("HeaderBorder"); header.Update(); groups.Spacing = 12 * f; listContent.Padding = new(8 * f);
            var wanted = owner.state.Groups.SelectMany(g => g.Actions.Select(a => (Group: g.Name, a.Id))).ToHashSet();
            var wantedGroupNames = owner.state.Groups.Select(g => g.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var key in rows.Keys.Where(k => !wanted.Contains(k)).ToArray()) { rows[key].Dispose(); rows.Remove(key); }
            foreach (var key in sections.Keys.Where(k => !wantedGroupNames.Contains(k)).ToArray()) { sections[key].Children.Clear(); sections.Remove(key); }
            var wantedSections = new List<UIElement>();
            foreach (var group in owner.state.Groups)
            {
                if (!sections.TryGetValue(group.Name, out var section)) { section = new() { UseLayoutRounding = false }; section.Children.Add(Text("SpotlightGroupLabel")); sections[group.Name] = section; }
                var label = (TextBlock)section.Children[0]; label.FontWeight = FontWeights.SemiBold; owner.SetText(label, 11, 16.5, owner.Brush("Secondary")); label.CharacterSpacing = 50; label.Text = BleeckerTextCasing.Uppercase(group.Name, owner.Language); label.Margin = new(12 * f, 0, 12 * f, 4 * f); AutomationProperties.SetName(label, group.Name);
                var wantedRows = new List<UIElement> { label };
                for (var i = 0; i < group.Actions.Count; i++) { var action = group.Actions[i]; var key = (group.Name, action.Id); if (!rows.TryGetValue(key, out var row)) rows[key] = row = new(owner, action); row.Set(action); row.Margin = new(0, i > 0 ? 4 * f : 0, 0, 0); wantedRows.Add(row); }
                Sync(section, wantedRows); wantedSections.Add(section);
            }
            if (owner.state.Groups.Count == 0) { empty.Text = owner.EmptyMessage; owner.SetText(empty, 14 * f, 20 * f, owner.Brush("Secondary")); empty.TextAlignment = TextAlignment.Center; empty.HorizontalAlignment = HorizontalAlignment.Stretch; empty.Margin = new(12 * f, 48 * f, 12 * f, 48 * f); wantedSections.Add(empty); }
            Sync(groups, wantedSections);
            UpdateInput(); InvalidateMeasure();
            if (lastSelected != owner.state.SelectedId)
            {
                lastSelected = owner.state.SelectedId; var selected = rows.Values.FirstOrDefault(r => r.Action.Id == lastSelected);
                if (selected is not null && !owner.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (selected.IsLoaded && owner.SurfaceInputAllowed) selected.StartBringIntoView(new() { AnimationDesired = false }); })) throw new InvalidOperationException("Spotlight could not scroll its selected row.");
            }
        }
        private static void Sync(Panel parent, IReadOnlyList<UIElement> wanted)
        {
            var wantedSet = wanted.ToHashSet();
            for (var i = parent.Children.Count - 1; i >= 0; i--) if (!wantedSet.Contains(parent.Children[i])) parent.Children.RemoveAt(i);
            for (var i = 0; i < wanted.Count; i++) { if (i < parent.Children.Count && ReferenceEquals(parent.Children[i], wanted[i])) continue; var existing = parent.Children.IndexOf(wanted[i]); if (existing >= 0) parent.Children.Move((uint)existing, (uint)i); else parent.Children.Insert(i, wanted[i]); }
        }
        internal void UpdateInput() { Editor.IsEnabled = owner.SurfaceInputAllowed; foreach (var row in rows.Values) row.IsEnabled = owner.SurfaceInputAllowed && !row.Action.Disabled && owner.ActionCommand?.CanExecute(row.Action) != false; }
        protected override Size MeasureOverride(Size availableSize) { var hh = 48 * owner.Rem + 1; headerBorder.Measure(new(availableSize.Width, hh)); list.Measure(new(availableSize.Width, MaximumListHeight)); return new(availableSize.Width, hh + list.DesiredSize.Height); }
        protected override Size ArrangeOverride(Size finalSize) { var hh = 48 * owner.Rem + 1; headerBorder.Arrange(new(0, 0, finalSize.Width, hh)); list.Arrange(new(0, hh, finalSize.Width, Math.Max(0, finalSize.Height - hh))); return finalSize; }
        public void Dispose() { Editor.TextChanged -= EditorChanged; Editor.RemoveHandler(KeyDownEvent, keyHandler); foreach (var row in rows.Values) row.Dispose(); rows.Clear(); groups.Children.Clear(); sections.Clear(); listContent.Child = null; list.Content = null; Children.Clear(); }
    }
    private sealed partial class Header : Panel
    {
        private readonly BleeckerCommandSpotlight owner;
        private readonly TextBox editor;
        private readonly TextBlock placeholder;
        private readonly BleeckerKbd hint;
        internal Header(BleeckerCommandSpotlight owner, TextBox editor, TextBlock placeholder)
        {
            this.owner = owner; this.editor = editor; this.placeholder = placeholder; UseLayoutRounding = false; FlowDirection = FlowDirection.LeftToRight; hint = owner.Hint("SpotlightHeaderHint", owner.HotkeyKeys); Children.Add(SearchIcon(18, owner.Brush("Secondary"))); Children.Add(editor); Children.Add(placeholder); Children.Add(hint);
        }
        internal void Update() { if (!(hint.Keys?.SequenceEqual(owner.HotkeyKeys) ?? false)) hint.Keys = owner.HotkeyKeys; owner.UpdateHint(hint); Children.RemoveAt(0); Children.Insert(0, SearchIcon(18, owner.Brush("Secondary"))); InvalidateMeasure(); }
        protected override Size MeasureOverride(Size availableSize) { hint.Measure(new(double.PositiveInfinity, 24 * owner.Rem)); editor.Measure(new(Math.Max(0, availableSize.Width - 32 * owner.Rem - 18 - 24 * owner.Rem - hint.DesiredSize.Width), 24 * owner.Rem)); placeholder.Measure(editor.DesiredSize); return new(availableSize.Width, 48 * owner.Rem); }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var f = owner.Rem; var hw = hint.DesiredSize.Width; var iw = Math.Max(0, finalSize.Width - 32 * f - 18 - 24 * f - hw); var rtl = owner.FlowDirection == FlowDirection.RightToLeft;
            void Place(UIElement e, double x, double y, double w, double h) => e.Arrange(new(rtl ? finalSize.Width - x - w : x, y, w, h));
            Place(Children[0], 16 * f, (finalSize.Height - 18) / 2, 18, 18); Place(editor, 16 * f + 18 + 12 * f, 12 * f, iw, 24 * f); Place(placeholder, 16 * f + 18 + 12 * f, 12 * f, iw, 24 * f); Place(hint, finalSize.Width - 16 * f - hw, (finalSize.Height - 20 * f) / 2, hw, 20 * f); return finalSize;
        }
    }
    private sealed class Row : Button, IDisposable
    {
        private readonly BleeckerCommandSpotlight owner;
        private readonly RowContent layout;
        private bool hover;
        internal BleeckerCommandSpotlightAction Action { get; private set; }
        internal Row(BleeckerCommandSpotlight owner, BleeckerCommandSpotlightAction action)
        {
            this.owner = owner; Action = action; UseLayoutRounding = false; Name = "SpotlightAction"; Style = (Style)owner.ResourcesForContent()["BleeckerCommandSpotlightActionStyle"]; layout = new(owner); Content = layout;
            Click += Clicked; PointerEntered += Entered; PointerExited += Exited; GotFocus += FocusChanged; LostFocus += FocusChanged;
        }
        private void Clicked(object sender, RoutedEventArgs args) => owner.InvokeAction(Action);
        private void Entered(object sender, PointerRoutedEventArgs args) { if (!owner.SurfaceInputAllowed || Action.Disabled) return; hover = true; owner.state.Hover(Action); owner.body?.Update(); }
        private void Exited(object sender, PointerRoutedEventArgs args) { hover = false; Set(Action); }
        private void FocusChanged(object sender, RoutedEventArgs args) => Set(Action);
        internal void Set(BleeckerCommandSpotlightAction action)
        {
            Action = action; Tag = action; var selected = action.Id == owner.state.SelectedId; var color = selected ? owner.Brush("SelectedForeground") : owner.Foreground;
            Background = selected ? owner.selectedFill!.Brush : hover ? owner.Brush("RowHover") : new SolidColorBrush(Microsoft.UI.Colors.Transparent); Foreground = color;
            Padding = new(12 * owner.Rem, 10 * owner.Rem, 12 * owner.Rem, 10 * owner.Rem); Opacity = action.Disabled ? .4 : 1; Height = (action.Description is { Length: > 0 } ? 56 : 40) * owner.Rem; CornerRadius = new(7);
            BorderBrush = FocusState == FocusState.Keyboard ? owner.Brush("Focus") : null; AutomationProperties.SetName(this, action.Title); AutomationProperties.SetHelpText(this, action.Description ?? ""); layout.Update(action, color);
        }
        public void Dispose() { Click -= Clicked; PointerEntered -= Entered; PointerExited -= Exited; GotFocus -= FocusChanged; LostFocus -= FocusChanged; layout.Release(); Content = null; }
    }
    private sealed partial class RowContent : Panel
    {
        private readonly BleeckerCommandSpotlight owner;
        private readonly TextBlock title = Text("SpotlightActionTitle", true), description = Text("SpotlightActionDescription");
        private readonly StackPanel texts = new() { UseLayoutRounding = false };
        private readonly ContentPresenter icon = new() { Name = "SpotlightActionIconContent", HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
        private readonly Border iconFrame;
        private readonly BleeckerKbd hint;
        internal RowContent(BleeckerCommandSpotlight owner)
        {
            this.owner = owner; UseLayoutRounding = false; FlowDirection = FlowDirection.LeftToRight; texts.Children.Add(title); texts.Children.Add(description); iconFrame = new() { Name = "SpotlightActionIcon", Child = icon, CornerRadius = new(7), BorderThickness = new(1), UseLayoutRounding = false }; hint = owner.Hint("SpotlightActionHint", []); Children.Add(iconFrame); Children.Add(texts); Children.Add(hint);
        }
        internal void Update(BleeckerCommandSpotlightAction action, Brush foreground)
        {
            var f = owner.Rem; title.Text = action.Title; description.Text = action.Description ?? ""; description.Visibility = description.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible; owner.SetText(title, 14 * f, 20 * f, foreground); owner.SetText(description, 12 * f, 16 * f, owner.Brush("Secondary"));
            iconFrame.Visibility = action.Icon is null ? Visibility.Collapsed : Visibility.Visible; if (!ReferenceEquals(icon.Content, action.Icon)) icon.Content = action.Icon; if (!ReferenceEquals(icon.ContentTemplate, action.IconTemplate)) icon.ContentTemplate = action.IconTemplate; icon.Foreground = owner.Brush("Secondary"); iconFrame.Width = iconFrame.Height = 32 * f; iconFrame.Background = owner.Brush("IconBackground"); iconFrame.BorderBrush = owner.Brush("IconBorder");
            var shortcut = action.Shortcut?.ToArray() ?? []; hint.Visibility = shortcut.Length == 0 ? Visibility.Collapsed : Visibility.Visible; if (!(hint.Keys?.SequenceEqual(shortcut) ?? false)) hint.Keys = shortcut; owner.UpdateHint(hint); InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            var f = owner.Rem; var hasIcon = iconFrame.Visibility == Visibility.Visible; var hasHint = hint.Visibility == Visibility.Visible; hint.Measure(new(double.PositiveInfinity, 20 * f)); iconFrame.Measure(new(32 * f, 32 * f)); texts.Measure(new(Math.Max(0, availableSize.Width - (hasIcon ? 44 * f : 0) - (hasHint ? hint.DesiredSize.Width + 12 * f : 0)), availableSize.Height)); return new(availableSize.Width, Math.Max(hasIcon ? 32 * f : 0, texts.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var f = owner.Rem; var rtl = owner.FlowDirection == FlowDirection.RightToLeft; var hasIcon = iconFrame.Visibility == Visibility.Visible; var width = Math.Min(texts.DesiredSize.Width, Math.Max(0, finalSize.Width - (hasIcon ? 44 * f : 0) - (hint.Visibility == Visibility.Visible ? hint.DesiredSize.Width + 12 * f : 0)));
            void Place(UIElement element, double x, double y, double w, double h) => element.Arrange(new(rtl ? finalSize.Width - x - w : x, y, w, h));
            Place(iconFrame, 0, (finalSize.Height - 32 * f) / 2, hasIcon ? 32 * f : 0, hasIcon ? 32 * f : 0); Place(texts, hasIcon ? 44 * f : 0, (finalSize.Height - texts.DesiredSize.Height) / 2, width, texts.DesiredSize.Height); Place(hint, finalSize.Width - hint.DesiredSize.Width, (finalSize.Height - 20 * f) / 2, hint.DesiredSize.Width, 20 * f); return finalSize;
        }
        internal void Release() { icon.Content = null; icon.ContentTemplate = null; Children.Clear(); }
    }
}
