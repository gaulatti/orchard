const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const source=path.resolve('artifacts/line-chart-browser/frozen-source/src/components/stepper.tsx'),sourceBytes=await fs.readFile(source);
 assert.equal(hash(sourceBytes),'a0cea6e7cc8fcc541d0089682d64ed8589fc4983a482095785477cb01d646c29');
 const runtime=path.resolve('artifacts/line-chart-browser/node_modules'),lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes),installed=JSON.parse(await fs.readFile('artifacts/line-chart-browser/package-lock.json'));
 for(const [key,value]of Object.entries(installed.packages))if(key){assert.equal(value.version,lock.packages[key]?.version,key);assert.equal(value.integrity,lock.packages[key]?.integrity,key);}
 for(const [name,version]of[['react','19.2.4'],['react-dom','19.2.4']])assert.equal(require(path.join(runtime,name,'package.json')).version,version);
 const icons=path.resolve('artifacts/error-state-browser/node_modules'),iconsLock=JSON.parse(await fs.readFile('artifacts/error-state-browser/package-lock.json'));
 assert.equal(require(path.join(icons,'lucide-react/package.json')).version,lock.packages['node_modules/lucide-react'].version);
 assert.equal(iconsLock.packages['node_modules/lucide-react'].integrity,lock.packages['node_modules/lucide-react'].integrity);
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const bundle=await require(esbuildPath).build({stdin:{contents:`import React from 'react';import{createRoot}from'react-dom/client';import{Stepper}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));window.invocations=[];window.show=p=>{window.currentProps=p;root.render(<Stepper {...p} onStepClick={p.interactive===false?undefined:step=>window.invocations.push({id:step.id,index:p.steps.indexOf(step),original:p.steps.includes(step),activeStep:p.activeStep??null})}/>)};`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',nodePaths:[runtime,icons],alias:{react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile('artifacts/line-chart-browser/styles.css'),css=cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'');let fonts='';const fontFiles=[];
 for(const [weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);fontFiles.push({weight,file,sha256:hash(bytes)});fonts+=`@font-face{font-family:'Encode Sans';font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const base=[{id:'draft',label:'Draft',description:'Write a story'},{id:'review',label:'Review',description:'Check facts'},{id:'publish',label:'Publish',description:'Release edition'}];
 const statuses=['pending','active','completed','error'].map((status,index)=>({id:status,label:status,description:`Stage ${index+1}`,status}));
 const long=[{id:'first',label:'A long heading that wraps across multiple lines',description:'Detailed multilingual guidance: revisão editorial 日本語 العربية'},{id:'second',label:'OneExtremelyLongUnbrokenLabelThatExceedsTheSourceMaximum',description:'Description\nwith   repeated whitespace'},{id:'third',label:'Publish',description:''}];
 const specs=[];const add=(id,props={},width=640,direction='ltr')=>specs.push({id,width,direction,props:{steps:base,activeStep:'review',...props}});
 for(const orientation of ['horizontal','vertical']){
  add(`${orientation}-default`,{orientation});add(`${orientation}-statuses`,{orientation,steps:statuses});
  add(`${orientation}-static`,{orientation,interactive:false});add(`${orientation}-disabled`,{orientation,steps:base.map((s,i)=>({...s,disabled:i===1}))});
  add(`${orientation}-long`,{orientation,steps:long},240);add(`${orientation}-rtl`,{orientation},640,'rtl');
 }
 add('no-active',{activeStep:undefined});add('active-first',{activeStep:'draft'});add('active-last',{activeStep:'publish'});add('unknown-active',{activeStep:'missing'});
 add('empty-active-id',{activeStep:'',steps:[{id:'',label:'Blank key'},{id:'next',label:'Next'}]});
 add('empty',{steps:[]});add('single',{steps:[base[0]],activeStep:'draft'});add('empty-labels',{steps:[{id:'a',label:''},{id:'b',label:' ',description:' '},{id:'c',label:'\n',description:''}]});
 add('horizontal-narrow',{},96);add('vertical-narrow',{orientation:'vertical'},64);
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),'155.0.4283.45');const cases=[],interactions=[],focusCases=[];
 try{for(const scheme of ['Light','Dark'])for(const spec of specs){
  const page=await browser.newPage({viewport:{width:1024,height:745},deviceScaleFactor:1,reducedMotion:'reduce'});page.setDefaultTimeout(2000);const errors=[];page.on('pageerror',error=>errors.push(error.message));
  try{
   await page.setContent(`<html class="${scheme==='Dark'?'dark':''}"><head><style>${css}${fonts}body{margin:0;padding:20px;background:${scheme==='Dark'?'#0D1821':'white'}}#root{width:${spec.width}px;direction:${spec.direction}}#before,#after{position:fixed;left:850px;width:160px;height:32px;visibility:hidden}#before{top:20px}#after{top:60px}</style></head><body><button id="before">Before stepper</button><div id="root"></div><button id="after">After stepper</button><script>${bundle.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(props=>window.show(props),spec.props);await page.waitForFunction(()=>document.querySelector('#root>div'));
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(weight=>document.fonts.load(weight+' 14px "Encode Sans"')));await document.fonts.ready;});
   const settle=()=>page.evaluate(()=>new Promise(resolve=>{let count=0;function frame(){if(++count>=3)resolve();else requestAnimationFrame(frame)}requestAnimationFrame(frame)}));
   const read=()=>page.locator('#root').evaluate(root=>{
    const r=root.getBoundingClientRect(),box=e=>{const b=e.getBoundingClientRect();return{x:b.x-r.x,y:b.y-r.y,width:b.width,height:b.height}};
    const color=value=>{const canvas=document.createElement('canvas');canvas.width=canvas.height=1;const ctx=canvas.getContext('2d');ctx.fillStyle=value;ctx.fillRect(0,0,1,1);return[...ctx.getImageData(0,0,1,1).data]};
    const text=e=>{if(!e)return null;const s=getComputedStyle(e);return{box:box(e),text:e.textContent,font:s.fontFamily,fontSize:s.fontSize,lineHeight:s.lineHeight,fontWeight:s.fontWeight,color:s.color,rgba:color(s.color),textAlign:s.textAlign}};
    return{box:box(root.firstElementChild),scrollWidth:root.scrollWidth,rows:[...root.firstElementChild.children].map(row=>{const button=row.querySelector('button'),style=getComputedStyle(button),labels=row.lastElementChild,connector=row.querySelector(':scope>div.absolute');return{box:box(row),button:{box:box(button),disabled:button.disabled,text:button.textContent,svg:button.querySelector('svg')?.outerHTML??null,background:style.backgroundColor,backgroundRgba:color(style.backgroundColor),foreground:style.color,foregroundRgba:color(style.color),opacity:style.opacity,font:style.fontFamily,fontSize:style.fontSize,fontWeight:style.fontWeight,lineHeight:style.lineHeight,transform:style.transform,shadow:style.boxShadow,focused:button===document.activeElement,focusVisible:button.matches(':focus-visible'),cursor:style.cursor},label:text(labels.children[0]),description:text(labels.children[1]),labels:box(labels),connector:connector?{box:box(connector),color:getComputedStyle(connector.firstElementChild).backgroundColor,rgba:color(getComputedStyle(connector.firstElementChild).backgroundColor)}:null}})};
   });
   await settle();const normal=await read(),file=`stepper-${spec.id}-${scheme}.png`;await page.screenshot({path:path.join(output,file)});
   cases.push({...spec,scheme,normal,file,pngSha256:hash(await fs.readFile(path.join(output,file)))});
   if(spec.id==='horizontal-default'){
    await page.locator('#before,#after').evaluateAll(elements=>elements.forEach(element=>element.style.visibility='visible'));
    await page.evaluate(()=>{window.events=[];for(const type of ['pointermove','pointerdown','pointerup','keydown','keyup','focusin'])document.addEventListener(type,e=>window.events.push({type,trusted:e.isTrusted,key:e.key??null}),true)});
    const first=page.locator('#root button').first(),rect=await first.boundingBox();await page.mouse.move(rect.x+rect.width/2,rect.y+rect.height/2);await settle();const hover=await read();
    await page.mouse.down();await settle();const held=await read();await page.mouse.up();await settle();const released=await read();
    await page.locator('#before').focus();await page.keyboard.press('Tab');await settle();const focus=await read();
    await page.keyboard.down('Space');await settle();const spaceHeld=await read();await page.keyboard.up('Space');await settle();const spaceReleased=await read();
    await page.keyboard.press('Enter');await settle();const enter=await read();
    const before=await first.evaluate(e=>{window.firstStepperButton=e;return true});assert.equal(before,true);
    const changed={...spec.props,steps:spec.props.steps.map((s,i)=>i?{...s}:{...s,label:'Updated draft',description:'Live consumer update',status:'error'})};await page.evaluate(p=>window.show(p),changed);await settle();const updated=await read();const retained=await first.evaluate(e=>e===window.firstStepperButton);
    const noCallback={...changed,interactive:false};await page.evaluate(p=>window.show(p),noCallback);await settle();const callbackRemoved=await read();
    const invocations=await page.evaluate(()=>window.invocations),events=await page.evaluate(()=>window.events);assert.ok(events.every(e=>e.trusted));assert.equal(invocations.length,3);assert.ok(invocations.every(e=>e.original&&e.id==='draft'&&e.activeStep==='review'));assert.equal(retained,true);
    interactions.push({scheme,hover,held,released,focus,spaceHeld,spaceReleased,enter,updated,callbackRemoved,keyedButtonRetained:retained,invocations,events});
   }
   if(spec.id==='horizontal-statuses'){
    await page.locator('#before').evaluate(e=>e.style.visibility='visible');await page.locator('#before').focus();
    for(let index=0;index<4;index++){await page.keyboard.press('Tab');await settle();const value=await read();assert.equal(value.rows[index].button.focusVisible,true);focusCases.push({scheme,index,status:spec.props.steps[index].status,button:value.rows[index].button});}
   }
   assert.deepEqual(errors,[]);console.log(JSON.stringify({scheme,id:spec.id,height:normal.box.height,width:normal.box.width}));
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-stepper-reference-v1',sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256:hash(sourceBytes),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),scriptNormalization:'lf',frozenLockSha256:hash(lockBytes),cssSha256:hash(cssBytes),bundleSha256:hash(bundle.outputFiles[0].contents),fontFiles,react:'19.2.4',lucide:lock.packages['node_modules/lucide-react'].version,esbuild:'0.25.12',playwright:'1.62.1',browser:browser.version(),cases,interactions,focusCases};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');
})().catch(error=>{console.error(error);process.exitCode=1});
