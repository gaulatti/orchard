using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard;

/// <summary>A compact Select with a placeholder option and intrinsic 144–208 DIP trigger width.</summary>
public sealed class BleeckerHeaderSelect:BleeckerSelect
{
    public BleeckerHeaderSelect()=>DefaultStyleKey=typeof(BleeckerHeaderSelect);
    internal override bool HeaderRecipe=>true;
    public static readonly DependencyProperty IconProperty=StartIconProperty;
    public static readonly DependencyProperty IconTemplateProperty=StartIconTemplateProperty;
    public static readonly DependencyProperty IconTemplateSelectorProperty=StartIconTemplateSelectorProperty;
    public object? Icon {get=>StartIcon;set=>StartIcon=value;}
    public DataTemplate? IconTemplate {get=>StartIconTemplate;set=>StartIconTemplate=value;}
    public DataTemplateSelector? IconTemplateSelector {get=>StartIconTemplateSelector;set=>StartIconTemplateSelector=value;}
}
