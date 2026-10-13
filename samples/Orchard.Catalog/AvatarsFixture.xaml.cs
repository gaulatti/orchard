using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Catalog;

public sealed partial class AvatarsFixture : UserControl
{
    public AvatarsFixture() => InitializeComponent();
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        var samples = new List<object>();
        var pixelSamples = new List<object>();
        foreach (var avatar in Sizes.Children.Concat(EmptySizes.Children).OfType<BleeckerAvatar>())
        {
            VerifyAvatar(avatar, scheme, false, false);
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(avatar);
            Require(!avatar.IsTabStop && !peer.IsControlElement() && peer.IsContentElement() == (avatar.Fallback is not null) &&
                peer.GetPattern(PatternInterface.Invoke) is null, "Avatar acquired an action, focus stop or decorative content semantics.");
            samples.Add(new { avatar.AvatarSize, avatar.ActualWidth, avatar.ActualHeight, avatar.FontSize,
                initials = Part<TextBlock>(avatar, "PART_Initials").Text, inkPixels = await VerifyInkPixelsAsync(avatar) });
        }
        var expected = new[] { "JD", "SG", "SSSS", "東港", "\uD83DH" };
        var unicode = Unicode.Children.OfType<BleeckerAvatar>().ToArray();
        for (var index = 0; index < expected.Length; index++)
            Require(Part<TextBlock>(unicode[index], "PART_Initials").Text == expected[index], "Avatar initials differ from the producer's UTF-16/full-uppercase algorithm.");
        var savedFallback = Medium.Fallback;
        Medium.Fallback = " \t\n ";
        Require(Part<TextBlock>(Medium, "PART_Initials").Visibility == Visibility.Collapsed && Part<Viewbox>(Medium, "PART_Person").Visibility == Visibility.Visible,
            "Whitespace-only fallback did not show the source silhouette.");
        Medium.Fallback = savedFallback;
        Medium.AvatarSize = BleeckerAvatarSize.Small;
        await SettleAsync(); VerifyAvatar(Medium, scheme, false, false);
        Medium.AvatarSize = BleeckerAvatarSize.Medium;
        await SettleAsync();

        var image = await FictionalImageAsync();
        Photo.Source = image;
        foreach (var size in Enum.GetValues<BleeckerAvatarSize>())
        {
            Photo.AvatarSize = size; await SettleAsync();
            Require(Part<Border>(Photo, "PART_Photo").Background is ImageBrush brush && ReferenceEquals(brush.ImageSource, image) &&
                brush.Stretch == Stretch.UniformToFill && Part<Border>(Photo, "PART_Photo").Visibility == Visibility.Visible,
                "Avatar image did not use the consumer source and object-cover geometry.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Photo);
            Require(peer.GetName() == "Fictional color blocks" && peer.GetAutomationControlType() == AutomationControlType.Image &&
                peer.IsContentElement() && peer.IsControlElement(), "Avatar lost its image alt semantics.");
            await CaptureImagePixelsAsync(Photo, Path.Combine(folder, $"avatar-image-{size}-{scheme}.png"), pixelSamples);
        }
        Photo.AvatarSize = BleeckerAvatarSize.ExtraLarge;
        Photo.Alt = ""; await SettleAsync();
        Require(!FrameworkElementAutomationPeer.CreatePeerForElement(Photo).IsContentElement(), "Explicit empty image alt was replaced with a fabricated name.");
        Photo.Alt = "Fictional color blocks";
        // The first theme establishes failure. Subsequent themes keep the
        // component's failure latch, as the producer does.
        FailedImage.Source = new BitmapImage(new Uri("ms-appx:///avatar-fixture-does-not-exist.png"));
        await AwaitAsync(() => Part<Border>(FailedImage, "PART_Photo").Visibility == Visibility.Collapsed && Part<TextBlock>(FailedImage, "PART_Initials").Text == "JD",
            "Failed image did not reach the consumer's initials.");
        FailedImage.Source = image; await SettleAsync();
        Require(ReferenceEquals(FailedImage.Source, image) && Part<Border>(FailedImage, "PART_Photo").Visibility == Visibility.Collapsed &&
            FrameworkElementAutomationPeer.CreatePeerForElement(FailedImage).GetName() == "JD",
            "Replacing a failed image silently retried, rewrote Source or retained image alt semantics.");

        foreach (var group in Groups.Children.OfType<BleeckerAvatarGroup>()) VerifyGroup(group, scheme);
        var sourceCollection = DefaultGroup.Avatars;
        try { DefaultGroup.Avatars = null!; throw new InvalidOperationException("AvatarGroup accepted a null consumer collection."); }
        catch (ArgumentNullException) { }
        Require(ReferenceEquals(DefaultGroup.Avatars, sourceCollection), "Rejected null input replaced the consumer collection.");
        var replacement = new System.Collections.ObjectModel.ObservableCollection<BleeckerAvatarData>(sourceCollection);
        DefaultGroup.Avatars = replacement; await SettleAsync();
        Require(Groups.Children.OfType<BleeckerAvatarGroup>().Where(group => group != EmptyGroup).All(group => ReferenceEquals(group.Avatars, replacement)),
            "A replacement consumer collection did not reach every bound group.");
        DefaultGroup.Avatars = sourceCollection; await SettleAsync();
        var retained = GroupAvatars(DefaultGroup)[0];
        var originalFallback = sourceCollection[0].Fallback;
        sourceCollection[0].Fallback = "ßeta ßeta";
        Require(ReferenceEquals(retained, GroupAvatars(DefaultGroup)[0]) && Part<TextBlock>(retained, "PART_Initials").Text == "SSSS",
            "Consumer item updates did not preserve the indexed Avatar instance.");
        sourceCollection[0].Fallback = originalFallback;
        sourceCollection.Move(0, 5);
        Require(ReferenceEquals(retained, GroupAvatars(DefaultGroup)[0]) && Part<TextBlock>(retained, "PART_Initials").Text == "BR",
            "Collection reorder lost the producer's index-key behavior.");
        sourceCollection.Move(5, 0);
        var item = new BleeckerAvatarData { Fallback = "Grace Vale" };
        sourceCollection.Add(item); await SettleAsync();
        VerifyGroup(DefaultGroup, scheme);
        Require(Part<TextBlock>(GroupAvatars(DefaultGroup)[^1], "PART_Initials").Text == "+3", "Collection addition did not reach overflow.");
        sourceCollection.Remove(item);
        foreach (var maximum in new[] { 0, -2, 9, 4 }) { DefaultGroup.Max = maximum; await SettleAsync(); VerifyGroup(DefaultGroup, scheme); }
        Require(ReferenceEquals(DefaultGroup.Avatars, sourceCollection) && sourceCollection.Count == 6, "AvatarGroup rewrote consumer data.");

        var template = DefaultGroup.Template;
        var indexInParent = Groups.Children.IndexOf(DefaultGroup);
        Groups.Children.Remove(DefaultGroup); await SettleAsync();
        DefaultGroup.Template = null;
        Require(VisualTreeHelper.GetChildrenCount(DefaultGroup) == 0, "Detached AvatarGroup template retained its visual tree.");
        DefaultGroup.Template = template; Groups.Children.Insert(indexInParent, DefaultGroup); DefaultGroup.ApplyTemplate();
        await SettleAsync(); VerifyGroup(DefaultGroup, scheme);

        foreach (var (element, name) in new (FrameworkElement, string)[] { (Sizes, "sizes"), (EmptySizes, "silhouettes"), (Images, "images"), (Unicode, "unicode"), (Groups, "groups") })
            await MainWindow.CaptureElementAsync(element, Path.Combine(folder, $"avatar-{name}-{scheme}.png"));
        var windows = new List<object>();
        using (var native = new NativeInputSession(XamlRoot, folder, $"avatar-{scheme}"))
        {
            await native.AcquireAsync(Sizes);
            foreach (var (element, name) in new (FrameworkElement, string)[] { (Sizes, "sizes"), (DefaultGroup, "group") })
            {
                await native.MoveAsync(element);
                var capture = $"avatar-window-{name}-{scheme}.png";
                var client = await NativeScreenCapture.CaptureClientAsync(native.Window, Path.Combine(folder, capture));
                windows.Add(new { capture, client, target = native.ScreenBounds(element) });
            }
        }
        await File.WriteAllTextAsync(Path.Combine(folder, $"avatar-measurements-{scheme}.json"),
            JsonSerializer.Serialize(new { passed = true, scheme, samples, initials = unicode.Select(avatar =>
                Part<TextBlock>(avatar, "PART_Initials").Text.Select(unit => (int)unit).ToArray()), imageErrorLatched = true,
                boundCollection = true, indexIdentity = true, negativeMaxPreserved = true, lifecycle = true, pixelSamples, windows },
                new JsonSerializerOptions { WriteIndented = true }));
    }
    private static async Task<int> VerifyInkPixelsAsync(BleeckerAvatar owner)
    {
        var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(owner);
        Require(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0, "Avatar fallback did not render.");
        var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[checked((int)buffer.Length)];
        using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        var background = ((SolidColorBrush)owner.Background).Color;
        var foreground = ((SolidColorBrush)owner.Foreground).Color;
        var ink = 0;
        // The middle square is clear of the circle edge and ring. It must
        // contain real text/silhouette ink directed toward the branded color.
        for (var y = bitmap.PixelHeight / 4; y < bitmap.PixelHeight * 3 / 4; y++)
        for (var x = bitmap.PixelWidth / 4; x < bitmap.PixelWidth * 3 / 4; x++)
        {
            var offset = (y * bitmap.PixelWidth + x) * 4;
            if (pixels[offset + 3] == 255 && Math.Abs(pixels[offset + 2] - background.R) > 20 &&
                Math.Sign(pixels[offset + 2] - background.R) == Math.Sign(foreground.R - background.R)) ink++;
        }
        Require(ink > 0, "Avatar's initials or silhouette were clipped away or failed to paint.");
        return ink;
    }
    private static async Task CaptureImagePixelsAsync(BleeckerAvatar owner, string path, List<object> samples)
    {
        var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(owner);
        Require(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0, "Avatar image did not render.");
        var buffer = await bitmap.GetPixelsAsync();
        var pixels = new byte[checked((int)buffer.Length)];
        using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        var width = bitmap.PixelWidth; var height = bitmap.PixelHeight;
        // The 120x60 three-band source is cropped to its centered 60x60 square.
        // Sampling away from band boundaries and the inset ring proves cover
        // cropping and actual consumer pixels, rather than merely a brush flag.
        foreach (var (fraction, red, green, blue) in new[] { (1d / width, 166, 93, 87), (.1, 166, 93, 87), (.5, 230, 213, 184), (.9, 26, 55, 77) })
        {
            var x = (int)Math.Floor(width * fraction); var y = height / 2; var offset = (y * width + x) * 4;
            Require(pixels[offset] == blue && pixels[offset + 1] == green && pixels[offset + 2] == red && pixels[offset + 3] == 255,
                $"Avatar image lost centered cover cropping at {owner.AvatarSize} ({x},{y}).");
            samples.Add(new { capture = Path.GetFileName(path), x, y, red, green, blue, alpha = 255 });
        }
        foreach (var (x, y) in new[] { (0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1) })
        {
            Require(pixels[(y * width + x) * 4 + 3] == 0, "Avatar image or ring painted outside its circular corners.");
            samples.Add(new { capture = Path.GetFileName(path), x, y, alpha = 0 });
        }
        await MainWindow.SavePngAsync(path, pixels, width, height);
    }
    private static void VerifyAvatar(BleeckerAvatar avatar, BleeckerColorScheme scheme, bool grouped, bool overflow)
    {
        var edge = avatar.AvatarSize switch { BleeckerAvatarSize.ExtraSmall => 24, BleeckerAvatarSize.Small => 32, BleeckerAvatarSize.Medium => 40, BleeckerAvatarSize.Large => 48, _ => 64 };
        var font = avatar.AvatarSize switch { BleeckerAvatarSize.ExtraSmall => 10, BleeckerAvatarSize.Small => 12, BleeckerAvatarSize.Medium => overflow ? 12 : 14,
            BleeckerAvatarSize.Large => overflow ? 14 : 16, _ => overflow ? 16 : 18 };
        Require(avatar.ActualWidth == edge && avatar.ActualHeight == edge && avatar.FontSize == font &&
            avatar.FontFamily.Source.Contains("EncodeSans-Medium.ttf"), "Avatar size or typography differs from the source.");
        var surface = Part<Border>(avatar, "PART_Surface"); var ring = Part<Border>(avatar, "PART_Ring");
        Require(surface.CornerRadius == new CornerRadius(edge / 2d) && Color(surface.Background) == (scheme == BleeckerColorScheme.Dark ? "#FF182533" : "#FFF9F6F2") &&
            Color(avatar.Foreground) == (scheme == BleeckerColorScheme.Dark ? "#FFF0F4F8" : "#FF595959"), "Avatar lost its circular branded surface.");
        Require(ring.BorderThickness == new Thickness(grouped ? 2 : 1) && ring.Margin == new Thickness(overflow ? -2 : 0) &&
            Color(ring.BorderBrush) == (grouped ? scheme == BleeckerColorScheme.Dark ? "#FF182533" : "#FFFFFFFF" : scheme == BleeckerColorScheme.Dark ? "#FFFFFFFF" : "#FF000000") &&
            ((SolidColorBrush)ring.BorderBrush).Opacity == (grouped ? 1 : .1), "Avatar used an incorrect ring width, placement or color.");
        var clip = ElementCompositionPreview.GetElementVisual(Part<Grid>(avatar, "PART_Content")).Clip;
        Require(overflow ? clip is null : clip is RectangleClip rectangle && rectangle.Right == edge && rectangle.Bottom == edge &&
            rectangle.TopLeftRadius == new System.Numerics.Vector2(edge / 2f), "Avatar overflow clipping differs from the source.");
        var shadows = ElementCompositionPreview.GetElementChildVisual(Part<Grid>(avatar, "PART_ShadowHost")) as ContainerVisual;
        if (overflow)
        {
            Require(shadows?.Children.Count == 2, "AvatarGroup overflow lost a shadow layer.");
            var layers = shadows!.Children.Cast<SpriteVisual>().Select(sprite => (DropShadow)sprite.Shadow).ToArray();
            Require(layers.Select(layer => layer.BlurRadius).Order().SequenceEqual(new[] { 2f, 3f }) &&
                layers.All(layer => layer.Offset == new System.Numerics.Vector3(0, 1, 0) && layer.Opacity == .1f && layer.Color == Microsoft.UI.Colors.Black),
                "AvatarGroup overflow used a stock elevation recipe.");
        }
        else Require(shadows is null, "Avatar acquired an unrequested shadow.");
    }
    private static void VerifyGroup(BleeckerAvatarGroup group, BleeckerColorScheme scheme)
    {
        var children = GroupAvatars(group);
        var visible = group.Max >= 0 ? Math.Min(group.Avatars.Count, group.Max) : Math.Max(0, group.Avatars.Count + group.Max);
        var overflow = (long)group.Avatars.Count - group.Max;
        Require(children.Length == visible + (overflow > 0 ? 1 : 0), "AvatarGroup slice/count differs from the producer.");
        var offset = 0d;
        foreach (var (avatar, index) in children.Select((avatar, index) => (avatar, index)))
        {
            var isOverflow = index == visible;
            VerifyAvatar(avatar, scheme, true, isOverflow);
            var point = avatar.TransformToVisual(group).TransformPoint(new Point());
            Require(Math.Abs(point.X - offset) < .001, "AvatarGroup lost its 8px overlap.");
            offset += avatar.ActualWidth - 8;
            if (isOverflow) Require(Part<TextBlock>(avatar, "PART_Initials").Text == "+" + overflow, "Overflow count was normalized or replaced.");
        }
        Require(group.ActualHeight == (children.Length == 0 ? 0 : children[0].ActualHeight), "AvatarGroup changed empty or populated row height.");
    }
    private static BleeckerAvatar[] GroupAvatars(BleeckerAvatarGroup group) =>
        ((Panel)Part<Grid>(group, "PART_Items").Children.Single()).Children.OfType<BleeckerAvatar>().ToArray();
    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        (T)((FrameworkElement)VisualTreeHelper.GetChild(owner, 0)).FindName(name);
    private static string Color(Brush brush) { var color = ((SolidColorBrush)brush).Color; return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"; }
    private async Task SettleAsync() { UpdateLayout(); await Task.Delay(100); UpdateLayout(); }
    private async Task AwaitAsync(Func<bool> condition, string message)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(2)) await SettleAsync();
        Require(condition(), message);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static async Task<BitmapImage> FictionalImageAsync()
    {
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        var pixels = new byte[120 * 60 * 4];
        var colors = new[] { Microsoft.UI.ColorHelper.FromArgb(255, 166, 93, 87), Microsoft.UI.ColorHelper.FromArgb(255, 230, 213, 184), Microsoft.UI.ColorHelper.FromArgb(255, 26, 55, 77) };
        for (var y = 0; y < 60; y++) for (var x = 0; x < 120; x++)
        { var color = colors[x / 40]; var index = (y * 120 + x) * 4; pixels[index] = color.B; pixels[index + 1] = color.G; pixels[index + 2] = color.R; pixels[index + 3] = 255; }
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, 120, 60, 96, 96, pixels);
        await encoder.FlushAsync(); stream.Seek(0);
        var source = new BitmapImage(); await source.SetSourceAsync(stream); return source;
    }
}
