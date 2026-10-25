using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using VirtualKey = Windows.System.VirtualKey;

namespace Orchard.Testing;

internal static partial class HeroCarouselFixture
{
    private static async Task<object> VerifyWindowInput(BleeckerHeroCarousel host, Grid scene, Grid root,
        Func<BleeckerHeroCarouselItems> items, Func<int, Task> layout)
    {
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        var type = typeof(BleeckerHeroCarousel);
        var rootField = type.GetField("keyboardRoot", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var method = type.GetMethod("ProcessWindowKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Action<VirtualKey, DependencyObject?> Processor(BleeckerHeroCarousel target) => method.CreateDelegate<Action<VirtualKey, DependencyObject?>>(target);
        var process = Processor(host);
        Require(ReferenceEquals(rootField.GetValue(host), root), "Hero keyboard listener did not attach to the owning XamlRoot content.");
        var outside = new Button { Content = "Outside source-equivalent button" };
        scene.Children.Add(outside); await layout(20);
        host.SelectSlide(0); process(VirtualKey.Right, outside); Require(host.CurrentIndex == 1, "Outside arrow still requires carousel focus.");
        process(VirtualKey.Left, outside); process(VirtualKey.Left, outside); Require(host.CurrentIndex == 2, "Owning-window previous did not wrap.");
        host.SelectSlide(0);
        DependencyObject[] inputs = [new TextBox(), new PasswordBox(), new CheckBox(), new RadioButton(), new Slider(), new ToggleSwitch(), new BleeckerFileInput(), new BleeckerSwitch(), new BleeckerSlider(), new BleeckerRadioGroup(), new BleeckerOtpInput()];
        foreach (var input in inputs) { process(VirtualKey.Right, input); Require(host.CurrentIndex == 0, "Native INPUT/TEXTAREA equivalent consumed its arrow: " + input.GetType().Name); }
        process(VirtualKey.Right, new RichEditBox()); Require(host.CurrentIndex == 1, "Contenteditable-equivalent RichEditBox was silently excluded.");
        process(VirtualKey.Up, outside); Require(host.CurrentIndex == 1, "Unrelated key advanced the carousel.");
        var second = new BleeckerHeroCarousel { Items = items(), AutoAdvanceMilliseconds = 1000000, VerticalAlignment = VerticalAlignment.Top };
        scene.Children.Add(second); await layout(60); host.SelectSlide(0);
        Require(ReferenceEquals(rootField.GetValue(second), root), "Second carousel does not subscribe to the same owning root.");
        process(VirtualKey.Right, outside); Processor(second)(VirtualKey.Right, outside);
        Require(host.CurrentIndex == 1 && second.CurrentIndex == 1, "Independent owning-window receivers did not both advance.");
        scene.Children.Remove(second); await layout(30); Require(rootField.GetValue(second) is null, "Unloaded carousel retained its window keyboard listener.");
        Processor(second)(VirtualKey.Right, outside); Require(second.CurrentIndex == 0, "Detached window-key callback still navigates.");
        scene.Children.Add(second); await layout(40); Require(ReferenceEquals(rootField.GetValue(second), root) && second.CurrentIndex == 0, "Remounted carousel did not restore its listener and initial index.");
        Processor(second)(VirtualKey.Right, outside); Require(second.CurrentIndex == 1, "Remounted keyboard boundary failed to navigate once.");
        scene.Children.Remove(second); scene.Children.Remove(outside); host.SelectSlide(0); await layout(550);
        var hits = VisualTreeHelper.FindElementsInHostCoordinates(new Point(500, 80), host).OfType<FrameworkElement>().ToArray();
        var hitSlides = hits.OfType<ContentControl>().Where(e => e.Name == "HeroSlide").Select(e => ((BleeckerHeroCarouselItem)e.Tag).Id).ToArray();
        Require(hitSlides.FirstOrDefault()=="third", "Invisible source slide is absent from native empty-area hit testing.");
        HyperlinkButton Target(Point point)
        {
            var hitElements=VisualTreeHelper.FindElementsInHostCoordinates(point,host).OfType<FrameworkElement>().ToArray();
            return hitElements.OfType<HyperlinkButton>().FirstOrDefault(e=>e.Name is "HeroSourceLinkHit" or "HeroSourceContentHit") ?? throw new InvalidOperationException("Missing source hit target: "+System.Text.Json.JsonSerializer.Serialize(new{point,hits=hitElements.Select(e=>new{e.Name,type=e.GetType().Name}),targets=Elements(host).OfType<HyperlinkButton>().Where(e=>e.Name.StartsWith("HeroSource",StringComparison.Ordinal)).Select(e=>new{e.Name,e.ActualWidth,e.ActualHeight,e.IsEnabled,e.IsHitTestVisible,e.Visibility,e.IsLoaded,box=e.TransformToVisual(root).TransformBounds(new Rect(0,0,e.ActualWidth,e.ActualHeight))})}));
        }
        var emptyTarget=Target(new Point(500,80));Require(emptyTarget.Tag is BleeckerHeroCarouselItem {Id:"third"},"Empty-area hit did not retain the source's last inactive item.");
        var title=Elements(host).OfType<TextBlock>().First(e=>e.Name=="HeroTitleText");var titleBox=title.TransformToVisual(root).TransformBounds(new Rect(0,0,title.ActualWidth,title.ActualHeight));
        var visibleTarget=Target(new Point(titleBox.X+titleBox.Width/2,titleBox.Y+titleBox.Height/2));Require(visibleTarget.Tag is BleeckerHeroCarouselItem {Id:"first"},"Visible content hit was stolen by an inactive item.");
        BleeckerHeroCarouselItem? invoked=null;void Invoked(object? sender,BleeckerHeroCarouselItem item)=>invoked=item;host.ItemInvoked+=Invoked;
        foreach(var target in new[]{emptyTarget,visibleTarget}){var peer=FrameworkElementAutomationPeer.CreatePeerForElement(target)!;Require(peer.GetPattern(PatternInterface.Invoke)is IInvokeProvider,"Source hit target has no native Invoke pattern.");((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();await layout(20);Require(ReferenceEquals(invoked,target.Tag),"Native hit-target invocation replaced its original item.");}host.ItemInvoked-=Invoked;
        return new { owningRootRetained = true, outsideArrows = true, wrapping = true, ignoredInputTypes = inputs.Select(i => i.GetType().Name).ToArray(), contenteditableAdvances = true, unrelatedKeysIgnored = true, independentReceiversAdvance = true, unloadDetaches = true, detachedCallbackIgnored = true, remountRestores = true, inactiveHitTestSlideIds = hitSlides, inactiveHitTestEligible = true, emptyHitOriginalItem="third",visibleContentHitOriginalItem="first",hitTargetNativeInvokesVerified=true,productionBoundaryInvokedDirectly = true, physicalRoutingVerified = false, tabTraversalVerified = false };
    }
}
