import fs from 'node:fs/promises';
const colors={Light:{background:'#FFFFFF',foreground:'#2D2D2D',secondary:'#595959',selected:'#2C5784',selectedText:'#FFFFFF',border:'#E6D5B8',borderAlpha:.3,sand:'#E6D5B8',hover:'#F9F6F2',hoverAlpha:.5},Default:{background:'#182533',foreground:'#F0F4F8',secondary:'#B8C5D6',selected:'#5BA3F5',selectedText:'#182533',border:'#FFFFFF',borderAlpha:.15,sand:'#1A2332',hover:'#FFFFFF',hoverAlpha:.06}};
await fs.mkdir('src/Orchard/Themes/Pagination',{recursive:true});
for(const role of ['Arrow','Page','Selected']){
 let xml='<!-- Generated from pinned Bleecker pagination.tsx; use scripts/generate-pagination-palettes.mjs. -->\n<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n <ResourceDictionary.ThemeDictionaries>\n';
 for(const[scheme,c]of Object.entries(colors)){
  xml+=`  <ResourceDictionary x:Key="${scheme}">\n`;
  const selected=role==='Selected',arrow=role==='Arrow',normal={Background:[selected?c.selected:c.background,1],Foreground:[selected?c.selectedText:arrow?c.foreground:c.secondary,1],BorderBrush:[selected?c.selected:c.border,selected?1:arrow?c.borderAlpha:0]};
  for(const state of['','PointerOver','Pressed','Disabled'])for(const[property,pair]of Object.entries(normal)){
   let [color,opacity]=pair;
   if(!selected&&(state==='PointerOver'||state==='Pressed')){
    if(property==='Background'){color=c.hover;opacity=c.hoverAlpha;}
    if(property==='Foreground')color=c.foreground;
    if(property==='BorderBrush'){color=arrow?c.sand:c.border;opacity=arrow?.6:c.borderAlpha;}
   }
   xml+=`   <SolidColorBrush x:Key="Button${property}${state}" Color="${color}" Opacity="${opacity}"/>\n`;
  }
  xml+='  </ResourceDictionary>\n';
 }
 xml+=' </ResourceDictionary.ThemeDictionaries>\n <Style TargetType="Button" BasedOn="{StaticResource DefaultButtonStyle}">\n';
 const setters={Background:'{ThemeResource ButtonBackground}',BackgroundSizing:'OuterBorderEdge',Foreground:'{ThemeResource ButtonForeground}',BorderBrush:'{ThemeResource ButtonBorderBrush}',BorderThickness:'1',Height:'36',MinHeight:'36',MinWidth:role==='Arrow'?'22':'36',Padding:role==='Arrow'?'0':'10,0',CornerRadius:'{StaticResource BleeckerButtonCornerRadius}',FontFamily:'{StaticResource BleeckerPrimaryMediumFontFamily}',FontSize:'13',FontWeight:'Medium',UseLayoutRounding:'False',HorizontalAlignment:'Stretch',HorizontalContentAlignment:'Center',VerticalContentAlignment:'Center',UseSystemFocusVisuals:'True',FocusVisualPrimaryBrush:'{ThemeResource BleeckerSeaBrush}',FocusVisualPrimaryThickness:'2',FocusVisualSecondaryThickness:'2',FocusVisualMargin:'-4'};
 for(const[property,value]of Object.entries(setters))xml+=`  <Setter Property="${property}" Value="${value}"/>\n`;
 xml+=' </Style>\n</ResourceDictionary>\n';await fs.writeFile(`src/Orchard/Themes/Pagination/${role}.g.xaml`,xml);
}
