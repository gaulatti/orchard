const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],`Pass ${name} <path>.`);return path.resolve(process.argv[i+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const source=path.resolve('artifacts/line-chart-browser/frozen-source/src/components/loading-overlay.tsx'),spinner=path.resolve('artifacts/line-chart-browser/frozen-source/src/components/loading-spinner.tsx');
 assert.equal(hash(await fs.readFile(source)),'9b4721d0cb08b99284bf0b59dc16ba2448cf8b065a926882f37b1099cea033da');
 const runtime=path.resolve('artifacts/line-chart-browser/node_modules'),lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes),installed=JSON.parse(await fs.readFile('artifacts/line-chart-browser/package-lock.json'));
 for(const [key,value]of Object.entries(installed.packages))if(key){assert.equal(value.version,lock.packages[key]?.version);assert.equal(value.integrity,lock.packages[key]?.integrity);}
 for(const name of['react','react-dom'])assert.equal(require(path.join(runtime,name,'package.json')).version,'19.2.4');
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const bundle=await require(esbuildPath).build({stdin:{contents:`import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{LoadingOverlay}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));window.show=p=>flushSync(()=>root.render(<div style={{width:100,height:40,overflow:'hidden',transform:'translate(31.25px,27.5px)'}}><LoadingOverlay {...p}/>{p.second&&<LoadingOverlay visible label="Second"/>}</div>));`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile('artifacts/line-chart-browser/styles.css'),css=cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'');let fonts='';const fontFiles=[];
 for(const [weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf']]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);fontFiles.push({family:'Encode Sans',weight,file,sha256:hash(bytes)});fonts+=`@font-face{font-family:'Encode Sans';font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const specs=[{id:'hidden',props:{visible:false}},{id:'default',props:{visible:true}},{id:'empty',props:{visible:true,label:''}},{id:'null',props:{visible:true,label:null}},{id:'space',props:{visible:true,label:' '}},{id:'long',props:{visible:true,label:'Preparing the fictional editorial record and associated local preview material'}},{id:'unbroken',props:{visible:true,label:'OneExtremelyLongUnbrokenLoadingLabel'}},{id:'rtl',direction:'rtl',props:{visible:true,label:'Preparing revisão 日本語 العربية'}},{id:'clipped',props:{visible:true,label:'Outside parent bounds'}},{id:'stacked',props:{visible:true,label:'First',second:true}}];
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),'155.0.4283.45');const cases=[],interactions=[];
 try{for(const [width,height]of[[1024,900],[320,240],[640,480]])for(const scheme of['Light','Dark']){
  const page=await browser.newPage({viewport:{width,height},deviceScaleFactor:1,reducedMotion:'reduce'});const errors=[];page.on('pageerror',error=>errors.push(error.message));
  try{
   const colors=scheme==='Dark'?['#132331','#071019']:['#FFFFFF','#E0E0E0'];
   await page.setContent(`<html class="${scheme==='Dark'?'dark':''}"><head><style>${css}${fonts}body{margin:0}#pattern{position:fixed;inset:0;background:repeating-conic-gradient(${colors[0]} 0% 25%,${colors[1]} 0% 50%) 0 0/40px 40px}#underlying{position:absolute;left:20px;top:20px;width:140px;height:32px;background:#fff;color:#000}#next{position:absolute;left:20px;top:70px}#root{position:absolute;left:70px;top:120px}</style></head><body><div id="pattern"></div><button id="underlying" onclick="window.actions++">Underlying action</button><button id="next">Next action</button><div id="root"></div><script>window.actions=0;${bundle.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(async()=>{await Promise.all([document.fonts.load('400 16px "Encode Sans"'),document.fonts.load('500 14px "Encode Sans"')]);await document.fonts.ready;});
   for(const spec of specs){
    const direction=spec.direction??'ltr';await page.evaluate(({props,direction})=>{document.body.dir=direction;window.show(props)}, {props:spec.props,direction});
    await page.evaluate(async()=>{for(const animation of document.getAnimations()){animation.pause();animation.currentTime=0;}await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));});
    const normal=await page.evaluate(()=>{
     const box=e=>{const b=e.getBoundingClientRect();return{x:b.x,y:b.y,width:b.width,height:b.height}},text=e=>{
      if(!e)return null;const style=getComputedStyle(e);let firstBaseline=null,index=e.textContent.search(/[^ \t\r\n\f]/);
      if(index>=0){const range=document.createRange(),node=e.firstChild,char=String.fromCodePoint(node.textContent.codePointAt(index));range.setStart(node,index);range.setEnd(node,index+char.length);const rect=range.getBoundingClientRect(),context=document.createElement('canvas').getContext('2d');context.font=style.font;const metrics=context.measureText(char);if(Math.abs(rect.height-metrics.fontBoundingBoxAscent-metrics.fontBoundingBoxDescent)>.001)throw Error('Font/range line boxes disagree.');firstBaseline=rect.y+metrics.fontBoundingBoxAscent;}
      return{box:box(e),text:e.textContent,font:style.font,fontSize:style.fontSize,lineHeight:style.lineHeight,fontWeight:style.fontWeight,color:style.color,firstBaseline};
     };
     return{parent:box(document.querySelector('#root>div')),overlays:[...document.querySelectorAll('body>div.fixed')].map(overlay=>{
      const card=overlay.firstElementChild,spinner=card.firstElementChild.firstElementChild,s=getComputedStyle(overlay),c=getComputedStyle(card),a=getComputedStyle(spinner);
      return{box:box(overlay),background:s.backgroundColor,blur:s.backdropFilter,zIndex:s.zIndex,card:{box:box(card),background:c.backgroundColor,border:c.borderColor,borderWidth:c.borderWidth,radius:c.borderRadius,padding:[c.paddingTop,c.paddingRight,c.paddingBottom,c.paddingLeft],gap:c.gap,shadow:c.boxShadow},spinner:{box:box(spinner),borderWidth:a.borderWidth,borderColor:a.borderColor,duration:a.animationDuration},label:text(card.querySelector('p'))};
     })};
    });
    assert.equal(normal.overlays.length,spec.props.visible?(spec.props.second?2:1):0);
    for(const overlay of normal.overlays){assert.deepEqual(overlay.box,{x:0,y:0,width,height});assert.equal(overlay.blur,'blur(2px)');assert.equal(overlay.spinner.box.width,48);}
    const file=`loading-overlay-${spec.id}-${width}x${height}-${scheme}.png`;await page.screenshot({path:path.join(output,file),fullPage:false});
    cases.push({id:spec.id,width,height,scheme,direction,props:spec.props,normal,file,pngSha256:hash(await fs.readFile(path.join(output,file)))});
   }
   await page.evaluate(()=>window.show({visible:false}));const button=page.locator('#underlying');await button.focus();
   await page.evaluate(()=>window.show({visible:true}));const focusOnOpen=await page.evaluate(()=>document.activeElement.id);
   assert.equal(focusOnOpen,'underlying');await page.keyboard.press('Space');await page.keyboard.press('Enter');assert.equal(await page.evaluate(()=>window.actions),2);
   await page.mouse.click(90,36);assert.equal(await page.evaluate(()=>window.actions),2);const focusAfterPointer=await page.evaluate(()=>({id:document.activeElement.id,tag:document.activeElement.tagName}));
   await page.keyboard.press('Escape');assert.equal(await page.locator('body>div.fixed').count(),1);
   await page.keyboard.press('Tab');const focusAfterTab=await page.evaluate(()=>document.activeElement.id);
   await page.evaluate(()=>window.show({visible:false}));const focusAfterHide=await page.evaluate(()=>document.activeElement.id);
   interactions.push({width,height,scheme,focusOnOpen,keyboardActivations:2,pointerBlocked:true,focusAfterPointer,escapeDoesNotDismiss:true,focusAfterTab,focusAfterHide});
   assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-loading-overlay-reference-v1',sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256:hash(await fs.readFile(source)),spinnerSha256:hash(await fs.readFile(spinner)),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),scriptNormalization:'lf',frozenLockSha256:hash(lockBytes),cssSha256:hash(cssBytes),bundleSha256:hash(bundle.outputFiles[0].contents),fontFiles,react:'19.2.4',esbuild:'0.25.12',playwright:'1.62.1',browser:browser.version(),snapshots:{animationsPaused:true,currentTime:0},cases,interactions};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,interactions}));
})().catch(error=>{console.error(error);process.exitCode=1});
