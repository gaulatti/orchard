using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Orchard.Catalog;

public sealed partial class HeroCarouselsFixture : UserControl
{
    private readonly List<BleeckerHeroCarouselItem> original = [];
    private readonly SvgImageSource landscape=new(new Uri("ms-appx:///Assets/HeroLandscape.svg")),portrait=new(new Uri("ms-appx:///Assets/HeroPortrait.svg"));
    public HeroCarouselsFixture()
    {
        InitializeComponent();
        original.Add(new BleeckerHeroCarouselItem("/fictional/coast?title=Original%20value&kind=project","Fictional coastal project","An original caller-owned description for a fictional local component example."){Id="first",Kind=BleeckerHeroCarouselItemKind.Project,Tags=new ObservableCollection<string>{"Local","Original"}});
        original.Add(new BleeckerHeroCarouselItem("/fictional/article#original","Fictional article","A second original description stays with its caller."){Id="second",Kind=BleeckerHeroCarouselItemKind.Post,External=true});
        original.Add(new BleeckerHeroCarouselItem("/fictional/mountain","Fictional mountain project","A third original item in the same reusable component."){Id="third",Kind=BleeckerHeroCarouselItemKind.Project,Tags=new ObservableCollection<string>{"Retained"}});
        foreach(var item in original)((BleeckerHeroCarouselItems)Hero.Items).Add(item);Custom.Items=new BleeckerHeroCarouselItems(original);
        var parameter=new object();var command=new OriginalCommand(value=>Result.Text="Original host command parameter retained: "+ReferenceEquals(value,parameter));
        Hero.Command=Custom.Command=command;Hero.CommandParameter=Custom.CommandParameter=parameter;
        Hero.ItemInvoked+=Invoked;Custom.ItemInvoked+=Invoked;Hero.ImageFailed+=(_,item)=>Result.Text="Original image failed for "+item.Title+"; no replacement artwork is selected.";
        Hero.CurrentIndexChanged+=(_,_)=>Result.Text="Current original numeric index: "+Hero.CurrentIndex;
    }
    private void Invoked(object? sender,BleeckerHeroCarouselItem item)=>Result.Text="Original item retained: "+original.Contains(item)+"; destination "+item.Href+"; external "+item.External;
    private void Previous(object sender,RoutedEventArgs args)=>Hero.MovePrevious();
    private void Next(object sender,RoutedEventArgs args)=>Hero.MoveNext();
    private void Reorder(object sender,RoutedEventArgs args){var items=(BleeckerHeroCarouselItems)Hero.Items;if(items.Count>1)items.Move(0,items.Count-1);}
    private void UpdateTitle(object sender,RoutedEventArgs args)=>original[0].Title=original[0].Title=="Fictional coastal project"?"Changed original title — Straße 東京":"Fictional coastal project";
    private void RemoveLast(object sender,RoutedEventArgs args){var items=(BleeckerHeroCarouselItems)Hero.Items;if(items.Count>0)items.RemoveAt(items.Count-1);}
    private void Restore(object sender,RoutedEventArgs args){var items=(BleeckerHeroCarouselItems)Hero.Items;foreach(var item in original)if(!items.Contains(item))items.Add(item);}
    private void Indicators(object sender,RoutedEventArgs args){if(Hero is not null)Hero.ShowIndicators=((CheckBox)sender).IsChecked!=true;}
    private void Controls(object sender,RoutedEventArgs args){if(Hero is not null)Hero.ShowControls=((CheckBox)sender).IsChecked!=true;}
    private void Images(object sender,RoutedEventArgs args){if(original.Count==0)return;var enabled=((CheckBox)sender).IsChecked==true;original[0].ImageSource=enabled?landscape:null;original[1].ImageSource=enabled?portrait:null;}
    private void Direction(object sender,RoutedEventArgs args){if(Hero is not null)Hero.FlowDirection=((CheckBox)sender).IsChecked==true?FlowDirection.RightToLeft:FlowDirection.LeftToRight;}
    private void Disable(object sender,RoutedEventArgs args){if(Hero is not null)Hero.IsEnabled=((CheckBox)sender).IsChecked!=true;}
    private void FontFamilyChanged(object sender,RoutedEventArgs args){if(Hero is null)return;if(((CheckBox)sender).IsChecked==true)Hero.FontFamily=(FontFamily)Application.Current.Resources["BleeckerSecondaryFontFamily"];else Hero.ClearValue(Control.FontFamilyProperty);}
    private void ForegroundChanged(object sender,RoutedEventArgs args){if(Hero is null)return;if(((CheckBox)sender).IsChecked==true)Hero.Foreground=(Brush)Resources["CallerHeroInk"];else Hero.ClearValue(Control.ForegroundProperty);}
    private sealed class OriginalCommand(Action<object?> execute):ICommand
    {public event EventHandler? CanExecuteChanged{add{}remove{}}public bool CanExecute(object? parameter)=>true;public void Execute(object? parameter)=>execute(parameter);}
}
