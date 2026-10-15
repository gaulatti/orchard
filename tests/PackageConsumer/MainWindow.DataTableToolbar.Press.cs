using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyToolbarPressAsync(Button reset)
    {
        Require(reset.Focus(FocusState.Keyboard),"Packaged Reset refused keyboard focus.");
        Rect Bounds()=>reset.TransformToVisual(Root).TransformBounds(new Rect(0,0,reset.ActualWidth,reset.ActualHeight));
        var normal=Bounds();var before=toolbarSource.Resets;var downs=0;var ups=0;var downOwned=true;var upOwned=true;
        var down=new KeyEventHandler((_,args)=>{if(args.Key==Windows.System.VirtualKey.Space){downs++;downOwned&=ReferenceEquals(args.OriginalSource,reset);}});
        var up=new KeyEventHandler((_,args)=>{if(args.Key==Windows.System.VirtualKey.Space){ups++;upOwned&=ReferenceEquals(args.OriginalSource,reset);}});
        void SendSpace(bool released)
        {
            var events=new[]{new ConsumerInput{Type=1,Data=new ConsumerInputData{Keyboard=new ConsumerKeyboard{VirtualKey=0x20,Flags=released?2u:0u}}}};
            Require(SendInput(1,events,Marshal.SizeOf<ConsumerInput>())==1,"Packaged Reset Space did not enter native input.");
        }
        Root.AddHandler(UIElement.KeyDownEvent,down,true);Root.AddHandler(UIElement.KeyUpEvent,up,true);
        try
        {
            SendSpace(false);
            try
            {
                await AwaitAsync(()=>reset.IsPressed&&downs==1,"Packaged Reset did not receive a held native Space press.");
                var held=Bounds();
                Require(downOwned&&toolbarSource.Resets==before&&
                    Math.Abs(held.Width-normal.Width*.99)<=1d/32&&Math.Abs(held.Height-normal.Height*.99)<=1d/32&&
                    Math.Abs(held.X-normal.X-normal.Width*.005)<=1d/32&&Math.Abs(held.Y-normal.Y-1-normal.Height*.005)<=1d/32,
                    "Packaged held Reset differs from source geometry or invoked before release.");
            }
            finally{SendSpace(true);}
            await AwaitAsync(()=>!reset.IsPressed&&ups==1&&toolbarSource.Resets==before+1,"Packaged Reset did not invoke exactly once on Space release.");
            var released=Bounds();Require(downs==1&&ups==1&&downOwned&&upOwned&&ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot),reset)&&
                Math.Abs(released.X-normal.X)<=1d/32&&Math.Abs(released.Y-normal.Y)<=1d/32&&Math.Abs(released.Width-normal.Width)<=1d/32&&Math.Abs(released.Height-normal.Height)<=1d/32,
                "Packaged Reset lost exact keyboard ownership, focus or released bounds.");
        }
        finally{Root.RemoveHandler(UIElement.KeyDownEvent,down);Root.RemoveHandler(UIElement.KeyUpEvent,up);}
    }
}
