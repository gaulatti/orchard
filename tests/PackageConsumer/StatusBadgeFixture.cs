using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class StatusBadgeFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child;
    }

    internal static async Task RunAsync(Window window, string output, string hostAssembly, Func<FrameworkElement>? catalogFactory = null)
    {
        var states = new List<object>(); var contracts = new List<object>(); var catalogs = new List<object>();
        var stage = "initial"; var guards = 0; var focus = 0; var activations = 0;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        try
        {
            var root = new Canvas { Width = 760, Height = 240, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 68, 85, 102)) };
            window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(760, 240));
            root.GotFocus += (_, _) => focus++;
            window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
            void Guard()
            {
                guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
                EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var p); visible |= p == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
                Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Status Badge probe left its hidden boundary.");
            }
            async Task Layout()
            {
                for (var i = 0; i < 3; i++) { root.Measure(new Size(760, 240)); root.Arrange(new Rect(0, 0, 760, 240)); root.UpdateLayout(); Guard(); await Task.Delay(16); }
                await NativeRenderReadiness.RenderAsync(root); Guard();
            }
            object Box(FrameworkElement element)
            {
                var r = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                return new { x = r.X, y = r.Y, width = r.Width, height = r.Height };
            }
            async Task<object> Capture(string file)
            {
                Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard();
                Require(bitmap.PixelWidth == 760 && bitmap.PixelHeight == 240, "Status Badge capture dimensions changed.");
                var bytes = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
                using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, 760, 240, 96, 96, bytes); await encoder.FlushAsync(); stream.Seek(0);
                using var reader = new DataReader(stream); await reader.LoadAsync((uint)stream.Size); var png = new byte[(int)stream.Size]; reader.ReadBytes(png);
                await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), png);
                return new { file, sha256 = Convert.ToHexStringLower(SHA256.HashData(png)) };
            }

            T Part<T>(DependencyObject owner,string name) where T:FrameworkElement => Elements(owner).OfType<T>().Single(e=>e.Name==name);
            object Paint(Brush value){var b=(SolidColorBrush)value;return new{color=$"#{b.Color.A:X2}{b.Color.R:X2}{b.Color.G:X2}{b.Color.B:X2}",opacity=b.Opacity};}
            object Snapshot(BleeckerStatusBadge badge)
            {
                var surface=Part<Border>(badge,"PART_Badge");var label=Part<TextBlock>(badge,"PART_Label");var description=Part<TextBlock>(badge,"PART_Description");var dot=Part<FrameworkElement>(badge,"PART_Dot");
                return new{box=Box(badge),surface=Box(surface),label=Box(label),description=Box(description),dot=Box(dot),badge.Label,badge.Description,variant=badge.Variant.ToString(),font=label.FontFamily.Source,label.FontSize,lineHeight=label.LineHeight,descriptionFont=description.FontFamily.Source,descriptionFontSize=description.FontSize,descriptionLineHeight=description.LineHeight,padding=new[]{surface.Padding.Left,surface.Padding.Top,surface.Padding.Right,surface.Padding.Bottom},radius=surface.CornerRadius.TopLeft,borderThickness=surface.BorderThickness.Left,background=Paint(badge.Background),border=Paint(badge.BorderBrush),foreground=Paint(badge.Foreground),dotFill=Paint(((Microsoft.UI.Xaml.Shapes.Ellipse)dot).Fill),descriptionVisible=description.Visibility==Visibility.Visible};
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                root.RequestedTheme=theme;
                foreach(var variant in Enum.GetValues<BleeckerStatusBadgeVariant>())
                {
                    stage=$"{theme}/variant/{variant}";root.Children.Clear();var badge=new BleeckerStatusBadge{Label=variant.ToString().ToUpperInvariant(),Description="Fictional local status",Variant=variant};Canvas.SetLeft(badge,24);Canvas.SetTop(badge,24);root.Children.Add(badge);await Layout();
                    Require(badge.IsLoaded&&!badge.IsTabStop&&Part<Border>(badge,"PART_Badge").ActualHeight==24,"Status Badge lost its normal layout or display-only contract.");
                    states.Add(new{theme=theme.ToString(),id="variant-"+variant,snapshot=Snapshot(badge),image=await Capture($"status-badge-{theme}-{variant}.png")});
                }
                foreach(var flow in new[]{FlowDirection.LeftToRight,FlowDirection.RightToLeft})
                {
                    root.Children.Clear();var badge=new BleeckerStatusBadge{Width=320,Label="REGIONAL COVERAGE TEMPORARILY DEGRADED",Description="Updates continue during local review",Variant=BleeckerStatusBadgeVariant.Live,FlowDirection=flow};root.Children.Add(badge);
                    foreach(var width in new[]{320d,640d})
                    {
                        stage=$"{theme}/layout/{flow}/{width}";badge.Width=width;await Layout();var surface=Part<Border>(badge,"PART_Badge");var description=Part<TextBlock>(badge,"PART_Description");
                        Require(width==320?surface.ActualHeight==39&&Math.Abs(surface.ActualWidth-190.203125)<3&&description.ActualHeight>17:surface.ActualHeight==24&&description.ActualHeight<=17,"Status Badge constrained/wide layout differs from the pinned registered fixture.");
                        states.Add(new{theme=theme.ToString(),id=$"layout-{flow}-{width}",snapshot=Snapshot(badge),image=await Capture($"status-badge-{theme}-{flow}-{width}.png")});
                    }
                }
                root.Children.Clear();var labelSource=new Border{Tag="LOCAL"};var badgeOwner=new BleeckerStatusBadge{Description="Consumer-owned status"};badgeOwner.SetBinding(BleeckerStatusBadge.LabelProperty,new Binding{Source=labelSource,Path=new PropertyPath("Tag")});root.Children.Add(badgeOwner);await Layout();var peer=FrameworkElementAutomationPeer.CreatePeerForElement(badgeOwner);
                void Check(string id,string name,string help)
                {
                    stage=theme+"/"+id;peer.InvalidatePeer();Require(peer.GetName()==name&&peer.GetHelpText()==help,$"Caller metadata differs at {stage}: name='{peer.GetName()}', help='{peer.GetHelpText()}'");Require(badgeOwner.GetBindingExpression(BleeckerStatusBadge.LabelProperty)is not null,"Status Badge lost caller label binding.");contracts.Add(new{theme=theme.ToString(),id,passed=true,name=peer.GetName(),help=peer.GetHelpText(),labelBindingRetained=true});
                }
                Check("default-metadata","LOCAL","Consumer-owned status");
                AutomationProperties.SetName(badgeOwner,"");AutomationProperties.SetHelpText(badgeOwner,"");Check("local-empty-metadata","","");
                labelSource.Tag="UPDATED";badgeOwner.Description="Updated local description";await Layout();Check("empty-after-label-update","","");
                var nameSource=new Border{Tag=""};var helpSource=new Border{Tag=""};badgeOwner.SetBinding(AutomationProperties.NameProperty,new Binding{Source=nameSource,Path=new PropertyPath("Tag")});badgeOwner.SetBinding(AutomationProperties.HelpTextProperty,new Binding{Source=helpSource,Path=new PropertyPath("Tag")});await Layout();Check("bound-empty-metadata","","");
                nameSource.Tag="Caller name";helpSource.Tag="Caller help";await Layout();Check("live-bound-metadata","Caller name","Caller help");Require(badgeOwner.GetBindingExpression(AutomationProperties.NameProperty)is not null&&badgeOwner.GetBindingExpression(AutomationProperties.HelpTextProperty)is not null,"Status Badge replaced caller metadata bindings.");
                badgeOwner.ClearValue(AutomationProperties.NameProperty);badgeOwner.ClearValue(AutomationProperties.HelpTextProperty);Check("clear-restores-default","UPDATED","Updated local description");
                var standard=badgeOwner.Style;var callerBase=new Style{TargetType=typeof(BleeckerStatusBadge),BasedOn=standard};callerBase.Setters.Add(new Setter(AutomationProperties.NameProperty,""));callerBase.Setters.Add(new Setter(AutomationProperties.HelpTextProperty,""));var callerStyle=new Style{TargetType=typeof(BleeckerStatusBadge),BasedOn=callerBase};badgeOwner.Style=callerStyle;await Layout();Check("basedon-style-empty","","");
                AutomationProperties.SetName(badgeOwner,"Local name");AutomationProperties.SetHelpText(badgeOwner,"Local help");Check("local-over-style","Local name","Local help");badgeOwner.ClearValue(AutomationProperties.NameProperty);badgeOwner.ClearValue(AutomationProperties.HelpTextProperty);Check("clear-reveals-style","","");
                root.Children.Remove(badgeOwner);await Layout();badgeOwner.ClearValue(FrameworkElement.StyleProperty);var implicitHost=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:local='using:Orchard'><Grid.Resources><Style x:Key='MetadataBase' TargetType='local:BleeckerStatusBadge'><Setter Property='AutomationProperties.Name' Value=''/><Setter Property='AutomationProperties.HelpText' Value=''/></Style><Style TargetType='local:BleeckerStatusBadge' BasedOn='{StaticResource MetadataBase}'><Setter Property='Tag' Value='implicit-style-applied'/></Style></Grid.Resources></Grid>");implicitHost.Children.Add(badgeOwner);root.Children.Add(implicitHost);await Layout();Require(badgeOwner.Tag as string=="implicit-style-applied","WinUI did not apply the ancestor implicit style.");Check("ancestor-implicit-empty","","");
                implicitHost.Children.Remove(badgeOwner);root.Children.Remove(implicitHost);await Layout();root.Children.Add(badgeOwner);var labeller=new TextBlock{Text=""};root.Children.Add(labeller);AutomationProperties.SetLabeledBy(badgeOwner,labeller);await Layout();Check("empty-labeledby","","Updated local description");badgeOwner.ClearValue(AutomationProperties.LabeledByProperty);Check("remove-label-restores-default","UPDATED","Updated local description");
                var template=badgeOwner.Template;badgeOwner.Template=null;await Layout();badgeOwner.Template=template;await Layout();root.Children.Remove(badgeOwner);await Layout();Require(!badgeOwner.IsLoaded,"Status Badge did not unload.");labelSource.Tag="REMOUNTED";root.Children.Add(badgeOwner);await Layout();Check("template-and-remount","REMOUNTED","Updated local description");
                if(catalogFactory is not null){root.Children.Clear();var fixture=catalogFactory();root.Children.Add(fixture);await Layout();var badges=Elements(fixture).OfType<BleeckerStatusBadge>().ToArray();Require(badges.Length==8&&badges.All(b=>b.IsLoaded&&!b.IsTabStop&&b.ActualWidth>0&&b.ActualHeight>0),"Status Badge catalog variants are missing.");var caller=FrameworkElementAutomationPeer.CreatePeerForElement(badges.Single(b=>b.Name=="Changing"));Require(caller.GetName()==""&&caller.GetHelpText()=="","Registered catalog lost its caller-empty metadata example.");catalogs.Add(new{theme=theme.ToString(),controls=badges.Length,callerEmptyMetadata=true,passed=true});}
            }
            Guard();await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-status-badge-native-v1",passed=true,states,contracts,catalogs,guards,focus,activations,rasterizationScale=root.XamlRoot.RasterizationScale,hardwareInputSent=false,osSettingsChanged=false,nativeARM64RuntimeVerified=false,displayScalingVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerStatusBadge).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-status-badge-native-v1",passed=false,stage,error=error.ToString(),states,contracts,guards,focus,activations,librarySha256=Hash(typeof(BleeckerStatusBadge).Assembly.Location),hostSha256=Hash(hostAssembly)}));Environment.Exit(1);}
    }
}
