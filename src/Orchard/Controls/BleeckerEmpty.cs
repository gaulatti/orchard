using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

/// <summary>A passive empty state with consumer-owned icon and action content.</summary>
public sealed class BleeckerEmpty : Control
{
    private readonly ResourceDictionary palette = new() { Source = new Uri("ms-appx:///Orchard/Themes/EmptyPalette.g.xaml") };
    private readonly AccessibilitySettings accessibility = new();
    private readonly UISettings settings = new();
    private Grid? host;
    private EmptyLayout? layout;
    private bool updatingMinimum, minimumWidthOwned, minimumHeightOwned;

    public BleeckerEmpty()
    {
        DefaultStyleKey = typeof(BleeckerEmpty);
        Resources.MergedDictionaries.Add(palette);
        RegisterPropertyChangedCallback(PaddingProperty, (_, _) => UpdateMinimum());
        RegisterPropertyChangedCallback(BorderThicknessProperty, (_, _) => UpdateMinimum());
        RegisterPropertyChangedCallback(StyleProperty, (_, _) => UpdateMinimum());
        RegisterPropertyChangedCallback(MinWidthProperty, (_, _) => { if(!updatingMinimum){minimumWidthOwned=false;if(ReadLocalValue(MinWidthProperty)==DependencyProperty.UnsetValue)UpdateMinimum();} });
        RegisterPropertyChangedCallback(MinHeightProperty, (_, _) => { if(!updatingMinimum){minimumHeightOwned=false;if(ReadLocalValue(MinHeightProperty)==DependencyProperty.UnsetValue)UpdateMinimum();} });
        foreach (var property in new[] { ForegroundProperty, FontFamilyProperty, FontSizeProperty, FontWeightProperty,
            FontStyleProperty, FontStretchProperty, CharacterSpacingProperty, IsTextScaleFactorEnabledProperty,
            FlowDirectionProperty, LanguageProperty }) RegisterPropertyChangedCallback(property, (_, _) => Refresh());
        ActualThemeChanged += (_, _) => Refresh();
        Loaded += (_, _) => { settings.ColorValuesChanged += ColorsChanged; Refresh(); };
        Unloaded += (_, _) => { settings.ColorValuesChanged -= ColorsChanged; layout?.ClearShadow(); };
    }
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(BleeckerEmpty), new PropertyMetadata("", Changed));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(BleeckerEmpty), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(BleeckerEmpty), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty ActionProperty = DependencyProperty.Register(nameof(Action), typeof(object), typeof(BleeckerEmpty), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(BleeckerEmpty), new PropertyMetadata(20d, (owner, args) =>
    {
        if (!double.IsFinite((double)args.NewValue) || (double)args.NewValue < 0) throw new ArgumentOutOfRangeException(nameof(Spacing));
        ((BleeckerEmpty)owner).Refresh();
    }));
    public static readonly DependencyProperty IconTemplateProperty = DependencyProperty.Register(nameof(IconTemplate), typeof(DataTemplate), typeof(BleeckerEmpty), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty ActionTemplateProperty = DependencyProperty.Register(nameof(ActionTemplate), typeof(DataTemplate), typeof(BleeckerEmpty), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty IconTemplateSelectorProperty = DependencyProperty.Register(nameof(IconTemplateSelector), typeof(DataTemplateSelector), typeof(BleeckerEmpty), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty ActionTemplateSelectorProperty = DependencyProperty.Register(nameof(ActionTemplateSelector), typeof(DataTemplateSelector), typeof(BleeckerEmpty), new PropertyMetadata(null, Changed));
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public object? Action { get => GetValue(ActionProperty); set => SetValue(ActionProperty, value); }
    public double Spacing { get => (double)GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public DataTemplate? IconTemplate { get => (DataTemplate?)GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public DataTemplate? ActionTemplate { get => (DataTemplate?)GetValue(ActionTemplateProperty); set => SetValue(ActionTemplateProperty, value); }
    public DataTemplateSelector? IconTemplateSelector { get => (DataTemplateSelector?)GetValue(IconTemplateSelectorProperty); set => SetValue(IconTemplateSelectorProperty, value); }
    public DataTemplateSelector? ActionTemplateSelector { get => (DataTemplateSelector?)GetValue(ActionTemplateSelectorProperty); set => SetValue(ActionTemplateSelectorProperty, value); }
    private static void Changed(DependencyObject sender, DependencyPropertyChangedEventArgs args) => ((BleeckerEmpty)sender).Refresh();
    private void ColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(Refresh);
    private Brush Paint(string name) => (Brush)((ResourceDictionary)palette.ThemeDictionaries[accessibility.HighContrast ? "HighContrast" : ActualTheme == ElementTheme.Dark ? "Default" : "Light"])["Empty" + name];
    private void Refresh() { layout?.Refresh(); InvalidateMeasure(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer(); }
    private bool Styled(DependencyProperty property)
    {
        for(var style=Style;style is not null;style=style.BasedOn)if(style.Setters.OfType<Setter>().Any(setter=>setter.Property==property))return true;
        return false;
    }
    private void UpdateMinimum()
    {
        if(updatingMinimum)return;updatingMinimum=true;
        try
        {
            // CSS border-box sizing cannot become smaller than its padding and
            // border. Express that minimum before WinUI clips DesiredSize.
            void Update(DependencyProperty property,double value,ref bool owned)
            {
                if(Styled(property)){if(owned)ClearValue(property);owned=false;return;}
                if(owned||ReadLocalValue(property)==DependencyProperty.UnsetValue){owned=true;if((double)GetValue(property)!=value)SetValue(property,value);}
            }
            Update(MinWidthProperty,Padding.Left+Padding.Right+BorderThickness.Left+BorderThickness.Right,ref minimumWidthOwned);
            Update(MinHeightProperty,Padding.Top+Padding.Bottom+BorderThickness.Top+BorderThickness.Bottom,ref minimumHeightOwned);
        }
        finally{updatingMinimum=false;}
    }
    protected override void OnApplyTemplate()
    {
        layout?.Release(); host?.Children.Clear();
        base.OnApplyTemplate();
        UpdateMinimum();
        host = GetTemplateChild("PART_LayoutHost") as Grid ?? throw new InvalidOperationException("BleeckerEmpty requires PART_LayoutHost.");
        layout = new(this); host.Children.Add(layout); Refresh();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var horizontal = Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;
        var vertical = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;
        var width = double.IsNaN(Width) ? availableSize.Width : Width;
        if (double.IsPositiveInfinity(width)) width = (layout?.MaximumWidth ?? 0) + horizontal;
        width = Math.Max(horizontal, Math.Max(MinWidth, Math.Min(MaxWidth, width)));
        base.MeasureOverride(new(width, double.PositiveInfinity));
        var height = double.IsNaN(Height) ? (layout?.StackHeight ?? 0) + vertical : Height;
        return new(width, Math.Max(vertical, Math.Max(MinHeight, Math.Min(MaxHeight, height))));
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new EmptyPeer(this);
    private sealed class EmptyPeer(BleeckerEmpty owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerEmpty);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetNameCore() => AutomationProperties.GetName(owner);
        protected override bool IsControlElementCore() => AutomationProperties.GetName(owner).Length > 0;
        protected override bool IsContentElementCore() => IsControlElementCore();
    }
    private static bool Numeric(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or BigInteger;
    private static bool Present(object? value) => value switch { null or false => false, string text => text.Length > 0, BigInteger integer => integer != BigInteger.Zero, _ when Numeric(value) => Convert.ToDouble(value, CultureInfo.InvariantCulture) is var number && number != 0 && !double.IsNaN(number), _ => true };
    private static string Scalar(object? value) => value switch { null or bool => "", double number => BleeckerNumberText.Format(number), float number => BleeckerNumberText.Format(double.Parse(number.ToString("R", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)), decimal number when number == 0 => "0", IFormattable number => number.ToString(null, CultureInfo.InvariantCulture), _ => value.ToString()! };
    private static string Collapse(string value) => Regex.Replace(value, "[ \\t\\r\\n\\f]+", " ").Trim(' ');
    // Reference centering truncates signed 1/64px offsets toward zero.
    private static double Center(double extent,double size) => Math.Truncate((extent-size)*32)/64;

    private sealed class EmptyLayout : Panel
    {
        private readonly BleeckerEmpty owner;
        private readonly Grid iconHost = new() { Name = "EmptyIconHost", UseLayoutRounding = false };
        private readonly BleeckerIconBadge icon = new() { Name = "EmptyIcon", Size = BleeckerIconBadgeSize.Medium };
        private readonly Grid shadow = new() { IsHitTestVisible = false };
        private readonly Canvas masks = new() { IsHitTestVisible = false };
        private readonly ShapePath[] shadowMasks = [new(), new()];
        private readonly Canvas glyph = new() { Name = "EmptyDefaultIcon", Width = 32, Height = 32, UseLayoutRounding = false };
        private readonly TextSlot title = new("EmptyTitle"), description = new("EmptyDescription"), actionText = new("EmptyActionText");
        private readonly Panel body;
        private readonly ContentPresenter action = new() { Name = "EmptyActionContent", UseLayoutRounding = false };
        private readonly Grid actionHost = new() { Name = "EmptyAction", UseLayoutRounding = false };
        private bool hasDescription, hasAction, plainAction;
        private double bodyWidth, bodyHeight, actionWidth, actionHeight;
        internal double StackHeight { get; private set; }
        internal double MaximumWidth => Math.Max(56, Math.Max(title.MaximumWidth, Math.Min(320, description.MaximumWidth)));
        internal EmptyLayout(BleeckerEmpty owner)
        {
            this.owner = owner; UseLayoutRounding = false;
            foreach (var mask in shadowMasks) masks.Children.Add(mask);
            iconHost.Children.Add(masks); iconHost.Children.Add(shadow); iconHost.Children.Add(icon);
            Children.Add(iconHost); body = new BodyPanel(this) { Name = "EmptyBody", UseLayoutRounding = false }; body.Children.Add(title); body.Children.Add(description); Children.Add(body);
            actionHost.Children.Add(actionText); actionHost.Children.Add(action); Children.Add(actionHost);
            var outline = new PathFigure { StartPoint = new(7,8), IsClosed = true, IsFilled = false };
            outline.Segments.Add(new LineSegment { Point = new(25,8) }); outline.Segments.Add(new ArcSegment { Point = new(28,11), Size = new(3,3), SweepDirection = SweepDirection.Clockwise });
            outline.Segments.Add(new LineSegment { Point = new(28,23) }); outline.Segments.Add(new ArcSegment { Point = new(25,26), Size = new(3,3), SweepDirection = SweepDirection.Clockwise });
            outline.Segments.Add(new LineSegment { Point = new(7,26) }); outline.Segments.Add(new ArcSegment { Point = new(4,23), Size = new(3,3), SweepDirection = SweepDirection.Clockwise });
            outline.Segments.Add(new LineSegment { Point = new(4,11) }); outline.Segments.Add(new ArcSegment { Point = new(7,8), Size = new(3,3), SweepDirection = SweepDirection.Clockwise });
            var geometry = new PathGeometry(); geometry.Figures.Add(outline);
            glyph.Children.Add(new ShapePath { Data = geometry, StrokeThickness = 1 });
            glyph.Children.Add(new ShapePath { Data = new LineGeometry { StartPoint = new(4,14), EndPoint = new(28,14) }, StrokeThickness = 1 });
            glyph.Children.Add(new ShapePath { Data = new LineGeometry { StartPoint = new(12,20), EndPoint = new(20,20) }, StrokeThickness = 1, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            AutomationProperties.SetAccessibilityView(glyph, AccessibilityView.Raw);
            AutomationProperties.SetHeadingLevel(title.Text, AutomationHeadingLevel.Level3);
            icon.SizeChanged += (_, _) => DrawShadow(); Loaded += (_, _) => DrawShadow();
        }
        internal void Refresh()
        {
            icon.Background = owner.Paint("IconBackground"); icon.BorderBrush = owner.Paint("IconBorderBrush"); icon.Foreground = owner.Paint("IconForeground");
            icon.FontFamily = owner.FontFamily; icon.FontSize = owner.FontSize; icon.FontWeight = owner.FontWeight;
            var wantedIcon = owner.Icon ?? glyph;
            if (!ReferenceEquals(icon.Content, wantedIcon) || !ReferenceEquals(icon.ContentTemplate, owner.IconTemplate) || !ReferenceEquals(icon.ContentTemplateSelector, owner.IconTemplateSelector))
            { icon.Content = null; icon.ContentTemplate = owner.Icon is null ? null : owner.IconTemplate; icon.ContentTemplateSelector = owner.Icon is null ? null : owner.IconTemplateSelector; icon.Content = wantedIcon; }
            foreach (var shape in glyph.Children.OfType<ShapePath>()) shape.Stroke = owner.Paint("DefaultIconForeground");
            title.Set(owner, owner.Title ?? "", 16, 24, true, owner.Foreground);
            description.Set(owner, owner.Description ?? "", 14, 22.75, false, owner.Paint("DescriptionForeground"));
            hasDescription = !string.IsNullOrEmpty(owner.Description); description.Visibility = hasDescription ? Visibility.Visible : Visibility.Collapsed;
            hasAction = Present(owner.Action); actionHost.Visibility = hasAction ? Visibility.Visible : Visibility.Collapsed;
            plainAction = owner.ActionTemplate is null && owner.ActionTemplateSelector is null && (owner.Action is null or bool or string or char || Numeric(owner.Action));
            if (!plainAction && owner.ActionTemplate is null && owner.ActionTemplateSelector is null && owner.Action is not UIElement) throw new ArgumentException("Empty action models require a DataTemplate, DataTemplateSelector, or UIElement.");
            actionText.Set(owner, plainAction ? Scalar(owner.Action) : "", 16, 24, false, owner.Foreground); actionText.Visibility = plainAction ? Visibility.Visible : Visibility.Collapsed; action.Visibility = plainAction ? Visibility.Collapsed : Visibility.Visible;
            var wantedAction = plainAction ? null : owner.Action;
            if (!ReferenceEquals(action.Content, wantedAction) || !ReferenceEquals(action.ContentTemplate, owner.ActionTemplate) || !ReferenceEquals(action.ContentTemplateSelector, owner.ActionTemplateSelector))
            { action.Content = null; action.ContentTemplate = owner.ActionTemplate; action.ContentTemplateSelector = owner.ActionTemplateSelector; action.Content = wantedAction; }
            action.FontFamily = owner.FontFamily; action.FontSize = owner.FontSize; action.Foreground = owner.Foreground;
            DrawShadow(); InvalidateMeasure();
        }
        internal void Release() { ClearShadow(); icon.Content = null; action.Content = null; }
        protected override Size MeasureOverride(Size available)
        {
            bodyWidth = Math.Min(Math.Max(title.MaximumWidth, Math.Min(320, description.MaximumWidth)), Math.Max(available.Width, Math.Max(title.MinimumWidth, hasDescription ? Math.Min(320, description.MinimumWidth) : 0)));
            if (!hasDescription) bodyWidth = Math.Min(title.MaximumWidth, Math.Max(available.Width, title.MinimumWidth));
            iconHost.Measure(new(56,56)); body.Measure(new(bodyWidth, double.PositiveInfinity)); bodyHeight = body.DesiredSize.Height;
            actionWidth = actionHeight = 0;
            if (hasAction)
            {
                var child = plainAction ? (FrameworkElement)actionText : action;
                var width = plainAction ? Math.Min(actionText.MaximumWidth, Math.Max(available.Width, actionText.MinimumWidth)) : double.PositiveInfinity;
                child.Measure(new(width, double.PositiveInfinity)); actionWidth = plainAction ? width : child.DesiredSize.Width; actionHeight = child.DesiredSize.Height;
                actionHost.Measure(new(actionWidth, actionHeight));
            }
            StackHeight = 56 + owner.Spacing + bodyHeight + (hasAction ? owner.Spacing + 4 + actionHeight : 0);
            return new(double.IsPositiveInfinity(available.Width) ? MaximumWidth : available.Width, StackHeight);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var top = Center(final.Height,StackHeight);
            iconHost.Arrange(new(Center(final.Width,56), top, 56, 56)); top += 56 + owner.Spacing;
            body.Arrange(new(Center(final.Width,bodyWidth), top, bodyWidth, bodyHeight)); top += bodyHeight + owner.Spacing + 4;
            if (hasAction) actionHost.Arrange(new(Center(final.Width,actionWidth), top, actionWidth, actionHeight));
            return final;
        }
        private sealed class BodyPanel(EmptyLayout owner) : Panel
        {
            protected override Size MeasureOverride(Size available)
            {
                owner.title.Measure(new(available.Width,double.PositiveInfinity));
                owner.description.Measure(new(Math.Min(320,available.Width),double.PositiveInfinity));
                return new(available.Width, owner.title.DesiredSize.Height + (owner.hasDescription ? 6 + owner.description.DesiredSize.Height : 0));
            }
            protected override Size ArrangeOverride(Size final)
            {
                owner.title.Arrange(new(0,0,final.Width,owner.title.DesiredSize.Height));
                if (owner.hasDescription) { var width=Math.Min(320,final.Width); owner.description.Arrange(new(owner.owner.FlowDirection==FlowDirection.RightToLeft?final.Width-width:0,owner.title.DesiredSize.Height+6,width,owner.description.DesiredSize.Height)); }
                return final;
            }
        }
        internal void ClearShadow() => Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetElementChildVisual(shadow,null);
        private void DrawShadow()
        {
            ClearShadow(); var visible = IsLoaded && !owner.accessibility.HighContrast;
            masks.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (!visible || icon.ActualWidth <= 0) return;
            var compositor = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(shadow).Compositor;
            var layers = compositor.CreateContainerVisual();
            for (var index=0;index<BleeckerTokens.SurfaceShadows.Count;index++)
            {
                var recipe=BleeckerTokens.SurfaceShadows[index];var mask=shadowMasks[index];var inset=-recipe.Spread;
                mask.Fill=icon.Background;mask.Width=icon.ActualWidth-inset*2;mask.Height=icon.ActualHeight-inset*2;mask.Data=BleeckerShadowGeometry.RoundedMask(mask.Width,mask.Height,icon.CornerRadius,inset);Canvas.SetLeft(mask,inset);Canvas.SetTop(mask,inset);
                var effect=compositor.CreateDropShadow();effect.BlurRadius=recipe.Blur;effect.Offset=new(recipe.X,recipe.Y,0);effect.Color=Windows.UI.Color.FromArgb(255,recipe.R,recipe.G,recipe.B);effect.Opacity=recipe.Opacity;effect.Mask=mask.GetAlphaMask();
                var sprite=compositor.CreateSpriteVisual();sprite.Size=new((float)mask.Width,(float)mask.Height);sprite.Offset=new(inset,inset,0);sprite.Shadow=effect;layers.Children.InsertAtTop(sprite);
            }
            Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetElementChildVisual(shadow,layers);
        }
    }
    private sealed class TextSlot(string name) : Panel
    {
        internal TextBlock Text { get; } = new() { UseLayoutRounding=false, TextWrapping=TextWrapping.NoWrap,TextAlignment=TextAlignment.Center,LineStackingStrategy=LineStackingStrategy.BlockLineHeight };
        private readonly TextBlock measure=new();
        private readonly BleeckerReferenceText.IntrinsicWidth intrinsic=new();
        private readonly BleeckerReferenceText.Presentation presentation=new();
        private string logical="";
        private double paintWidth;
        internal double MaximumWidth => WidthOf(logical);
        internal double MinimumWidth => logical.Length==0?0:logical.Split(' ').Max(WidthOf);
        private double WidthOf(string value) { BleeckerReferenceText.Set(measure,value);return intrinsic.Measure(measure,value); }
        internal void Set(BleeckerEmpty owner,string value,double size,double line,bool semibold,Brush foreground)
        {
            Name=name;UseLayoutRounding=false;if(Children.Count==0)Children.Add(Text);
            var family=semibold&&owner.FontFamily.Source.Contains("/Orchard/Assets/Fonts/EncodeSans-Regular.ttf",StringComparison.Ordinal)?(FontFamily)Application.Current.Resources["BleeckerPrimarySemiboldFontFamily"]:owner.FontFamily;
            Text.FontFamily=measure.FontFamily=family;Text.FontSize=measure.FontSize=owner.FontSize*size/16;Text.FontWeight=measure.FontWeight=semibold?Microsoft.UI.Text.FontWeights.SemiBold:owner.FontWeight;Text.Foreground=foreground;Text.FontStyle=measure.FontStyle=owner.FontStyle;Text.FontStretch=measure.FontStretch=owner.FontStretch;Text.CharacterSpacing=measure.CharacterSpacing=owner.CharacterSpacing;Text.IsTextScaleFactorEnabled=measure.IsTextScaleFactorEnabled=owner.IsTextScaleFactorEnabled;Text.FlowDirection=measure.FlowDirection=owner.FlowDirection;if(Text.Language!=owner.Language)Text.Language=owner.Language;if(measure.Language!=owner.Language)measure.Language=owner.Language;Text.LineHeight=owner.FontSize*line/16;
            if(semibold)Text.CharacterSpacing=measure.CharacterSpacing=owner.CharacterSpacing+(int)(BleeckerTokens.LetterSpacing["refined"]*1000);
            logical=Collapse(value);Text.Visibility=logical.Length==0?Visibility.Collapsed:Visibility.Visible;if(logical.Length==0&&Text.Text.Length>0)Text.Text="";InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            if(logical.Length==0){paintWidth=0;return new(available.Width,0);}
            var lines=new List<string>();var current="";foreach(var word in logical.Split(' ')){var next=current.Length==0?word:current+" "+word;if(current.Length>0&&WidthOf(next)>available.Width){lines.Add(current);current=word;}else current=next;}lines.Add(current);
            paintWidth=Math.Max(available.Width,lines.Max(WidthOf));var rendered=string.Join('\n',lines);presentation.Update(Text,rendered);Text.Measure(new(paintWidth,double.PositiveInfinity));return new(available.Width,Text.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final){Text.Arrange(new(Center(final.Width,paintWidth),0,paintWidth,final.Height));return final;}
    }
}
