const fs = require('node:fs/promises'), path = require('node:path'), assert = require('node:assert/strict');
const crypto = require('node:crypto'), {execFileSync} = require('node:child_process');
const arg = name => {const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]);};
const source = arg('--source-root'), tooling = arg('--tooling-root'), output = arg('--output'), playwright = arg('--playwright-module');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex'), pin = 'a86059013cd59684ce452849278ea81e734f2066';
(async () => {
  await fs.mkdir(output, {recursive: true});
  const file = 'src/components/field.tsx', sourceBytes = await fs.readFile(path.join(source, file));
  assert.equal(hash(sourceBytes), '552df290321007582f3605c1bc4538ad5664ed267dee1bd22cb45bac203ec11f');
  const dependencies = [];
  for (const name of [file, 'src/components/input.tsx', 'src/components/button.tsx', 'src/utils/cn.ts', 'src/styles/global.css', 'src/styles/tokens.generated.css', 'package-lock.json']) {
    const bytes = await fs.readFile(path.join(source, name));
    assert.deepEqual(bytes, execFileSync('git', ['--git-dir', path.resolve(source, '../bleecker.git'), 'show', pin + ':' + name], {maxBuffer: 8 * 1024 * 1024}));
    dependencies.push({path: name, sha256: hash(bytes)});
  }
  const runtime = path.join(source, 'node_modules'), esbuild = require(path.join(tooling, 'node_modules/esbuild')), client = path.join(__dirname, 'field-client.tsx');
  const bundle = await esbuild.build({entryPoints: [client], alias: {'frozen/field': path.join(source, file), 'frozen/input': path.join(source, 'src/components/input.tsx'), 'frozen/button': path.join(source, 'src/components/button.tsx'), react: path.join(runtime, 'react'), 'react-dom': path.join(runtime, 'react-dom')}, bundle: true, write: false, platform: 'browser', jsx: 'automatic', nodePaths: [runtime], define: {'process.env.NODE_ENV': '"production"'}});
  const slash = value => value.replaceAll('\\', '/');
  const input = (await fs.readFile(path.join(source, 'src/styles/global.css'), 'utf8')).replace(/@import url\([^\r\n]*\);/g, '')
    .replace('@import "tailwindcss" source(none);', '@import "' + slash(path.join(tooling, 'node_modules/tailwindcss/index.css')) + '" source(none);')
    .replace('@import "./tokens.generated.css";', '@import "' + slash(path.join(source, 'src/styles/tokens.generated.css')) + '";')
    .replace('@source "../**/*.{ts,tsx}";', '@source "' + slash(path.join(source, 'src/**/*.{ts,tsx}')) + '";');
  await fs.writeFile(path.join(output, 'input.css'), input);
  execFileSync(process.execPath, [path.join(tooling, 'node_modules/@tailwindcss/cli/dist/index.mjs'), '-i', path.join(output, 'input.css'), '-o', path.join(output, 'source.css'), '--minify']);
  const cssBytes = await fs.readFile(path.join(output, 'source.css')); let css = cssBytes.toString(); const fonts = [];
  for (const [family, weight, name] of [['Encode Sans', 400, 'EncodeSans-Regular'], ['Encode Sans', 500, 'EncodeSans-Medium'], ['Encode Sans', 600, 'EncodeSans-SemiBold'], ['Libre Franklin', 400, 'LibreFranklin-Regular']]) {
    const bytes = await fs.readFile('src/Orchard/Assets/Fonts/' + name + '.ttf'); fonts.push({name, sha256: hash(bytes)});
    css += '@font-face{font-family:"' + family + '";font-weight:' + weight + ';src:url(data:font/ttf;base64,' + bytes.toString('base64') + ')}';
  }
  const definitions = [
    {id: 'normal'}, {id: 'required', required: true}, {id: 'optional', optional: true}, {id: 'both-flags', required: true, optional: true},
    {id: 'action', action: 'button', optional: true}, {id: 'action-false', action: false, optional: true}, {id: 'action-zero', action: 0, optional: true},
    {id: 'description', description: 'Use a concise title.'}, {id: 'error', error: 'A title is needed.'},
    {id: 'messages', description: 'A useful public title.', error: 'Choose a different title.'},
    {id: 'existing-metadata', controlId: 'consumer-control', describedBy: 'external-help', invalid: false, description: 'Field description.', error: 'Field error.'},
    {id: 'empty-id', controlId: ''}, {id: 'child-invalid', invalid: true}, {id: 'child-required', childRequired: true},
    {id: 'disabled', disabled: true, required: true, description: 'The child owns disabled state.'},
    {id: 'empty-label', label: ''}, {id: 'rich-label', rich: true, required: true},
    {id: 'unicode', label: '東京 · العربية', description: 'Consumer 日本語 help.'},
    {id: 'narrow', width: 160, label: 'A longer coverage display name', required: true, optional: true, description: 'A useful description wraps at the available width.', error: 'The consumer supplies this longer error.'},
    {id: 'narrow-action', width: 160, label: 'A longer coverage title', action: 'button'},
    {id: 'false-messages', description: 0, error: false}, {id: 'true-messages', description: true, error: true}
  ];
  const browser = await require(playwright).chromium.launch({headless: true, channel: 'msedge'}), cases = [], interactions = [];
  try {
    for (const scheme of ['Light', 'Dark']) {
      const page = await browser.newPage({viewport: {width: 640, height: 360}, deviceScaleFactor: 1, reducedMotion: 'reduce'});
      try {
        const errors = []; page.on('pageerror', error => errors.push(error.message));
        await page.setContent(`<!doctype html><html class="${scheme === 'Dark' ? 'dark' : ''}"><head><style>${css}body{margin:0}#root{position:absolute;left:20px;top:20px}</style></head><body><div id="root"></div><span id="external-help" hidden>Consumer description</span><script>${bundle.outputFiles[0].text}</script></body></html>`);
        await page.evaluate(async () => {for (const font of ['500 13px "Encode Sans"', '400 12px "Encode Sans"', '400 12px "Libre Franklin"']) await document.fonts.load(font); await document.fonts.ready;});
        const settle = () => page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
        const measure = () => page.evaluate(() => {
          const root = document.querySelector('[data-field-fixture]'), input = root.querySelector('input'), label = root.querySelector('label'), header = label.parentElement;
          const item = node => {if (!node) return null; const s = getComputedStyle(node), b = node.getBoundingClientRect(); return {box: {x: b.x, y: b.y, width: b.width, height: b.height}, text: node.textContent, foreground: s.color, fontFamily: s.fontFamily, fontSize: s.fontSize, fontWeight: s.fontWeight, lineHeight: s.lineHeight, letterSpacing: s.letterSpacing};};
          const paragraphs = [...root.querySelectorAll(':scope>p')], description = paragraphs.find(node => node.id.endsWith('-description')), error = paragraphs.find(node => node.id.endsWith('-error'));
          const describedBy = input.getAttribute('aria-describedby')?.split(' ').filter(Boolean).map(id => id === description?.id ? 'description' : id === error?.id ? 'error' : id) ?? [];
          return {root: item(root), header: item(header), label: item(label), required: item(label.querySelector('[aria-hidden]')), right: item(header.children[1]), description: item(description), error: item(error), control: item(input),
            controlId: input.id, labelFor: label.htmlFor, describedBy, invalid: input.getAttribute('aria-invalid'), nativeRequired: input.required, disabled: input.disabled,
            value: input.value, inputBackground: getComputedStyle(input).backgroundColor, inputBorder: getComputedStyle(input).borderColor};
        });
        for (const spec of definitions) {
          await page.evaluate(spec => window.showField(spec), spec); await settle();
          const file = `field-${spec.id}-${scheme}.png`, png = await page.screenshot({path: path.join(output, file)});
          let strongFonts;
          if (spec.rich) {
            const cdp = await page.context().newCDPSession(page); await cdp.send('DOM.enable'); await cdp.send('CSS.enable');
            const {root} = await cdp.send('DOM.getDocument'); const {nodeId} = await cdp.send('DOM.querySelector', {nodeId: root.nodeId, selector: '[data-field-fixture] strong'});
            strongFonts = (await cdp.send('CSS.getPlatformFontsForNode', {nodeId})).fonts; await cdp.detach();
          }
          cases.push({id: spec.id, scheme, spec, measurement: await measure(), file, pngSha256: hash(png), strongFonts});
        }
        const live = {id: 'live', key: 'live', describedBy: 'external-help', invalid: false};
        await page.evaluate(spec => window.showField(spec), live); await settle();
        await page.locator('label').click(); const labelFocused = await page.locator('input').evaluate(node => node === document.activeElement);
        await page.locator('input').fill('Consumer edit'); const initial = await measure();
        await page.evaluate(() => window.savedFieldControl = document.querySelector('input'));
        await page.evaluate(spec => window.showField(spec), {...live, required: true, description: 'Live description.', error: 'Live error.'}); await settle(); const added = await measure();
        const identityRetained = await page.evaluate(() => window.savedFieldControl === document.querySelector('input'));
        await page.evaluate(spec => window.showField(spec), {...live, description: 'Live description.'}); await settle(); const removedError = await measure();
        await page.evaluate(spec => window.showField(spec), {...live, optional: true, action: 'button'}); await settle();
        await page.getByRole('button', {name: 'Reset', exact: true}).click(); const removedMessages = await measure();
        interactions.push({scheme, labelFocused, identityRetained, initial, added, removedError, removedMessages, events: await page.evaluate(() => [...window.fieldEvents])});
        assert.deepEqual(errors, []);
      } finally {await page.close();}
    }
    await fs.writeFile(path.join(output, 'reference.json'), JSON.stringify({format: 'orchard-field-reference-v1', repository: 'gaulatti/bleecker', commit: pin, source: file, sourceSha256: hash(sourceBytes), dependencies, fonts, scriptSha256: hash(await fs.readFile(__filename)), clientSha256: hash(await fs.readFile(client)), cssSha256: hash(cssBytes), browser: browser.version(), cases, interactions}, null, 2) + '\n');
    console.log(JSON.stringify({passed: true, cases: cases.length, interactions: interactions.length}));
  } finally {await browser.close();}
})().catch(error => {console.error(error); process.exitCode = 1;});
