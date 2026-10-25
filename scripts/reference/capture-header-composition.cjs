const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const {execFileSync} = require('node:child_process');
const option = name => {
  const at = process.argv.indexOf(name);
  assert(at >= 0 && process.argv[at + 1], name);
  return path.resolve(process.argv[at + 1]);
};
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const source = option('--source-root'), tooling = option('--tooling-root');
const output = option('--output'), playwright = option('--playwright-module');

(async () => {
  await fs.mkdir(output, {recursive: true});
  const header = JSON.parse(await fs.readFile('tests/Reference/Header.json'));
  const dependencies = [...header.dependencies, {path: 'src/layout/app-shell.tsx', sha256: '21cbb676b184bcad28ffb5577b19a1ccc34d18ab420221b08cf936a23919c3e1'}];
  for (const row of dependencies) {
    const bytes = await fs.readFile(path.join(source, row.path));
    assert.equal(hash(bytes), row.sha256, row.path);
    assert.deepEqual(bytes, execFileSync('git', ['--git-dir', path.resolve(source, '../bleecker.git'), 'show', header.commit + ':' + row.path], {maxBuffer: 8 * 1024 * 1024}));
  }
  const runtime = path.join(source, 'node_modules');
  for (const row of header.modules) assert.equal(hash(await fs.readFile(path.join(runtime, row.name, 'package.json'))), row.packageSha256);
  const client = path.join(__dirname, 'header-composition-client.tsx');
  const bundle = await require(path.join(tooling, 'node_modules/esbuild')).build({
    entryPoints: [client], bundle: true, write: false, platform: 'browser', jsx: 'automatic', nodePaths: [runtime],
    alias: {'frozen/header': path.join(source, 'src/layout/header.tsx'), 'frozen/app-shell': path.join(source, 'src/layout/app-shell.tsx'), react: path.join(runtime, 'react'), 'react-dom': path.join(runtime, 'react-dom')},
    define: {'process.env.NODE_ENV': '"production"'}
  });
  const slash = value => value.replaceAll('\\', '/');
  const input = (await fs.readFile(path.join(source, 'src/styles/global.css'), 'utf8'))
    .replace(/@import url\([^\r\n]*\);/g, '')
    .replace('@import "tailwindcss" source(none);', '@import "' + slash(path.join(tooling, 'node_modules/tailwindcss/index.css')) + '" source(none);')
    .replace('@import "./tokens.generated.css";', '@import "' + slash(path.join(source, 'src/styles/tokens.generated.css')) + '";')
    .replace('@source "../**/*.{ts,tsx}";', '@source "' + slash(path.join(source, 'src/**/*.{ts,tsx}')) + '";');
  await fs.writeFile(path.join(output, 'input.css'), input);
  execFileSync(process.execPath, [path.join(tooling, 'node_modules/@tailwindcss/cli/dist/index.mjs'), '-i', path.join(output, 'input.css'), '-o', path.join(output, 'source.css'), '--minify']);
  const css = await fs.readFile(path.join(output, 'source.css'));
  let faces = '';
  for (const font of header.fonts) {
    const bytes = await fs.readFile('src/Orchard/Assets/Fonts/' + font.name);
    assert.equal(hash(bytes), font.sha256);
    faces += `@font-face{font-family:"${font.family}";font-weight:${font.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;
  }
  const browser = await require(playwright).chromium.launch({headless: true, channel: 'msedge'});
  const cases = [];
  try {
    for (const scheme of ['Light', 'Dark']) {
      const page = await browser.newPage({viewport: {width: 1280, height: 900}, deviceScaleFactor: 1, reducedMotion: 'reduce', locale: 'en-US', timezoneId: 'America/New_York'});
      const errors = [];
      page.on('pageerror', error => errors.push(error.message));
      try {
        await page.setContent(`<!doctype html><html class="${scheme === 'Dark' ? 'dark' : ''}"><head><style>${css}body{margin:0;overflow:hidden}${faces}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
        await page.locator('header').waitFor({state: 'attached'});
        await page.evaluate(async () => {
          await Promise.all([['Encode Sans', 400], ['Encode Sans', 500], ['Encode Sans', 600], ['Libre Franklin', 400]].map(([family, weight]) => document.fonts.load(weight + ' 16px "' + family + '"')));
          await document.fonts.ready;
          await Promise.all([...document.images].map(image => image.decode()));
        });
        await page.locator('[data-composition-body]').fill('Original body edited');
        await page.evaluate(() => { const editor = document.querySelector('[data-composition-body]'); window.originalCompositionBody = editor; editor.setSelectionRange(2, 5); });
        for (const spec of [{id: 'wide', width: 1280, open: false}, {id: 'narrow', width: 640, open: false}, {id: 'narrow-open', width: 640, open: true}]) {
          await page.setViewportSize({width: spec.width, height: 900});
          if (spec.open) await page.getByRole('button', {name: 'Toggle navigation menu'}).click();
          await page.evaluate(async () => { await Promise.all(document.querySelector('header').getAnimations({subtree: true}).map(animation => animation.finished)); });
          await page.mouse.move(spec.width - 1, 899);
          await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
          const measurement = await page.evaluate(spec => {
            const header = document.querySelector('header'), main = document.querySelector('main'), body = document.querySelector('[data-composition-body]');
            const box = element => { const rect = element.getBoundingClientRect(); return {x: rect.x, y: rect.y, width: rect.width, height: rect.height}; };
            const point = {x: Math.min(spec.width - 80, 1100), y: spec.open ? 100 : 30};
            return {header: box(header), main: box(main), content: box(main.firstElementChild), body: box(body), footer: box(document.querySelector('[data-composition-footer]')), point, headerOwnsHit: header.contains(document.elementFromPoint(point.x, point.y)), originalBody: body === window.originalCompositionBody, text: body.value, selectionStart: body.selectionStart, selectionLength: body.selectionEnd - body.selectionStart};
          }, spec);
          assert.equal(measurement.headerOwnsHit, true);
          assert.equal(measurement.content.y, 72);
          assert.equal(measurement.footer.y, 868);
          assert.equal(measurement.originalBody, true);
          const file = `header-composition-${spec.id}-${scheme}.png`;
          const png = await page.screenshot({path: path.join(output, file), caret: 'hide'});
          cases.push({id: spec.id, scheme, spec, measurement, file, pngSha256: hash(png)});
        }
        assert.deepEqual(errors, []);
      } finally { await page.close(); }
    }
    const reference = {format: 'orchard-header-composition-reference-v1', commit: header.commit, dependencies, modules: header.modules, fonts: header.fonts, scriptSha256: hash(await fs.readFile(__filename)), clientSha256: hash(await fs.readFile(client)), cssSha256: hash(css), browser: await browser.version(), locale: 'en-US', timezone: 'America/New_York', motion: 'reduce', cases};
    await fs.writeFile(path.join(output, 'reference.json'), JSON.stringify(reference, null, 2) + '\n');
    console.log(JSON.stringify({sourceCases: cases.length, headerOwnsAllHits: true, originalBodyRetained: true}));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
