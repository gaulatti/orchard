import {menuStyles} from './menu-styles.mjs';

// The two recipes share native presenter/command hosts, but Context Menu has
// distinct source typography, padding, row radius, height and focus colors.
export function contextMenuStyles(){
 const menus=menuStyles();
 const templateStart=menus.indexOf('<Setter Property="Template">'),templateEnd=menus.indexOf('</Setter>',templateStart);
 if(templateStart<0||templateEnd<templateStart)throw new Error('Native menu presenter template not found');
 const presenterTemplate=menus.slice(templateStart,templateEnd+9).replace('<local:BleeckerOverlaySurface ','<Grid x:Name="PART_ContextPortalSlot"><local:BleeckerOverlaySurface x:Name="PART_ContextSurface" FontFamily="{TemplateBinding FontFamily}" FontSize="{TemplateBinding FontSize}" FontWeight="{TemplateBinding FontWeight}" Foreground="{TemplateBinding Foreground}" Language="{TemplateBinding Language}" FlowDirection="{TemplateBinding FlowDirection}" IsTextScaleFactorEnabled="{TemplateBinding IsTextScaleFactorEnabled}" ').replace('</local:BleeckerOverlaySurface>','</local:BleeckerOverlaySurface></Grid>');
 function item(key,next){
  const start=menus.indexOf(`<Style x:Key="${key}"`),end=menus.indexOf('</Style>',start);if(start<0||end<0)throw new Error('Pinned menu template not found: '+key);
  return menus.slice(start,end+8).replace(`x:Key="${key}"`,`x:Key="${next}"`).replaceAll('BleeckerMenuHoverBrush','BleeckerContextMenuHoverBrush').replace('Property="FontSize" Value="14"','Property="FontSize" Value="13"').replace('Property="Padding" Value="8,6"','Property="Padding" Value="10,6"').replace('Property="MinHeight" Value="0"','Property="MinHeight" Value="36"').replace('Property="CornerRadius" Value="{StaticResource BleeckerUiCornerRadius}"','Property="CornerRadius" Value="5"').replaceAll('Value="0.5"','Value="0.4"').replace('LineHeight="20"','LineHeight="19.5" local:BleeckerTextLayout.UseReferenceLineBox="True"');
 }
 return `<!-- Generated from the pinned Bleecker context-menu recipe; do not edit. -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="using:Orchard">
 <Style x:Key="BleeckerContextMenuPresenterStyle" TargetType="MenuFlyoutPresenter" BasedOn="{StaticResource BleeckerMenuPresenterStyle}"><Setter Property="MinWidth" Value="160"/>${presenterTemplate}</Style>
 ${item('BleeckerMenuItemStyle','BleeckerContextMenuItemStyle')}
 ${item('BleeckerMenuContentItemStyle','BleeckerContextMenuContentItemStyle')}
 <Style x:Key="BleeckerContextMenuInsetItemStyle" TargetType="MenuFlyoutItem" BasedOn="{StaticResource BleeckerContextMenuItemStyle}"><Setter Property="Padding" Value="32,6,10,6"/></Style>
 <Style x:Key="BleeckerContextMenuInsetContentItemStyle" TargetType="local:BleeckerMenuContentItem" BasedOn="{StaticResource BleeckerContextMenuContentItemStyle}"><Setter Property="Padding" Value="32,6,10,6"/></Style>
</ResourceDictionary>\n`;
}
