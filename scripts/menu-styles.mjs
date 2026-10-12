// Bleecker dropdown-menu.tsx appearance over native WinUI menu behavior.
export function menuStyles() {
  let xaml = `<!-- Generated from the pinned Bleecker dropdown-menu recipe; do not edit. -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="using:Orchard">
    <Style x:Key="BleeckerMenuPresenterStyle" TargetType="MenuFlyoutPresenter">
        <Setter Property="MinWidth" Value="176" /><Setter Property="MinHeight" Value="0" />
        <Setter Property="Padding" Value="6" /><Setter Property="CornerRadius" Value="{StaticResource BleeckerUiCornerRadius}" />
        <Setter Property="BorderThickness" Value="1" /><Setter Property="IsDefaultShadowEnabled" Value="False" />
        <Setter Property="Background" Value="{ThemeResource BleeckerMenuBackgroundBrush}" />
        <Setter Property="Foreground" Value="{ThemeResource BleeckerTextPrimaryBrush}" />
        <Setter Property="BorderBrush" Value="{ThemeResource BleeckerMenuBorderBrush}" />
        <Setter Property="FontFamily" Value="{StaticResource BleeckerPrimaryFontFamily}" />
        <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="MenuFlyoutPresenter">
            <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="{TemplateBinding CornerRadius}">
                <ScrollViewer x:Name="MenuFlyoutPresenterScrollViewer" HorizontalScrollMode="Disabled" HorizontalScrollBarVisibility="Disabled"
                    VerticalScrollMode="Auto" VerticalScrollBarVisibility="Auto" IsTabStop="False" AutomationProperties.AccessibilityView="Raw"><ItemsPresenter Margin="{TemplateBinding Padding}" /></ScrollViewer>
            </Border>
        </ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style x:Key="BleeckerMenuSeparatorStyle" TargetType="MenuFlyoutSeparator">
        <Setter Property="Margin" Value="-6,4" /><Setter Property="MinHeight" Value="0" /><Setter Property="Height" Value="1" />
        <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="MenuFlyoutSeparator"><Border Background="{ThemeResource BleeckerBorderBrush}" /></ControlTemplate></Setter.Value></Setter>
    </Style>
`;
  for (const [kind, type] of [['Item','MenuFlyoutItem'], ['Checkbox','ToggleMenuFlyoutItem'], ['Radio','RadioMenuFlyoutItem'], ['Submenu','MenuFlyoutSubItem']]) {
    const selection = kind === 'Checkbox' || kind === 'Radio', submenu = kind === 'Submenu';
    xaml += `    <Style x:Key="BleeckerMenu${kind}Style" TargetType="${type}">
        <Setter Property="local:BleeckerMenuFocus.IsEnabled" Value="True" />
        <Setter Property="FontFamily" Value="{StaticResource BleeckerPrimaryFontFamily}" /><Setter Property="FontSize" Value="14" />
        <Setter Property="FontWeight" Value="Normal" /><Setter Property="Foreground" Value="{ThemeResource BleeckerTextPrimaryBrush}" />
        <Setter Property="Background" Value="Transparent" /><Setter Property="Padding" Value="8,6" />
        <Setter Property="MinHeight" Value="0" /><Setter Property="UseSystemFocusVisuals" Value="False" />
        <Setter Property="CornerRadius" Value="{StaticResource BleeckerUiCornerRadius}" />
        <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="${type}">
            <Grid x:Name="LayoutRoot" Background="{TemplateBinding Background}" CornerRadius="{TemplateBinding CornerRadius}">
                <VisualStateManager.VisualStateGroups><VisualStateGroup x:Name="CommonStates">
                    <VisualState x:Name="Normal" />
                    ${['PointerOver','Pressed', ...(submenu ? ['SubMenuOpened'] : [])].map(state=>`<VisualState x:Name="${state}"><VisualState.Setters><Setter Target="LayoutRoot.Background" Value="{ThemeResource BleeckerMenuHoverBrush}" /></VisualState.Setters></VisualState>`).join('\n                    ')}
                    <VisualState x:Name="Disabled"><VisualState.Setters><Setter Target="LayoutRoot.Opacity" Value="0.5" /></VisualState.Setters></VisualState>
                </VisualStateGroup>
                <VisualStateGroup x:Name="FocusStates">
                    <VisualState x:Name="Unfocused" /><VisualState x:Name="PointerFocused" />
                    <VisualState x:Name="Focused"><VisualState.Setters><Setter Target="FocusHighlight.Opacity" Value="1" /></VisualState.Setters></VisualState>
                </VisualStateGroup>
                ${selection ? `<VisualStateGroup x:Name="CheckStates"><VisualState x:Name="Unchecked" /><VisualState x:Name="UncheckedWithIcon" />${['Checked','CheckedWithIcon'].map(state=>`<VisualState x:Name="${state}"><VisualState.Setters><Setter Target="CheckGlyph.Opacity" Value="1" /></VisualState.Setters></VisualState>`).join('')}</VisualStateGroup>` : ''}
                </VisualStateManager.VisualStateGroups>
                <Border x:Name="FocusHighlight" Background="{ThemeResource BleeckerMenuBackgroundBrush}" CornerRadius="{TemplateBinding CornerRadius}" Opacity="0" IsHitTestVisible="False"><Border Background="{ThemeResource BleeckerMenuHoverBrush}" CornerRadius="{TemplateBinding CornerRadius}" /></Border>
                <Grid Padding="{TemplateBinding Padding}">
                <Grid.ColumnDefinitions>${selection ? '<ColumnDefinition Width="24" />' : ''}<ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>
                ${selection ? kind === 'Radio' ? '<Viewbox x:Name="CheckGlyph" Width="8" Height="8" Margin="3,0,0,0" Opacity="0" HorizontalAlignment="Left" VerticalAlignment="Center"><Canvas Width="24" Height="24"><Ellipse Canvas.Left="1" Canvas.Top="1" Width="22" Height="22" Fill="{TemplateBinding Foreground}" Stroke="{TemplateBinding Foreground}" StrokeThickness="2" /></Canvas></Viewbox>' : '<Viewbox x:Name="CheckGlyph" Width="16" Height="16" Margin="-1,0,0,0" Opacity="0" HorizontalAlignment="Left" VerticalAlignment="Center"><Canvas Width="24" Height="24"><Path Data="M20 6 9 17 4 12" Stroke="{TemplateBinding Foreground}" StrokeThickness="2" StrokeStartLineCap="Round" StrokeEndLineCap="Round" StrokeLineJoin="Round" /></Canvas></Viewbox>' : ''}
                <TextBlock x:Name="TextBlock" Grid.Column="${selection ? 1 : 0}" Text="{TemplateBinding Text}" Foreground="{TemplateBinding Foreground}" FontFamily="{TemplateBinding FontFamily}" FontSize="{TemplateBinding FontSize}" LineHeight="20" LineStackingStrategy="BlockLineHeight" VerticalAlignment="Center" TextWrapping="Wrap" />
                ${submenu ? '<Viewbox Grid.Column="1" Width="16" Height="16" Margin="8,0,0,0"><Canvas Width="24" Height="24"><Path Data="M9 18l6-6-6-6" Stroke="{TemplateBinding Foreground}" StrokeThickness="2" StrokeStartLineCap="Round" StrokeEndLineCap="Round" StrokeLineJoin="Round" /></Canvas></Viewbox>' : `<TextBlock Grid.Column="${selection ? 2 : 1}" Text="{TemplateBinding KeyboardAcceleratorTextOverride}" Foreground="{ThemeResource BleeckerTextSecondaryBrush}" FontFamily="{StaticResource BleeckerPrimaryFontFamily}" FontSize="12" CharacterSpacing="100" Margin="8,0,0,0" VerticalAlignment="Center" AutomationProperties.AccessibilityView="Raw" />`}
                </Grid>
            </Grid>
        </ControlTemplate></Setter.Value></Setter>
    </Style>
`;
  }
  return xaml + '</ResourceDictionary>\n';
}
