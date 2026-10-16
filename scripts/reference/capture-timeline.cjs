const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],`Pass ${name} <path>.`);return path.resolve(process.argv[i+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const source=path.resolve('artifacts/line-chart-browser/frozen-source/src/components/timeline.tsx'),sourceBytes=await fs.readFile(source);
 assert.equal(hash(sourceBytes),'d070c4100c562241a3cc14585c7ddc30fa4be5bd0c96bb5b9ad7303734333f61');
 const runtime=path.resolve('artifacts/line-chart-browser/node_modules'),lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes);
 const installed=JSON.parse(await fs.readFile('artifacts/line-chart-browser/package-lock.json'));
 for(const [key,value]of Object.entries(installed.packages))if(key){assert.equal(value.version,lock.packages[key]?.version,key);assert.equal(value.integrity,lock.packages[key]?.integrity,key);}
 for(const name of ['react','react-dom'])assert.equal(require(path.join(runtime,name,'package.json')).version,'19.2.4');
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const bundle=await require(esbuildPath).build({stdin:{contents:`import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{Timeline}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));window.actions=[];const icon=v=>v&&typeof v==='object'&&v.action?React.createElement('button',{style:{width:20,height:20,lineHeight:'20px'},onClick:()=>window.actions.push(v.action),'aria-label':v.action},'E'):v;window.show=p=>flushSync(()=>root.render(<Timeline {...p} items={p.items.map(i=>({...i,icon:icon(i.icon)}))}/>));`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile('artifacts/line-chart-browser/styles.css'),css=cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'');let fonts='';const fontFiles=[];
 for(const [family,weight,file]of[['Encode Sans',400,'EncodeSans-Regular.ttf'],['Encode Sans',500,'EncodeSans-Medium.ttf'],['Encode Sans',600,'EncodeSans-SemiBold.ttf'],['Libre Franklin',400,'LibreFranklin-Regular.ttf']]){
  const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);fontFiles.push({family,weight,file,sha256:hash(bytes)});fonts+=`@font-face{font-family:'${family}';font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;
 }
 const base=[{id:'created',title:'Record created',description:'A consumer-owned description.',timestamp:'09:15'},{id:'review',title:'Editorial review',timestamp:'10:30'},{id:'ready',title:'Ready for handoff',description:'Fictional local metadata'}];
 const long=[{id:'a',title:'A longer metadata title wrapping across multiple lines',description:'Detailed multilingual guidance: revisão editorial 日本語 العربية.',timestamp:'Yesterday at 14:30'},{id:'b',title:'OneExtremelyLongUnbrokenTitle',timestamp:'Before the editorial handoff',description:'Description\nwith   repeated whitespace'}];
 const specs=[{id:'default',items:base},{id:'statuses',items:['completed','active','pending','error'].map(status=>({id:status,title:status+' record',status,description:'Status is consumer-owned',timestamp:'11:45'}))},
  {id:'single',items:[{id:'only',title:'One record'}]},{id:'empty',items:[]},{id:'long',items:long,width:280},{id:'narrow',items:long,width:112},
  {id:'rtl',items:base,direction:'rtl'},{id:'rtl-long',items:long,width:280,direction:'rtl'},
  {id:'raw',items:[{id:'empty',title:'',description:'',timestamp:''},{id:'space',title:' ',description:' ',timestamp:' '},{id:'text',title:'Title\nwith   whitespace',description:'Summary\nwith   whitespace',timestamp:'  Yesterday  '}]},
  {id:'icons',items:[{id:'default',title:'Default ordinal',icon:null},{id:'false',title:'Empty icon',icon:false},{id:'zero',title:'Zero icon',icon:0},{id:'text',title:'Text icon',icon:'!'},{id:'long',title:'Long scalar icon',icon:'LongScalarIcon'},{id:'words',title:'Wrapping scalar icon',icon:'Two words'}]},
  {id:'action',items:[{id:'action',title:'Consumer icon action',description:'Preserves its native command',timestamp:'12:00',icon:{action:'Edit record'}}]},
  {id:'timestamp',width:280,items:[{id:'long',title:'Review title',timestamp:'AnExtremelyLongUnbrokenTimestamp',description:'Timestamp minimum width'},{id:'wrap',title:'A title with several words',timestamp:'Before the next review meeting'}]}
 ];
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),'155.0.4283.45');const cases=[],interactions=[];
 try{for(const viewportWidth of[1024,639,640])for(const scheme of['Light','Dark'])for(const spec of specs){
  const width=spec.width??Math.min(640,viewportWidth-40),direction=spec.direction??'ltr',props={items:spec.items};
  const page=await browser.newPage({viewport:{width:viewportWidth,height:900},deviceScaleFactor:1,reducedMotion:'reduce'});const errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{
   await page.setContent(`<html class="${scheme==='Dark'?'dark':''}"><head><style>${css}${fonts}body{margin:0;padding:20px;background:${scheme==='Dark'?'#0D1821':'white'}}#root{width:${width}px;direction:${direction}}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(p=>{window.timelineItems=p.items;window.show(p)},props);await page.evaluate(async()=>{await Promise.all([document.fonts.load('400 16px "Encode Sans"'),document.fonts.load('500 14px "Encode Sans"'),document.fonts.load('600 12px "Encode Sans"'),document.fonts.load('400 14px "Libre Franklin"')]);await document.fonts.ready;await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));});
   const normal=await page.locator('#root').evaluate(root=>{
    const origin=root.getBoundingClientRect(),box=e=>{const b=e.getBoundingClientRect();return{x:b.x-origin.x,y:b.y-origin.y,width:b.width,height:b.height}};
    const text=e=>{
     if(!e)return null;const s=getComputedStyle(e),walker=document.createTreeWalker(e,NodeFilter.SHOW_TEXT);let first=null;
     while(walker.nextNode()){const node=walker.currentNode,index=node.textContent.search(/[^ \t\r\n\f]/);if(index>=0){first={node,index};break;}}
     let baseline=null;if(first){const {node,index}=first,character=String.fromCodePoint(node.textContent.codePointAt(index)),range=document.createRange();range.setStart(node,index);range.setEnd(node,index+character.length);const rect=range.getBoundingClientRect(),context=document.createElement('canvas').getContext('2d');context.font=getComputedStyle(node.parentElement).font;const metrics=context.measureText(character);assertLineBox(rect,metrics);baseline=rect.y-origin.y+metrics.fontBoundingBoxAscent;}
     return{box:box(e),text:e.textContent,font:s.fontFamily,fontSize:s.fontSize,lineHeight:s.lineHeight,fontWeight:s.fontWeight,color:s.color,firstBaseline:baseline};
    };
    function assertLineBox(rect,metrics){if(Math.abs(rect.height-metrics.fontBoundingBoxAscent-metrics.fontBoundingBoxDescent)>.001)throw Error('DOM range and font bounds disagree.');}
    return{box:box(root.firstElementChild),rows:[...root.firstElementChild.children].map((row,index)=>{
     const connector=row.children.length===3?row.children[0]:null,icon=row.children[connector?1:0],body=row.lastElementChild,header=body.firstElementChild,style=getComputedStyle(icon),lineStyle=connector?getComputedStyle(connector):null;
     return{box:box(row),connector:connector?{box:box(connector),color:lineStyle.backgroundColor}:null,icon:{...text(icon),background:style.backgroundColor,borderColor:style.borderColor,borderWidth:style.borderWidth,radius:style.borderRadius,shadow:style.boxShadow,child:icon.firstElementChild?box(icon.firstElementChild):null},body:box(body),header:box(header),headerDirection:getComputedStyle(header).flexDirection,title:text(header.firstElementChild),timestamp:text(header.querySelector('time')),description:text(body.children[1]??null),status:window.timelineItems[index].status??(index===0?'active':'pending')};
    })};
   });
   const file=`timeline-${spec.id}-${viewportWidth}-${scheme}.png`;await page.screenshot({path:path.join(output,file),fullPage:true});
   cases.push({id:spec.id,viewportWidth,width,direction,props,scheme,normal,file,pngSha256:hash(await fs.readFile(path.join(output,file)))});
   assert(normal.rows.every(row=>row.headerDirection===(viewportWidth<640?'column':'row')),'Source breakpoint CSS missing.');
   if(spec.id==='action'){const action=page.getByRole('button',{name:'Edit record'});await action.click();await action.focus();await page.keyboard.press('Space');await page.keyboard.press('Enter');assert.deepEqual(await page.evaluate(()=>window.actions),['Edit record','Edit record','Edit record']);interactions.push({viewportWidth,scheme,activations:3});}
   assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-timeline-reference-v1',sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256:hash(sourceBytes),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),scriptNormalization:'lf',frozenLockSha256:hash(lockBytes),cssSha256:hash(cssBytes),bundleSha256:hash(bundle.outputFiles[0].contents),fontFiles,react:'19.2.4',esbuild:'0.25.12',playwright:'1.62.1',browser:browser.version(),cases,interactions};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,interactions:interactions.length,sha256:hash(await fs.readFile(path.join(output,'reference.json')))}));
})().catch(error=>{console.error(error);process.exitCode=1});
