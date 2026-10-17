const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});const source=path.resolve('artifacts/line-chart-browser/frozen-source/src/components/input.tsx'),runtime=path.resolve('artifacts/line-chart-browser/node_modules');
 assert.equal(hash(await fs.readFile(source)),'01bce586edd4c1ad4aed30415b5aa9cbc6e620a874e4751cf7299bfa54ccc192');
 const normal=JSON.parse(await fs.readFile('tests/Reference/SearchInput.json')),lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes),installed=JSON.parse(await fs.readFile('artifacts/line-chart-browser/package-lock.json'));
 for(const[key,value]of Object.entries(installed.packages))if(key){assert.equal(value.version,lock.packages[key]?.version);assert.equal(value.integrity,lock.packages[key]?.integrity);}
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const bundle=await require(esbuildPath).build({alias:{react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},stdin:{contents:`import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{Input}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));let key=0;window.show=(state,fresh=false)=>{if(fresh)key++;flushSync(()=>root.render(<Input key={key} aria-label='Paint reference' value='Culture' onChange={()=>{}} disabled={state.includes('disabled')} error={state.includes('error')}/>));};`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile('artifacts/line-chart-browser/styles.css');assert.equal(hash(cssBytes),normal.cssSha256);let css=cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'');
 for(const font of normal.fontFiles){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+font.file);assert.equal(hash(bytes),font.sha256);css+=`@font-face{font-family:'Encode Sans';font-weight:${font.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const pairs=[['normal','disabled'],['disabled','normal'],['error','disabled-error'],['disabled-error','error'],['normal','error'],['error','normal'],['normal','hover'],['hover','normal'],['normal','focus'],['focus','normal']];
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),normal.browser);const transitions=[];
 try{for(const scheme of['Light','Dark'])for(const reduced of[false,true]){
  const page=await browser.newPage({viewport:{width:640,height:480},deviceScaleFactor:1,reducedMotion:reduced?'reduce':'no-preference'});const errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{
   await page.setContent(`<html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0;background:${scheme==='Dark'?'#0D1821':'white'}}#root{position:absolute;left:20px;top:20px;width:600px}</style></head><body><div id='root'></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(async()=>{await document.fonts.load('400 14px "Encode Sans"');await document.fonts.ready;});
   const read=(includeAnimations=false)=>page.locator('input').evaluate((input,includeAnimations)=>{const s=getComputedStyle(input);const rgba=value=>{const c=document.createElement('canvas');c.width=c.height=1;const ctx=c.getContext('2d');ctx.fillStyle=value;ctx.fillRect(0,0,1,1);return[...ctx.getImageData(0,0,1,1).data];};return{background:rgba(s.backgroundColor),border:rgba(s.borderTopColor),opacity:Number(s.opacity),rawBackground:s.backgroundColor,rawBorder:s.borderTopColor,...(includeAnimations?{animations:input.getAnimations().map(a=>({property:a.transitionProperty,timing:a.effect.getTiming(),frames:a.effect.getKeyframes()}))}:{})};},includeAnimations);
   const change=async(state,fresh)=>{await page.mouse.move(0,0);await page.locator('input').evaluate(input=>input.blur());await page.evaluate(p=>window.show(p.state,p.fresh),{state,fresh});if(state==='hover')await page.locator('input').hover();if(state==='focus')await page.locator('input').focus();await page.locator('input').evaluate(input=>void getComputedStyle(input).backgroundColor);};
   await page.evaluate(()=>window.show('normal',true));
   for(const[from,to]of pairs){
    await change(from,true);await page.locator('input').evaluate(input=>input.getAnimations().forEach(a=>a.finish()));const before=await read();await change(to,false);
    await page.locator('input').evaluate(async input=>{window.transitions=input.getAnimations();for(const a of window.transitions)a.pause();await Promise.all(window.transitions.map(a=>a.ready));});
    const definition=await read(true),samples=[];for(const milliseconds of[0,47.5,95,142.5,190]){await page.evaluate(t=>{for(const a of window.transitions)a.currentTime=t;},milliseconds);samples.push({milliseconds,...await read()});}
    await page.evaluate(()=>window.transitions.forEach(a=>a.finish()));transitions.push({scheme,reduced,from,to,before,definition,samples,after:await read()});
   }
   assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-input-paint-motion-v1',sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256:hash(await fs.readFile(source)),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),cssSha256:hash(cssBytes),frozenLockSha256:hash(lockBytes),browser:normal.browser,transitions};await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({transitions:transitions.length}));
})().catch(error=>{console.error(error);process.exitCode=1});
