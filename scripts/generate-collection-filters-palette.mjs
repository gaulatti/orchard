import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const reference = JSON.parse(await fs.readFile('tests/Reference/CollectionFiltersPaint.json'));
assert.equal(reference.format, 'orchard-collection-filters-css-paint-v1');
assert.equal(reference.commit, 'a86059013cd59684ce452849278ea81e734f2066');
assert.equal(reference.referenceSha256, hash(await fs.readFile('tests/Reference/CollectionFilters.json')));
assert.equal(reference.scriptSha256, hash(await fs.readFile('scripts/reference/capture-collection-filters-paint.cjs')));
assert.equal(reference.sourceScenePixelsPainted, false);
const keys = ['outerBackground', 'outerBorder', 'activeBackground', 'activeBorder', 'bodyBackground', 'bodyBorder', 'countBackground', 'countForeground', 'chipBorder'];
assert.deepEqual(reference.themes.map(row => row.theme), ['Light', 'Dark']);
let xml = '<!-- Generated from pinned CSS token DOM swatches; run scripts/generate-collection-filters-palette.mjs. -->\n';
xml += '<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n <ResourceDictionary.ThemeDictionaries>\n';
for (const row of reference.themes) {
    assert.equal(row.paints.length, 11);
    xml += `  <ResourceDictionary x:Key="${row.theme === 'Dark' ? 'Default' : row.theme}">\n`;
    for (const paint of row.paints) {
        const bytes = await fs.readFile(`tests/Reference/CollectionFiltersPaint/${paint.file}`);
        assert.equal(hash(bytes), paint.pngSha256);
        const png = readRgbaPng(bytes);
        assert.deepEqual([png.width, png.height], [1, 1]);
        assert.deepEqual([...png.rgba], paint.domRgba);
        assert.equal(paint.domArgb, '#' + [png.rgba[3], ...png.rgba.subarray(0, 3)].map(value => value.toString(16).padStart(2, '0')).join(''));
    }
    for (const key of keys) {
        const paint = row.paints.filter(paint => paint.key === key);
        assert.equal(paint.length, 1);
        xml += `   <SolidColorBrush x:Key="Collection${key[0].toUpperCase() + key.slice(1)}" Color="${paint[0].domArgb.toUpperCase()}"/>\n`;
    }
    xml += '  </ResourceDictionary>\n';
}
xml += ' </ResourceDictionary.ThemeDictionaries>\n</ResourceDictionary>\n';
const target = 'src/Orchard/Themes/CollectionFiltersPalette.g.xaml';
if (process.argv.includes('--check')) assert.equal(await fs.readFile(target, 'utf8'), xml, 'Collection Filters palette is stale.');
else await fs.writeFile(target, xml);
console.log(JSON.stringify({passed: true, themes: 2, brushes: 18, sourceSwatches: 22, checked: process.argv.includes('--check')}));
