import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const reference = JSON.parse(await fs.readFile(new URL('../../tests/Reference/CollectionFiltersPaint.json', import.meta.url)));
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
assert.equal(reference.referenceSha256, hash(await fs.readFile(new URL('../../tests/Reference/CollectionFilters.json', import.meta.url))));
assert.equal(reference.scriptSha256, hash(await fs.readFile(new URL('./capture-collection-filters-paint.cjs', import.meta.url))));
assert.equal(reference.sourceScenePixelsPainted, false);

export function assertCollectionFiltersPaint(frame) {
    if (!frame.visible) { assert.equal(frame.paint, null); return 0; }
    const keys = ['outerBackground', 'outerBorder', 'activeBackground', 'activeBorder', 'bodyBackground', 'bodyBorder', 'countBackground', 'countForeground'];
    if (frame.chips.length) keys.push('chipBorder');
    assert.deepEqual(Object.keys(frame.paint).sort(), keys.sort(), `${frame.scheme}/${frame.id}: owned brush inventory`);
    const theme = reference.themes.find(row => row.theme === frame.scheme);
    assert(theme);
    for (const key of keys) {
        const expected = theme.paints.find(paint => paint.key === key), actual = frame.paint[key];
        assert.equal(actual.color.toLowerCase(), expected.domArgb, `${frame.scheme}/${frame.id}/${key}: CSS DOM color encoding`);
        assert.equal(actual.opacity, 1, `${frame.scheme}/${frame.id}/${key}: alpha is encoded once`);
    }
    return keys.length;
}
