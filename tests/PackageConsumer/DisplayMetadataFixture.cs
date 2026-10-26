using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class DisplayMetadataFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static IEnumerable<DependencyObject> Descendants(DependencyObject node){for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var child=VisualTreeHelper.GetChild(node,i);yield return child;foreach(var nested in Descendants(child))yield return nested;}}
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var defaults=new List<object>();var cases=new List<object>();var helpCases=new List<object>();var eyebrowCasing=new List<object>();var titleCasing=new List<object>();var guards=0;var focus=0;var activations=0;var matches=true;var stage="initial";
        try
        {
            var root=new Canvas{Width=480,Height=240};window.Content=root;root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
            void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);if(visible||window.Visible||window.AppWindow.IsVisible||foreground==Environment.ProcessId||focus!=0||activations!=0)throw new InvalidOperationException("Display metadata probe left its hidden boundary.");}
            async Task Layout(){root.Measure(new Size(480,240));root.Arrange(new Rect(0,0,480,240));root.UpdateLayout();await NativeRenderReadiness.RenderAsync(root);Guard();}
            Func<Control>[] factories=[()=>new BleeckerAvatar{Fallback="AB"},()=>new BleeckerLabel{Content="Label"},()=>new BleeckerEyebrow{Content="Desk"},()=>new BleeckerMenuLabel{Text="Section"},()=>new BleeckerMetric{Value=42},()=>new BleeckerStatCard{Title="Audience",Value=42},()=>new BleeckerErrorState{Title="Unavailable",Description="Fictional local error"},()=>new BleeckerPageHeader{Title="Today"},()=>new BleeckerFooter{Brand=new BleeckerFooterBrand{BrandName="Fictional brand"}},()=>new BleeckerStatusBadge{Label="LOCAL",Description="Fictional local status"},()=>new BleeckerKbd{Content="Esc"}];
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var factory in factories)
            {
                root.RequestedTheme=theme;var owner=factory();owner.Width=320;stage=$"{theme}/{owner.GetType().Name}/mount";root.Children.Add(owner);await Layout();var peer=FrameworkElementAutomationPeer.CreatePeerForElement(owner);var type=owner.GetType().Name;var generated=peer.GetName();var defaultHelp=peer.GetHelpText();defaults.Add(new{theme=theme.ToString(),type,name=generated,help=defaultHelp,width=owner.ActualWidth,height=owner.ActualHeight});
                if(owner is BleeckerEyebrow eyebrow)
                {
                    var originalLanguage=eyebrow.Language;eyebrow.Content="istanbul";var displayText=Descendants(eyebrow).OfType<TextBlock>().Single(text=>text.Name=="PART_Text");
                    foreach(var (language,expected) in new[]{("tr-TR","İSTANBUL"),("en-US","ISTANBUL")})
                    {
                        stage=$"{theme}/{type}/language-{language}";eyebrow.Language=language;await Layout();peer.InvalidatePeer();var actual=displayText.Text;var name=peer.GetName();var passed=actual==expected&&Equals(eyebrow.Content,"istanbul")&&name=="istanbul";matches&=passed;eyebrowCasing.Add(new{theme=theme.ToString(),language,expected,actual,content=eyebrow.Content,name,passed});
                    }
                    eyebrow.Content="Desk";eyebrow.Language=originalLanguage;await Layout();
                }
                void Check(string id,string expected){stage=$"{theme}/{type}/{id}";peer.InvalidatePeer();var actual=peer.GetName();var passed=actual==expected;matches&=passed;cases.Add(new{theme=theme.ToString(),type,id,expected,actual,passed});}
                Check("default",generated);AutomationProperties.SetName(owner,"");Check("local-empty","");AutomationProperties.SetName(owner,"Caller name");Check("local-name","Caller name");var source=new Border{Tag=""};owner.SetBinding(AutomationProperties.NameProperty,new Binding{Source=source,Path=new PropertyPath("Tag")});await Layout();Check("bound-empty","");source.Tag="Bound name";await Layout();Check("bound-name","Bound name");source.Tag="";await Layout();Check("bound-empty-again","");if(owner.GetBindingExpression(AutomationProperties.NameProperty)is null)throw new InvalidOperationException("Caller name binding was replaced.");owner.ClearValue(AutomationProperties.NameProperty);Check("clear-restores-default",generated);
                var standard=owner.Style;var callerBase=new Style{TargetType=owner.GetType(),BasedOn=standard};callerBase.Setters.Add(new Setter(AutomationProperties.NameProperty,""));owner.Style=new Style{TargetType=owner.GetType(),BasedOn=callerBase};await Layout();Check("basedon-empty","");AutomationProperties.SetName(owner,"Local name");Check("local-over-style","Local name");owner.ClearValue(AutomationProperties.NameProperty);Check("clear-reveals-style","");
                root.Children.Remove(owner);await Layout();owner.ClearValue(FrameworkElement.StyleProperty);var host=(Grid)XamlReader.Load($"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:local='using:Orchard'><Grid.Resources><Style x:Key='MetadataBase' TargetType='local:{type}'><Setter Property='AutomationProperties.Name' Value=''/></Style><Style TargetType='local:{type}' BasedOn='{{StaticResource MetadataBase}}'><Setter Property='Tag' Value='metadata-style-applied'/></Style></Grid.Resources></Grid>");host.Children.Add(owner);root.Children.Add(host);await Layout();if(owner.Tag as string!="metadata-style-applied")throw new InvalidOperationException("Implicit metadata style was not applied.");Check("implicit-empty","");host.Children.Remove(owner);root.Children.Remove(host);owner.ClearValue(FrameworkElement.StyleProperty);root.Children.Add(owner);await Layout();Check("remove-style-restores-default",generated);
                var label=new TextBlock();root.Children.Add(label);AutomationProperties.SetLabeledBy(owner,label);await Layout();Check("empty-label","");label.Text="Caller label";await Layout();FrameworkElementAutomationPeer.CreatePeerForElement(label).InvalidatePeer();Check("caller-label","Caller label");owner.ClearValue(AutomationProperties.LabeledByProperty);Check("remove-label-restores-default",generated);
                owner.SetBinding(AutomationProperties.NameProperty,new Binding{Source=source,Path=new PropertyPath("Tag")});var template=owner.Template;owner.Template=null;await Layout();owner.Template=template;await Layout();root.Children.Remove(owner);await Layout();root.Children.Add(owner);await Layout();Check("template-remount-empty","");if(owner.GetBindingExpression(AutomationProperties.NameProperty)is null)throw new InvalidOperationException("Remount replaced caller name binding.");owner.ClearValue(AutomationProperties.NameProperty);
                if(owner is BleeckerErrorState or BleeckerStatusBadge)
                {
                    void Help(string id,string expected){peer.InvalidatePeer();var actual=peer.GetHelpText();var passed=actual==expected;matches&=passed;helpCases.Add(new{theme=theme.ToString(),type,id,expected,actual,passed});}
                    AutomationProperties.SetHelpText(owner,"");Help("local-empty","");var helpSource=new Border{Tag=""};owner.SetBinding(AutomationProperties.HelpTextProperty,new Binding{Source=helpSource,Path=new PropertyPath("Tag")});await Layout();Help("bound-empty","");helpSource.Tag="Caller help";await Layout();Help("bound-help","Caller help");helpSource.Tag="";await Layout();Help("bound-empty-again","");owner.ClearValue(AutomationProperties.HelpTextProperty);Help("clear-restores-default",defaultHelp);
                }
                root.Children.Clear();await Layout();
            }
            var titleFactories=new (Func<Control> Create,string TextName)[]{
                (()=>new BleeckerFeedColumn{Title="istanbul",Width=440,Height=200},"FeedColumnTitleText"),
                (()=>new BleeckerPanel{Title="istanbul",Width=440,Height=200},"PanelTitleText"),
                (()=>new BleeckerStatCard{Title="istanbul",Value=42,Width=440,Height=200},"PART_Title"),
                (()=>new BleeckerFooter{Brand=new BleeckerFooterBrand{BrandName="Fictional brand"},Sections=new[]{new BleeckerFooterSection{Title="istanbul"}},Width=440,Height=200},"FooterSectionHeadingText")};
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var (create,textName) in titleFactories)
            {
                root.RequestedTheme=theme;var owner=create();var type=owner.GetType().Name;owner.Language="tr-TR";stage=$"{theme}/{type}/title-mount";root.Children.Add(owner);await Layout();
                var displayText=Descendants(owner).OfType<TextBlock>().Single(text=>text.Name==textName);
                foreach(var (language,expected) in new[]{("tr-TR","İSTANBUL"),("en-US","ISTANBUL")})
                {
                    stage=$"{theme}/{type}/title-language-{language}";owner.Language=language;await Layout();
                    var originalTitle=owner switch{BleeckerFeedColumn feed=>feed.Title,BleeckerPanel panel=>panel.Title,BleeckerStatCard stat=>stat.Title,BleeckerFooter footer=>footer.Sections.Single().Title,_=>throw new InvalidOperationException(type)};
                    var expectedName=owner is BleeckerStatCard?expected:"istanbul";
                    var name=owner is BleeckerFooter?AutomationProperties.GetName(Descendants(owner).OfType<FrameworkElement>().Single(element=>element.Name=="FooterSectionHeading")):FrameworkElementAutomationPeer.CreatePeerForElement(owner).GetName();
                    var actual=displayText.Text;var passed=actual==expected&&originalTitle=="istanbul"&&name==expectedName;matches&=passed;
                    titleCasing.Add(new{theme=theme.ToString(),type,language,expected,actual,originalTitle,expectedName,name,passed});
                }
                root.Children.Clear();await Layout();
            }
            Guard();await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-display-metadata-v3",passed=true,metadataContractPassed=matches,defaults,cases,helpCases,eyebrowCasing,titleCasing,guards,focus,activations,osSettingsChanged=false,hardwareInputSent=false,nativeARM64RuntimeVerified=false,librarySha256=Hash(typeof(BleeckerKbd).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=false,stage,error=error.ToString(),defaults,cases,helpCases,eyebrowCasing,titleCasing,guards,focus,activations}));Environment.Exit(1);}
    }
}
