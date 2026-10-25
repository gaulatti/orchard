using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
namespace Orchard.Catalog;
public sealed partial class ProgressStacksFixture:UserControl
{
 public ProgressStacksFixture(){InitializeComponent();PartialStack.Max=20;foreach(var control in new[]{PartialStack,HiddenStack,WrappedStack})foreach(var item in Sample())control.Items.Add(item);}
 internal static BleeckerProgressStackItem[] Sample()=>[new(){Label="Published",Value=6,Color=new SolidColorBrush(Windows.UI.Color.FromArgb(255,44,87,132))},new(){Label="Draft",Value=3,Color=new SolidColorBrush(Windows.UI.Color.FromArgb(255,189,112,103))},new(){Label="Review",Value=1,Color=new SolidColorBrush(Windows.UI.Color.FromArgb(255,179,139,77))}];
 internal async Task VerifyAsync(){await MainWindow.AwaitPresentationAsync(this);if(DefaultStack.ActualHeight!=40||HiddenStack.ActualHeight!=12||WrappedStack.ActualHeight!=68||UnicodeStack.ActualHeight!=84||BreakControlStack.ActualHeight!=84)throw new InvalidOperationException("ProgressStack source geometry changed.");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(DefaultStack);if(DefaultStack.IsTabStop||peer.GetAutomationControlType()!=AutomationControlType.Group||peer.GetPattern(PatternInterface.Invoke)is not null||peer.GetPattern(PatternInterface.RangeValue)is not null)throw new InvalidOperationException("ProgressStack gained an interactive pattern.");}
}
