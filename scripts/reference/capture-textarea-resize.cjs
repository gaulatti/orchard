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
  const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});
  assert.equal(browser.version(),'155.0.4283.45');const cases=[];
  try{
    for(const width of[320,640])for(const scheme of['Light','Dark']){
      const page=await browser.newPage({viewport:{width,height:600},deviceScaleFactor:1,reducedMotion:'reduce'});
      const errors=[];page.on('pageerror',e=>errors.push(e.message));
      try{
        await page.setContent(`<html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0;background:${scheme==='Dark'?'#0D1821':'white'}}#root{position:absolute;left:20px;top:20px;width:${width-40}px}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text};window.events=[];for(const type of['pointerdown','pointermove','pointerup'])document.addEventListener(type,e=>window.events.push({type,trusted:e.isTrusted,x:e.clientX,y:e.clientY,buttons:e.buttons}),true);</script></body></html>`);
        await page.evaluate(async()=>{await document.fonts.load('14px "Encode Sans"');await document.fonts.ready;});
        const measure=async()=>page.evaluate(()=>{const e=document.querySelector('textarea'),b=e.getBoundingClientRect(),s=getComputedStyle(e);return{x:b.x,y:b.y,width:b.width,height:b.height,value:e.value,disabled:e.disabled,readOnly:e.readOnly,focused:document.activeElement===e,minHeight:parseFloat(s.minHeight),direction:s.direction,resize:s.resize,cursor:s.cursor};});
        const settle=async()=>page.evaluate(async()=>{for(const a of document.getAnimations())a.finish();await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));});
        for(const size of['sm','md','lg'])for(const mode of['editable','readonly','disabled'])for(const dir of['ltr','rtl']){
          const id=`${size}-${mode}-${dir}`;await page.mouse.move(0,0);
          await page.evaluate(p=>{window.events=[];window.show(p);},{id,textareaSize:size,dir,defaultValue:'Retained notes\nSecond line',disabled:mode==='disabled',readOnly:mode==='readonly'});await settle();
          const initial=await measure(),edgeX=dir==='rtl'?initial.x+3:initial.x+initial.width-3,edgeY=initial.y+initial.height-3;
          await page.mouse.move(edgeX,edgeY);await page.mouse.down();await page.mouse.move(edgeX+40,edgeY+48,{steps:8});await settle();const enlargedHeld=await measure();await page.mouse.up();
          assert.equal(enlargedHeld.width,initial.width);assert.equal(enlargedHeld.height,initial.height+48,`${id}: resize must work even when read-only/disabled.`);
          const x=dir==='rtl'?enlargedHeld.x+3:enlargedHeld.x+enlargedHeld.width-3,y=enlargedHeld.y+enlargedHeld.height-3;
          await page.mouse.move(x,y);await page.mouse.down();await page.mouse.move(x,y-240,{steps:8});await settle();const minimumHeld=await measure();
          await page.mouse.move(x,y-220,{steps:4});await settle();const reverseAboveMinimum=await measure();await page.mouse.up();
          assert.equal(minimumHeld.height,initial.minHeight);assert.equal(reverseAboveMinimum.height,initial.minHeight,'Reversal must retain the source pointer anchor while above its minimum.');
          assert.equal((await measure()).value,initial.value);
          const events=await page.evaluate(()=>window.events);assert(events.length>4&&events.every(e=>e.trusted));
          const file=`textarea-resize-${id}-${width}-${scheme}.png`;await page.screenshot({path:path.join(output,file)});
          cases.push({id,size,mode,dir,width,scheme,initial,enlargedHeld,minimumHeld,reverseAboveMinimum,events,file,pngSha256:hash(await fs.readFile(path.join(output,file)))});
        }
        assert.deepEqual(errors,[]);
      }finally{await page.close();}
    }
  }finally{await browser.close();}
  const report={format:'orchard-textarea-resize-reference-v1',sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256:hash(sourceBytes),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),frozenLockSha256:hash(lockBytes),cssSha256:hash(cssBytes),fontFiles:fonts,browser:browser.version(),playwright:'1.62.1',esbuild:'0.25.12',freshMountPerCase:true,cases};
  await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,disabledResizes:cases.filter(c=>c.mode==='disabled').length,readonlyResizes:cases.filter(c=>c.mode==='readonly').length}));
})().catch(error=>{console.error(error);process.exitCode=1;});
