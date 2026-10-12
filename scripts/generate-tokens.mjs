import { createHash } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import * as source from '../tokens/bleecker.source.ts';

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
  // Pinned separator.tsx: bg-black/5 dark:bg-white/10; no interaction styling.
  xaml += `            <SolidColorBrush x:Key="BleeckerSeparatorBrush" Color="${theme === 'HighContrast' ? '{ThemeResource SystemColorWindowTextColor}' : theme === 'Default' ? '#FFFFFFFF' : '#FF000000'}" Opacity="${theme === 'HighContrast' ? 1 : theme === 'Default' ? .1 : .05}" />\n`;
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
for (const [key, value] of Object.entries(tokens.radii)) xaml += `    <CornerRadius x:Key="Bleecker${pascal(key)}CornerRadius">${value}</CornerRadius>\n`;
xaml += '</ResourceDictionary>\n';

const dictionary = (name, entries, type, format) => `    public static FrozenDictionary<string, ${type}> ${name} { get; } =\n        new Dictionary<string, ${type}>(StringComparer.Ordinal)\n        {\n${Object.entries(entries).map(([key, value]) => `            ["${key}"] = ${format(value)},`).join('\n')}\n        }.ToFrozenDictionary(StringComparer.Ordinal);\n`;
let cs = `// Generated from Bleecker ${lock.commit}; do not edit.\nusing System.Collections.Frozen;\n\nnamespace Orchard;\n\npublic enum BleeckerColorScheme { Light, Dark }\n\npublic static class BleeckerTokens\n{\n`;
for (const name of ['fontFamilies', 'fontWeights']) cs += dictionary(pascal(name), tokens[name], 'string', JSON.stringify);
for (const name of ['spacing', 'radii', 'durations', 'letterSpacing']) cs += dictionary(pascal(name), tokens[name], 'double', value => value.toString());
cs += dictionary('LightColors', tokens.themes.light, 'string', value => JSON.stringify(argb(value)));
cs += dictionary('DarkColors', tokens.themes.dark, 'string', value => JSON.stringify(argb(value)));
const raisedLayers = [...tokens.shadows.raised.matchAll(/(-?\d+) (-?\d+)px (\d+)px(?: (-?\d+)px)? rgb\((\d+) (\d+) (\d+) \/ ([.\d]+)\)/g)];
if (raisedLayers.length !== 2) throw new Error('The raised shadow recipe must contain two recognized layers.');
cs += `    internal static IReadOnlyList<(float X, float Y, float Blur, float Spread, float Opacity, byte R, byte G, byte B)> RaisedShadows { get; } =\n        Array.AsReadOnly(new (float, float, float, float, float, byte, byte, byte)[]\n        {\n${raisedLayers.map(m => `            (${m[1]}f, ${m[2]}f, ${m[3]}f, ${m[4] ?? 0}f, ${m[8]}f, ${m[5]}, ${m[6]}, ${m[7]}),`).join('\n')}\n        });\n`;
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
        button += `            <SolidColorBrush x:Key="Button${property}${state}" Color="${color}" Opacity="${opacity}" />\n`;
      }
    }
    button += '        </ResourceDictionary>\n';
  }
  button += `    </ResourceDictionary.ThemeDictionaries>
    <Style TargetType="Button" BasedOn="{StaticResource DefaultButtonStyle}">
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
