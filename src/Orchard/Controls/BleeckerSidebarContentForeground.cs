using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Orchard;

// Same ownership contract as BleeckerIconButton: bridge the native default
// only, preserve explicit values/bindings/styles, and remove only our binding.
internal sealed class BleeckerSidebarContentForeground
{
    private FrameworkElement? element;
    private BleeckerSidebarLinkContext? context;
    private readonly Dictionary<DependencyProperty,Binding> bindings=[];
    private (DependencyProperty Property,string Name)[] properties=[];
    private long styleToken;
    internal void Bind(object? value,BleeckerSidebarLinkContext source)
    {
        if(ReferenceEquals(element,value)&&ReferenceEquals(context,source))return;
        Release();
        if(value is not Control && value is not TextBlock && value is not IconElement)return;
        element=(FrameworkElement)value;context=source;
        properties=value is Control?[(Control.ForegroundProperty,"Foreground"),(Control.FontFamilyProperty,"FontFamily"),(Control.FontSizeProperty,"FontSize"),(Control.FontWeightProperty,"FontWeight"),(Control.CharacterSpacingProperty,"CharacterSpacing")]:value is TextBlock?[(TextBlock.ForegroundProperty,"Foreground"),(TextBlock.FontFamilyProperty,"FontFamily"),(TextBlock.FontSizeProperty,"FontSize"),(TextBlock.FontWeightProperty,"FontWeight"),(TextBlock.CharacterSpacingProperty,"CharacterSpacing")]:[(IconElement.ForegroundProperty,"Foreground")];
        styleToken=element.RegisterPropertyChangedCallback(FrameworkElement.StyleProperty,(_,_)=>Apply());Apply();
    }
    private void Apply()
    {
        ClearOwnedBinding();
        foreach(var(property,name)in properties)
        {
            if(element!.ReadLocalValue(property)!=DependencyProperty.UnsetValue)continue;
            var specified=false;for(var style=element.Style;style is not null;style=style.BasedOn)if(style.Setters.OfType<Setter>().Any(setter=>setter.Property==property)){specified=true;break;}
            if(specified)continue;
            var binding=new Binding{Source=context,Path=new PropertyPath(name)};bindings.Add(property,binding);element.SetBinding(property,binding);
        }
    }
    private void ClearOwnedBinding()
    {
        foreach(var(property,binding)in bindings)if(element?.GetBindingExpression(property)?.ParentBinding==binding)element.ClearValue(property);
        bindings.Clear();
    }
    internal void Release(){ClearOwnedBinding();if(element is not null)element.UnregisterPropertyChangedCallback(FrameworkElement.StyleProperty,styleToken);element=null;properties=[];context=null;}
}
