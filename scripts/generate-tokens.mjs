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
  for (const [key, value] of Object.entries(colors)) {
    const color = theme === 'HighContrast' ? `{ThemeResource ${highContrastColor(key)}}` : argb(value);
    xaml += `            <SolidColorBrush x:Key="Bleecker${pascal(key)}Brush" Color="${color}" />\n`;
  }
  for (const [name, key] of Object.entries(buttonTokens)) {
    for (const state of ['', 'PointerOver', 'Pressed', 'Disabled']) {
      // Retain WinUI's state machine. Opacity gives visible feedback without inventing new palette entries.
      const opacity = state === 'Disabled' ? '0.4' : state === 'Pressed' ? '0.8' : state === 'PointerOver' ? '0.9' : '1';
      const color = theme === 'HighContrast'
        ? `{ThemeResource ${state === 'Disabled' ? 'SystemColorGrayTextColor' : highContrastColor(key)}}` : argb(colors[key]);
      xaml += `            <SolidColorBrush x:Key="${name}${state}" Color="${color}" Opacity="${theme === 'HighContrast' ? '1' : opacity}" />\n`;
    }
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
for (const [key, value] of Object.entries(tokens.radii)) xaml += `    <CornerRadius x:Key="Bleecker${pascal(key)}CornerRadius">${value}</CornerRadius>\n`;
xaml += '</ResourceDictionary>\n';

const dictionary = (name, entries, type, format) => `    public static FrozenDictionary<string, ${type}> ${name} { get; } =\n        new Dictionary<string, ${type}>(StringComparer.Ordinal)\n        {\n${Object.entries(entries).map(([key, value]) => `            ["${key}"] = ${format(value)},`).join('\n')}\n        }.ToFrozenDictionary(StringComparer.Ordinal);\n`;
let cs = `// Generated from Bleecker ${lock.commit}; do not edit.\nusing System.Collections.Frozen;\n\nnamespace Orchard;\n\npublic enum BleeckerColorScheme { Light, Dark }\n\npublic static class BleeckerTokens\n{\n`;
for (const name of ['fontFamilies', 'fontWeights']) cs += dictionary(pascal(name), tokens[name], 'string', JSON.stringify);
for (const name of ['spacing', 'radii', 'durations', 'letterSpacing']) cs += dictionary(pascal(name), tokens[name], 'double', value => value.toString());
cs += dictionary('LightColors', tokens.themes.light, 'string', value => JSON.stringify(argb(value)));
cs += dictionary('DarkColors', tokens.themes.dark, 'string', value => JSON.stringify(argb(value)));
cs += `    public static FrozenDictionary<string, string> Colors(BleeckerColorScheme scheme) => scheme switch\n    {\n        BleeckerColorScheme.Light => LightColors,\n        BleeckerColorScheme.Dark => DarkColors,\n        _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, "Unknown color scheme.")\n    };\n}\n`;
const outputs = { 'tokens/bleecker.tokens.json': JSON.stringify(tokens, null, 2) + '\n',
  'src/Orchard/Themes/Tokens.g.xaml': xaml, 'src/Orchard/Tokens/BleeckerTokens.g.cs': cs };
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
