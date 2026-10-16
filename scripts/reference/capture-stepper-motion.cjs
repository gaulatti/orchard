const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const option=name=>{const i=process.argv.indexOf(name);assert.ok(i>=0&&process.argv[i+1],`Pass ${name} <path>.`);return path.resolve(process.argv[i+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module');
const hash=value=>crypto.createHash('sha256').update(value).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const source=path.resolve('artifacts/line-chart-browser/frozen-source/src/components/stepper.tsx'),sourceBytes=await fs.readFile(source);
 assert.equal(hash(sourceBytes),'a0cea6e7cc8fcc541d0089682d64ed8589fc4983a482095785477cb01d646c29');
 const normalBytes=await fs.readFile('tests/Reference/Stepper.json'),normal=JSON.parse(normalBytes);
 const runtime=path.resolve('artifacts/line-chart-browser/node_modules'),icons=path.resolve('artifacts/error-state-browser/node_modules');
 const lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes);
 for(const directory of [runtime,icons]){
  const installed=JSON.parse(await fs.readFile(path.join(directory,'../package-lock.json')));
  for(const name of ['react','react-dom','lucide-react'])if(installed.packages['node_modules/'+name]){
   const actual=installed.packages['node_modules/'+name],expected=lock.packages['node_modules/'+name];assert.equal(actual.version,expected.version);assert.equal(actual.integrity,expected.integrity);
  }
 }
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const bundle=await require(esbuildPath).build({stdin:{contents:`import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{Stepper}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));window.invocations=[];window.show=(status,interactive=true)=>flushSync(()=>root.render(<Stepper steps={[{id:'stage',label:'Draft',description:'Write a story',status}]} onStepClick={interactive?step=>window.invocations.push(step.id):undefined}/>));`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',nodePaths:[runtime,icons],alias:{react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile('artifacts/line-chart-browser/styles.css');assert.equal(hash(cssBytes),normal.cssSha256);
 let fonts='';for(const {file,weight,sha256}of normal.fontFiles){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);assert.equal(hash(bytes),sha256);fonts+=`@font-face{font-family:'Encode Sans';font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),normal.browser);
 const transitions=[],presses=[],availability=[];
 try{for(const reduced of [false,true])for(const scheme of ['Light','Dark']){
  const page=await browser.newPage({viewport:{width:1024,height:745},deviceScaleFactor:1,reducedMotion:reduced?'reduce':'no-preference'});page.setDefaultTimeout(2000);
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{
   await page.setContent(`<html class="${scheme==='Dark'?'dark':''}"><head><style>${cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'')}${fonts}body{padding:20px;margin:0}#root{width:640px}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(()=>window.show('pending'));await page.waitForFunction(()=>document.querySelector('#root button'));
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(w=>document.fonts.load(w+' 14px "Encode Sans"')));});
   const settle=()=>page.evaluate(()=>new Promise(resolve=>{let n=0;const tick=()=>++n>=3?resolve():requestAnimationFrame(tick);requestAnimationFrame(tick)}));
   const read=()=>page.locator('#root button').evaluate(button=>{
    const s=getComputedStyle(button),r=button.getBoundingClientRect();
    const rgba=value=>{const c=document.createElement('canvas');c.width=c.height=1;const x=c.getContext('2d');x.fillStyle=value;x.fillRect(0,0,1,1);return[...x.getImageData(0,0,1,1).data]};
    return{background:rgba(s.backgroundColor),foreground:rgba(s.color),scale:s.scale,transform:s.transform,width:r.width,height:r.height,transitionProperty:s.transitionProperty,transitionDuration:s.transitionDuration,transitionTimingFunction:s.transitionTimingFunction,animations:button.getAnimations().map(a=>({property:a.transitionProperty,currentTime:a.currentTime,pending:a.pending,playState:a.playState,computed:a.effect.getComputedTiming(),timing:a.effect.getTiming(),frames:a.effect.getKeyframes()}))};
   });
   const show=async status=>{await page.evaluate(value=>window.show(value),status);await settle();await page.locator('#root button').evaluate(button=>button.getAnimations().forEach(a=>a.finish()));};
   const capture=async(kind,from,to,change,status=null)=>{
    const before=await read();await change();
    await page.locator('#root button').evaluate(async button=>{window.motionAnimations=button.getAnimations();for(const animation of window.motionAnimations)animation.pause();await Promise.all(window.motionAnimations.map(animation=>animation.ready));});
    const definition=await read(),samples=[];
    for(const milliseconds of [0,47.5,95,142.5,190]){
     await page.evaluate(time=>{for(const animation of window.motionAnimations)animation.currentTime=time;},milliseconds);
     samples.push({milliseconds,...await read()});
    }
    await page.evaluate(()=>{for(const animation of window.motionAnimations)animation.finish();});
    transitions.push({kind,scheme,reduced,from,to,status,before,definition,samples,after:await read()});
   };
   const statuses=['pending','active','completed','error'];
   if(!reduced){for(const from of statuses)for(const to of statuses)if(from!==to){
    await show(from);await capture('status',from,to,async()=>{await page.evaluate(value=>window.show(value),to);await page.waitForFunction(value=>{const button=document.querySelector('#root button');return value==='completed'?button.querySelector('svg')!==null:button.textContent==='1'},to);});
   }}
   else{await show('pending');await capture('status','pending','completed',async()=>{await page.evaluate(()=>window.show('completed'));await settle();});}
   for(const status of statuses){
    await show(status);await capture('theme',scheme,scheme==='Light'?'Dark':'Light',async()=>{await page.evaluate(()=>document.documentElement.classList.toggle('dark'));await page.locator('#root button').evaluate(button=>void getComputedStyle(button).color);},status);
    await page.evaluate(value=>document.documentElement.classList.toggle('dark',value),scheme==='Dark');await settle();await page.locator('#root button').evaluate(button=>button.getAnimations().forEach(a=>a.finish()));
   }
   if(!reduced){
    await show('pending');await page.evaluate(()=>window.show('completed'));
    await page.locator('#root button').evaluate(async button=>{window.retainedAnimations=button.getAnimations();for(const animation of window.retainedAnimations)animation.pause();await Promise.all(window.retainedAnimations.map(animation=>animation.ready));for(const animation of window.retainedAnimations)animation.currentTime=47.5;});
    const before=await read();await page.evaluate(()=>window.show('completed',false));const after=await read();
    const retained=await page.locator('#root button').evaluate(button=>button.disabled&&button.getAnimations().every((animation,index)=>animation===window.retainedAnimations[index]));
    assert.equal(retained,true);assert.deepEqual(after.background,before.background);assert.deepEqual(after.foreground,before.foreground);
    availability.push({scheme,before,after,retained});await page.locator('#root button').evaluate(button=>button.getAnimations().forEach(animation=>animation.finish()));
   }
   await show('active');await page.locator('#root button').focus();
   await page.evaluate(()=>{window.keyReceipt=[];document.addEventListener('keydown',e=>window.keyReceipt.push({type:e.type,key:e.key,trusted:e.isTrusted}));document.addEventListener('keyup',e=>window.keyReceipt.push({type:e.type,key:e.key,trusted:e.isTrusted}));});
   const idle=await read();await page.keyboard.down('Space');const held=await read(),heldInvocations=await page.evaluate(()=>window.invocations.length);await page.keyboard.up('Space');const released=await read(),releasedInvocations=await page.evaluate(()=>window.invocations.length);
   const events=await page.evaluate(()=>window.keyReceipt);assert.ok(events.every(e=>e.trusted));assert.equal(events.length,2);
   assert.equal(held.transform,'none');assert.equal(held.scale,'0.94');assert.ok(!held.animations.some(a=>a.property==='scale'||a.property==='transform'));assert.equal(released.scale,'1');
   assert.equal(heldInvocations,0);assert.equal(releasedInvocations,1);
   presses.push({scheme,reduced,idle,held,released,heldInvocations,releasedInvocations,events});assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-stepper-motion-reference-v1',sourceCommit:normal.sourceCommit,sourceSha256:hash(sourceBytes),normalReferenceSha256:hash(normalBytes),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),scriptNormalization:'lf',cssSha256:hash(cssBytes),frozenLockSha256:hash(lockBytes),browser:normal.browser,transitions,presses,availability};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({transitions:transitions.length,presses:presses.length,sourceScaleTransition:false}));
})().catch(error=>{console.error(error);process.exitCode=1});
