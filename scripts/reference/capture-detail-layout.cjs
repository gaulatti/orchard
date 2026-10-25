const fs = require('node:fs/promises'), path = require('node:path'), assert = require('node:assert/strict');
const crypto = require('node:crypto'), {execFileSync} = require('node:child_process');
const arg = name => {const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]);};
const source = arg('--source-root'), tooling = arg('--tooling-root'), output = arg('--output'), playwright = arg('--playwright-module');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex'), pin = 'a86059013cd59684ce452849278ea81e734f2066';
(async () => {
  await fs.mkdir(output, {recursive: true}); const file = 'src/layout/detail-layout.tsx', dependencies = [];
  for (const name of [file, 'src/utils/cn.ts', 'src/styles/global.css', 'src/styles/tokens.generated.css', 'package-lock.json']) {
    const bytes = await fs.readFile(path.join(source, name));
    assert.deepEqual(bytes, execFileSync('git', ['--git-dir', path.resolve(source, '../bleecker.git'), 'show', pin + ':' + name], {maxBuffer: 8 * 1024 * 1024})); dependencies.push({path: name, sha256: hash(bytes)});
  }
  assert.equal(dependencies[0].sha256, '98be08a0c16be98b8846f43afb4edca93e143d4f5857cf77511c92e2874c836d');
  const client = path.join(__dirname, 'detail-layout-client.tsx'), runtime = path.join(source, 'node_modules');
  const bundle = await require(path.join(tooling, 'node_modules/esbuild')).build({entryPoints: [client], alias: {'frozen/detail-layout': path.join(source, file), react: path.join(runtime, 'react'), 'react-dom': path.join(runtime, 'react-dom')}, bundle: true, write: false, platform: 'browser', jsx: 'automatic', nodePaths: [runtime], define: {'process.env.NODE_ENV': '"production"'}});
  const slash = value => value.replaceAll('\\', '/');
  const input = (await fs.readFile(path.join(source, 'src/styles/global.css'), 'utf8')).replace(/@import url\([^\r\n]*\);/g, '')
    .replace('@import "tailwindcss" source(none);', '@import "' + slash(path.join(tooling, 'node_modules/tailwindcss/index.css')) + '" source(none);')
    .replace('@import "./tokens.generated.css";', '@import "' + slash(path.join(source, 'src/styles/tokens.generated.css')) + '";')
    .replace('@source "../**/*.{ts,tsx}";', '@source "' + slash(path.join(source, 'src/**/*.{ts,tsx}')) + '";');
  await fs.writeFile(path.join(output, 'input.css'), input);
  execFileSync(process.execPath, [path.join(tooling, 'node_modules/@tailwindcss/cli/dist/index.mjs'), '-i', path.join(output, 'input.css'), '-o', path.join(output, 'source.css'), '--minify']);
  const css = await fs.readFile(path.join(output, 'source.css')), font = await fs.readFile('src/Orchard/Assets/Fonts/EncodeSans-Regular.ttf');
  const browser = await require(playwright).chromium.launch({headless: true, channel: 'msedge'}), cases = [], interactions = [];
  try {
    for (const scheme of ['Light', 'Dark']) {
      const page = await browser.newPage({viewport: {width: 1536, height: 680}, deviceScaleFactor: 1, reducedMotion: 'reduce'}), errors = [];
      page.on('pageerror', error => errors.push(error.message));
      try {
        await page.setContent(`<!doctype html><html class="${scheme === 'Dark' ? 'dark' : ''}"><head><style>${css}body{margin:0;overflow:hidden}@font-face{font-family:"Encode Sans";font-weight:400;src:url(data:font/ttf;base64,${font.toString('base64')})}#viewport::-webkit-scrollbar{display:none}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
        await page.evaluate(async () => {await document.fonts.load('400 16px "Encode Sans"'); await document.fonts.ready;});
        const settle = () => page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
        const measure = () => page.evaluate(() => {
          const layout = document.querySelector('[data-ratio]'), main = layout.querySelector('[data-slot="content"]'), rail = layout.querySelector('[data-slot="side-rail"]'), scroll = document.getElementById('viewport');
          const box = node => {if (!node) return null; const r = node.getBoundingClientRect(); return {x:r.x,y:r.y,width:r.width,height:r.height};};
          const style = getComputedStyle(layout);
          return {layout: box(layout), main: box(main), rail: box(rail), mainBody: box(layout.querySelector('[data-body="main"]')), railBody: box(layout.querySelector('[data-body="rail"]')),
            gap: parseFloat(style.columnGap), columns: style.gridTemplateColumns, label: rail?.getAttribute('aria-label') ?? null, railText: rail?.textContent ?? null,
            stickyPosition: rail ? getComputedStyle(rail).position : null, stickyTop: rail ? getComputedStyle(rail).top : null, scrollOffset: scroll?.scrollTop ?? 0, scrollViewport: box(scroll)};
        });
        const capture = async (spec, offset = 0) => {
          await page.setViewportSize({width:spec.viewport,height:680}); await page.evaluate(spec => window.showDetail(spec), spec); await settle();
          if (spec.scroll) {await page.evaluate(offset => {document.getElementById('viewport').scrollTop = offset;}, offset); await settle();}
          const row = {id: spec.id + (spec.scroll ? '-' + offset : ''), scheme, spec, measurement: await measure()};
          if (spec.png) {row.file = 'detail-layout-' + row.id + '-' + scheme + '.png'; row.pngSha256 = hash(await page.screenshot({path:path.join(output,row.file)}));}
          cases.push(row);
        };
        for (const viewport of [639,1023,1024,1279,1280,1536]) for (const width of [240,500,900.375]) for (const ratio of ['balanced','primary']) for (const side of ['start','end']) for (const sticky of [false,true]) for (const rail of [null,'box'])
          await capture({id:`${viewport}-${width}-${ratio}-${side}-${sticky}-${rail ?? 'none'}`,viewport,width,ratio,side,sticky,rail,
            png: viewport === 1280 && width === 900.375 && rail === 'box'});
        for (const [id,spec] of [
          ['defaults',{rail:'box'}],['empty-main',{empty:true,rail:'box'}],['false-rail',{rail:false}],['zero-rail',{rail:0}],['empty-rail',{rail:''}],['true-rail',{rail:true}],
          ['scalar-rail',{rail:'Related details'}],['blank-rail',{rail:'   '}],['named-rail',{rail:'box',label:'Related details'}],
          ['rtl',{rail:'box',rtl:true,side:'start'}],['fixed-height',{rail:'box',layoutHeight:600,mainHeight:160}],['tall-rail',{rail:'box',railHeight:500,mainHeight:100}]
        ]) await capture({id,viewport:1280,width:900.375,...spec,png: !['scalar-rail','blank-rail'].includes(id)});
        for (const viewport of [1023,1024,1280]) for (const ratio of ['balanced','primary']) for (const side of ['start','end']) for (const sticky of [false,true])
          for (const offset of [0,80,160,400,600,880]) await capture({id:`scroll-${viewport}-${ratio}-${side}-${sticky}`,viewport,width:500,ratio,side,sticky,rail:'box',scroll:true,
            png: viewport === 1280 && ratio === 'primary' && side === 'end' && sticky},offset);
        await page.evaluate(spec => window.showDetail(spec), {id:'live',key:'live',viewport:1280,width:900,rail:'box',live:true}); await settle();
        await page.locator('[data-input="main"]').fill('Changed main'); await page.locator('[data-input="rail"]').fill('Changed rail');
        await page.evaluate(() => {window.originalMain=document.querySelector('[data-input="main"]');window.originalRail=document.querySelector('[data-input="rail"]');window.updateDetail({side:'start',ratio:'balanced',sticky:false});}); await settle();
        const retained = await page.evaluate(() => ({main:window.originalMain===document.querySelector('[data-input="main"]'),rail:window.originalRail===document.querySelector('[data-input="rail"]'),mainValue:document.querySelector('[data-input="main"]').value,railValue:document.querySelector('[data-input="rail"]').value}));
        await page.locator('[data-input="main"]').focus(); await page.keyboard.press('Tab');
        const focusOrder = await page.evaluate(() => document.activeElement===document.querySelector('[data-input="rail"]'));
        interactions.push({scheme,retained,mainThenRailTabOrder:focusOrder}); assert.deepEqual(errors,[]);
      } finally {await page.close();}
    }
  } finally {await browser.close();}
  await fs.writeFile(path.join(output,'reference.json'),JSON.stringify({format:'orchard-detail-layout-reference-v1',repository:'gaulatti/bleecker',commit:pin,source:file,sourceSha256:dependencies[0].sha256,
    dependencies,scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(client)),cssSha256:hash(css),fontSha256:hash(font),browser:browser.version(),cases,interactions},null,2)+'\n');
  console.log(JSON.stringify({cases:cases.length,pngs:cases.filter(row=>row.file).length}));
})().catch(error=>{console.error(error);process.exitCode=1;});
