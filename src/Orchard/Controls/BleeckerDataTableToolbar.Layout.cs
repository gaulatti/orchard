using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerDataTableToolbar
{
    private sealed class ToolbarPanel:Panel
    {
        internal BleeckerInput? Search;
        internal Button? Reset;
        internal bool Wide;
        internal double Spacing=12;
        private UIElement[] left=[],right=[];
        private double leftWidth,leftHeight,rightWidth,rightHeight,wrapperWidth,inputWidth;
        internal void SetItems(BleeckerInput? input,IReadOnlyList<UIElement> filters,Button? reset,IReadOnlyList<UIElement> actions)
        {
            left=(input is null?filters:new[]{input}.Cast<UIElement>().Concat(filters)).ToArray();
            right=(reset is null?actions:new[]{reset}.Cast<UIElement>().Concat(actions)).ToArray();
            var next=left.Concat(right).ToArray();
            for(var at=Children.Count-1;at>=0;at--)if(!next.Contains(Children[at]))Children.RemoveAt(at);
            for(var at=0;at<next.Length;at++){if(at<Children.Count&&ReferenceEquals(Children[at],next[at]))continue;if(Children.Contains(next[at]))Children.Remove(next[at]);Children.Insert(at,next[at]);}
        }
        protected override Size MeasureOverride(Size available)
        {
            foreach(var item in left.Concat(right))item.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            rightWidth=right.Sum(item=>item.DesiredSize.Width)+Math.Max(0,right.Length-1)*8;rightHeight=right.Select(item=>item.DesiredSize.Height).DefaultIfEmpty(0).Max();
            var hasInput=left.Length>0&&ReferenceEquals(left[0],Search);var filters=left.Skip(hasInput?1:0).ToArray();
            var fixedWidth=filters.Sum(item=>item.DesiredSize.Width)+Math.Max(0,left.Length-1)*12;
            if(Wide)
            {
                var minimum=fixedWidth+(hasInput?30:0);
                leftWidth=double.IsFinite(available.Width)?Math.Max(minimum,available.Width-Spacing-rightWidth):fixedWidth+(hasInput?320:0);
                wrapperWidth=hasInput?Math.Max(30,leftWidth-fixedWidth):0;inputWidth=Math.Min(320,wrapperWidth);
                if(hasInput)Search!.Measure(new Size(inputWidth,double.PositiveInfinity));
                leftHeight=left.Select(item=>item.DesiredSize.Height).DefaultIfEmpty(0).Max();
            }
            else
            {
                leftWidth=double.IsFinite(available.Width)?available.Width:fixedWidth+(hasInput?320:0);wrapperWidth=leftWidth;inputWidth=Math.Max(30,leftWidth);
                foreach(var item in left){var width=ReferenceEquals(item,Search)?inputWidth:item is FrameworkElement frame&&!double.IsNaN(frame.Width)?item.DesiredSize.Width:leftWidth;item.Measure(new Size(Math.Max(0,width),double.PositiveInfinity));}
                leftHeight=left.Sum(item=>item.DesiredSize.Height)+Math.Max(0,left.Length-1)*12;
            }
            var widthWanted=Wide?leftWidth+Spacing+rightWidth:Math.Max(leftWidth,rightWidth);
            return new Size(double.IsFinite(available.Width)?Math.Min(available.Width,widthWanted):widthWanted,Wide?Math.Max(leftHeight,rightHeight):leftHeight+Spacing+rightHeight);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if(Wide)
            {
                var x=0d;
                foreach(var item in left){var isInput=ReferenceEquals(item,Search);var width=isInput?inputWidth:item.DesiredSize.Width;item.Arrange(new Rect(x,(finalSize.Height-item.DesiredSize.Height)/2,width,item.DesiredSize.Height));x+=(isInput?wrapperWidth:width)+12;}
                x=Math.Max(leftWidth+Spacing,finalSize.Width-rightWidth);
                foreach(var item in right){item.Arrange(new Rect(x,(finalSize.Height-item.DesiredSize.Height)/2,item.DesiredSize.Width,item.DesiredSize.Height));x+=item.DesiredSize.Width+8;}
            }
            else
            {
                var y=0d;foreach(var item in left){var width=ReferenceEquals(item,Search)?inputWidth:item is FrameworkElement frame&&!double.IsNaN(frame.Width)?item.DesiredSize.Width:Math.Max(0,leftWidth);item.Arrange(new Rect(0,y,width,item.DesiredSize.Height));y+=item.DesiredSize.Height+12;}
                var x=0d;foreach(var item in right){item.Arrange(new Rect(x,leftHeight+Spacing+(rightHeight-item.DesiredSize.Height)/2,item.DesiredSize.Width,item.DesiredSize.Height));x+=item.DesiredSize.Width+8;}
            }
            return finalSize;
        }
    }
}
