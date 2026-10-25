const fs = require('node:fs/promises'), path = require('node:path'), assert = require('node:assert/strict');
const crypto = require('node:crypto'), {execFileSync} = require('node:child_process');
const arg = name => { const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]); };
const source = arg('--source-root'), tooling = arg('--tooling-root'), output = arg('--output'), playwright = arg('--playwright-module');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const pin = 'a86059013cd59684ce452849278ea81e734f2066';
(async () => {
  await fs.mkdir(output, {recursive: true});
  const file = 'src/components/dialog-close-button.tsx';
  const sourceBytes = await fs.readFile(path.join(source, file));
  assert.equal(hash(sourceBytes), 'a1614f004198702d1325af4dac2e2ab18fe6cf227574df2b1caf22a2e341c7e8');
  for (const name of [file, 'src/utils/cn.ts', 'src/styles/global.css', 'src/styles/tokens.generated.css', 'package-lock.json'])
    assert.deepEqual(await fs.readFile(path.join(source, name)), execFileSync('git', ['--git-dir', path.resolve(source, '../bleecker.git'), 'show', pin + ':' + name], {maxBuffer: 8 * 1024 * 1024}));
  const runtime = path.join(source, 'node_modules'), esbuild = require(path.join(tooling, 'node_modules/esbuild'));
  const client = path.join(__dirname, 'dialog-close-button-client.tsx');
  const bundle = await esbuild.build({entryPoints: [client], alias: {'frozen/dialog-close-button': path.join(source, file), react: path.join(runtime, 'react'), 'react-dom': path.join(runtime, 'react-dom')}, bundle: true, write: false, platform: 'browser', jsx: 'automatic', nodePaths: [runtime], define: {'process.env.NODE_ENV': '"production"'}});
  const slash = value => value.replaceAll('\\', '/');
  const input = (await fs.readFile(path.join(source, 'src/styles/global.css'), 'utf8')).replace(/@import url\([^\r\n]*\);/g, '')
    .replace('@import "tailwindcss" source(none);', '@import "' + slash(path.join(tooling, 'node_modules/tailwindcss/index.css')) + '" source(none);')
    .replace('@import "./tokens.generated.css";', '@import "' + slash(path.join(source, 'src/styles/tokens.generated.css')) + '";')
    .replace('@source "../**/*.{ts,tsx}";', '@source "' + slash(path.join(source, 'src/**/*.{ts,tsx}')) + '";');
  await fs.writeFile(path.join(output, 'input.css'), input);
  execFileSync(process.execPath, [path.join(tooling, 'node_modules/@tailwindcss/cli/dist/index.mjs'), '-i', path.join(output, 'input.css'), '-o', path.join(output, 'source.css'), '--minify']);
  const cssBytes = await fs.readFile(path.join(output, 'source.css'));
  const browser = await require(playwright).chromium.launch({headless: true, channel: 'msedge'}), cases = [], transitions = [], interactions = [];
  try {
    for (const scheme of ['Light', 'Dark']) for (const reduced of [false, true]) {
      const page = await browser.newPage({viewport: {width: 200, height: 120}, deviceScaleFactor: 1, reducedMotion: reduced ? 'reduce' : 'no-preference'});
      try {
        const errors = []; page.on('pageerror', error => errors.push(error.message));
        await page.setContent(`<!doctype html><html class="${scheme === 'Dark' ? 'dark' : ''}"><head><style>${cssBytes.toString()}body{margin:0}#root{position:absolute;left:20px;top:20px}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
        const target = () => page.getByRole('button', {name: 'Close preview', exact: true});
        const settle = () => page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
        const finish = () => page.evaluate(() => document.querySelector('#root>button').getAnimations().forEach(animation => animation.finish()));
        const read = () => target().evaluate(node => {
          const s = getComputedStyle(node), b = node.getBoundingClientRect(), svg = node.querySelector('svg'), g = svg.getBoundingClientRect();
          return {box: {x: b.x, y: b.y, width: b.width, height: b.height}, icon: {x: g.x, y: g.y, width: g.width, height: g.height}, disabled: node.disabled,
            foreground: s.color, background: s.backgroundColor, border: s.borderColor, radius: s.borderRadius, borderWidth: s.borderWidth, opacity: s.opacity,
            shadow: s.boxShadow, translate: s.translate, scale: s.scale, transform: s.transform, focusVisible: node.matches(':focus-visible'),
            strokeWidth: svg.getAttribute('stroke-width'), paths: [...svg.querySelectorAll('path')].map(path => path.getAttribute('d'))};
        });
        async function show(spec) { await page.mouse.up(); await page.mouse.move(0, 0); await page.evaluate(spec => window.showCloseButton(spec), spec); await target().waitFor(); await settle(); await finish(); }
        if (reduced) {
          for (const [id, iconSize, disabled] of [['normal', undefined, false], ['icon-0', 0, false], ['icon-24', 24, false], ['disabled', undefined, true]]) {
            const spec = {id, iconSize, disabled}; await show(spec);
            const filename = `dialog-close-${id}-${scheme}.png`, png = await page.screenshot({path: path.join(output, filename)});
            cases.push({id, scheme, spec, measurement: await read(), file: filename, pngSha256: hash(png)});
          }
          for (const state of ['hover', 'pressed', 'focus', 'keyboard-pressed']) {
            await show({id: state});
            if (state === 'focus' || state === 'keyboard-pressed') {
              await page.keyboard.press('Tab');
              if (state === 'keyboard-pressed') await page.keyboard.down('Space');
            }
            else { await target().hover(); if (state === 'pressed') await page.mouse.down(); }
            await finish();
            const filename = `dialog-close-${state}-${scheme}.png`, png = await page.screenshot({path: path.join(output, filename)});
            cases.push({id: state, scheme, spec: {id: state}, measurement: await read(), file: filename, pngSha256: hash(png)});
            if (state === 'keyboard-pressed') await page.keyboard.up('Space');
          }
          await show({id: 'consumer-request'}); await page.evaluate(() => window.closeRequests = []);
          await target().click(); await page.keyboard.press('Space'); await page.keyboard.press('Enter');
          const accepted = await page.evaluate(() => [...window.closeRequests]);
          await show({id: 'disabled-request', disabled: true}); await page.mouse.click(38, 38);
          interactions.push({scheme, requests: accepted, afterDisabled: await page.evaluate(() => [...window.closeRequests]), remainsMounted: await target().count() === 1});
        }
        for (const kind of ['hover', 'leave', 'press', 'release', 'theme']) {
          await show({id: kind});
          if (kind === 'leave' || kind === 'press' || kind === 'release') await target().hover();
          if (kind === 'release') await page.mouse.down(); await finish();
          const before = await read();
          if (kind === 'hover') await target().hover();
          if (kind === 'leave') await page.mouse.move(0, 0);
          if (kind === 'press') await page.mouse.down();
          if (kind === 'release') await page.mouse.up();
          if (kind === 'theme') await page.evaluate(() => document.documentElement.classList.toggle('dark'));
          const timeline = await target().evaluate(async node => {
            const animations = node.getAnimations(); animations.forEach(animation => animation.pause()); await Promise.all(animations.map(animation => animation.ready));
            const definitions = animations.map(animation => ({property: animation.transitionProperty, timing: animation.effect.getTiming(), frames: animation.effect.getKeyframes()})), samples = [];
            for (const milliseconds of [0, 47.5, 95, 142.5, 190, 220]) {
              animations.forEach(animation => animation.currentTime = milliseconds); const s = getComputedStyle(node);
              samples.push({milliseconds, foreground: s.color, background: s.backgroundColor, border: s.borderColor, shadow: s.boxShadow, translate: s.translate, scale: s.scale});
            }
            animations.forEach(animation => animation.finish()); return {definitions, samples};
          });
          transitions.push({kind, scheme, reduced, before, ...timeline, after: await read()});
          await page.evaluate(dark => document.documentElement.classList.toggle('dark', dark), scheme === 'Dark');
        }
        assert.deepEqual(errors, []);
      } finally { await page.close(); }
    }
    await fs.writeFile(path.join(output, 'reference.json'), JSON.stringify({repository: 'gaulatti/bleecker', commit: pin, source: file, sourceSha256: hash(sourceBytes), scriptSha256: hash(await fs.readFile(__filename)), clientSha256: hash(await fs.readFile(client)), cssSha256: hash(cssBytes), browser: browser.version(), cases, transitions, interactions}, null, 2) + '\n');
    console.log(JSON.stringify({passed: true, cases: cases.length, transitions: transitions.length, interactions: interactions.length}));
  } finally { await browser.close(); }
})().catch(error => {console.error(error); process.exitCode = 1;});
