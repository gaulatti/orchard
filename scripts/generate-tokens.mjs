import { createHash } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import * as source from '../tokens/bleecker.source.ts';
import { menuStyles } from './menu-styles.mjs';
import { contextMenuStyles } from './context-menu-styles.mjs';

const root = new URL('../', import.meta.url);
const check = process.argv.includes('--check');
if (process.argv.slice(2).some(arg => arg !== '--check')) {
  throw new Error('Usage: node scripts/generate-tokens.mjs [--check]');
}
const lock = JSON.parse(await readFile(new URL('tokens/source-lock.json', root), 'utf8'));
const snapshot = await readFile(new URL('tokens/bleecker.source.ts', root));
if (createHash('sha256').update(snapshot).digest('hex') !== lock.sha256) {
  throw new Error('Bleecker snapshot differs from source-lock.json. Verify the upstream commit before updating both.');
}
const tokens = Object.fromEntries(['fontFamilies', 'fontWeights', 'spacing', 'radii', 'durations',
  'letterSpacing', 'easing', 'shadows', 'themes'].map(name => [name, source[name]]));
const pascal = value => value[0].toUpperCase() + value.slice(1);
const argb = value => {
  if (/^#[\da-f]{6}$/i.test(value)) return '#FF' + value.slice(1).toUpperCase();
  const rgba = /^rgba\((\d+), (\d+), (\d+), ([\d.]+)\)$/.exec(value);
  if (!rgba) throw new Error(`Unsupported color representation: ${value}`);
  return '#' + [Math.round(Number(rgba[4]) * 255), ...rgba.slice(1, 4).map(Number)]
    .map(number => number.toString(16).padStart(2, '0')).join('').toUpperCase();
};
// Windows accessibility colors are semantic platform mappings, not brand colors.
const highContrastColor = key => {
  if (['primary', 'ring', 'accentBlue', 'sea'].includes(key)) return 'SystemColorHighlightColor';
  if (key === 'primaryForeground') return 'SystemColorHighlightTextColor';
  if (['background', 'card', 'popover', 'secondary', 'muted', 'accent'].includes(key)) return 'SystemColorWindowColor';
  return 'SystemColorWindowTextColor';
};
const buttonTokens = {
  AccentButtonBackground: 'primary', AccentButtonForeground: 'primaryForeground', AccentButtonBorderBrush: 'primary',
  ButtonBackground: 'secondary', ButtonForeground: 'secondaryForeground', ButtonBorderBrush: 'border'
};
let xaml = `<!-- Generated from Bleecker ${lock.commit}; do not edit. -->\n<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n    <ResourceDictionary.ThemeDictionaries>\n`;
for (const [theme, colors] of [['Light', tokens.themes.light], ['Default', tokens.themes.dark], ['HighContrast', tokens.themes.light]]) {
  xaml += `        <ResourceDictionary x:Key="${theme}">\n`;
  for (const [name, color, alpha] of [
    ['Background', theme === 'HighContrast' ? '{ThemeResource SystemColorWindowColor}' : argb(theme === 'Default' ? colors.deepSea : colors.card), 1],
    ['Border', theme === 'HighContrast' ? '{ThemeResource SystemColorWindowTextColor}' : argb(theme === 'Default' ? '#ffffff' : colors.sand), theme === 'Default' ? .15 : .35],
    ['Hover', theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightColor}' : argb(colors.sand), theme === 'Default' ? .15 : .1]])
    xaml += `            <SolidColorBrush x:Key="BleeckerMenu${name}Brush" Color="${color}" Opacity="${theme === 'HighContrast' ? 1 : alpha}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerContextMenuHoverBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightColor}' : theme === 'Default' ? '#FFFFFFFF' : argb(colors.lightSand)}" Opacity="${theme === 'HighContrast' ? 1 : theme === 'Default' ? .07 : 1}" />\n`;
  for (const [name, color, system] of [['Background', theme === 'Default' ? colors.sand : colors.deepSea, 'SystemColorWindowColor'], ['Foreground', theme === 'Default' ? colors.textPrimary : '#ffffff', 'SystemColorWindowTextColor']])
    xaml += `            <SolidColorBrush x:Key="BleeckerTooltip${name}Brush" Color="${theme === 'HighContrast' ? `{ThemeResource ${system}}` : argb(color)}" />\n`;
  // Pinned auth-shell.tsx: theme canvas, white aside ink and clipped white/10 circles.
  for (const [name, color, opacity] of [
    ['Canvas', theme === 'Default' ? colors.background : colors.lightSand, theme === 'Default' ? 1 : .35],
    ['AsideForeground', '#ffffff', 1], ['Circle', '#ffffff', .1]])
    xaml += `            <SolidColorBrush x:Key="BleeckerAuthShell${name}Brush" Color="${theme === 'HighContrast' ? (name === 'Canvas' ? '{ThemeResource SystemColorWindowColor}' : '{ThemeResource SystemColorWindowTextColor}') : argb(color)}" Opacity="${theme === 'HighContrast' ? 1 : opacity}" />\n`;
  // Pinned separator.tsx: bg-black/5 dark:bg-white/10; no interaction styling.
  xaml += `            <SolidColorBrush x:Key="BleeckerNotificationLinkHoverBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightColor}' : argb(theme === 'Default' ? colors.textPrimary : colors.deepSea)}" />\n`;
  // Pinned sonner.tsx: sand/25 and white/10 borders; sand/10 and sand/20 close hover.
  // Pinned hero-carousel.tsx and Tailwind 4.2.2 gray endpoints. These semantic
  // brushes share the existing brand/theme source rather than a second palette.
  for (const [name, light, dark, alpha] of [
    ['Foreground', '#ffffff', colors.textPrimary, 1], ['EmptyForeground', '#ffffff', '#ffffff', 1], ['TitleHover', '#ffffff', '#ffffff', .9],
    ['Description', '#f3f4f6', '#f3f4f6', 1], ['DescriptionHover', '#e5e7eb', '#e5e7eb', 1],
    ['CtaBackground', '#ffffff', colors.sand, theme === 'Default' ? .9 : .92],
    ['CtaHover', '#ffffff', colors.darkSand, theme === 'Default' ? .9 : 1],
    ['CtaForeground', '#101828', colors.textPrimary, 1], ['CtaBorder', '#ffffff', colors.sand, theme === 'Default' ? .35 : .25],
    ['ArrowBackground', '#000000', '#000000', .2], ['ArrowHover', '#000000', '#000000', .3],
    ['ArrowBorder', '#ffffff', '#ffffff', .15], ['ArrowBorderHover', '#ffffff', '#ffffff', .25],
    ['ArrowForeground', '#ffffff', '#ffffff', .8], ['ArrowForegroundHover', '#ffffff', '#ffffff', 1],
    ['TagBackground', '#000000', '#000000', .15], ['TagBorder', '#ffffff', '#ffffff', .15],
    ['Indicator', '#ffffff', '#ffffff', .5], ['IndicatorHover', '#ffffff', '#ffffff', .8], ['IndicatorActive', '#ffffff', '#ffffff', 1],
    ['NavigationBackground', '#000000', '#000000', .2], ['NavigationBorder', '#ffffff', '#ffffff', .1], ['Separator', '#ffffff', '#ffffff', .3]
  ]) {
    const foreground = /Foreground|Title|Description|Indicator|Separator/.test(name);
    xaml += `            <SolidColorBrush x:Key="BleeckerHero${name}Brush" Color="${theme === 'HighContrast' ? `{ThemeResource SystemColor${foreground ? 'WindowText' : 'Window'}Color}` : argb(theme === 'Default' ? dark : light)}" Opacity="${theme === 'HighContrast' ? 1 : alpha}" />\n`;
  }
  xaml += `            <SolidColorBrush x:Key="BleeckerSonnerBorderBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorWindowTextColor}' : argb(theme === 'Default' ? '#ffffff' : colors.sand)}" Opacity="${theme === 'HighContrast' ? 1 : theme === 'Default' ? .1 : .25}" />\n`;
  // Pinned file-input.tsx: outer sand border, native file-button sea/accent ink.
  // Pinned Command Spotlight. Windows hides the whole narrow shortcut hint.
  for (const [name, color, alpha, system] of [
    ['Surface', theme === 'Default' ? colors.deepSea : '#ffffff', 1, 'Window'],
    ['Border', theme === 'Default' ? '#ffffff' : colors.sand, theme === 'Default' ? .12 : .3, 'WindowText'],
    ['HeaderBorder', colors.sand, theme === 'Default' ? .2 : .15, 'WindowText'],
    ['SelectedBackground', theme === 'Default' ? colors.accentBlue : colors.sea, theme === 'Default' ? .15 : .1, 'Highlight'],
    ['SelectedForeground', theme === 'Default' ? colors.accentBlue : colors.sea, 1, 'Highlight'],
    ['RowHover', colors.sand, theme === 'Default' ? .15 : .1, 'Highlight'],
    ['IconBackground', theme === 'Default' ? '#ffffff' : colors.lightSand, theme === 'Default' ? .05 : .5, 'Window'],
    ['IconBorder', theme === 'Default' ? '#ffffff' : colors.sand, theme === 'Default' ? .06 : .15, 'WindowText'],
    ['TriggerBackground', theme === 'Default' ? colors.deepSea : '#ffffff', 1, 'Window'],
    ['TriggerBorder', theme === 'Default' ? '#ffffff' : colors.sand, theme === 'Default' ? .15 : .35, 'WindowText'],
    ['TriggerHover', theme === 'Default' ? '#ffffff' : colors.lightSand, theme === 'Default' ? .06 : .35, 'Highlight'],
    ['TriggerHoverBorder', theme === 'Default' ? '#ffffff' : colors.sand, theme === 'Default' ? .15 : .65, 'WindowText'],
    ['Focus', colors.sea, .25, 'Highlight'], ['Backdrop', '#000000', .45, 'Window']
  ]) xaml += `            <SolidColorBrush x:Key="BleeckerSpotlight${name}Brush" Color="${theme === 'HighContrast' ? `{ThemeResource SystemColor${system}Color}` : argb(color)}" Opacity="${theme === 'HighContrast' ? 1 : alpha}" />\n`;
  // Pinned media-library.tsx. The user requires pointer Delete above Open.
  for (const [name, color, alpha, system] of [
    ['TileBackground', theme === 'Default' ? colors.deepSea : '#ffffff', 1, 'Window'],
    ['TileBorder', theme === 'Default' ? '#ffffff' : colors.sand, theme === 'Default' ? .1 : .3, 'WindowText'],
    ['TileHoverBorder', theme === 'Default' ? '#ffffff' : colors.sand, theme === 'Default' ? .2 : .55, 'WindowText'],
    ['Overlay', colors.deepSea, .82, 'Window'], ['OverlayText', '#ffffff', 1, 'WindowText'],
    ['Placeholder', theme === 'Default' ? '#ffffff' : colors.lightSand, theme === 'Default' ? .04 : .6, 'Window'],
    ['EmptyBackground', theme === 'Default' ? '#ffffff' : colors.lightSand, theme === 'Default' ? .025 : .25, 'Window'],
    ['EmptyBorder', theme === 'Default' ? '#ffffff' : colors.sand, theme === 'Default' ? .1 : .25, 'WindowText'],
    ['PreviewBackground', theme === 'Default' ? '#000000' : colors.lightSand, theme === 'Default' ? .2 : .35, 'Window'],
    ['PreviewBorder', theme === 'Default' ? '#ffffff' : colors.sand, theme === 'Default' ? .07 : .2, 'WindowText'],
    ['Focus', colors.sea, .25, 'Highlight']
  ]) xaml += `            <SolidColorBrush x:Key="BleeckerMedia${name}Brush" Color="${theme === 'HighContrast' ? `{ThemeResource SystemColor${system}Color}` : argb(color)}" Opacity="${theme === 'HighContrast' ? 1 : alpha}" />\n`;
  for (const [name, color, alpha, system] of [
    ['Background', theme === 'Default' ? colors.darkSand : '#ffffff', 1, 'Window'],
    ['Border', colors.sand, theme === 'Default' ? .5 : .3, 'WindowText'],
    ['ChooseBackground', theme === 'Default' ? colors.accentBlue : colors.sea, .1, 'Window'],
    ['ChooseHover', theme === 'Default' ? colors.accentBlue : colors.sea, .2, 'Highlight'],
    ['ChooseForeground', theme === 'Default' ? colors.accentBlue : colors.sea, 1, 'WindowText'],
    ['Focus', theme === 'Default' ? colors.accentBlue : colors.sea, 1, 'Highlight']
  ]) xaml += `            <SolidColorBrush x:Key="BleeckerFile${name}Brush" Color="${theme === 'HighContrast' ? `{ThemeResource SystemColor${system}Color}` : argb(color)}" Opacity="${theme === 'HighContrast' ? 1 : alpha}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerSonnerCloseHoverBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightColor}' : argb(colors.sand)}" Opacity="${theme === 'HighContrast' ? 1 : theme === 'Default' ? .2 : .1}" />\n`;
  for (const [name, color, system] of [['Background', colors.terracotta, 'SystemColorHighlightColor'], ['Foreground', '#ffffff', 'SystemColorHighlightTextColor']])
    xaml += `            <SolidColorBrush x:Key="BleeckerNotificationCount${name}Brush" Color="${theme === 'HighContrast' ? `{ThemeResource ${system}}` : argb(color)}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerSeparatorBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorWindowTextColor}' : theme === 'Default' ? '#FFFFFFFF' : '#FF000000'}" Opacity="${theme === 'HighContrast' ? 1 : theme === 'Default' ? .1 : .05}" />\n`;
  // Pinned skeleton.tsx: sand/20, dark:white/10; SkeletonCard uses the border ring.
  xaml += `            <SolidColorBrush x:Key="BleeckerSkeletonBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorWindowTextColor}' : theme === 'Default' ? '#FFFFFFFF' : argb(colors.sand)}" Opacity="${theme === 'HighContrast' ? 1 : theme === 'Default' ? .1 : .2}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerSkeletonCardBorderBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorWindowTextColor}' : theme === 'Default' ? '#FFFFFFFF' : '#FF000000'}" Opacity="${theme === 'HighContrast' ? 1 : .08}" />\n`;
  // Pinned avatar.tsx: circular lightSand/deepSea surface and distinct group rings.
  xaml += `            <SolidColorBrush x:Key="BleeckerBreadcrumbHoverBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightColor}' : argb(colors.sand)}" Opacity="${theme === 'HighContrast' ? 1 : theme === 'Default' ? .2 : .1}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerBreadcrumbHoverForegroundBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightTextColor}' : argb(colors.textPrimary)}" />\n`;
  for (const [name, value, system] of [['AvatarBackground', theme === 'Default' ? colors.deepSea : colors.lightSand, 'SystemColorWindowColor'], ['AvatarForeground', theme === 'Default' ? colors.textPrimary : colors.textSecondary, 'SystemColorWindowTextColor'], ['AvatarGroupRing', theme === 'Default' ? colors.deepSea : '#ffffff', 'SystemColorWindowTextColor']])
    xaml += `            <SolidColorBrush x:Key="Bleecker${name}Brush" Color="${theme === 'HighContrast' ? `{ThemeResource ${system}}` : argb(value)}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerAvatarRingBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorWindowTextColor}' : theme === 'Default' ? '#FFFFFFFF' : '#FF000000'}" Opacity="${theme === 'HighContrast' ? 1 : .1}" />\n`;
  // Pinned alert.tsx: independent portals, type borders/icons and close wash.
  // Pinned activity-item.tsx: sand/20 border and lightSand/35 hover; white/7 and white/3 in Dark.
  xaml += `            <SolidColorBrush x:Key="BleeckerActivityBorderBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorWindowTextColor}' : theme === 'Default' ? '#FFFFFFFF' : argb(colors.sand)}" Opacity="${theme === 'HighContrast' ? 1 : theme === 'Default' ? .07 : .2}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerActivityHoverBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightColor}' : theme === 'Default' ? '#FFFFFFFF' : argb(colors.lightSand)}" Opacity="${theme === 'HighContrast' ? 1 : theme === 'Default' ? .03 : .35}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerActivityHoverForegroundBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightTextColor}' : argb(colors.textPrimary)}" />\n`;
  // Frozen attention-surface.tsx: sea/30 focus ring in either appearance.
  xaml += `            <SolidColorBrush x:Key="BleeckerAttentionFocusBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightColor}' : argb(colors.sea)}" Opacity="${theme === 'HighContrast' ? 1 : .3}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerBrandSeparatorBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorWindowTextColor}' : theme === 'Default' ? '#FFFFFFFF' : argb(colors.sand)}" Opacity="${theme === 'HighContrast' ? 1 : theme === 'Default' ? .18 : .55}" />\n`;
  const alertDark = theme === 'Default', alertContrast = theme === 'HighContrast';
  xaml += `            <SolidColorBrush x:Key="BleeckerAlertBackgroundBrush" Color="${alertContrast ? '{ThemeResource SystemColorWindowColor}' : argb(alertDark ? colors.deepSea : '#ffffff')}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerAlertCloseHoverBrush" Color="${alertContrast ? '{ThemeResource SystemColorHighlightColor}' : alertDark ? '#FFFFFFFF' : '#FF000000'}" Opacity="${alertContrast ? 1 : alertDark ? .1 : .05}" />\n`;
  xaml += `            <SolidColorBrush x:Key="BleeckerAlertCloseHoverTextBrush" Color="${alertContrast ? '{ThemeResource SystemColorHighlightTextColor}' : argb(colors.textPrimary)}" />\n`;
  for (const [name, border, opacity, icon] of [
    ['Success', alertDark ? 'accentBlue' : 'sea', .25, alertDark ? 'accentBlue' : 'sea'],
    ['Error', 'terracotta', alertDark ? .3 : .25, 'terracotta'],
    ['Warning', 'sunset', alertDark ? .2 : .3, alertDark ? 'accentGold' : 'sunset'],
    ['Info', 'desert', .25, alertDark ? 'accentGold' : 'desert']]) {
    xaml += `            <SolidColorBrush x:Key="BleeckerAlert${name}BorderBrush" Color="${alertContrast ? '{ThemeResource SystemColorWindowTextColor}' : argb(colors[border])}" Opacity="${alertContrast ? 1 : opacity}" />\n`;
    xaml += `            <SolidColorBrush x:Key="BleeckerAlert${name}IconBrush" Color="${alertContrast ? '{ThemeResource SystemColorWindowTextColor}' : argb(colors[icon])}" />\n`;
  }
  for (const [key, value] of Object.entries(colors)) {
    const color = theme === 'HighContrast' ? `{ThemeResource ${highContrastColor(key)}}` : argb(value);
    xaml += `            <SolidColorBrush x:Key="Bleecker${pascal(key)}Brush" Color="${color}" />\n`;
  }
  for (const [name, alpha] of [['BleeckerButtonFocusRingBrush', theme === 'Default' ? 0.45 : 0.35], ['BleeckerToggleFocusRingBrush', 1]]) {
    const color = theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightColor}' : argb(colors[theme === 'Default' ? 'accentBlue' : 'sea']);
    xaml += `            <SolidColorBrush x:Key="${name}" Color="${color}" Opacity="${theme === 'HighContrast' ? 1 : alpha}" />\n`;
  }
  const offsetColor = theme === 'HighContrast' ? '{ThemeResource SystemColorWindowColor}' : argb(theme === 'Default' ? colors.deepSea : '#ffffff');
  xaml += `            <SolidColorBrush x:Key="BleeckerFocusOffsetBrush" Color="${offsetColor}" />\n`;
  for (const [name, key] of Object.entries(buttonTokens)) {
    for (const state of ['', 'PointerOver', 'Pressed', 'Disabled']) {
      // Retain WinUI's state machine. Opacity gives visible feedback without inventing new palette entries.
      const opacity = state === 'Disabled' ? '0.4' : state === 'Pressed' ? '0.8' : state === 'PointerOver' ? '0.9' : '1';
      const disabledColor = name.endsWith('Background') ? 'SystemColorWindowColor' : 'SystemColorGrayTextColor';
      const color = theme === 'HighContrast'
        ? `{ThemeResource ${state === 'Disabled' ? disabledColor : highContrastColor(key)}}` : argb(colors[key]);
      xaml += `            <SolidColorBrush x:Key="${name}${state}" Color="${color}" Opacity="${theme === 'HighContrast' ? '1' : opacity}" />\n`;
    }
  }
  // Native ToggleButton owns input, focus and automation. Cover all of its
  // template states, including the optional Windows indeterminate state.
  for (const selection of ['', 'Checked', 'Indeterminate']) {
    for (const interaction of ['', 'PointerOver', 'Pressed', 'Disabled']) {
      const selected = selection !== '';
      const disabled = interaction === 'Disabled';
      for (const property of ['Background', 'Foreground', 'BorderBrush']) {
        const key = selected ? 'primary' : property === 'Foreground'
          ? interaction === 'PointerOver' || interaction === 'Pressed' ? 'textPrimary' : 'textSecondary'
          : 'sand';
        let opacity = property === 'Background'
          ? selected ? (theme === 'Default' ? 0.15 : 0.1) : interaction === '' || disabled ? 0 : theme === 'Default' ? 0.15 : 0.1
          : property === 'BorderBrush' && !selected ? theme === 'Default' ? 0.4 : 0.3 : 1;
        if (disabled) opacity *= 0.5;
        const systemColor = property === 'Background'
          ? selected ? 'SystemColorHighlightColor' : 'SystemColorWindowColor'
          : disabled ? 'SystemColorGrayTextColor' : selected ? 'SystemColorHighlightTextColor' : 'SystemColorWindowTextColor';
        const color = theme === 'HighContrast' ? `{ThemeResource ${systemColor}}` : argb(colors[key]);
        xaml += `            <SolidColorBrush x:Key="ToggleButton${property}${selection}${interaction}" Color="${color}" Opacity="${theme === 'HighContrast' ? 1 : opacity}" />\n`;
      }
    }
  }
  for (const property of ['Background', 'Border']) {
    const dark = theme === 'Default';
    const key = dark ? 'textPrimary' : property === 'Background' ? 'lightSand' : 'sand';
    const color = theme === 'HighContrast' ? `{ThemeResource ${property === 'Background' ? 'SystemColorWindowColor' : 'SystemColorWindowTextColor'}}` : argb(dark ? '#ffffff' : colors[key]);
    const opacity = theme === 'HighContrast' ? 1 : dark ? property === 'Background' ? 0.05 : 0.07 : property === 'Background' ? 0.65 : 0.2;
    xaml += `            <SolidColorBrush x:Key="BleeckerToggleGroup${property}Brush" Color="${color}" Opacity="${opacity}" />\n`;
  }
  xaml += '        </ResourceDictionary>\n';
}
xaml += '    </ResourceDictionary.ThemeDictionaries>\n';
for (const [role, family] of Object.entries(tokens.fontFamilies)) {
  const filename = family.replaceAll(' ', '') + '-Regular.ttf';
  xaml += `    <FontFamily x:Key="Bleecker${pascal(role)}FontFamily">ms-appx:///Orchard/Assets/Fonts/${filename}#${family}</FontFamily>\n`;
  const semibold = family.replaceAll(' ', '') + '-SemiBold.ttf';
  xaml += `    <FontFamily x:Key="Bleecker${pascal(role)}SemiboldFontFamily">ms-appx:///Orchard/Assets/Fonts/${semibold}#${family}</FontFamily>\n`;
}
for (const [key, value] of Object.entries(tokens.spacing)) xaml += `    <x:Double x:Key="BleeckerSpacing${pascal(key)}">${value}</x:Double>\n`;
xaml += '    <FontFamily x:Key="BleeckerPrimaryMediumFontFamily">ms-appx:///Orchard/Assets/Fonts/EncodeSans-Medium.ttf#Encode Sans</FontFamily>\n';
xaml += '    <FontFamily x:Key="BleeckerPrimaryBoldFontFamily">ms-appx:///Orchard/Assets/Fonts/EncodeSans-Bold.ttf#Encode Sans</FontFamily>\n';
for (const [key, value] of Object.entries(tokens.radii)) xaml += `    <CornerRadius x:Key="Bleecker${pascal(key)}CornerRadius">${value}</CornerRadius>\n`;
xaml += '    <x:Double x:Key="BleeckerIconBadgeMediumExtent">56</x:Double>\n';
xaml += '    <x:Double x:Key="BleeckerIconBadgeLargeExtent">64</x:Double>\n';
xaml += '    <CornerRadius x:Key="BleeckerIconBadgeCornerRadius">10</CornerRadius>\n';
xaml += '</ResourceDictionary>\n';

const dictionary = (name, entries, type, format) => `    public static FrozenDictionary<string, ${type}> ${name} { get; } =\n        new Dictionary<string, ${type}>(StringComparer.Ordinal)\n        {\n${Object.entries(entries).map(([key, value]) => `            ["${key}"] = ${format(value)},`).join('\n')}\n        }.ToFrozenDictionary(StringComparer.Ordinal);\n`;
let cs = `// Generated from Bleecker ${lock.commit}; do not edit.\nusing System.Collections.Frozen;\n\nnamespace Orchard;\n\npublic enum BleeckerColorScheme { Light, Dark }\n\npublic static class BleeckerTokens\n{\n`;
for (const name of ['fontFamilies', 'fontWeights']) cs += dictionary(pascal(name), tokens[name], 'string', JSON.stringify);
for (const name of ['spacing', 'radii', 'durations', 'letterSpacing']) cs += dictionary(pascal(name), tokens[name], 'double', value => value.toString());
cs += dictionary('LightColors', tokens.themes.light, 'string', value => JSON.stringify(argb(value)));
cs += dictionary('DarkColors', tokens.themes.dark, 'string', value => JSON.stringify(argb(value)));
for (const [shadowName, recipe] of [['surface', tokens.shadows.surface], ['raised', tokens.shadows.raised], ['overlay', tokens.shadows.overlay]]) {
const shadowLayers = [...recipe.matchAll(/(-?\d+) (-?\d+)px (\d+)px(?: (-?\d+)px)? rgb\((\d+) (\d+) (\d+) \/ ([.\d]+)\)/g)];
if (shadowLayers.length !== 2) throw new Error('Shadow recipes must contain two recognized layers.');
cs += `    internal static IReadOnlyList<(float X, float Y, float Blur, float Spread, float Opacity, byte R, byte G, byte B)> ${pascal(shadowName)}Shadows { get; } =\n        Array.AsReadOnly(new (float, float, float, float, float, byte, byte, byte)[]\n        {\n${shadowLayers.map(m => `            (${m[1]}f, ${m[2]}f, ${m[3]}f, ${m[4] ?? 0}f, ${m[8]}f, ${m[5]}, ${m[6]}, ${m[7]}),`).join('\n')}\n        });\n`;
}
cs += `    public static FrozenDictionary<string, string> Colors(BleeckerColorScheme scheme) => scheme switch\n    {\n        BleeckerColorScheme.Light => LightColors,\n        BleeckerColorScheme.Dark => DarkColors,\n        _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, "Unknown color scheme.")\n    };\n}\n`;
const outputs = { 'tokens/bleecker.tokens.json': JSON.stringify(tokens, null, 2) + '\n',
  'src/Orchard/Themes/Tokens.g.xaml': xaml, 'src/Orchard/Tokens/BleeckerTokens.g.cs': cs };
// Button recipes follow the fixed component inventory at a86059013cd59684ce452849278ea81e734f2066.
// Keep native Button templates; each instance imports only its selected palette.
for (const variant of ['Primary', 'Secondary', 'Outline', 'Subtle', 'Ghost', 'Link', 'Destructive', 'IconDefault', 'IconSubtle', 'IconGhost']) {
  let button = `<!-- Generated Bleecker ${variant} native button palette; do not edit. -->\n<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n    <ResourceDictionary.ThemeDictionaries>\n`;
  for (const [theme, colors] of [['Light', tokens.themes.light], ['Default', tokens.themes.dark], ['HighContrast', tokens.themes.light]]) {
    const dark = theme === 'Default';
    button += `        <ResourceDictionary x:Key="${theme}">\n`;
    const focusAlpha = variant.startsWith('Icon') ? dark ? 0.4 : 0.3 : dark ? 0.45 : 0.35;
    const focusColor = theme === 'HighContrast' ? '{ThemeResource SystemColorHighlightColor}' : argb(colors[dark ? 'accentBlue' : 'sea']);
    button += `            <SolidColorBrush x:Key="ButtonFocusRingBrush" Color="${focusColor}" Opacity="${theme === 'HighContrast' ? 1 : focusAlpha}" />\n`;
    for (const state of ['', 'PointerOver', 'Pressed', 'Disabled']) {
      const hover = state === 'PointerOver' || state === 'Pressed';
      let background, foreground, border;
      switch (variant) {
        case 'Primary': background = [hover && !dark ? 'deepSea' : 'primary', hover && dark ? 0.9 : 1]; foreground = [dark ? 'deepSea' : '#ffffff', 1]; border = ['primary', 1]; if (hover && !dark) border = ['deepSea', 1]; break;
        case 'Secondary': background = [dark ? hover ? '#ffffff' : 'deepSea' : hover ? 'lightSand' : '#ffffff', hover ? dark ? 0.06 : 0.35 : 1]; foreground = ['textPrimary', 1]; border = [dark ? '#ffffff' : 'sand', dark ? 0.18 : hover ? 0.75 : 0.45]; break;
        case 'Outline': background = ['primary', hover ? dark ? 0.07 : 0.045 : 0]; foreground = ['primary', 1]; border = ['primary', hover ? 0.7 : 0.45]; break;
        case 'Subtle': background = [dark ? '#ffffff' : hover ? 'sand' : 'lightSand', dark ? hover ? 0.11 : 0.07 : hover ? 0.25 : 0.75]; foreground = [dark ? 'textPrimary' : 'deepSea', 1]; border = ['primary', 0]; break;
        case 'Ghost': background = [dark ? '#ffffff' : 'sand', hover ? dark ? 0.06 : 0.12 : 0]; foreground = ['textPrimary', 1]; border = ['primary', 0]; break;
        case 'Link': background = ['primary', 0]; foreground = [hover ? dark ? 'lightSand' : 'deepSea' : 'primary', 1]; border = ['primary', 0]; break;
        case 'Destructive': background = ['terracotta', hover ? 0.9 : 1]; foreground = ['#ffffff', 1]; border = background; break;
        case 'IconDefault': background = [dark ? hover ? '#ffffff' : 'deepSea' : hover ? 'lightSand' : '#ffffff', hover ? dark ? 0.06 : 0.45 : 1]; foreground = ['textPrimary', 1]; border = [dark ? '#ffffff' : 'sand', dark ? 0.15 : hover ? 0.65 : 0.35]; break;
        case 'IconSubtle': background = [dark ? '#ffffff' : hover ? 'sand' : 'lightSand', dark ? hover ? 0.11 : 0.07 : hover ? 0.25 : 0.75]; foreground = ['textPrimary', 1]; border = ['primary', 0]; break;
        case 'IconGhost': background = [dark ? '#ffffff' : 'sand', hover ? dark ? 0.06 : 0.12 : 0]; foreground = ['textPrimary', 1]; border = ['primary', 0]; break;
      }
      for (const [property, [key, alpha]] of Object.entries({ Background: background, Foreground: foreground, BorderBrush: border })) {
        const filled = variant === 'Primary' || variant === 'Destructive';
        const system = property === 'Background' ? filled ? 'SystemColorHighlightColor' : 'SystemColorWindowColor'
          : state === 'Disabled' ? 'SystemColorGrayTextColor' : filled ? 'SystemColorHighlightTextColor' : 'SystemColorWindowTextColor';
        const color = theme === 'HighContrast' ? `{ThemeResource ${system}}` : argb(key.startsWith('#') ? key : colors[key]);
        const opacity = theme === 'HighContrast' ? 1 : alpha * (state === 'Disabled' ? 0.45 : 1);
        // Native brush-opacity paints light Ghost sand like alpha 30/255 here.
        // The frozen browser and the actual native Reset paint match at 31/255.
        // Keep the dark recipe and all other palettes on their measured paths.
        const byteAlpha = theme === 'Light' && (variant === 'Ghost' || variant === 'IconGhost') && property === 'Background' && hover;
        const brushColor = byteAlpha ? '#' + Math.round(opacity * 255).toString(16).padStart(2, '0').toUpperCase() + color.slice(3) : color;
        button += `            <SolidColorBrush x:Key="Button${property}${state}" Color="${brushColor}" Opacity="${byteAlpha ? 1 : opacity}" />\n`;
      }
    }
    button += '        </ResourceDictionary>\n';
  }
  button += `    </ResourceDictionary.ThemeDictionaries>
    <Style TargetType="Button" BasedOn="{StaticResource DefaultButtonStyle}">
        <Setter Property="HighContrastAdjustment" Value="None" />
        <Setter Property="Background" Value="{ThemeResource ButtonBackground}" />
        <Setter Property="Foreground" Value="{ThemeResource ButtonForeground}" />
        <Setter Property="BorderBrush" Value="{ThemeResource ButtonBorderBrush}" />
        <Setter Property="FontFamily" Value="{StaticResource BleeckerPrimaryMediumFontFamily}" />
        <Setter Property="FontWeight" Value="Medium" />
        <Setter Property="UseSystemFocusVisuals" Value="True" />
        <Setter Property="FocusVisualPrimaryBrush" Value="{ThemeResource ButtonFocusRingBrush}" />
        <Setter Property="FocusVisualSecondaryBrush" Value="{ThemeResource BleeckerFocusOffsetBrush}" />
        <Setter Property="FocusVisualPrimaryThickness" Value="2" />
        <Setter Property="FocusVisualSecondaryThickness" Value="2" />
        <Setter Property="FocusVisualMargin" Value="-4" />
        <Setter Property="CornerRadius" Value="{StaticResource BleeckerButtonCornerRadius}" />
        <Setter Property="HorizontalAlignment" Value="${variant.startsWith('Icon') ? 'Left' : 'Stretch'}" />
        <Setter Property="HorizontalContentAlignment" Value="Center" />
        <Setter Property="VerticalContentAlignment" Value="Center" />
    </Style>
</ResourceDictionary>\n`;
  outputs[`src/Orchard/Themes/Buttons/${variant}.g.xaml`] = button;
}
for (const variant of ['Surface', 'Outlined', 'Elevated', 'Subtle', 'Transparent']) {
  let card = `<!-- Generated from the pinned Bleecker card recipe; do not edit. -->\n<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="using:Orchard">\n    <ResourceDictionary.ThemeDictionaries>\n`;
  for (const [theme, colors] of [['Light', tokens.themes.light], ['Default', tokens.themes.dark], ['HighContrast', tokens.themes.light]]) {
    const dark = theme === 'Default', contrast = theme === 'HighContrast';
    const subtle = variant === 'Subtle', transparent = variant === 'Transparent';
    const borderAlpha = dark ? { Surface: .09, Outlined: .15, Elevated: .1, Subtle: .07, Transparent: 0 }[variant]
      : { Surface: .3, Outlined: .4, Elevated: .25, Subtle: .2, Transparent: 0 }[variant];
    card += `        <ResourceDictionary x:Key="${theme}">\n`;
    for (const [name, color, opacity] of [
      ['Background', contrast ? '{ThemeResource SystemColorWindowColor}' : argb(dark ? subtle ? '#ffffff' : colors.deepSea : subtle ? colors.lightSand : '#ffffff'), contrast ? 1 : transparent ? 0 : subtle ? dark ? .035 : .45 : 1],
      ['BorderBrush', contrast ? '{ThemeResource SystemColorWindowTextColor}' : argb(dark ? '#ffffff' : colors.sand), contrast ? 1 : borderAlpha]])
      card += `            <SolidColorBrush x:Key="Card${name}" Color="${color}" Opacity="${opacity}" />\n`;
    card += `        </ResourceDictionary>\n`;
  }
  card += `    </ResourceDictionary.ThemeDictionaries>\n</ResourceDictionary>\n`;
  outputs[`src/Orchard/Themes/Cards/${variant}.g.xaml`] = card;
}
for (const variant of ['Primary', 'Subtle', 'Outlined']) {
  let badge = `<!-- Generated from pinned icon-badge.tsx; do not edit. -->\n<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n    <ResourceDictionary.ThemeDictionaries>\n`;
  for (const [theme, colors] of [['Light', tokens.themes.light], ['Default', tokens.themes.dark], ['HighContrast', tokens.themes.light]]) {
    const dark = theme === 'Default', contrast = theme === 'HighContrast';
    const primary = variant === 'Primary', subtle = variant === 'Subtle';
    const tint = dark ? colors.accentBlue : colors.sea;
    for (const [name, color, opacity] of [
      ['Background', primary ? dark ? colors.accentBlue : colors.deepSea : subtle ? tint : dark ? colors.deepSea : '#ffffff', subtle ? dark ? .1 : .06 : 1],
      ['BorderBrush', primary ? dark ? colors.accentBlue : colors.deepSea : subtle ? tint : dark ? '#ffffff' : colors.sand, primary ? 1 : subtle ? dark ? .15 : .1 : dark ? .15 : .4],
      ['Foreground', primary ? dark ? colors.deepSea : '#ffffff' : subtle ? tint : colors.textPrimary, 1]]) {
      if (name === 'Background') badge += `        <ResourceDictionary x:Key="${theme}">\n`;
      badge += `            <SolidColorBrush x:Key="IconBadge${name}" Color="${contrast ? `{ThemeResource SystemColor${name === 'Background' ? 'Window' : 'WindowText'}Color}` : argb(color)}" Opacity="${contrast ? 1 : opacity}" />\n`;
    }
    badge += '        </ResourceDictionary>\n';
  }
  outputs[`src/Orchard/Themes/IconBadges/${variant}.g.xaml`] = badge + '    </ResourceDictionary.ThemeDictionaries>\n</ResourceDictionary>\n';
}
let emptyPalette = `<!-- Generated from pinned empty.tsx; do not edit. -->\n<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n    <ResourceDictionary.ThemeDictionaries>\n`;
for (const [theme, colors] of [['Light', tokens.themes.light], ['Default', tokens.themes.dark], ['HighContrast', tokens.themes.light]]) {
  const dark = theme === 'Default', contrast = theme === 'HighContrast';
  emptyPalette += `        <ResourceDictionary x:Key="${theme}">\n`;
  for (const [name,color,opacity] of [['Background',dark?'#ffffff':colors.lightSand,dark?.025:.25],['BorderBrush',dark?'#ffffff':colors.sand,dark?.1:.3],['IconBackground',dark?colors.deepSea:'#ffffff',1],['IconBorderBrush',dark?'#ffffff':colors.sand,dark?.1:.25],['IconForeground',dark?colors.textPrimary:colors.textSecondary,1],['DefaultIconForeground',dark?colors.accentBlue:colors.sea,1],['DescriptionForeground',colors.textSecondary,1]])
    emptyPalette += `            <SolidColorBrush x:Key="Empty${name}" Color="${contrast?`{ThemeResource SystemColor${name.endsWith('Background')?'Window':'WindowText'}Color}`:argb(color)}" Opacity="${contrast?1:opacity}" />\n`;
  emptyPalette += '        </ResourceDictionary>\n';
}
outputs['src/Orchard/Themes/EmptyPalette.g.xaml'] = emptyPalette + '    </ResourceDictionary.ThemeDictionaries>\n</ResourceDictionary>\n';
let progressStackPalette = `<!-- Generated from pinned progress-stack.tsx; do not edit. -->\n<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n    <ResourceDictionary.ThemeDictionaries>\n`;
for (const [theme,colors] of [['Light',tokens.themes.light],['Default',tokens.themes.dark],['HighContrast',tokens.themes.light]]) {
  // The source's .2 alpha is exactly 51/255. Brush.Opacity premultiplies
  // opaque channels by truncation; color alpha retains the browser's rounding.
  progressStackPalette += `        <ResourceDictionary x:Key="${theme}">\n            <SolidColorBrush x:Key="ProgressStackTrack" Color="${theme==='HighContrast'?'{ThemeResource SystemColorWindowTextColor}':'#33'+argb(colors.sand).slice(3)}" Opacity="1" />\n        </ResourceDictionary>\n`;
}
outputs['src/Orchard/Themes/ProgressStackPalette.g.xaml'] = progressStackPalette + '    </ResourceDictionary.ThemeDictionaries>\n</ResourceDictionary>\n';
// Tailwind 4.2.2 theme.css, pinned by the Bleecker component reference.
// OKLCH -> linear sRGB -> encoded 8-bit sRGB for WinUI Color brushes.
const emerald = (lightness, chroma, hue) => {
  const a = chroma * Math.cos(hue * Math.PI / 180), b = chroma * Math.sin(hue * Math.PI / 180);
  const l = (lightness + .3963377774 * a + .2158037573 * b) ** 3;
  const m = (lightness - .1055613458 * a - .0638541728 * b) ** 3;
  const s = (lightness - .0894841775 * a - 1.291485548 * b) ** 3;
  return '#' + [4.0767416621*l-3.3077115913*m+.2309699292*s, -1.2684380046*l+2.6097574011*m-.3413193965*s, -.0041960863*l-.7034186147*m+1.707614701*s]
    .map(v => Math.round(Math.max(0, Math.min(1, v <= .0031308 ? 12.92*v : 1.055*v**(1/2.4)-.055))*255).toString(16).padStart(2,'0')).join('');
};
const emeraldColors = { 300: emerald(.845,.143,164.978), 400: emerald(.765,.177,163.223), 500: emerald(.696,.17,162.48), 600: emerald(.596,.145,163.225), 700: emerald(.508,.118,165.612) };
for (const variant of ['Default', 'Live', 'Offline', 'Warning', 'Info']) {
  let badge = `<!-- Generated from pinned status-badge.tsx and Tailwind 4.2.2; do not edit. -->\n<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n    <ResourceDictionary.ThemeDictionaries>\n`;
  for (const [theme, colors] of [['Light', tokens.themes.light], ['Default', tokens.themes.dark], ['HighContrast', tokens.themes.light]]) {
    const dark = theme === 'Default', contrast = theme === 'HighContrast';
    const tint = variant === 'Live' ? emeraldColors[dark ? 400 : 600] : variant === 'Offline' ? colors.terracotta : variant === 'Warning' ? colors.desert : variant === 'Info' ? dark ? colors.accentBlue : colors.sea : colors.sand;
    const text = variant === 'Live' ? emeraldColors[dark ? 300 : 700] : variant === 'Offline' ? dark ? colors.terracotta : '#874641' : variant === 'Warning' ? colors.accentText : variant === 'Info' ? tint : colors.textSecondary;
    const dot = variant === 'Live' ? emeraldColors[500] : variant === 'Default' ? colors.textSecondary : tint;
    badge += `        <ResourceDictionary x:Key="${theme}">\n`;
    for (const [name, color, opacity] of [['Background', tint, variant === 'Default' && !dark ? .2 : variant === 'Live' && !dark ? .08 : .1], ['Border', tint, variant === 'Default' && !dark ? .3 : .2], ['Foreground', text, 1], ['Dot', dot, 1]])
      badge += `            <SolidColorBrush x:Key="StatusBadge${name}" Color="${contrast ? `{ThemeResource SystemColor${name === 'Background' ? 'Window' : 'WindowText'}Color}` : argb(color)}" Opacity="${contrast ? 1 : opacity}" />\n`;
    badge += '        </ResourceDictionary>\n';
  }
  outputs[`src/Orchard/Themes/StatusBadges/${variant}.g.xaml`] = badge + '    </ResourceDictionary.ThemeDictionaries>\n</ResourceDictionary>\n';
}
outputs['src/Orchard/Themes/Menus.g.xaml'] = menuStyles();
outputs['src/Orchard/Themes/ContextMenus.g.xaml'] = contextMenuStyles();
let checkbox = `<!-- Generated from the pinned checkbox.tsx contract and Bleecker token snapshot ${lock.commit}; do not edit. -->\n<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n    <ResourceDictionary.ThemeDictionaries>\n`;
for (const [theme, colors] of [['Light', tokens.themes.light], ['Default', tokens.themes.dark], ['HighContrast', tokens.themes.light]]) {
  const dark = theme === 'Default', contrast = theme === 'HighContrast';
  checkbox += `        <ResourceDictionary x:Key="${theme}">\n`;
  for (const [name, color, opacity, system] of [
    ['OffFill', dark ? colors.deepSea : '#ffffff', 1, 'Window'],
    ['OnFill', dark ? colors.accentBlue : colors.sea, 1, 'Highlight'],
    ['OffStroke', dark ? '#ffffff' : colors.sand, dark ? .2 : .6, 'WindowText'],
    ['HoverStroke', colors.sea, .5, 'Highlight'],
    ['OnStroke', dark ? colors.accentBlue : colors.sea, 1, 'Highlight'],
    ['FocusStroke', colors.sea, .2, 'WindowText'],
    ['Glyph', '#ffffff', 1, 'HighlightText']])
    checkbox += `            <SolidColorBrush x:Key="Checkbox${name}" Color="${contrast ? `{ThemeResource SystemColor${system}Color}` : argb(color)}" Opacity="${contrast ? 1 : opacity}" />\n`;
  checkbox += '        </ResourceDictionary>\n';
}
outputs['src/Orchard/Themes/Checkbox.xaml'] = checkbox + `    </ResourceDictionary.ThemeDictionaries>
    <Style TargetType="TextBlock">
        <Setter Property="LineHeight" Value="20" />
        <Setter Property="LineStackingStrategy" Value="BlockLineHeight" />
    </Style>
</ResourceDictionary>\n`;
for (const [path, content] of Object.entries(outputs)) {
  const url = new URL(path, root);
  if (check) {
    if (await readFile(url, 'utf8') !== content) throw new Error(`Generated tokens are stale: ${path}`);
  } else {
    await mkdir(fileURLToPath(new URL('./', url)), { recursive: true });
    await writeFile(url, content);
  }
}
console.log(check ? 'Bleecker token outputs match the pinned source.' : 'Generated Orchard token resources.');
