using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

public sealed partial class DataTableToolbarsFixture
{
    private async Task TransferToolbarTabAsync(NativeInputSession input,ToolbarScenario scenario,List<object> reports,string id,int step,string sourceRole)
    {
        var from=FocusManager.GetFocusedElement(XamlRoot);var to=scenario.FocusTarget(sourceRole);var downs=0;var ups=0;var downOwned=true;var upOwned=true;
        var released=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var down=new KeyEventHandler((_,args)=>{if(args.Key!=Windows.System.VirtualKey.Tab)return;downs++;downOwned&=ReferenceEquals(args.OriginalSource,from);});
        var up=new KeyEventHandler((_,args)=>{if(args.Key!=Windows.System.VirtualKey.Tab)return;ups++;upOwned&=ReferenceEquals(args.OriginalSource,to);released.TrySetResult();});
        XamlRoot.Content.AddHandler(UIElement.KeyDownEvent,down,true);XamlRoot.Content.AddHandler(UIElement.KeyUpEvent,up,true);
        try
        {input.Key(0x09);await released.Task.WaitAsync(TimeSpan.FromSeconds(2));Require(downs==1&&ups==1&&downOwned&&upOwned&&ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),to),"Interaction Tab lost its original press/release focus transfer: "+sourceRole+"/"+scenario.DescribeFocus());}
        finally{XamlRoot.Content.RemoveHandler(UIElement.KeyDownEvent,down);XamlRoot.Content.RemoveHandler(UIElement.KeyUpEvent,up);reports.Add(new{id,step,sourceRole,downs,ups,downOwned,upOwned});}
    }
    private async Task ReleaseToolbarPointerAsync(NativeInputSession input,string key,string folder)
    {
        var expected=input.ReadPointerPosition();var delivered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var receipts=0;var exact=false;
        var released=new PointerEventHandler((_,args)=>
        {
            var point=args.GetCurrentPoint(XamlRoot.Content);var client=NativeScreenCapture.ClientBounds(input.Window);receipts++;
            exact=!args.IsGenerated&&args.Pointer.PointerDeviceType==Microsoft.UI.Input.PointerDeviceType.Mouse&&!point.Properties.IsLeftButtonPressed&&
                Math.Abs(client.X+point.Position.X*XamlRoot.RasterizationScale-expected.X)<.001&&Math.Abs(client.Y+point.Position.Y*XamlRoot.RasterizationScale-expected.Y)<.001;delivered.TrySetResult();
        });
        XamlRoot.Content.AddHandler(UIElement.PointerReleasedEvent,released,true);
        try{input.PointerButton(false);await delivered.Task.WaitAsync(TimeSpan.FromSeconds(2));Require(receipts==1&&exact&&input.OwnsCurrentPoint,"Interaction Reset lost its exact owned native release.");}
        finally{XamlRoot.Content.RemoveHandler(UIElement.PointerReleasedEvent,released);await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-toolbar-matrix-release-{key}.json"),JsonSerializer.Serialize(new{receipts,exact,expected}));}
    }
}
