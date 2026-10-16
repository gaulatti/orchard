const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],`Pass ${name} <path>.`);return path.resolve(process.argv[i+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const source=path.resolve('artifacts/line-chart-browser/frozen-source/src/components/data-list.tsx'),sourceBytes=await fs.readFile(source);
 assert.equal(hash(sourceBytes),'23da195177b59a57fb1031ba4c8fdd900f7484febc4d73d13b96770cc55bde5a');
 const runtime=path.resolve('artifacts/line-chart-browser/node_modules'),lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes);
 const installed=JSON.parse(await fs.readFile('artifacts/line-chart-browser/package-lock.json'));for(const [key,value]of Object.entries(installed.packages))if(key){assert.equal(value.version,lock.packages[key]?.version,key);assert.equal(value.integrity,lock.packages[key]?.integrity,key);}
 for(const name of ['react','react-dom'])assert.equal(require(path.join(runtime,name,'package.json')).version,'19.2.4');
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const bundle=await require(esbuildPath).build({stdin:{contents:`import React from 'react';import{createRoot}from'react-dom/client';import{DataList}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));const node=v=>v&&typeof v==='object'&&v.action?React.createElement('button',{onClick:()=>window.actions.push(v.action)},v.action):v;window.actions=[];window.show=p=>root.render(<DataList {...p} items={p.items.map(i=>({...i,label:node(i.label),value:node(i.value),description:node(i.description)}))}/>);`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile('artifacts/line-chart-browser/styles.css'),css=cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'');let fonts='';const fontFiles=[];
 for(const [family,weight,file]of[['Encode Sans',400,'EncodeSans-Regular.ttf'],['Encode Sans',500,'EncodeSans-Medium.ttf'],['Libre Franklin',400,'LibreFranklin-Regular.ttf']]){
  const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);fontFiles.push({family,weight,file,sha256:hash(bytes)});fonts+=`@font-face{font-family:'${family}';font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;
 }
 const base=[{id:'title',label:'Title',value:'Harbor dispatch',description:'A consumer-owned summary.'},{id:'status',label:'Status',value:'Ready'},{id:'count',label:'Items',value:42,description:'Fictional records'}];
 const long=[{id:'a',label:'A longer metadata label wrapping across lines',value:'Editorial review and international distribution',description:'Detailed multilingual guidance: revisão editorial 日本語 العربية.'},{id:'b',label:'OneExtremelyLongUnbrokenLabel',value:'AnotherExtremelyLongUnbrokenValue',description:'Description\nwith   repeated whitespace'}];
 const raw=[{id:'zero',label:0,value:0,description:0},{id:'false',label:false,value:null,description:false},{id:'empty',label:'',value:'',description:''},{id:'space',label:' ',value:' ',description:' '},{id:'true',label:true,value:7,description:3}];
 const specs=[
  {id:'default',items:base},{id:'striped',items:base,striped:true},{id:'long',items:long,width:280},{id:'long-striped',items:long,width:280,striped:true},
  {id:'empty',items:[]},{id:'raw',items:raw},{id:'raw-striped',items:raw,striped:true},
  {id:'rtl',items:base,direction:'rtl'},{id:'rtl-striped',items:base,direction:'rtl',striped:true},
  {id:'narrow',items:long,width:112},{id:'narrow-striped',items:long,width:112,striped:true},
  {id:'action',items:[{id:'edit',label:'Headline',value:{action:'Edit title'},description:'Consumer action'}]},
 ];
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),'155.0.4283.45');const cases=[],interactions=[];
 try{for(const viewportWidth of [1024,639,640])for(const scheme of ['Light','Dark'])for(const spec of specs){
  const width=spec.width??Math.min(640,viewportWidth-40),direction=spec.direction??'ltr',props={items:spec.items,striped:spec.striped??false};
  const page=await browser.newPage({viewport:{width:viewportWidth,height:900},deviceScaleFactor:1,reducedMotion:'reduce'});const errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{
   await page.setContent(`<html class="${scheme==='Dark'?'dark':''}"><head><style>${css}${fonts}body{margin:0;padding:20px;background:${scheme==='Dark'?'#0D1821':'white'}}#root{width:${width}px;direction:${direction}}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(p=>window.show(p),props);await page.waitForFunction(()=>document.querySelector('#root>dl'));
   await page.evaluate(async()=>{await Promise.all([document.fonts.load('500 13px "Encode Sans"'),document.fonts.load('400 12px "Libre Franklin"')]);await document.fonts.ready;await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));});
   const normal=await page.locator('#root').evaluate(root=>{
    const origin=root.getBoundingClientRect(),box=e=>{const b=e.getBoundingClientRect();return{x:b.x-origin.x,y:b.y-origin.y,width:b.width,height:b.height}};
    const text=e=>{const s=getComputedStyle(e);return{box:box(e),text:e.textContent,font:s.fontFamily,fontSize:s.fontSize,lineHeight:s.lineHeight,fontWeight:s.fontWeight,color:s.color,textAlign:s.textAlign}};
    return{box:box(root.firstElementChild),rows:[...root.firstElementChild.children].map(e=>{const s=getComputedStyle(e),label=e.querySelector('dt'),value=e.querySelector('dd'),description=value.querySelector('p');return{box:box(e),label:text(label),value:text(value),description:description?text(description):null,display:s.display,padding:[s.paddingTop,s.paddingRight,s.paddingBottom,s.paddingLeft],gap:s.gap,borderTopWidth:s.borderTopWidth,borderTopColor:s.borderTopColor,borderBottomWidth:s.borderBottomWidth,borderBottomColor:s.borderBottomColor,background:s.backgroundColor,radius:s.borderRadius,gridTemplateColumns:s.gridTemplateColumns}})};
   });
   assert(normal.rows.every(row=>row.display===(viewportWidth<640?'flex':'grid')),'Pinned CSS did not implement the source breakpoint.');
   const file=`data-list-${spec.id}-${viewportWidth}-${scheme}.png`;await page.screenshot({path:path.join(output,file),fullPage:true});
   cases.push({id:spec.id,viewportWidth,width,direction,props,scheme,normal,file,pngSha256:hash(await fs.readFile(path.join(output,file)))});
   if(spec.id==='action'){
    const button=page.locator('#root button');await button.click();await button.focus();await page.keyboard.press('Space');await page.keyboard.press('Enter');
    assert.deepEqual(await page.evaluate(()=>window.actions),['Edit title','Edit title','Edit title']);
    interactions.push({viewportWidth,scheme,activations:3,consumerActionPreserved:true});
   }
   assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-data-list-reference-v1',sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256:hash(sourceBytes),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),scriptNormalization:'lf',frozenLockSha256:hash(lockBytes),cssSha256:hash(cssBytes),bundleSha256:hash(bundle.outputFiles[0].contents),fontFiles,react:'19.2.4',esbuild:'0.25.12',playwright:'1.62.1',browser:browser.version(),cases,interactions};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,interactions:interactions.length,sha256:hash(await fs.readFile(path.join(output,'reference.json')))}));
})().catch(error=>{console.error(error);process.exitCode=1});
