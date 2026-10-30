using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;
namespace Orchard.Catalog;
public sealed partial class FeedItemsFixture:UserControl
{
    public FeedItemsFixture(){InitializeComponent();First.PostedAt=DateTimeOffset.Now.AddMinutes(-10);First.Categories=new[]{"  LOCAL ","Review"};Second.PostedAt=DateTimeOffset.Now.AddDays(-2);Second.Categories=new[]{"İSTANBUL","ΟΣ","CAFÉ"};}
    private void SourceRequested(object? sender,BleeckerFeedItemNavigationEventArgs args)=>Status.Text="Consumer requested: "+args.SourceUrl;

    internal async Task VerifyAsync(ElementTheme theme,string folder)
    {
        var link=MainWindow.Desc<HyperlinkButton>(First).Single(control=>control.Name=="FeedItemSourceLink");
        var label=MainWindow.Desc<TextBlock>(First).Single(control=>control.Name=="FeedItemSourceTextText");
        var requests=new List<string>();
        void OnRequest(object? sender,BleeckerFeedItemNavigationEventArgs args)=>requests.Add(args.SourceUrl);
        First.SourceRequested+=OnRequest;
        using var input=new NativeInputSession(XamlRoot,folder,"feed-item-"+theme);
        try
        {
            await MainWindow.AwaitPresentationAsync(First);
            await input.AcquireAsync(link);
            await input.MoveAsync(Status);
            await input.MoveAsync(link);
            await MainWindow.AwaitPresentationAsync(First);
            if(label.TextDecorations!=Windows.UI.Text.TextDecorations.Underline)throw new InvalidOperationException("Feed Item link did not show pointer hover.");
            var reduced=!new UISettings().AnimationsEnabled;
            if(reduced)
            {
                var brush=(SolidColorBrush)label.Foreground;
                var color=brush.Color;
                await Task.Delay(50);
                if(brush.Color!=color)throw new InvalidOperationException("Feed Item link animated under reduced motion.");
            }
            await input.ClickAsync(link);
            if(requests.Count!=1||requests[0]!=First.SourceUrl||Status.Text!="Consumer requested: "+First.SourceUrl)
                throw new InvalidOperationException("Feed Item pointer click lost the caller-owned source request.");
            if(!link.Focus(FocusState.Keyboard))throw new InvalidOperationException("Feed Item source link refused keyboard focus.");
            await input.KeyAsync(0x0D,link);
            if(requests.Count!=2||requests[1]!=First.SourceUrl)throw new InvalidOperationException("Feed Item Enter did not invoke its source link.");
            await File.WriteAllTextAsync(Path.Combine(folder,$"feed-item-runtime-{theme}.json"),JsonSerializer.Serialize(new{passed=true,theme=theme.ToString(),pointerRequests=1,keyboardRequests=1,focus=true,reducedMotionVerified=reduced}));
        }
        finally{First.SourceRequested-=OnRequest;}
    }
}
