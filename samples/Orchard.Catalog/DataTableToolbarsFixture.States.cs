using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class DataTableToolbarsFixture
{
    private async Task VerifyResetStatesAsync(NativeInputSession input,BleeckerColorScheme scheme,string folder)
    {
        using var reference=JsonDocument.Parse(typeof(DataTableToolbarsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.DataTableToolbarReset.json")!);
        var source=reference.RootElement.GetProperty("results").EnumerateArray().Single(item=>item.GetProperty("scheme").GetString()==scheme.ToString()&&item.GetProperty("reducedMotion").GetString()=="reduce");
        var sourceNormal=source.GetProperty("hover").GetProperty("box");var sourceHeld=source.GetProperty("held").GetProperty("box");
        var scaleX=sourceHeld.GetProperty("width").GetDouble()/sourceNormal.GetProperty("width").GetDouble();
        var scaleY=sourceHeld.GetProperty("height").GetDouble()/sourceNormal.GetProperty("height").GetDouble();
        var shift=sourceHeld.GetProperty("y").GetDouble()-sourceNormal.GetProperty("y").GetDouble()-(sourceNormal.GetProperty("height").GetDouble()-sourceHeld.GetProperty("height").GetDouble())/2;
        table.SetFilters(new[]{new KeyValuePair<string,object?>("title","Harbor")});table.SetGlobal("Harbor");
        await MainWindow.AwaitPresentationAsync(Preview);
        var reset=Descendants<Button>(Preview).Single(button=>button.Name=="PART_Reset");
        var search=Descendants<BleeckerInput>(Preview).Single();await input.ClickAsync(search);
        Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),search),"Search did not receive real pointer focus before Reset hover.");
        await input.MoveAsync(reset);await MainWindow.AwaitPresentationAsync(Preview);
        var normal=Bounds(reset,Preview);var resets=table.Resets;var states=new List<object>();
        object Paint()=>Descendants<FrameworkElement>(reset).Select(element=>new { element.Name,type=element.GetType().Name,
            background=DescribeBrush(element.GetType().GetProperty("Background")?.GetValue(element) as Brush) }).Where(item=>item.background is not null).ToArray();
        void CheckPaint()
        {
            var surface=Descendants<ContentPresenter>(reset).Single(element=>element.Name=="ContentPresenter");
            Require(surface.Background is SolidColorBrush background&&background.Color.A==255&&
                background.Color.R==(scheme==BleeckerColorScheme.Light?230:255)&&background.Color.G==(scheme==BleeckerColorScheme.Light?213:255)&&
                background.Color.B==(scheme==BleeckerColorScheme.Light?184:255)&&Math.Abs(background.Opacity-(scheme==BleeckerColorScheme.Light?.12:.06))<.000001,
                "Reset's rendered hover/held background differs from the original Ghost palette.");
            Require(surface.Foreground is SolidColorBrush foreground&&foreground.Color.A==255&&foreground.Opacity==1&&
                foreground.Color.R==(scheme==BleeckerColorScheme.Light?45:240)&&foreground.Color.G==(scheme==BleeckerColorScheme.Light?45:244)&&foreground.Color.B==(scheme==BleeckerColorScheme.Light?45:248),
                "Reset's rendered foreground differs from the original Ghost palette.");
        }
        try
        {
            Require(reset.IsPointerOver&&!reset.IsPressed,"Reset did not receive actual hover.");CheckPaint();
            states.Add(new{phase="hover",bounds=normal,reset.IsPointerOver,paint=Paint()});
            await MainWindow.CaptureElementAsync(XamlRoot.Content,Path.Combine(folder,$"data-table-toolbar-reset-hover-{scheme}.png"));
            await input.PressPointerAsync();
            try
            {
                await AwaitAsync(()=>reset.IsPressed,"Reset did not enter an actual held press.");await MainWindow.AwaitPresentationAsync(Preview);
                var held=Bounds(reset,Preview);CheckPaint();
                states.Add(new{phase="held",bounds=held,reset.IsPressed,paint=Paint(),transform=reset.RenderTransform.GetType().Name});
                await MainWindow.CaptureElementAsync(XamlRoot.Content,Path.Combine(folder,$"data-table-toolbar-reset-held-{scheme}.png"));
                Require(table.Resets==resets,"Pointer down invoked the consumer reset.");
                Require(Math.Abs(held.Width-normal.Width*scaleX)<=1d/32&&Math.Abs(held.Height-normal.Height*scaleY)<=1d/32&&
                    Math.Abs(held.X-normal.X-normal.Width*(1-scaleX)/2)<=1d/32&&Math.Abs(held.Y-normal.Y-shift-normal.Height*(1-scaleY)/2)<=1d/32,
                    "Held Reset did not match the source's centered .99 scale and one-unit translation.");
                Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),reset),"Pointer press did not focus Reset.");
            }
            finally{await ReleaseResetAsync(input,scheme,folder);}
            await AwaitAsync(()=>!reset.IsPressed&&table.Resets==resets+1,"Reset release did not invoke the original consumer exactly once.");
            await MainWindow.AwaitPresentationAsync(Preview);var released=Bounds(reset,Preview);CheckPaint();
            Require(Math.Abs(released.X-normal.X)<=1d/32&&Math.Abs(released.Y-normal.Y)<=1d/32&&Math.Abs(released.Width-normal.Width)<=1d/32&&Math.Abs(released.Height-normal.Height)<=1d/32,
                "Released Reset retained its held transform.");
            Require(Equals(table.GlobalFilter,"Harbor")&&table.ColumnFilterCount==0&&reset.IsLoaded,"Held/released Reset lost global filtering or did not clear column filtering.");
            states.Add(new{phase="released",bounds=released,reset.IsPressed,paint=Paint(),callbacks=table.Resets-resets});
            await MainWindow.CaptureElementAsync(XamlRoot.Content,Path.Combine(folder,$"data-table-toolbar-reset-released-{scheme}.png"));
            var stationary=input.ReadPointerPosition();var template=Preview.Template;var originalReset=reset;
            Preview.Template=null;await AwaitAsync(()=>!originalReset.IsLoaded,"Old Reset did not unload during stationary template replacement.");
            Preview.Template=template;Preview.ApplyTemplate();UpdateLayout();
            reset=Descendants<Button>(Preview).Single(button=>button.Name=="PART_Reset");
            await AwaitAsync(()=>reset.IsLoaded&&ResetHoverPresented(reset,scheme,input),"Recreated Reset did not reconcile its real stationary hover.");await MainWindow.AwaitPresentationAsync(Preview);
            var after=input.ReadPointerPosition();Require(stationary==after&&input.OwnsCurrentPoint&&!ReferenceEquals(originalReset,reset)&&table.Resets==resets+1,
                "Stationary Reset replacement moved the pointer, reused the old button or invoked its callback.");
            await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-toolbar-reset-stationary-{scheme}.json"),JsonSerializer.Serialize(new{passed=true,stationary,after,reset.IsPointerOver,recreated=true,callbacks=table.Resets-resets}));
            await MainWindow.CaptureElementAsync(XamlRoot.Content,Path.Combine(folder,$"data-table-toolbar-reset-stationary-hover-{scheme}.png"));
        }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-toolbar-reset-states-{scheme}.json"),JsonSerializer.Serialize(states,new JsonSerializerOptions{WriteIndented=true}));
        }
        table.SetGlobal(null);table.SetFilters([]);await MainWindow.AwaitPresentationAsync(Preview);
    }
    private static object? DescribeBrush(Brush? brush)=>brush is SolidColorBrush solid?new{solid.Color.A,solid.Color.R,solid.Color.G,solid.Color.B,brush.Opacity}:brush is null?null:new{type=brush.GetType().Name};
    private async Task ReleaseResetAsync(NativeInputSession input,BleeckerColorScheme scheme,string folder)
    {
        var expected=input.ReadPointerPosition();var completed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var receipts=0;var exact=false;
        var released=new Microsoft.UI.Xaml.Input.PointerEventHandler((_,args)=>
        {
            var point=args.GetCurrentPoint(XamlRoot.Content);var client=NativeScreenCapture.ClientBounds(input.Window);receipts++;
            exact=!args.IsGenerated&&args.Pointer.PointerDeviceType==Microsoft.UI.Input.PointerDeviceType.Mouse&&!point.Properties.IsLeftButtonPressed&&
                Math.Abs(client.X+point.Position.X*XamlRoot.RasterizationScale-expected.X)<.001&&Math.Abs(client.Y+point.Position.Y*XamlRoot.RasterizationScale-expected.Y)<.001;
            completed.TrySetResult();
        });
        XamlRoot.Content.AddHandler(UIElement.PointerReleasedEvent,released,true);
        try{input.PointerButton(false);await completed.Task.WaitAsync(TimeSpan.FromSeconds(2));Require(receipts==1&&exact&&input.OwnsCurrentPoint,"Reset did not deliver one exact owned native mouse release.");}
        finally{XamlRoot.Content.RemoveHandler(UIElement.PointerReleasedEvent,released);await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-toolbar-reset-release-{scheme}.json"),JsonSerializer.Serialize(new{receipts,exact,expected}));}
    }
}
