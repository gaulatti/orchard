using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
namespace Orchard;

internal static class BleeckerModalViewport
{
    internal static Style CreateStyle() => (Style)XamlReader.Load("""
        <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ScrollView">
          <Setter Property="UseLayoutRounding" Value="False"/><Setter Property="HorizontalScrollRailMode" Value="Disabled"/><Setter Property="VerticalScrollRailMode" Value="Disabled"/>
          <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ScrollView">
            <Grid x:Name="PART_Root" Background="Transparent" UseLayoutRounding="False"><Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
              <ScrollPresenter x:Name="PART_ScrollPresenter" Content="{TemplateBinding Content}" ContentOrientation="{TemplateBinding ContentOrientation}" HorizontalScrollMode="{TemplateBinding HorizontalScrollMode}" VerticalScrollMode="{TemplateBinding VerticalScrollMode}" HorizontalScrollRailMode="{TemplateBinding HorizontalScrollRailMode}" VerticalScrollRailMode="{TemplateBinding VerticalScrollRailMode}" ZoomMode="Disabled" UseLayoutRounding="False"/>
              <ScrollBar x:Name="PART_HorizontalScrollBar" Visibility="Collapsed"/>
              <ScrollBar x:Name="PART_VerticalScrollBar" Grid.Column="1" Width="15" MinWidth="0" Orientation="Vertical" Visibility="{TemplateBinding ComputedVerticalScrollBarVisibility}" IsTabStop="False" SmallChange="40" UseLayoutRounding="False">
                <ScrollBar.Template><ControlTemplate TargetType="ScrollBar">
                  <Grid Background="{ThemeResource AlertDialogScrollTrackBrush}" UseLayoutRounding="False">
                    <Grid x:Name="HorizontalRoot" Visibility="Collapsed"/>
                    <Grid x:Name="VerticalRoot" UseLayoutRounding="False">
                      <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
                      <RepeatButton x:Name="VerticalSmallDecrease" Width="15" HorizontalAlignment="Stretch" Height="17" MinHeight="0" MinWidth="0" IsTabStop="False" AllowFocusOnInteraction="False" UseLayoutRounding="False"><RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Grid Background="Transparent"><Path Data="M0 6 L3.5 0 L7 6 Z" Width="7" Height="6" HorizontalAlignment="Center" VerticalAlignment="Center" Fill="{ThemeResource AlertDialogScrollThumbBrush}"/></Grid></ControlTemplate></RepeatButton.Template></RepeatButton>
                      <RepeatButton x:Name="VerticalLargeDecrease" Grid.Row="1" Height="0" IsTabStop="False" AllowFocusOnInteraction="False"><RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Border Background="Transparent"/></ControlTemplate></RepeatButton.Template></RepeatButton>
                      <Thumb x:Name="VerticalThumb" Grid.Row="2" MinHeight="17" Margin="3,0,3,0" IsTabStop="False" UseLayoutRounding="True" AutomationProperties.AccessibilityView="Raw"><Thumb.Template><ControlTemplate TargetType="Thumb"><Border Margin="0,1,0,1" Background="{ThemeResource AlertDialogScrollThumbBrush}" CornerRadius="4.5"/></ControlTemplate></Thumb.Template></Thumb>
                      <RepeatButton x:Name="VerticalLargeIncrease" Grid.Row="3" IsTabStop="False" AllowFocusOnInteraction="False"><RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Border Background="Transparent"/></ControlTemplate></RepeatButton.Template></RepeatButton>
                      <RepeatButton x:Name="VerticalSmallIncrease" Grid.Row="4" Width="15" HorizontalAlignment="Stretch" Height="17" MinHeight="0" MinWidth="0" IsTabStop="False" AllowFocusOnInteraction="False" UseLayoutRounding="False"><RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Grid Background="Transparent"><Path Data="M0 0 L7 0 L3.5 6 Z" Width="7" Height="6" HorizontalAlignment="Center" VerticalAlignment="Center" Fill="{ThemeResource AlertDialogScrollThumbBrush}"/></Grid></ControlTemplate></RepeatButton.Template></RepeatButton>
                    </Grid>
                  </Grid>
                </ControlTemplate></ScrollBar.Template>
              </ScrollBar>
            </Grid>
          </ControlTemplate></Setter.Value></Setter>
        </Style>
        """);
}
