using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Text;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunFilterChipTypographyAsync(string folder)
    {
        var original=Content;
        var page=new Page();var canvas=new Canvas();page.Content=canvas;
        var reports=new List<object>();var phase="mount";var passed=false;
        NativeInputSession? input=null;
        try
        {
            Content=page;await AwaitPresentationAsync(page);
            input=new NativeInputSession(page.XamlRoot,folder,"filter-chip-typography");await input.AcquireAsync(canvas);
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                page.RequestedTheme=theme;
                canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                canvas.Children.Clear();var group=new BleeckerFilterGroup{Width=600};Canvas.SetLeft(group,20);Canvas.SetTop(group,20);canvas.Children.Add(group);
                var invoked=0;var status=new BleeckerFilter("Status","Draft");status.RemoveRequested+=(_,_)=>invoked++;
                group.Filters.Add(status);group.Filters.Add(new("Desk","Culture"));group.ClearAllRequested+=(_,_)=>{};
                await AwaitPresentationAsync(page);
                var defaultFamily=group.FontFamily;var defaultWeight=group.FontWeight;
                var action=FilterTypographyDescendants<Button>(group).Single(button=>AutomationProperties.GetName(button)=="Remove Status filter");
                foreach(var step in new[]{"normal","large","tracking","system-family","italic-expanded","secondary-family","rtl","constrained","unicode","restored"})
                {
                    phase=theme+"/"+step;
                    switch(step)
                    {
                        case "large":group.FontSize=20;break;
                        case "tracking":group.CharacterSpacing=125;break;
                        case "system-family":group.FontFamily=new FontFamily("Segoe UI");break;
                        case "italic-expanded":group.FontStyle=FontStyle.Italic;group.FontStretch=FontStretch.Expanded;break;
                        case "secondary-family":group.FontFamily=new FontFamily("ms-appx:///Orchard/Assets/Fonts/LibreFranklin-Regular.ttf#Libre Franklin");break;
                        case "rtl":group.FlowDirection=FlowDirection.RightToLeft;break;
                        case "constrained":group.Width=220;break;
                        case "unicode":group.Width=600;status.Label="Révision 日本語";status.Value="العربية";break;
                        case "restored":group.FontFamily=defaultFamily;group.FontSize=12;group.FontWeight=defaultWeight;group.FontStyle=FontStyle.Normal;group.FontStretch=FontStretch.Normal;group.CharacterSpacing=0;group.FlowDirection=FlowDirection.LeftToRight;group.Width=600;status.Label="Status";status.Value="Draft";break;
                    }
                    await AwaitPresentationAsync(page);await input.MoveAsync(canvas,true,new Point(page.XamlRoot.Size.Width-10,page.XamlRoot.Size.Height-20));
                    var texts=FilterTypographyDescendants<TextBlock>(group).Where(text=>VisualTreeHelper.GetParent(text)is FrameworkElement parent&&parent.Name is "FilterLabel" or "FilterValue" or "ClearFilterText").ToArray();
                    reports.Add(new{phase,owner=new{group.FontSize,family=group.FontFamily.Source,style=group.FontStyle.ToString(),stretch=group.FontStretch.ToString(),group.CharacterSpacing,direction=group.FlowDirection.ToString(),group.Width},box=Box(group),
                        chips=FilterTypographyDescendants<BleeckerFilterChip>(group).Select(chip=>new{chip.Label,box=Box(chip)}).ToArray(),
                        texts=texts.Select(text=>new{text.Text,role=((FrameworkElement)VisualTreeHelper.GetParent(text)).Name,family=text.FontFamily.Source,text.FontSize,style=text.FontStyle.ToString(),stretch=text.FontStretch.ToString(),text.CharacterSpacing,direction=text.FlowDirection.ToString(),text.IsTextScaleFactorEnabled,lineHeight=text.LineHeight,box=Box((FrameworkElement)VisualTreeHelper.GetParent(text)),intrinsic=Intrinsic(text),baseline=text.TransformToVisual(page).TransformPoint(new Point()).Y+text.BaselineOffset}).ToArray()});
                    await SaveAsync();
                    Require(texts.Length==5,phase+": missing rendered label/value/Clear all text.");
                    foreach(var text in texts)
                    {
                        var role=((FrameworkElement)VisualTreeHelper.GetParent(text)).Name;
                        Require(text.FontSize==group.FontSize&&text.CharacterSpacing==group.CharacterSpacing,phase+"/"+role+": live font size or spacing did not reach rendered text.");
                        Require(text.FontStyle==group.FontStyle&&text.FontStretch==group.FontStretch,phase+"/"+role+": font style/stretch did not reach rendered text.");
                        Require(text.FlowDirection==group.FlowDirection&&text.IsTextScaleFactorEnabled==group.IsTextScaleFactorEnabled,phase+"/"+role+": direction or text-scale preference differs.");
                        var paragraph=(FrameworkElement)VisualTreeHelper.GetParent(text);
                        Require(Math.Abs(paragraph.ActualWidth-Intrinsic(text))<.02,phase+"/"+role+": paragraph retained stale intrinsic text width.");
                    }
                    Require(ReferenceEquals(action,FilterTypographyDescendants<Button>(group).Single(button=>AutomationProperties.GetName(button)=="Remove "+status.Label+" filter")),phase+": typography change replaced the native action.");
                    var chips=FilterTypographyDescendants<BleeckerFilterChip>(group).ToArray();
                    if(step is "rtl" or "unicode")Require(Box(chips[0]).x>Box(chips[1]).x,phase+": RTL did not reverse physical chip order.");
                    if(step=="constrained")Require(chips.Select(chip=>Box(chip).y).Distinct().Count()>1,phase+": increased typography did not wrap the constrained group.");
                    await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"filter-chip-typography-{step}-{theme}.png"));
                }
                Require(action.Focus(FocusState.Keyboard),"Typography-restored action refused focus.");await input.KeyAsync(0x20,action);Require(invoked==1,"Typography changes duplicated or lost the original consumer callback.");
                var before=group.ActualHeight;canvas.Children.Remove(group);await AwaitPresentationAsync(page);group.FontSize=20;canvas.Children.Add(group);await AwaitPresentationAsync(page);
                Require(group.ActualHeight>before&&ReferenceEquals(action,FilterTypographyDescendants<Button>(group).Single(button=>AutomationProperties.GetName(button)=="Remove Status filter")),"Detached typography update did not refresh the remounted group.");
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            }
            passed=true;
        }
        catch
        {
            if(input is not null)await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,"failure.png"));
            throw;
        }
        finally{input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);await SaveAsync();}
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="filter-chip-typography",environment=RuntimeEnvironment()}));
        (double x,double y,double width,double height) Box(FrameworkElement element){var position=element.TransformToVisual(page).TransformPoint(new Point());return(position.X,position.Y,element.ActualWidth,element.ActualHeight);}
        static double Intrinsic(TextBlock text)
        {
            var measure=new TextBlock{Text=text.Text,FontFamily=text.FontFamily,FontSize=text.FontSize,FontWeight=text.FontWeight,FontStyle=text.FontStyle,FontStretch=text.FontStretch,CharacterSpacing=text.CharacterSpacing,FlowDirection=text.FlowDirection,IsTextScaleFactorEnabled=text.IsTextScaleFactorEnabled,UseLayoutRounding=false};
            measure.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));return measure.DesiredSize.Width;
        }
        Task SaveAsync()=>File.WriteAllTextAsync(Path.Combine(folder,"filter-chip-typography.json"),JsonSerializer.Serialize(new{passed,phase,reports,originalContentRestored=ReferenceEquals(Content,original)},new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
    }
    private static IEnumerable<T> FilterTypographyDescendants<T>(DependencyObject parent)where T:DependencyObject
    {for(var index=0;index<VisualTreeHelper.GetChildrenCount(parent);index++){var child=VisualTreeHelper.GetChild(parent,index);if(child is T match)yield return match;foreach(var nested in FilterTypographyDescendants<T>(child))yield return nested;}}
}
