const fs = require('node:fs/promises'), path = require('node:path'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const option = name => { const i = process.argv.indexOf(name); assert(i >= 0 && process.argv[i + 1]); return path.resolve(process.argv[i + 1]); };
const output = option('--output'), esbuildPath = option('--esbuild-module'), playwrightPath = option('--playwright-module');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
(async () => {
  await fs.mkdir(output, { recursive: true });
  const source = path.resolve('artifacts/line-chart-browser/frozen-source/src/components/textarea.tsx');
  const sourceBytes = await fs.readFile(source), runtime = path.resolve('artifacts/line-chart-browser/node_modules');
  assert.equal(hash(sourceBytes), 'b523ba0962a02492a5e5954957eede4e1ad683ee7ce7ac21c04097325eb82082');
  const lockBytes = await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'), lock = JSON.parse(lockBytes);
  const installed = JSON.parse(await fs.readFile('artifacts/line-chart-browser/package-lock.json'));
  for (const [key, value] of Object.entries(installed.packages)) if (key) {
    assert.equal(value.version, lock.packages[key]?.version); assert.equal(value.integrity, lock.packages[key]?.integrity);
  }
  assert.equal(require(path.join(esbuildPath, 'package.json')).version, '0.25.12');
  assert.equal(require(path.join(playwrightPath, 'package.json')).version, '1.62.1');
  const bundle = await require(esbuildPath).build({
    alias: { react: path.join(runtime, 'react'), 'react-dom': path.join(runtime, 'react-dom') },
    stdin: { contents: `import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{Textarea}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));window.actions=[];window.show=p=>flushSync(()=>root.render(<Textarea key={p.id} aria-label="Editorial notes" {...p} onChange={e=>window.actions.push({kind:'change',text:e.target.value})}/>));`, loader: 'tsx', resolveDir: process.cwd() },
    bundle: true, write: false, platform: 'browser', jsx: 'automatic', nodePaths: [runtime], define: { 'process.env.NODE_ENV': '"production"' }
  });
  const cssBytes = await fs.readFile('artifacts/line-chart-browser/styles.css');
  let css = cssBytes.toString().replace(/@import url\([^\r\n]*\);/g, ''); const fonts = [];
  for (const [weight, file] of [[400, 'EncodeSans-Regular.ttf'], [500, 'EncodeSans-Medium.ttf'], [600, 'EncodeSans-SemiBold.ttf']]) {
    const bytes = await fs.readFile('src/Orchard/Assets/Fonts/' + file); fonts.push({ weight, file, sha256: hash(bytes) });
    css += `@font-face{font-family:'Encode Sans';font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;
  }
  const specs = [
    { id: 'small', textareaSize: 'sm', defaultValue: 'First line\nSecond line' },
    { id: 'medium', defaultValue: 'First line\nSecond line' },
    { id: 'large', textareaSize: 'lg', defaultValue: 'First line\nSecond line' },
    { id: 'empty', placeholder: 'Write editorial notes' },
    { id: 'rows-one', rows: 1 }, { id: 'rows-ten', rows: 10 },
    { id: 'small-rows-one', textareaSize: 'sm', rows: 1 },
    { id: 'large-rows-ten', textareaSize: 'lg', rows: 10 },
    { id: 'error', error: true, defaultValue: 'Review required' },
    { id: 'error-aria-override', error: true, 'aria-invalid': false },
    { id: 'disabled', disabled: true, defaultValue: 'Unavailable notes' },
    { id: 'disabled-error', disabled: true, error: true },
    { id: 'readonly', readOnly: true, defaultValue: 'Retained notes\nSecond line' },
    { id: 'rtl', dir: 'rtl', defaultValue: 'ملاحظات التحرير\nالسطر الثاني' },
    { id: 'long', defaultValue: Array.from({ length: 16 }, (_, i) => `Line ${i + 1}: Révision 日本語 editorial notes`).join('\n') }
  ];
  const browser = await require(playwrightPath).chromium.launch({ headless: true, channel: 'msedge' });
  assert.equal(browser.version(), '155.0.4283.45'); const cases = [], interactions = [];
  try {
    for (const width of [320, 640]) for (const scheme of ['Light', 'Dark']) {
      const page = await browser.newPage({ viewport: { width, height: 600 }, deviceScaleFactor: 1, reducedMotion: 'reduce' });
      const errors = []; page.on('pageerror', e => errors.push(e.message));
      try {
        await page.setContent(`<html class="${scheme === 'Dark' ? 'dark' : ''}"><head><style>${css}body{margin:0;background:${scheme === 'Dark' ? '#0D1821' : '#FFFFFF'}}#root{position:absolute;left:20px;top:20px;width:${width - 40}px}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text};window.inputEvents=[];for(const type of ['pointerdown','pointerup','keydown','keyup','input'])document.addEventListener(type,e=>window.inputEvents.push({type,trusted:e.isTrusted,key:e.key??null}),true);</script></body></html>`);
        await page.evaluate(async () => { await document.fonts.load('400 14px "Encode Sans"'); await document.fonts.ready; });
        const settle = async () => page.evaluate(async () => {
          for (const a of document.getAnimations()) a.finish();
          await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
        });
        const measure = async () => page.evaluate(() => {
          const e = document.querySelector('textarea'), s = getComputedStyle(e), p = getComputedStyle(e, '::placeholder'), b = e.getBoundingClientRect();
          return { box: { x: b.x, y: b.y, width: b.width, height: b.height }, rows: e.rows, value: e.value, ariaInvalid: e.getAttribute('aria-invalid'), disabled: e.disabled, readOnly: e.readOnly, scrollHeight: e.scrollHeight, clientHeight: e.clientHeight,
            paint: Object.fromEntries(['minHeight', 'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft', 'fontFamily', 'fontSize', 'lineHeight', 'color', 'backgroundColor', 'borderColor', 'borderRadius', 'boxShadow', 'opacity', 'resize', 'cursor', 'direction', 'transitionDuration', 'transitionTimingFunction'].map(k => [k, s[k]])), placeholder: { color: p.color, opacity: p.opacity } };
        });
        for (const spec of specs) for (const state of ['normal', 'hover', 'focus']) {
          if (state === 'focus' && spec.disabled) continue;
          await page.mouse.move(0, 0); await page.evaluate(p => window.show(p), { ...spec, id: `${spec.id}-${state}` });
          const textarea = page.getByRole('textbox', { name: 'Editorial notes' });
          if (state === 'hover') await textarea.hover(); else if (state === 'focus') await textarea.focus();
          await settle(); const measurement = await measure();
          assert.equal(measurement.paint.opacity, spec.disabled ? '0.6' : '1');
          assert.equal(measurement.paint.resize, 'vertical');
          const file = `textarea-${spec.id}-${state}-${width}-${scheme}.png`; await page.screenshot({ path: path.join(output, file) });
          cases.push({ id: spec.id, width, scheme, state, props: spec, ...measurement, file, pngSha256: hash(await fs.readFile(path.join(output, file))) });
        }
        const textarea = page.getByRole('textbox', { name: 'Editorial notes' });
        await page.evaluate(() => { window.actions = []; window.inputEvents = []; window.show({ id: 'editing', defaultValue: '' }); });
        await textarea.click(); await page.keyboard.type('First line'); await page.keyboard.press('Enter'); await page.keyboard.type('Second line');
        const typed = await textarea.inputValue(); assert.equal(typed, 'First line\nSecond line');
        await page.keyboard.press('Control+a'); await page.keyboard.type('Replacement'); await page.keyboard.press('Control+z');
        assert.equal(await textarea.inputValue(), typed);
        await page.evaluate(() => window.show({ id: 'resize', defaultValue: 'Resize notes' })); await settle();
        const before = await measure();
        await page.mouse.move(before.box.x + before.box.width - 3, before.box.y + before.box.height - 3);
        await page.mouse.down(); await page.mouse.move(before.box.x + before.box.width + 37, before.box.y + before.box.height + 45, { steps: 8 }); await page.mouse.up();
        const resized = await measure(); assert.equal(resized.box.width, before.box.width); assert(resized.box.height >= before.box.height + 40);
        await page.mouse.move(resized.box.x + resized.box.width - 3, resized.box.y + resized.box.height - 3);
        await page.mouse.down(); await page.mouse.move(resized.box.x + resized.box.width - 3, resized.box.y + 10, { steps: 8 }); await page.mouse.up();
        const shrunk = await measure(); assert.equal(shrunk.box.height, before.box.height, 'Vertical resize must respect source min-height.');
        await page.evaluate(() => window.show({ id: 'readonly-input', readOnly: true, defaultValue: 'Retained notes' }));
        await textarea.click(); await page.keyboard.press('Control+a'); await page.keyboard.type('Rejected'); assert.equal(await textarea.inputValue(), 'Retained notes');
        await page.keyboard.press('ArrowLeft'); await page.keyboard.press('Shift+ArrowRight');
        const readonlySelection = await textarea.evaluate(e => ({ start: e.selectionStart, end: e.selectionEnd })); assert(readonlySelection.end > readonlySelection.start);
        const inputEvents = await page.evaluate(() => window.inputEvents);
        assert(inputEvents.length > 40); assert(inputEvents.every(e => e.trusted));
        assert(inputEvents.some(e => e.type === 'input') && inputEvents.some(e => e.key === 'Enter'));
        interactions.push({ width, scheme, multilineEditing: true, undoRestoresBothLines: true, resize: { before: before.box, after: resized.box, minimum: shrunk.box, widthUnchanged: true }, readOnlyRejectsEditing: true, readonlySelection, inputEvents });
        assert.deepEqual(errors, []);
      } finally { await page.close(); }
    }
  } finally { await browser.close(); }
  const report = { format: 'orchard-textarea-reference-v1', sourceCommit: 'a86059013cd59684ce452849278ea81e734f2066', sourceSha256: hash(sourceBytes), scriptSha256: hash((await fs.readFile(__filename, 'utf8')).replace(/\r\n/g, '\n')), frozenLockSha256: hash(lockBytes), cssSha256: hash(cssBytes), fontFiles: fonts, browser: browser.version(), react: '19.2.4', esbuild: '0.25.12', playwright: '1.62.1', freshMountPerState: true, animationsFinishedForEndpointMeasurements: true, cases, interactions };
  await fs.writeFile(path.join(output, 'reference.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify({ cases: cases.length, interactions: interactions.map(({ inputEvents, ...interaction }) => ({ ...interaction, trustedEventCount: inputEvents.length })) }));
})().catch(error => { console.error(error); process.exitCode = 1; });
