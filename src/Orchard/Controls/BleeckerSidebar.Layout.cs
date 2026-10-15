using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerSidebar
{
    internal static bool Numeric(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    internal static bool Truthy(object? value) => value is not null && value is not false && value is not "" && (!Numeric(value) || Convert.ToDouble(value,CultureInfo.InvariantCulture) is var number && number!=0 && !double.IsNaN(number));
    internal static object? DisplayValue(object? value) => value is bool ? null : value;
    internal static void ValidateContent(object? value,DataTemplate? template)
    {
        if(template is null&&value is not null&&value is not UIElement&&value is not string&&value is not bool&&!Numeric(value))throw new ArgumentException("Sidebar model content requires an explicit DataTemplate.");
    }
    internal static Border SourceBorder(string name) => (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Name='"+name+"' BorderBrush='{ThemeResource BleeckerSidebarSectionBorderBrush}'/>");
    private sealed class Slot
    {
        internal readonly Grid Root=new(){UseLayoutRounding=false};
        internal readonly Border Chrome;
        private readonly ContentPresenter content=new(){UseLayoutRounding=false};
        private readonly TextBlock text=new(){FontSize=16,LineHeight=24,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,UseLayoutRounding=false};
        private readonly bool header;
        internal Slot(BleeckerSidebar owner,bool header)
        {
            this.header=header;Chrome=SourceBorder(header?"PART_Header":"PART_Footer");
            Chrome.Child=content;Root.Children.Add(Chrome);
            content.SetBinding(ContentPresenter.FontFamilyProperty,new Binding{Source=owner,Path=new PropertyPath(nameof(FontFamily))});
            text.SetBinding(TextBlock.FontFamilyProperty,new Binding{Source=owner,Path=new PropertyPath(nameof(FontFamily))});
            content.SetBinding(ContentPresenter.ForegroundProperty,new Binding{Source=owner,Path=new PropertyPath(nameof(Foreground))});
            text.SetBinding(TextBlock.ForegroundProperty,new Binding{Source=owner,Path=new PropertyPath(nameof(Foreground))});
        }
        internal void Update(object? value,DataTemplate? template,bool collapsed)
        {
            ValidateContent(value,template);var truthy=template is not null||Truthy(value);var anonymous=!truthy&&Numeric(value);
            Root.Visibility=truthy||anonymous?Visibility.Visible:Visibility.Collapsed;
            Chrome.Margin=header&&truthy?new Thickness(0,0,0,20):new Thickness(0);
            Chrome.Padding=truthy?header?new Thickness(0,0,0,20):new Thickness(0,16,0,0):new Thickness(0);
            Chrome.BorderThickness=truthy?header?new Thickness(0,0,0,1):new Thickness(0,1,0,0):new Thickness(0);
            content.HorizontalAlignment=collapsed&&truthy?HorizontalAlignment.Center:HorizontalAlignment.Stretch;
            content.ContentTemplate=template;
            var display=template is null?DisplayValue(value):value;
            if(template is null&&(display is string||Numeric(display)))
            {text.Text=Convert.ToString(display,CultureInfo.InvariantCulture)??"";content.Content=text;}
            else content.Content=display;
        }
        internal void Release(){content.Content=null;Root.Children.Clear();Chrome.Child=null;}
    }
    private sealed class SidebarLayout:Panel
    {
        private readonly BleeckerSidebar owner;
        private readonly Slot header,footer;
        internal StackPanel NavigationItems {get;}=new(){Name="PART_Rows",Spacing=4,UseLayoutRounding=false};
        private readonly ScrollViewer navigation;
        internal Thickness Insets {get;set;}
        private double headerHeight,footerHeight,bodyHeight;
        internal SidebarLayout(BleeckerSidebar owner)
        {
            this.owner=owner;UseLayoutRounding=false;
            header=new(owner,true);footer=new(owner,false);
            navigation=new ScrollViewer{ Name="PART_Navigation", Content=NavigationItems,Padding=new Thickness(0),BorderThickness=new Thickness(0),IsTabStop=false,
                HorizontalScrollMode=ScrollMode.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,VerticalScrollMode=ScrollMode.Enabled,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
                HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Top,UseLayoutRounding=false };
            Children.Add(header.Root);Children.Add(navigation);Children.Add(footer.Root);
        }
        internal void UpdateSlots(){header.Update(owner.Header,owner.HeaderTemplate,owner.Collapsed);footer.Update(owner.Footer,owner.FooterTemplate,owner.Collapsed);}
        internal void Release(){header.Release();footer.Release();NavigationItems.Children.Clear();navigation.Content=null;Children.Clear();}
        protected override Size MeasureOverride(Size available)
        {
            var width=Math.Max(0,available.Width-Insets.Left-Insets.Right);
            header.Root.Measure(new Size(width,double.PositiveInfinity));footer.Root.Measure(new Size(width,double.PositiveInfinity));
            headerHeight=header.Root.DesiredSize.Height;footerHeight=footer.Root.DesiredSize.Height;
            bodyHeight=Math.Max(0,available.Height-Insets.Top-Insets.Bottom-headerHeight-footerHeight);
            navigation.Measure(new Size(width,bodyHeight));
            if(!double.IsFinite(bodyHeight))bodyHeight=navigation.DesiredSize.Height;
            return new Size(available.Width,Insets.Top+headerHeight+bodyHeight+footerHeight+Insets.Bottom);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var width=Math.Max(0,final.Width-Insets.Left-Insets.Right);var y=Insets.Top;
            header.Root.Arrange(new Rect(Insets.Left,y,width,headerHeight));y+=headerHeight;
            bodyHeight=Math.Max(0,final.Height-Insets.Top-Insets.Bottom-headerHeight-footerHeight);
            navigation.Arrange(new Rect(Insets.Left,y,width,bodyHeight));y+=bodyHeight;
            footer.Root.Arrange(new Rect(Insets.Left,y,width,footerHeight));return final;
        }
    }
}
