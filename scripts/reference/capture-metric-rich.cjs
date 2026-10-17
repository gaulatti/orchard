const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});const base=path.resolve('artifacts/line-chart-browser'),source=path.join(base,'frozen-source/src/components/metric.tsx'),runtime=path.join(base,'node_modules');
 assert.equal(hash(await fs.readFile(source)),'2aeb702dc6a115a9b24ff62842d564a9fcd92f1069925b3e07929532daca47c7');
 const lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes),installed=JSON.parse(await fs.readFile(path.join(base,'package-lock.json')));
 for(const[key,value]of Object.entries(installed.packages))if(key){assert.equal(value.version,lock.packages[key]?.version);assert.equal(value.integrity,lock.packages[key]?.integrity);}
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const bundle=await require(esbuildPath).build({alias:{react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},stdin:{contents:`import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{Metric}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));window.clicks=0;window.show=p=>flushSync(()=>root.render(<Metric value={p.value} format={p.format} currency={p.currency} decimals={p.decimals} prefix={p.prefix??<span id='prefix'>Reviewed: </span>} suffix={<button id='details' onClick={()=>window.clicks++}>Details</button>}/>));`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile(path.join(base,'styles.css')),font=await fs.readFile('src/Orchard/Assets/Fonts/EncodeSans-Regular.ttf');
 const css=cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'')+`@font-face{font-family:'Encode Sans';font-weight:400;src:url(data:font/ttf;base64,${font.toString('base64')})}html,body{background:var(--color-background)}body{margin:0;font-family:'Encode Sans';font-size:16px;line-height:24px}#root{margin:20px;color:var(--color-foreground)}`;
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),'155.0.4283.45');const cases=[];
 try{for(const width of[320,640])for(const scheme of['Light','Dark']){
  const page=await browser.newPage({locale:'en-US',viewport:{width,height:160},deviceScaleFactor:1,reducedMotion:'reduce'});
  try{await page.setContent(`<html class='${scheme==='Dark'?'dark':''}'><head><style>${css}</style></head><body><div id='root'></div><button id='outside'>Outside</button><script>${bundle.outputFiles[0].text};window.focusEvents=[];for(const type of['focusin','focusout'])document.addEventListener(type,e=>window.focusEvents.push({type,id:e.target.id,trusted:e.isTrusted}));window.lifecycle=[];new MutationObserver(records=>{for(const r of records)for(const node of [...r.removedNodes,...r.addedNodes])if(node.id==='details')window.lifecycle.push(node.id);}).observe(document.getElementById('root'),{childList:true,subtree:true});</script></body></html>`);
   await page.evaluate(async()=>{await document.fonts.load('400 16px "Encode Sans"');await document.fonts.ready;window.show({value:12});});
   await page.keyboard.press('Tab');assert.equal(await page.evaluate(()=>document.activeElement.id),'details');await page.keyboard.press('Space');assert.equal(await page.evaluate(()=>window.clicks),1);
   await page.evaluate(()=>{window.focusEvents=[];window.lifecycle=[];window.originalDetails=document.getElementById('details');});
   const operations=[{id:'value',props:{value:13}},{id:'batch',props:{value:40}},{id:'format',props:{value:40,format:'currency',currency:'EUR',decimals:2}},{id:'prefix',props:{value:40,prefix:'Updated '}}];
   for(const operation of operations){await page.evaluate(p=>{if(p.id==='batch')for(let value=14;value<40;value++)window.show({value});window.show(p.props);},operation);
    await page.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r))));
    const state=await page.evaluate(()=>{const e=document.getElementById('details'),b=e.getBoundingClientRect();return{focused:document.activeElement===e,sameNode:e===window.originalDetails,events:[...window.focusEvents],lifecycle:[...window.lifecycle],text:document.querySelector('#root>span').textContent,button:{x:b.x,y:b.y,width:b.width,height:b.height}};});
    assert(state.focused&&state.sameNode);assert.deepEqual(state.events,[]);assert.deepEqual(state.lifecycle,[]);cases.push({width,scheme,id:operation.id,state});
   }
   await page.keyboard.press('Tab');assert.equal(await page.evaluate(()=>document.activeElement.id),'outside');await page.evaluate(()=>window.show({value:41}));assert.equal(await page.evaluate(()=>document.activeElement.id),'outside');
   cases.push({width,scheme,id:'outside-focus',state:{outsideFocusRetained:true}});
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-metric-rich-v1',sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256:hash(await fs.readFile(source)),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),cssSha256:hash(cssBytes),fontSha256:hash(font),frozenLockSha256:hash(lockBytes),browser:browser.version(),cases};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,focusOrLifecycleEvents:0}));
})().catch(e=>{console.error(e);process.exitCode=1});
