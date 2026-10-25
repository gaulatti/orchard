const fs = require('node:fs/promises'), path = require('node:path'), assert = require('node:assert/strict');
const crypto = require('node:crypto'), { execFileSync } = require('node:child_process');
const arg = name => { const index = process.argv.indexOf(name); assert(index >= 0 && process.argv[index + 1]); return path.resolve(process.argv[index + 1]); };
const source = arg('--source-root'), tooling = arg('--tooling-root'), output = arg('--output'), playwright = arg('--playwright-module');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const pin = 'a86059013cd59684ce452849278ea81e734f2066';
(async () => {
  await fs.mkdir(output, { recursive: true });
  const file = 'src/components/tabs.tsx', sourceBytes = await fs.readFile(path.join(source, file));
  assert.equal(hash(sourceBytes), '812294042eecf36f03ccfe890bac6f1542a6573c1fd8c24b42e963bb560c6827');
  for (const name of [file, 'src/components/nav-menu.tsx', 'src/utils/cn.ts', 'src/styles/global.css', 'src/styles/tokens.generated.css', 'package-lock.json'])
    assert.deepEqual(await fs.readFile(path.join(source, name)), execFileSync('git', ['--git-dir', path.resolve(source, '../bleecker.git'), 'show', pin + ':' + name], { maxBuffer: 8 * 1024 * 1024 }));
  const runtime = path.join(source, 'node_modules'), esbuild = require(path.join(tooling, 'node_modules/esbuild'));
  const client = path.join(__dirname, 'tabs-client.tsx'), clientBytes = await fs.readFile(client);
  const bundle = await esbuild.build({ entryPoints: [client], alias: { 'frozen/tabs': path.join(source, file), react: path.join(runtime, 'react'), 'react-dom': path.join(runtime, 'react-dom') }, bundle: true, write: false, platform: 'browser', jsx: 'automatic', nodePaths: [runtime], define: { 'process.env.NODE_ENV': '"production"' } });
  const slash = value => value.replaceAll('\\', '/');
  const input = (await fs.readFile(path.join(source, 'src/styles/global.css'), 'utf8')).replace(/@import url\([^\r\n]*\);/g, '')
    .replace('@import "tailwindcss" source(none);', '@import "' + slash(path.join(tooling, 'node_modules/tailwindcss/index.css')) + '" source(none);')
    .replace('@import "./tokens.generated.css";', '@import "' + slash(path.join(source, 'src/styles/tokens.generated.css')) + '";')
    .replace('@source "../**/*.{ts,tsx}";', '@source "' + slash(path.join(source, 'src/**/*.{ts,tsx}')) + '";');
  await fs.writeFile(path.join(output, 'input.css'), input);
  execFileSync(process.execPath, [path.join(tooling, 'node_modules/@tailwindcss/cli/dist/index.mjs'), '-i', path.join(output, 'input.css'), '-o', path.join(output, 'source.css'), '--minify']);
  const cssBytes = await fs.readFile(path.join(output, 'source.css'));
  let css = cssBytes.toString();
  for (const [weight, name] of [[400, 'Regular'], [500, 'Medium']]) css += '@font-face{font-family:"Encode Sans";font-weight:' + weight + ';src:url(data:font/ttf;base64,' + (await fs.readFile('src/Orchard/Assets/Fonts/EncodeSans-' + name + '.ttf')).toString('base64') + ')}';
  const tabs = [{ id: 'overview', label: 'Overview', panelId: 'overview-panel' }, { id: 'activity', label: 'Activity', panelId: 'activity-panel' }, { id: 'settings', label: 'Settings' }];
  const browser = await require(playwright).chromium.launch({ headless: true, channel: 'msedge' }), cases = [], interactions = [];
  try {
    for (const scheme of ['Light', 'Dark']) {
      const page = await browser.newPage({ viewport: { width: 640, height: 240 }, deviceScaleFactor: 1, reducedMotion: 'reduce' });
      try {
        const errors = []; page.on('pageerror', error => errors.push(error.message));
        await page.setContent(`<!doctype html><html class="${scheme === 'Dark' ? 'dark' : ''}"><head><style>${css}body{margin:0}#root{position:absolute;left:20px;top:20px;width:480px}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
        await page.evaluate(async () => { await document.fonts.load('500 14px "Encode Sans"'); await document.fonts.ready; });
        const settle = () => page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
        const measure = () => page.evaluate(() => {
          const nav = document.querySelector('#root>nav');
          const item = node => { const box = node.getBoundingClientRect(), style = getComputedStyle(node); return { box: { x: box.x, y: box.y, width: box.width, height: box.height }, role: node.getAttribute('role'), selected: node.getAttribute('aria-selected'), tabIndex: node.tabIndex, text: node.textContent, href: node.getAttribute('href'), controls: node.getAttribute('aria-controls'), foreground: style.color, background: style.backgroundColor, border: style.borderColor, borderThickness: [style.borderLeftWidth, style.borderTopWidth, style.borderRightWidth, style.borderBottomWidth].map(parseFloat), fontSize: style.fontSize, fontWeight: style.fontWeight, lineHeight: style.lineHeight, padding: style.padding, radius: style.borderRadius }; };
          return { root: item(nav), items: [...nav.children].map(item) };
        });
        for (const variant of ['underline', 'segmented', 'enclosed']) for (const size of ['sm', 'md', 'lg']) for (const stretch of [false, true]) {
          const spec = { id: `${variant}-${size}-${stretch}`, tabs, activeTab: 'overview', variant, size, stretch };
          await page.evaluate(spec => window.showTabs(spec), spec); await settle();
          const filename = `tabs-${spec.id}-${scheme}.png`, measurement = await measure();
          const png = await page.screenshot({ path: path.join(output, filename) });
          cases.push({ scheme, spec, measurement, file: filename, pngSha256: hash(png) });
        }
        for (const [id, items, activeTab, customLink] of [['empty', [], undefined, false], ['unknown', tabs, 'unknown', false], ['unicode', [{ id: 'jp', label: '日本語のタブ' }, { id: 'rtl', label: 'العربية' }], 'jp', false], ['navigation', tabs.map(item => ({ ...item, href: '/demo/' + item.id })), 'overview', true]]) {
          const spec = { id, tabs: items, activeTab, customLink };
          await page.evaluate(spec => window.showTabs(spec), spec); await settle();
          cases.push({ scheme, spec, measurement: await measure() });
        }
        for (const accept of [false, true]) {
          await page.evaluate(spec => window.showTabs(spec), { id: 'interaction-' + accept, tabs, activeTab: 'overview', accept }); await settle();
          await page.getByRole('tab', { name: 'Activity', exact: true }).click(); await settle();
          const clicked = await measure();
          await page.keyboard.press('End'); await settle();
          const ended = await measure();
          interactions.push({ scheme, accept, requests: await page.evaluate(() => window.tabRequests), clicked, ended });
        }
        assert.deepEqual(errors, []);
      } finally { await page.close(); }
    }
  } finally { await browser.close(); }
  const report = { format: 'orchard-tabs-reference-v1', sourceCommit: pin, sourceFile: file, sourceSha256: hash(sourceBytes), sourceCssSha256: hash(cssBytes), clientSha256: hash(clientBytes), scriptSha256: hash(await fs.readFile(__filename)), browser: browser.version(), cases, interactions };
  await fs.writeFile(path.join(output, 'reference.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify({ cases: cases.length, interactions: interactions.length }));
})().catch(error => { console.error(error); process.exitCode = 1; });
