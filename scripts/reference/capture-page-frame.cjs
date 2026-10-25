const fs = require('node:fs/promises'), path = require('node:path'), assert = require('node:assert/strict');
const crypto = require('node:crypto'), {execFileSync} = require('node:child_process');
const arg = name => { const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]); };
const source = arg('--source-root'), tooling = arg('--tooling-root'), output = arg('--output'), playwright = arg('--playwright-module');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex'), pin = 'a86059013cd59684ce452849278ea81e734f2066';
(async () => {
  await fs.mkdir(output, {recursive: true});
  const file = 'src/layout/page-frame.tsx', dependencies = [];
  for (const name of [file, 'src/utils/cn.ts', 'src/styles/global.css', 'src/styles/tokens.generated.css', 'package-lock.json']) {
    const bytes = await fs.readFile(path.join(source, name));
    assert.deepEqual(bytes, execFileSync('git', ['--git-dir', path.resolve(source, '../bleecker.git'), 'show', pin + ':' + name], {maxBuffer: 8 * 1024 * 1024}));
    dependencies.push({path: name, sha256: hash(bytes)});
  }
  assert.equal(dependencies[0].sha256, 'dacc54a6df068a390544945b15e38cc5d094cc164ef809311d4712a45acf823b');
  const client = path.join(__dirname, 'page-frame-client.tsx'), runtime = path.join(source, 'node_modules');
  const bundle = await require(path.join(tooling, 'node_modules/esbuild')).build({entryPoints: [client], alias: {'frozen/page-frame': path.join(source, file), react: path.join(runtime, 'react'), 'react-dom': path.join(runtime, 'react-dom')}, bundle: true, write: false, platform: 'browser', jsx: 'automatic', nodePaths: [runtime], define: {'process.env.NODE_ENV': '"production"'}});
  const slash = value => value.replaceAll('\\', '/');
  const input = (await fs.readFile(path.join(source, 'src/styles/global.css'), 'utf8')).replace(/@import url\([^\r\n]*\);/g, '')
    .replace('@import "tailwindcss" source(none);', '@import "' + slash(path.join(tooling, 'node_modules/tailwindcss/index.css')) + '" source(none);')
    .replace('@import "./tokens.generated.css";', '@import "' + slash(path.join(source, 'src/styles/tokens.generated.css')) + '";')
    .replace('@source "../**/*.{ts,tsx}";', '@source "' + slash(path.join(source, 'src/**/*.{ts,tsx}')) + '";');
  await fs.writeFile(path.join(output, 'input.css'), input);
  execFileSync(process.execPath, [path.join(tooling, 'node_modules/@tailwindcss/cli/dist/index.mjs'), '-i', path.join(output, 'input.css'), '-o', path.join(output, 'source.css'), '--minify']);
  const css = await fs.readFile(path.join(output, 'source.css'));
  const browser = await require(playwright).chromium.launch({headless: true, channel: 'msedge'}), cases = [];
  try {
    for (const scheme of ['Light', 'Dark']) {
      const page = await browser.newPage({viewport: {width: 1536, height: 360}, deviceScaleFactor: 1, reducedMotion: 'reduce'}), errors = [];
      page.on('pageerror', error => errors.push(error.message));
      try {
        await page.setContent(`<!doctype html><html class="${scheme === 'Dark' ? 'dark' : ''}"><head><style>${css}body{margin:0}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
        const settle = () => page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
        const capture = async spec => {
          await page.setViewportSize({width: spec.viewport, height: 360}); await page.evaluate(spec => window.showPageFrame(spec), spec); await settle();
          const measurement = await page.evaluate(() => {
            const frame = document.querySelector('#host').firstElementChild, body = frame.firstElementChild, style = getComputedStyle(frame);
            const box = node => {const b = node.getBoundingClientRect(); return {x: b.x, y: b.y, width: b.width, height: b.height};};
            return {frame: box(frame), body: body ? box(body) : null, padding: [style.paddingLeft, style.paddingTop, style.paddingRight, style.paddingBottom].map(parseFloat), element: frame.tagName.toLowerCase(), name: frame.getAttribute('aria-label'), maxWidth: style.maxWidth};
          });
          const row = {id: spec.id, scheme, spec, measurement};
          if (spec.png) {row.file = `page-frame-${spec.id}-${scheme}.png`; row.pngSha256 = hash(await page.screenshot({path: path.join(output, row.file)}));}
          cases.push(row);
        };
        for (const viewport of [320, 639, 640, 1023, 1024, 1536])
          for (const width of ['reading', 'content', 'wide', 'full'])
            for (const gutter of ['none', 'compact', 'comfortable', 'spacious'])
              for (const verticalSpacing of ['none', 'compact', 'comfortable', 'spacious'])
                await capture({id: `${viewport}-${width}-${gutter}-${verticalSpacing}`, viewport, hostWidth: viewport - 40, width, gutter, verticalSpacing,
                  png: gutter === 'comfortable' && verticalSpacing === 'comfortable' && (viewport === 320 || viewport === 1536)});
        for (const [id, spec] of [
          ['defaults', {}], ['empty', {empty: true}], ['fractional-host', {hostWidth: 900.375}],
          ['narrow-host-wide-viewport', {hostWidth: 240}],
          ...['div', 'article', 'main', 'section'].map(element => ['element-' + element, {element, name: 'Consumer region'}]),
          ['padding-override', {style: {padding: '3px 7px'}}], ['maximum-override', {style: {maxWidth: 500}}],
          ['fixed-width', {width: 'reading', style: {width: 1200}}], ['fixed-width-maximum-override', {width: 'reading', style: {width: 1200, maxWidth: 1300}}]
        ]) await capture({id, viewport: 1536, hostWidth: 1496, png: true, ...spec});
        assert.deepEqual(errors, []);
      } finally { await page.close(); }
    }
  } finally { await browser.close(); }
  const report = {format: 'orchard-page-frame-reference-v1', repository: 'gaulatti/bleecker', commit: pin, source: file, sourceSha256: dependencies[0].sha256,
    dependencies, scriptSha256: hash(await fs.readFile(__filename)), clientSha256: hash(await fs.readFile(client)), cssSha256: hash(css), browser: browser.version(), cases};
  await fs.writeFile(path.join(output, 'reference.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify({cases: cases.length, pngs: cases.filter(row => row.file).length}));
})().catch(error => {console.error(error); process.exitCode = 1;});
