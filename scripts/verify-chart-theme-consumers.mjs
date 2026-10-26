import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';

const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const argb = value => {
  if (/^#[0-9a-f]{6}$/i.test(value)) return '#FF' + value.slice(1).toUpperCase();
  const rgba = /^rgba\((\d+),\s*(\d+),\s*(\d+),\s*([\d.]+)\)$/.exec(value); assert(rgba, value);
  return '#' + [Math.round(Number(rgba[4]) * 255), ...rgba.slice(1, 4).map(Number)].map(c => c.toString(16).padStart(2, '0').toUpperCase()).join('');
};
export function verifyChartThemeConsumers(report, tokens, reference) {
  assert.equal(report.format, 'orchard-chart-theme-consumers-v1');
  assert.equal(report.passed, true); assert.equal(report.error, undefined);
  for (const k of ['showWindowCalled', 'focusCalled', 'hardwareInputSent', 'osSettingsChanged', 'sourceRasterParityVerified', 'nativeComponentAccepted']) assert.equal(report[k], false, k);
  assert.equal(report.focus, 0); assert.equal(report.activations, 0); assert.equal(report.processArchitecture, 'X64'); assert(report.guards >= 300);
  const ids = [];
  for (const kind of ['Line', 'Bar', 'Area']) for (const scope of ['regular', 'scoped']) for (const theme of ['Light', 'Dark']) {
    const prefix = `${kind}/${scope}/${theme}`;
    const phases = ['source-key-name', 'automatic-palette', 'grid-axis-tooltip-defaults', 'rendered-tooltip-defaults', 'live-key-name', 'explicit-transparent', 'live-explicit-paint', 'gradient-ownership', 'restore-automatic', ...(scope === 'scoped' ? ['live-scoped-paint', 'live-scoped-defaults', 'rendered-live-scoped-tooltip'] : [])];
    const shapeCount = {Line: 1, Bar: 3, Area: 2}[kind];
    for (const phase of phases) {
      const id = prefix + '/' + phase; ids.push(id);
      const check = report.checks.find(c => c.id === id); assert(check, id); assert.equal(check.passed, true, id); const o = check.observed;
      if (phase === 'source-key-name') { assert.equal(o.shapes, shapeCount); assert(o.firstY > o.lastY); assert.equal(o.aliases, true); assert.equal(o.label, 'Fictional alpha'); }
      if (phase === 'live-key-name') { assert(o.firstY < o.lastY); assert.equal(o.label, 'Updated alpha'); }
      if (['automatic-palette', 'restore-automatic', 'live-scoped-paint'].includes(phase)) {
        const expected = phase === 'live-scoped-paint' ? '#FFCD2D5F' : scope === 'scoped' ? '#FF197391' : '#FF' + tokens.themes[theme.toLowerCase()].sea.slice(1).toUpperCase();
        assert.equal(o.expected, expected); assert.deepEqual(o.paints, Array(shapeCount).fill(expected)); assert.equal(o.labelPaint, expected);
      }
      if (['rendered-tooltip-defaults', 'rendered-live-scoped-tooltip'].includes(phase)) {
        const values = tokens.themes[theme.toLowerCase()], live = phase === 'rendered-live-scoped-tooltip';
        assert.equal(o.controlledRendererState, true); assert.equal(o.visibility, 'Visible'); assert(o.width > 0 && o.height > 0);
        assert.equal(o.heading, 'A'); assert.equal(o.item, (kind === 'Line' ? (live ? 'Updated alpha' : 'Fictional alpha') : '') + ' : ' + (live ? 30 : 10));
        assert.equal(o.headingWeight, reference.theme.tooltip.labelStyle.fontWeight);
        assert.equal(o.headingPaint, live ? '#FF9B4E22' : scope === 'scoped' ? '#FF704090' : argb(values.textSecondary));
        assert.equal(o.itemPaint, live ? '#FF60A0C0' : scope === 'scoped' ? '#FF403020' : argb(values.textPrimary));
        assert.equal(o.background, live ? '#FF14283C' : scope === 'scoped' ? '#FF203040' : argb(values.card));
        assert.equal(o.border, live ? '#FF21735B' : scope === 'scoped' ? '#FF805020' : argb(values.border));
        assert.equal(o.tooltipInteractionVerified, false); assert.equal(o.tooltipEffectsVerified, false);
      }
      if (['explicit-transparent', 'gradient-ownership'].includes(phase)) { assert.equal(o.originalBrushRetained, true); assert.equal(o.legendBrushRetained, true); }
      if (phase === 'explicit-transparent') assert.match(o.paint, /^#00[0-9A-F]{6}$/);
      if (phase === 'live-explicit-paint') { assert.deepEqual(o.paints, Array(shapeCount).fill('#FFFF4500')); assert.equal(o.labelPaint, '#FFFF4500'); }
      if (['grid-axis-tooltip-defaults', 'live-scoped-defaults'].includes(phase)) {
        const values = tokens.themes[theme.toLowerCase()], live = phase === 'live-scoped-defaults';
        const border = live ? '#FF21735B' : scope === 'scoped' ? '#FF805020' : argb(values.border);
        const tick = live ? '#FF9B4E22' : scope === 'scoped' ? '#FF704090' : argb(values.textSecondary);
        const card = live ? '#FF14283C' : scope === 'scoped' ? '#FF203040' : argb(values.card);
        assert(o.gridStrokes.length > 0); assert(o.tickPaints.length >= 3);
        assert.deepEqual(o.gridStrokes, Array(o.gridStrokes.length).fill(border)); assert.deepEqual(o.gridOpacities, Array(o.gridStrokes.length).fill(1)); assert.deepEqual(o.gridWidths, Array(o.gridStrokes.length).fill(1));
        assert.deepEqual(o.gridDashes, Array.from({length: o.gridStrokes.length}, () => reference.theme.grid.strokeDasharray.split(' ').map(Number)));
        assert.deepEqual(o.tickPaints, Array(o.tickPaints.length).fill(tick)); assert.deepEqual(o.tickOpacities, Array(o.tickPaints.length).fill(1)); assert.deepEqual(o.tickSizes, Array(o.tickPaints.length).fill(reference.theme.axis.tick.fontSize));
        assert.equal(o.tooltipBackground, card); assert.equal(o.tooltipBackgroundOpacity, 1); assert.equal(o.tooltipBorder, border); assert.equal(o.tooltipBorderOpacity, 1);
        assert.deepEqual(o.tooltipBorderThickness, Array(4).fill(Number.parseFloat(reference.theme.tooltip.contentStyle.border))); assert.deepEqual(o.tooltipRadii, Array(4).fill(Number.parseFloat(reference.theme.tooltip.contentStyle.borderRadius)));
        assert.equal(o.tooltipVisibility, 'Collapsed'); assert.equal(o.tooltipInteractionVerified, false); assert.equal(o.tooltipEffectsVerified, false);
      }
    }
  }
  for (const scope of ['regular', 'scoped']) for (const theme of ['Light', 'Dark']) {
    const prefix = `Scatter/${scope}/${theme}`, values = tokens.themes[theme.toLowerCase()];
    const sea = scope === 'scoped' ? '#FF197391' : argb(values.sea), desert = scope === 'scoped' ? '#FFA96527' : argb(values.desert);
    const expected = [sea, sea, sea, desert, desert, desert];
    const phases = ['automatic-palette', 'grid-axis-tooltip-defaults', 'explicit-transparent', 'live-explicit-paint', 'restore-automatic', ...(scope === 'scoped' ? ['live-scoped-palette'] : [])];
    for (const phase of phases) {
      const id = `${prefix}/${phase}`; ids.push(id); const check = report.checks.find(c => c.id === id);
      assert(check, id); assert.equal(check.passed, true, id); const o = check.observed;
      if (phase === 'automatic-palette') { assert.equal(o.aliases, true); assert.deepEqual(o.paints, expected); assert.deepEqual(o.labels, ['Fictional first', 'Fictional second']); assert.deepEqual(o.labelPaints, [sea, desert]); }
      if (phase === 'grid-axis-tooltip-defaults') {
        const border = scope === 'scoped' ? '#FF805020' : argb(values.border), tick = scope === 'scoped' ? '#FF704090' : argb(values.textSecondary), card = scope === 'scoped' ? '#FF203040' : argb(values.card);
        assert(o.gridStrokes.length > 0 && o.tickPaints.length >= 3); assert(o.gridStrokes.every(p => p === border)); assert(o.tickPaints.every(p => p === tick)); assert(o.tickSizes.every(size => size === reference.theme.axis.tick.fontSize));
        assert.equal(o.tooltipBackground, card); assert.equal(o.tooltipBorder, border); assert.deepEqual(o.gridDashes, Array.from({length: o.gridStrokes.length}, () => reference.theme.grid.strokeDasharray.split(' ').map(Number)));
      }
      if (phase === 'explicit-transparent') { assert.equal(o.pointBrushRetained, true); assert.equal(o.legendBrushRetained, true); }
      if (phase === 'live-explicit-paint') { assert.deepEqual(o.paints, Array(3).fill('#FFFF4500')); assert.equal(o.labelPaint, '#FFFF4500'); }
      if (phase === 'restore-automatic') { assert.deepEqual(o.paints, Array(3).fill(desert)); assert.equal(o.labelPaint, desert); }
      if (phase === 'live-scoped-palette') { assert.deepEqual(o.paints, ['#FFFF0000', '#FFFF0000', '#FFFF0000', '#FF0000FF', '#FF0000FF', '#FF0000FF']); assert.deepEqual(o.labelPaints, ['#FFFF0000', '#FF0000FF']); }
    }
  }
  assert.deepEqual(report.checks.map(c => c.id).sort(), ids.sort()); assert.equal(ids.length, 148);
  for (const k of ['librarySha256', 'hostSha256']) assert.match(report[k], /^[0-9a-f]{64}$/);
  return {passed: true, checks: 148, chartConsumers: ['Line', 'Bar', 'Area', 'Scatter'], themes: 2, scopes: 2, literalAndLiveKeysVerified: true, liveLegendNamesVerified: true, originalTransparentAndGradientBrushesRetained: true, automaticAndScopedPaletteVerified: true, axisGridAndTooltipSurfaceDefaultsVerified: true, liveScopedDefaultRoleUpdatesVerified: true, controlledTooltipRendererVerified: true, tooltipInteractionVerified: false, tooltipEffectsVerified: false, guards: report.guards, librarySha256: report.librarySha256, hostSha256: report.hostSha256, physicalInputVerified: false, sourceRasterParityVerified: false, nativeComponentAccepted: false};
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  const arg = k => { const i = process.argv.indexOf(k); assert(i >= 0 && process.argv[i + 1], k); return process.argv[i + 1]; };
  const folder = arg('--native'), app = arg('--app'), host = arg('--host');
  const bytes = await fs.readFile(path.join(folder, 'chart-theme-consumers.json')), report = JSON.parse(bytes), tokens = JSON.parse(await fs.readFile(new URL('../tokens/bleecker.tokens.json', import.meta.url))), reference = JSON.parse(await fs.readFile(new URL('../tests/Reference/ChartTheme.json', import.meta.url)));
  const result = verifyChartThemeConsumers(report, tokens, reference), exit = JSON.parse(await fs.readFile(path.join(folder, 'process-exit.json')));
  assert.equal(exit.exited, true); assert.equal(exit.exitCode, 0); assert.equal(exit.observationExpired, false);
  assert.equal(hash(await fs.readFile(path.join(app, 'Orchard.dll'))), report.librarySha256); assert.equal(hash(await fs.readFile(path.join(app, host + '.dll'))), report.hostSha256);
  const mutations = [r => r.checks.pop(), r => r.checks[0].observed.aliases = false, r => r.checks[0].observed.firstY = -1000,
    r => r.checks.find(c => c.id.endsWith('/live-key-name')).observed.label = 'Old name',
    r => r.checks.find(c => c.id.endsWith('/explicit-transparent')).observed.originalBrushRetained = false,
    r => r.checks.find(c => c.id.endsWith('/gradient-ownership')).observed.legendBrushRetained = false,
    r => r.checks.find(c => c.id.endsWith('/automatic-palette')).observed.paints[0] = '#FF000000',
    r => r.checks.find(c => c.id.endsWith('/live-scoped-paint')).observed.labelPaint = '#FF000000', r => r.focus = 1];
  mutations.push(r => r.checks.find(c => c.id.endsWith('/grid-axis-tooltip-defaults')).observed.gridDashes[0][0] = 2,
    r => r.checks.find(c => c.id.endsWith('/grid-axis-tooltip-defaults')).observed.tickSizes[0] = 13,
    r => r.checks.find(c => c.id.endsWith('/grid-axis-tooltip-defaults')).observed.tooltipRadii[0] = 8,
    r => r.checks.find(c => c.id.endsWith('/live-scoped-defaults')).observed.tooltipBackground = '#FFFFFFFF',
    r => r.checks.find(c => c.id.endsWith('/rendered-tooltip-defaults')).observed.headingWeight = 400,
    r => r.checks.find(c => c.id.endsWith('/rendered-tooltip-defaults')).observed.item = 'Wrong series : 10',
    r => r.checks.find(c => c.id.endsWith('/rendered-live-scoped-tooltip')).observed.itemPaint = '#FFFFFFFF',
    r => r.checks.find(c => c.id === 'Scatter/regular/Light/automatic-palette').observed.paints[3] = '#FF000000',
    r => r.checks.find(c => c.id === 'Scatter/scoped/Dark/explicit-transparent').observed.legendBrushRetained = false);
  for (const mutate of mutations) { const bad = structuredClone(report); mutate(bad); assert.throws(() => verifyChartThemeConsumers(bad, tokens, reference)); }
  const audit = {...result, reportSha256: hash(bytes), negativeCasesRejected: mutations.length};
  await fs.writeFile(arg('--output'), JSON.stringify(audit, null, 2) + '\n', {flag: 'wx'}); console.log(JSON.stringify(audit));
}
