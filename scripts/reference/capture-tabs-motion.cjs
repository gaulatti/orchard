const fs = require('node:fs/promises'), path = require('node:path'), assert = require('node:assert/strict'), crypto = require('node:crypto'), {execFileSync} = require('node:child_process');
const option = name => { const index = process.argv.indexOf(name); assert(index >= 0 && process.argv[index + 1]); return path.resolve(process.argv[index + 1]); };
const source = option('--source-root'), tooling = option('--tooling-root'), cssDirectory = option('--source-css'), playwright = option('--playwright-module'), output = option('--output');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
(async () => {
  await fs.mkdir(output, {recursive: true});
  const baselineBytes = await fs.readFile('tests/Reference/Tabs.json'), baseline = JSON.parse(baselineBytes);
  const sourceBytes = await fs.readFile(path.join(source, baseline.sourceFile)); assert.equal(hash(sourceBytes), baseline.sourceSha256);
  assert.deepEqual(sourceBytes, execFileSync('git', ['--git-dir', path.resolve(source, '../bleecker.git'), 'show', baseline.sourceCommit + ':' + baseline.sourceFile]));
  const runtime = path.join(source, 'node_modules'), client = path.join(__dirname, 'tabs-motion-client.tsx'), clientBytes = await fs.readFile(client);
  const bundle = await require(path.join(tooling, 'node_modules/esbuild')).build({entryPoints: [client], alias: {'frozen/tabs': path.join(source, baseline.sourceFile), react: path.join(runtime, 'react'), 'react-dom': path.join(runtime, 'react-dom')}, nodePaths: [runtime], bundle: true, write: false, jsx: 'automatic', platform: 'browser', define: {'process.env.NODE_ENV': '"production"'}});
  const cssBytes = await fs.readFile(path.join(cssDirectory, 'source.css')); assert.equal(hash(cssBytes), baseline.sourceCssSha256);
  let css = cssBytes.toString();
  for (const [weight, name] of [[400, 'Regular'], [500, 'Medium']]) css += '@font-face{font-family:"Encode Sans";font-weight:' + weight + ';src:url(data:font/ttf;base64,' + (await fs.readFile('src/Orchard/Assets/Fonts/EncodeSans-' + name + '.ttf')).toString('base64') + ')}';
  const browser = await require(playwright).chromium.launch({headless: true, channel: 'msedge'}); assert.equal(browser.version(), baseline.browser);
  const transitions = [];
  try {
    for (const scheme of ['Light', 'Dark']) for (const reduced of [false, true]) {
      const page = await browser.newPage({viewport: {width: 640, height: 240}, reducedMotion: reduced ? 'reduce' : 'no-preference', deviceScaleFactor: 1});
      const errors = []; page.on('pageerror', error => errors.push(error.message));
      try {
        await page.setContent(`<!doctype html><html class="${scheme === 'Dark' ? 'dark' : ''}"><head><style>${css}body{margin:0}#root{position:absolute;left:20px;top:20px;width:480px}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
        await page.evaluate(async () => { await document.fonts.load('500 14px "Encode Sans"'); await document.fonts.ready; });
        const target = () => page.getByRole('tab', {name: 'Activity', exact: true});
        const read = () => target().evaluate(node => { const s = getComputedStyle(node); return {foreground: s.color, background: s.backgroundColor, border: s.borderBottomColor, shadow: s.boxShadow, transform: s.transform, translate: s.translate, active: node.getAttribute('aria-selected') === 'true'}; });
        const finish = () => page.evaluate(() => document.querySelector('#root>nav').getAnimations({subtree: true}).forEach(animation => animation.finish()));
        async function setup(variant, active, hover, pressed, id) {
          await page.mouse.up(); await page.mouse.move(0, 0);
          await page.evaluate(spec => window.showTabsMotion(spec), {id, variant, active});
          await target().waitFor(); await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
          if (hover) await target().hover(); if (pressed) await page.mouse.down(); await finish();
        }
        async function capture(variant, kind, trigger) {
          const before = await read(); await trigger();
          const timeline = await target().evaluate(async node => {
            const animations = node.getAnimations(); for (const animation of animations) animation.pause();
            await Promise.all(animations.map(animation => animation.ready));
            const definitions = animations.map(animation => ({property: animation.transitionProperty, timing: animation.effect.getTiming(), frames: animation.effect.getKeyframes()}));
            const samples = [];
            for (const milliseconds of [0, 47.5, 95, 142.5, 190, 220]) {
              for (const animation of animations) animation.currentTime = milliseconds;
              const s = getComputedStyle(node); samples.push({milliseconds, foreground: s.color, background: s.backgroundColor, border: s.borderBottomColor, shadow: s.boxShadow, transform: s.transform, translate: s.translate});
            }
            animations.forEach(animation => animation.finish()); return {definitions, samples};
          });
          transitions.push({variant, scheme, reduced, kind, before, ...timeline, after: await read()});
        }
        for (const variant of ['underline', 'segmented', 'enclosed']) {
          await setup(variant, false, false, false, variant + '-hover'); await capture(variant, 'hover', () => target().hover());
          await setup(variant, false, true, false, variant + '-leave'); await capture(variant, 'leave', () => page.mouse.move(0, 0));
          await setup(variant, false, false, false, variant + '-select'); await capture(variant, 'select', () => target().click());
          await setup(variant, true, false, false, variant + '-deselect'); await capture(variant, 'deselect', () => page.getByRole('tab', {name: 'Overview', exact: true}).click());
          await setup(variant, false, true, false, variant + '-press'); await capture(variant, 'press', () => page.mouse.down());
          await setup(variant, false, true, true, variant + '-release'); await capture(variant, 'release', () => page.mouse.up());
          for (const active of [false, true]) {
            await page.evaluate(dark => document.documentElement.classList.toggle('dark', dark), scheme === 'Dark');
            await setup(variant, active, false, false, variant + '-theme-' + active);
            await capture(variant, active ? 'theme-active' : 'theme-inactive', () => page.evaluate(() => document.documentElement.classList.toggle('dark')));
          }
          await page.evaluate(dark => document.documentElement.classList.toggle('dark', dark), scheme === 'Dark');
        }
        assert.deepEqual(errors, []);
      } finally { await page.close(); }
    }
  } finally { await browser.close(); }
  const reference = {format: 'orchard-tabs-motion-reference-v1', sourceCommit: baseline.sourceCommit, sourceSha256: baseline.sourceSha256, normalReferenceSha256: hash(baselineBytes), sourceCssSha256: hash(cssBytes), scriptSha256: hash(await fs.readFile(__filename)), clientSha256: hash(clientBytes), browser: baseline.browser, transitions};
  await fs.writeFile(path.join(output, 'reference.json'), JSON.stringify(reference, null, 2) + '\n'); console.log(JSON.stringify({transitions: transitions.length}));
})().catch(error => { console.error(error); process.exitCode = 1; });
