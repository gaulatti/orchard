using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
namespace Orchard.Testing;

internal static class OtpInputFixture
{
    private sealed class ValueOwner : INotifyPropertyChanged
    {
        private string value="482";
        public string Value { get=>value;set{this.value=value;PropertyChanged?.Invoke(this,new(nameof(Value)));} }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private delegate bool Visitor(nint handle,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static Control[] Editors(BleeckerOtpInput input)=>Elements(input).OfType<Control>().Where(e=>e is TextBox or PasswordBox).ToArray();
    private static string Text(Control editor)=>editor is TextBox text?text.Text:((PasswordBox)editor).Password;
    private static void SetText(Control editor,string value){if(editor is TextBox text)text.Text=value;else((PasswordBox)editor).Password=value;}
    internal static async Task RunAsync(Window window,string output,string hostPath,string referencePath)
    {
        var folder=Path.GetDirectoryName(output)!;Directory.CreateDirectory(folder);var root=new Grid{Width=480,Height=200,Padding=new Thickness(20)};root.Clip=new RectangleGeometry{Rect=new Rect(0,0,480,200)};window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(480,200));var guards=0;var focus=0;var activations=0;var edits=0;var cases=new List<object>();var behaviors=new List<object>();var pngs=new List<object>();var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0&&VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count==0,"OTP probe left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<4;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain OTP layout.");await idle.Task;root.Measure(new Size(480,200));root.Arrange(new Rect(0,0,480,200));root.UpdateLayout();Guard();await Task.Delay(12);}}
        object Box(FrameworkElement target){var p=target.TransformToVisual(root).TransformBounds(new Rect(0,0,target.ActualWidth,target.ActualHeight));return new{x=p.X,y=p.Y,width=p.Width,height=p.Height};}
        async Task SavePng(string file,byte[] pixels,int width,int height)
        {
            using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)width,(uint)height,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(folder,file),bytes);
        }
        async Task Capture(string file)
        {
            Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
            // RenderTargetBitmap includes negative descendant ink in its bounds,
            // even with a viewport clip. Locate the opaque host's top edge and
            // preserve both the raw buffer and its actual viewport crop.
            var cropX=0;while(cropX<bitmap.PixelWidth&&pixels[cropX*4+3]!=255)cropX++;
            Require(bitmap.PixelHeight==200&&cropX+480<=bitmap.PixelWidth,"Could not locate the opaque 480x200 capture viewport.");
            var viewport=new byte[480*200*4];for(var y=0;y<200;y++)System.Buffer.BlockCopy(pixels,(y*bitmap.PixelWidth+cropX)*4,viewport,y*480*4,480*4);
            var rawFile="raw-"+file;await SavePng(rawFile,pixels,bitmap.PixelWidth,bitmap.PixelHeight);await SavePng(file,viewport,480,200);
            string Hash(string name)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(folder,name))));
            pngs.Add(new{file,width=480,height=200,sha256=Hash(file),rawFile,rawSha256=Hash(rawFile),rawWidth=bitmap.PixelWidth,rawHeight=bitmap.PixelHeight,cropX,cropY=0});Guard();
        }
        void Theme(string scheme){root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);root.Background=new SolidColorBrush(scheme=="Dark"?Windows.UI.Color.FromArgb(255,13,24,33):Microsoft.UI.Colors.White);}
        async Task Edit(Control editor,string value){Guard();SetText(editor,value);edits++;await Layout();Guard();}
        try
        {
            using var reference=JsonDocument.Parse(await File.ReadAllBytesAsync(referencePath));
            foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var scheme=row.GetProperty("scheme").GetString()!;var id=row.GetProperty("id").GetString()!;var proposal=row.GetProperty("width").GetInt32();var sourceSlots=row.GetProperty("slots");var count=sourceSlots.GetArrayLength();var original=row.GetProperty("value").GetString()!;var secure=count>0&&sourceSlots[0].GetProperty("type").GetString()=="password";var disabled=count>0&&sourceSlots[0].GetProperty("disabled").GetBoolean();var rtl=count>0&&sourceSlots[0].GetProperty("paint").GetProperty("direction").GetString()=="rtl";stage=$"{scheme}-{proposal}-{id}";Theme(scheme);
                var input=new BleeckerOtpInput{Value=original,Length=count,IsSecure=secure,IsEnabled=!disabled,Spacing=double.Parse(row.GetProperty("group").GetProperty("gap").GetString()!.Replace("px",""),System.Globalization.CultureInfo.InvariantCulture),Width=proposal,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,FlowDirection=rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight};var requests=0;input.ValueRequested+=(_,_)=>requests++;root.Children.Add(input);await Layout();var editors=Editors(input);Require(editors.Length==count&&input.Value==original&&requests==0,"Initial source slots changed caller value or requested edits.");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(input);Require(peer.GetAutomationControlType()==AutomationControlType.Group&&peer.GetName()=="One-time password input","Native group metadata differs from source.");var styles=new List<object>();
                for(var i=0;i<count;i++)
                {
                    var editor=editors[i];var actual=Text(editor);var expected=sourceSlots[i].GetProperty("value").GetString()!;Require(actual==expected,$"Source display differs at slot {i}: expected length {expected.Length}, actual length {actual.Length}.");var nativePeer=FrameworkElementAutomationPeer.CreatePeerForElement(editor);Require(nativePeer.GetName()==$"Digit {i+1}"&&nativePeer.IsPassword()==secure&&editor.IsEnabled==!disabled&&editor.ActualWidth==40&&editor.ActualHeight==48,"Native slot name/protection/disabled state or bounds differs from source.");if(editor is PasswordBox password){Require(password.PasswordRevealMode==PasswordRevealMode.Hidden,"Secure slot can reveal its value.");var exposed=false;try{if(nativePeer.GetPattern(PatternInterface.Value)is IValueProvider p)exposed=p.Value==actual&&actual.Length>0;}catch(Exception){ }Require(!exposed,"Secure slot exposed its raw value through UIA.");}
                    Require(editor.FontSize==18&&editor.FontWeight.Weight==600&&editor.CharacterSpacing==80&&editor.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf"),"Source typography did not reach the native editor.");var slot=Elements(input).OfType<Grid>().Single(e=>e.Name=="OtpSlot"+i);var chrome=Elements(slot).OfType<Border>().Single(e=>e.Name=="OtpChrome");var border=(SolidColorBrush)chrome.BorderBrush;Require(Math.Abs(slot.Opacity-(disabled ? .5 : 1))<1e-6&&chrome.CornerRadius.TopLeft==7&&chrome.BorderThickness.Left==1,"Slot opacity, border or radius differs from source.");styles.Add(new{index=i,text=actual,box=Box(editor),slotBox=Box(slot),background=((SolidColorBrush)chrome.Background).Color.ToString().ToLowerInvariant(),foreground=((SolidColorBrush)editor.Foreground).Color.ToString().ToLowerInvariant(),border=border.Color.ToString().ToLowerInvariant(),borderOpacity=border.Opacity,opacity=slot.Opacity,font=editor.FontSize,weight=editor.FontWeight.Weight,tracking=editor.CharacterSpacing,password=nativePeer.IsPassword(),label=nativePeer.GetName()});
                }
                Require(input.ActualWidth==proposal&&input.ActualHeight==(count==0?0:48),"Native group bounds differ from source.");cases.Add(new{scheme,width=proposal,id,passed=true,value=input.Value,groupBox=Box(input),slots=styles,requests,originalValueRetained=true,sourceDisplayMatched=true,nativeMetadataMatched=true});await Capture($"otp-native-{scheme}-{proposal}-{id}.png");root.Children.Remove(input);await Layout();
            }
            foreach(var scheme in new[]{"Light","Dark"})foreach(var secure in new[]{false,true})foreach(var accept in new[]{false,true})
            {
                stage=$"{scheme}-{secure}-{accept}-behavior";Theme(scheme);var owner=new ValueOwner();var input=new BleeckerOtpInput{Length=6,IsSecure=secure,Width=320,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};input.SetBinding(BleeckerOtpInput.ValueProperty,new Binding{Source=owner,Path=new PropertyPath(nameof(ValueOwner.Value)),Mode=BindingMode.OneWay});var requests=new List<string>();input.ValueRequested+=(_,e)=>{requests.Add(e.Value);if(accept)owner.Value=e.Value;};root.Children.Add(input);await Layout();var editors=Editors(input);await Edit(editors[1],"7");Require(requests.SequenceEqual(new[]{"472"})&&input.Value==(accept?"472":"482")&&owner.Value==input.Value&&Text(editors[1])==(accept?"7":"8"),$"Native edit bypassed controlled caller acceptance: requests={string.Join("|",requests)}, value={input.Value}, caller={owner.Value}, editor={Text(editors[1])}, loaded={input.IsLoaded}/{editors[1].IsLoaded}.");Require(Editors(input).SequenceEqual(editors),"Value changes replaced stable native editors.");behaviors.Add(new{scheme,secure,accept,state="controlled-edit",passed=true,requested="472",callerValue=owner.Value,propertyValue=input.Value,editedSlot=Text(editors[1]),callerOwnsValue=true,rejectedEditRestored=true,editorIdentityRetained=true});
                owner.Value="123456789";await Layout();Require(input.Value==owner.Value&&Editors(input).Select(Text).SequenceEqual(new[]{"1","2","3","4","5","6"}),"Caller value was clamped before an edit.");await Edit(editors[0],"x");Require(requests.Last()=="23456"&&input.Value==(accept?"23456":"123456789"),"Invalid native edit did not request source compaction.");behaviors.Add(new{scheme,secure,accept,state="caller-and-filter",passed=true,originalLongValueRetained=true,asciiFilteringAndCompaction=true});
                input.Length=4;await Layout();Require(Editors(input).Length==4&&Editors(input).Take(4).SequenceEqual(editors.Take(4)),"Length shrink replaced retained native slots.");var removed=editors[5];var before=requests.Count;SetText(removed,"9");await Layout();Require(requests.Count==before,"Removed editor still requested a value.");input.Length=8;await Layout();Require(Editors(input).Length==8&&Editors(input).Take(4).SequenceEqual(editors.Take(4)),"Length growth replaced retained slots.");var previous=Editors(input);input.IsSecure=!secure;await Layout();Require(Editors(input).All(e=>secure?e is TextBox:e is PasswordBox)&&!Editors(input).Intersect(previous).Any()&&input.Value==owner.Value,"Secure mode replacement lost caller data or retained the wrong editor type.");before=requests.Count;SetText(previous[0],"9");await Layout();Require(requests.Count==before,"Replaced native editor still requested edits.");behaviors.Add(new{scheme,secure,accept,state="length-and-mode",passed=true,retainedIndicesStable=true,removedEditorDetached=true,replacedEditorDetached=true,modeRetainsCallerValue=true});
                input.IsEnabled=false;await Layout();before=requests.Count;SetText(Editors(input)[0],"9");await Layout();Require(requests.Count==before&&Editors(input).All(e=>!e.IsEnabled)&&Text(Editors(input)[0])==owner.Value[0].ToString(),"Disabled edit produced a request.");input.IsEnabled=true;await Layout();var current=Editors(input);root.Children.Remove(input);await Layout();SetText(current[0],"9");await Layout();Require(requests.Count==before,"Unloaded edit produced a request.");root.Children.Add(input);await Layout();Require(Editors(input).SequenceEqual(current)&&input.Value==owner.Value&&Text(current[0])==owner.Value[0].ToString(),"Reload lost native editor identity or caller data.");input.Length=0;await Layout();Require(Editors(input).Length==0&&input.ActualHeight==0&&input.Value==owner.Value,"Zero length invented default slots or changed caller data.");behaviors.Add(new{scheme,secure,accept,state="disabled-empty-reload",passed=true,disabledEditRejected=true,unloadedEditRejected=true,reloadRetainsCallerAndEditors=true,zeroLengthRetainsCallerValue=true});root.Children.Remove(input);await Layout();
            }
            string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-otp-input-native-v1",passed=true,cases,behaviors,pngs,guards,focus,activations,edits,publicApisOnly=true,activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,osClipboardUsed=false,physicalInputVerified=false,nativeClipboardVerified=false,nativeKeyRoutingVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,sourceReferenceSha256=Hash(referencePath),librarySha256=Hash(typeof(BleeckerOtpInput).Assembly.Location),hostSha256=Hash(hostPath)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-otp-input-native-v1",passed=false,stage,error=error.ToString(),cases,behaviors,guards,focus,activations,edits},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
