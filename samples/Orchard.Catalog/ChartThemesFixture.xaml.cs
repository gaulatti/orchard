using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System.Runtime.InteropServices.WindowsRuntime;
using Path = System.IO.Path;

namespace Orchard.Catalog;

public sealed partial class ChartThemesFixture : UserControl
{
    private const string Namespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private readonly List<(string Key, Rectangle Swatch)> roles = [], palette = [], scoped = [];
    private readonly SolidColorBrush brandSea = new(Windows.UI.Color.FromArgb(255,25,115,145));
    private readonly Line gridLine, axisLine;
    private readonly TextBlock tick, tooltipLabel, tooltipItem;
    private readonly Border tooltipSurface;

    public ChartThemesFixture()
    {
        InitializeComponent();
        BrandScope.Resources["BleeckerSeaBrush"] = brandSea;
        foreach(var role in BleeckerChartTheme.Colors) AddSwatch(Roles,roles,role.Value,role.Key,6);
        for(var index=0;index<BleeckerChartTheme.Palette.Count;index++)
        {
            var key=BleeckerChartTheme.PickColor(index)!;
            AddSwatch(Palette,palette,key,$"{index+1}",10);
            AddSwatch(ScopedPalette,scoped,key,$"{index+1}",10);
        }
        var grid=BleeckerChartTheme.Grid; var axis=BleeckerChartTheme.Axis; var tooltip=BleeckerChartTheme.Tooltip;
        var sample=(Grid)XamlReader.Load(FormattableString.Invariant($$"""
            <Grid xmlns="{{Namespace}}" Height="84">
             <Line x:Name="GridLine" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" X1="24" X2="296" Y1="24" Y2="24" Stroke="{ThemeResource {{grid.StrokeResourceKey}}}" StrokeDashArray="{{string.Join(",",grid.StrokeDashes.Select(value=>value.ToString(CultureInfo.InvariantCulture)))}}" StrokeThickness="1" />
             <Line x:Name="AxisLine" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" X1="24" X2="296" Y1="60" Y2="60" Stroke="{ThemeResource {{axis.StrokeResourceKey}}}" StrokeThickness="1" />
             <TextBlock x:Name="Tick" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Text="$100" Margin="24,64,0,0" FontFamily="{StaticResource BleeckerPrimaryFontFamily}" FontSize="{{axis.TickFontSize}}" Foreground="{ThemeResource {{axis.TickForegroundResourceKey}}}" />
             <Border x:Name="TooltipSurface" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" HorizontalAlignment="Right" VerticalAlignment="Top" Padding="12" Background="{ThemeResource {{tooltip.BackgroundResourceKey}}}" BorderBrush="{ThemeResource {{tooltip.BorderResourceKey}}}" BorderThickness="{{tooltip.BorderThickness}}" CornerRadius="{{tooltip.CornerRadius}}">
              <StackPanel Spacing="4"><TextBlock x:Name="TooltipLabel" Text="Harbor" FontFamily="{StaticResource BleeckerPrimaryFontFamily}" FontWeight="{{tooltip.LabelFontWeight}}" Foreground="{ThemeResource {{tooltip.LabelForegroundResourceKey}}}" /><TextBlock x:Name="TooltipItem" Text="Series: $100" FontFamily="{StaticResource BleeckerPrimaryFontFamily}" Foreground="{ThemeResource {{tooltip.ItemForegroundResourceKey}}}" /></StackPanel>
             </Border>
            </Grid>
            """));
        StyleHost.Children.Add(sample);
        gridLine=(Line)sample.FindName("GridLine"); axisLine=(Line)sample.FindName("AxisLine"); tick=(TextBlock)sample.FindName("Tick");
        tooltipSurface=(Border)sample.FindName("TooltipSurface"); tooltipLabel=(TextBlock)sample.FindName("TooltipLabel"); tooltipItem=(TextBlock)sample.FindName("TooltipItem");
    }

    private static void AddSwatch(Grid host,List<(string Key,Rectangle Swatch)> entries,string key,string label,int columns)
    {
        if(host.ColumnDefinitions.Count==0) for(var index=0;index<columns;index++)host.ColumnDefinitions.Add(new ColumnDefinition());
        var row=entries.Count/columns;
        if(host.RowDefinitions.Count<=row)host.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        // A real ThemeResource expression retains the mounted element's scope.
        // Resolving a brush from Application.Resources would lose local brands.
        var swatch=(Rectangle)XamlReader.Load($"<Rectangle xmlns=\"{Namespace}\" Height=\"28\" Fill=\"{{ThemeResource {key}}}\" />");
        var cell=new StackPanel{Spacing=4}; cell.Children.Add(swatch);
        var caption=(TextBlock)XamlReader.Load($"<TextBlock xmlns=\"{Namespace}\" FontFamily=\"{{StaticResource BleeckerPrimaryFontFamily}}\" FontSize=\"12\" Foreground=\"{{ThemeResource BleeckerTextPrimaryBrush}}\" TextWrapping=\"Wrap\" />");
        caption.Text=label;cell.Children.Add(caption);
        Grid.SetColumn(cell,entries.Count%columns);Grid.SetRow(cell,row);host.Children.Add(cell);entries.Add((key,swatch));
    }

    internal async Task VerifyAsync(Grid root,BleeckerColorScheme scheme,string folder)
    {
        var originalTheme=root.RequestedTheme;
        var samples=new List<object>();
        try
        {
            foreach(var mode in new[]{scheme,scheme==BleeckerColorScheme.Light?BleeckerColorScheme.Dark:BleeckerColorScheme.Light,scheme})
            {
                root.RequestedTheme=mode==BleeckerColorScheme.Light?ElementTheme.Light:ElementTheme.Dark;
                await AwaitAsync(()=>roles.All(entry=>Matches(entry.Swatch.Fill,Expected(mode,entry.Key))) &&
                    palette.All(entry=>Matches(entry.Swatch.Fill,Expected(mode,entry.Key))) &&
                    scoped.All(entry=>Matches(entry.Swatch.Fill,entry.Key=="BleeckerSeaBrush"?Hex(brandSea):Expected(mode,entry.Key))),
                    "Chart resource references did not retain live root appearance or the local brand scope.");
                VerifyStyles(mode);
                samples.Add(new{mode,roles=roles.Select(entry=>new{entry.Key,color=Hex(entry.Swatch.Fill)}),palette=palette.Select(entry=>Hex(entry.Swatch.Fill)),
                    scoped=scoped.Select(entry=>Hex(entry.Swatch.Fill))});
            }
            Require(BleeckerChartTheme.PickColor(10)==palette[0].Key && BleeckerChartTheme.PickColor(-10)==palette[0].Key &&
                BleeckerChartTheme.PickColor(-1) is null && BleeckerChartTheme.PickColor(.5) is null && BleeckerChartTheme.PickColor(double.NaN) is null,
                "Native chart palette selection changed the frozen remainder behavior.");
            using var input=new NativeInputSession(XamlRoot,folder,$"chart-theme-{scheme}");await input.AcquireAsync(Palette);await input.MoveAsync(Palette);
            var paint=new List<object>{new{phase="initial",pixels=await CapturePaintAsync(input,folder,scheme,"initial")}};
            var originalColor=brandSea.Color;
            try
            {
                brandSea.Color=Windows.UI.Color.FromArgb(255,145,55,95);
                await AwaitAsync(()=>ReferenceEquals(scoped[0].Swatch.Fill,brandSea) && Matches(scoped[0].Swatch.Fill,Hex(brandSea)),
                    "Mounted chart swatch lost its consumer-owned mutable brush.");
                Require(Matches(palette[0].Swatch.Fill,Expected(scheme,palette[0].Key)),"Local chart brand mutation escaped its scope.");
                paint.Add(new{phase="mutated",pixels=await CapturePaintAsync(input,folder,scheme,"mutated")});
            }
            finally{brandSea.Color=originalColor;}
            paint.Add(new{phase="restored",pixels=await CapturePaintAsync(input,folder,scheme,"restored")});
            await File.WriteAllTextAsync(Path.Combine(folder,$"chart-theme-{scheme}.json"),JsonSerializer.Serialize(new{passed=true,samples,paint}));
        }
        finally{root.RequestedTheme=originalTheme;}
    }
    private async Task<List<object>> CapturePaintAsync(NativeInputSession input,string folder,BleeckerColorScheme scheme,string phase)
    {
        var presented=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var frames=0;
        void Frame(object? sender,object args){if(++frames==2)presented.TrySetResult();}
        CompositionTarget.Rendering+=Frame;
        try{await presented.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{CompositionTarget.Rendering-=Frame;}
        await input.RequireForegroundAsync();
        var entries=palette.Concat(scoped).Select(entry=>new{entry.Key,entry.Swatch,bounds=input.ScreenBounds(entry.Swatch)}).ToArray();
        var path=Path.Combine(folder,$"chart-theme-{scheme}-{phase}-window.png");
        var client=await NativeScreenCapture.CaptureClientAsync(input.Window,path);
        using var stream=File.OpenRead(path);using var random=stream.AsRandomAccessStream();
        var decoder=await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);
        var pixels=(await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,Windows.Graphics.Imaging.BitmapAlphaMode.Ignore,
            new Windows.Graphics.Imaging.BitmapTransform(),Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation,
            Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
        var paint=new List<object>();
        foreach(var entry in entries)
        {
            Require(entry.bounds==input.ScreenBounds(entry.Swatch),"Chart swatch moved during composed capture encoding.");
            var x=(int)Math.Floor(entry.bounds.X+entry.bounds.Width/2-client.X);var y=(int)Math.Floor(entry.bounds.Y+entry.bounds.Height/2-client.Y);
            Require(x>=0&&y>=0&&x<decoder.PixelWidth&&y<decoder.PixelHeight,"Chart palette swatch is outside the composed client capture.");
            var offset=checked((int)((y*decoder.PixelWidth+x)*4));var expected=((SolidColorBrush)entry.Swatch.Fill).Color;
            Require(pixels[offset]==expected.B&&pixels[offset+1]==expected.G&&pixels[offset+2]==expected.R&&pixels[offset+3]==255,
                $"Composed chart swatch {entry.Key} did not render its authoritative brush.");
            paint.Add(new{entry.Key,x,y,color=Hex(entry.Swatch.Fill)});
        }
        return paint;
    }
    private void VerifyStyles(BleeckerColorScheme mode)
    {
        var grid=BleeckerChartTheme.Grid;var axis=BleeckerChartTheme.Axis;var tooltip=BleeckerChartTheme.Tooltip;
        Require(Matches(gridLine.Stroke,Expected(mode,grid.StrokeResourceKey))&&gridLine.StrokeDashArray.SequenceEqual(grid.StrokeDashes)&&
            Matches(axisLine.Stroke,Expected(mode,axis.StrokeResourceKey))&&Matches(tick.Foreground,Expected(mode,axis.TickForegroundResourceKey))&&tick.FontSize==axis.TickFontSize,
            "Native chart axis or grid did not consume the frozen defaults.");
        Require(Matches(tooltipSurface.Background,Expected(mode,tooltip.BackgroundResourceKey))&&Matches(tooltipSurface.BorderBrush,Expected(mode,tooltip.BorderResourceKey))&&
            tooltipSurface.BorderThickness.Top==tooltip.BorderThickness&&tooltipSurface.CornerRadius.TopLeft==tooltip.CornerRadius&&
            Matches(tooltipItem.Foreground,Expected(mode,tooltip.ItemForegroundResourceKey))&&Matches(tooltipLabel.Foreground,Expected(mode,tooltip.LabelForegroundResourceKey))&&tooltipLabel.FontWeight.Weight==tooltip.LabelFontWeight,
            "Native chart tooltip surface did not consume its chart-specific defaults.");
    }
    private static string Expected(BleeckerColorScheme scheme,string resourceKey)
    {
        var role=resourceKey["Bleecker".Length..^"Brush".Length];role=char.ToLowerInvariant(role[0])+role[1..];
        return BleeckerTokens.Colors(scheme)[role];
    }
    private static string Hex(Brush brush){var color=((SolidColorBrush)brush).Color;return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";}
    private static bool Matches(Brush brush,string expected)=>brush is SolidColorBrush solid&&solid.Opacity==1&&Hex(brush)==expected;
    private static async Task AwaitAsync(Func<bool> condition,string message)
    {
        var clock=System.Diagnostics.Stopwatch.StartNew();while(!condition()&&clock.Elapsed<TimeSpan.FromSeconds(2))await Task.Delay(16);
        Require(condition(),message);
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
