const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto'),{execFileSync}=require('node:child_process');
const arg=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const source=arg('--source-root'),tooling=arg('--tooling-root'),output=arg('--output'),playwright=arg('--playwright-module');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex'),pin='a86059013cd59684ce452849278ea81e734f2066';
(async()=>{
 await fs.mkdir(output,{recursive:true});const file='src/components/panel.tsx',dependencies=[];
 for(const name of [file,'src/utils/cn.ts','src/styles/global.css','src/styles/tokens.generated.css','package-lock.json']){
  const bytes=await fs.readFile(path.join(source,name));assert.deepEqual(bytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024}));dependencies.push({path:name,sha256:hash(bytes)});
 }
 assert.equal(dependencies[0].sha256,'a4a37fedfd21f848895d73837397de962f0dc28596927ef9479c3d4d6f06af38');
 const client=path.join(__dirname,'panel-client.tsx'),runtime=path.join(source,'node_modules');
 const bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/panel':path.join(source,file),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const slash=value=>value.replaceAll('\\','/');
 const input=(await fs.readFile(path.join(source,'src/styles/global.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'')
  .replace('@import "tailwindcss" source(none);','@import "'+slash(path.join(tooling,'node_modules/tailwindcss/index.css'))+'" source(none);')
  .replace('@import "./tokens.generated.css";','@import "'+slash(path.join(source,'src/styles/tokens.generated.css'))+'";')
  .replace('@source "../**/*.{ts,tsx}";','@source "'+slash(path.join(source,'src/**/*.{ts,tsx}'))+'";');
 await fs.writeFile(path.join(output,'input.css'),input);execFileSync(process.execPath,[path.join(tooling,'node_modules/@tailwindcss/cli/dist/index.mjs'),'-i',path.join(output,'input.css'),'-o',path.join(output,'source.css'),'--minify']);
 const css=await fs.readFile(path.join(output,'source.css')),fonts=[];let faces='';
 for(const [weight,name] of [[400,'EncodeSans-Regular.ttf'],[600,'EncodeSans-SemiBold.ttf']]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+name);fonts.push({weight,name,sha256:hash(bytes)});faces+=`@font-face{font-family:"Encode Sans";font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[],interactions=[],motion=[],keyboard=[];
 try{
  for(const scheme of ['Light','Dark']){
   const page=await browser.newPage({viewport:{width:900,height:680},deviceScaleFactor:1,reducedMotion:'reduce'}),errors=[];page.on('pageerror',error=>errors.push(error.message));
   try{
    await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0;overflow:hidden}${faces}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
    await page.evaluate(async()=>{await document.fonts.load('400 16px "Encode Sans"');await document.fonts.load('600 11px "Encode Sans"');await document.fonts.ready;});
    await page.mouse.move(840,620);
    const settle=()=>page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
    const measure=()=>page.evaluate(()=>{
     const panel=document.querySelector('#host>div'),header=panel.children[0],row=header.children[0],feed=panel.children[1],parts=[...row.children];
     const title=parts.find(node=>node.classList.contains('uppercase')),count=parts.find(node=>node.classList.contains('tabular-nums')),dot=parts.find(node=>node.classList.contains('rounded-full')),drag=parts.find(node=>getComputedStyle(node).cursor==='grab');
     const filter=[...header.children].find(node=>node.classList.contains('pb-4')),toolbar=[...header.children].find(node=>node.classList.contains('justify-center'));
     const box=node=>{if(!node)return null;const r=node.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};};
     const paint=node=>{if(!node)return null;const s=getComputedStyle(node);return{background:s.backgroundColor,color:s.color,border:s.borderColor,opacity:s.opacity};};
     const text=node=>{if(!node)return null;const s=getComputedStyle(node),range=document.createRange();range.selectNodeContents(node);const clone=node.cloneNode(true);Object.assign(clone.style,{position:'absolute',display:'block',left:'-10000px',top:'0',height:'auto',width:'min-content',flex:'none',direction:s.direction});document.body.appendChild(clone);const minimumWidth=clone.getBoundingClientRect().width;clone.style.width='max-content';const naturalWidth=clone.getBoundingClientRect().width;clone.remove();return{value:node.textContent,box:box(node),minimumWidth,naturalWidth,glyphs:[...range.getClientRects()].map(r=>({x:r.x,y:r.y,width:r.width,height:r.height})),fontSize:s.fontSize,fontWeight:s.fontWeight,lineHeight:s.lineHeight,tracking:s.letterSpacing,transform:s.textTransform};};
     return{panel:box(panel),header:box(header),row:box(row),title:text(title),count:text(count),dot:box(dot),drag:box(drag),filter:box(filter),toolbar:box(toolbar),feed:box(feed),
      bodies:Object.fromEntries([...panel.querySelectorAll('[data-body]')].map(node=>[node.dataset.body,box(node)])),
      scroll:{offset:feed.scrollTop,clientWidth:feed.clientWidth,clientHeight:feed.clientHeight,extent:feed.scrollHeight,scrollbarWidth:feed.offsetWidth-feed.clientWidth},
      paint:{panel:paint(panel),header:paint(header),dot:paint(dot),title:paint(title),count:paint(count),toolbar:paint(toolbar),feed:paint(feed),left:getComputedStyle(panel).borderLeftColor,right:getComputedStyle(panel).borderRightColor},
      transition:{duration:getComputedStyle(panel).transitionDuration,timing:getComputedStyle(panel).transitionTimingFunction}};
    });
    const capture=async spec=>{
     await page.evaluate(spec=>window.showPanel(spec),spec);await settle();if(spec.offset){await page.evaluate(offset=>document.querySelector('#host>div').children[1].scrollTop=offset,spec.offset);await settle();}
     const row={id:spec.id,scheme,spec,measurement:await measure()};if(spec.png){row.file='panel-'+spec.id+'-'+scheme+'.png';row.pngSha256=hash(await page.screenshot({path:path.join(output,row.file)}));}cases.push(row);
    };
    for(const width of [180,320,500.375])for(const [kind,title]of [['normal','Live coverage'],['long','Very long local coverage title with wrapping'],['unicode','東京 · Straße']])for(const count of [undefined,0,12])for(const slots of ['none','filter','toolbar','all'])
     await capture({id:`${width}-${kind}-${count??'absent'}-${slots}`,width,height:320,title,count,slots,drag:slots==='all',png:kind==='normal'&&count===12});
    const extras=[['empty',{empty:true}],['short',{bodyHeight:20}],['scalar',{scalar:'Consumer body'}],['false-filter',{filter:false}],['zero-filter',{filter:0}],['true-filter',{filter:true}],['text-filter',{filter:'Filter text'}],['empty-title',{title:''}],['rtl',{rtl:true,slots:'all',drag:true,count:12}],['accent',{accent:'#407fa3',slots:'all',count:12}],['alpha',{accent:'rgba(24, 90, 130, .4)',slots:'all',count:12}],['fixed-width',{panelWidth:240,slots:'all',count:12}],['grow-ignored',{grow:true,isDragging:true,slots:'all',count:12}]];
    for(const [id,extra]of extras)await capture({id,width:320,height:320,...extra,png:!['scalar','text-filter'].includes(id)});
    for(const height of [40,80,560])await capture({id:'height-'+height,width:320,height,slots:'all',drag:true,count:12,png:true});
    for(const [id,count,countKind]of [['negative',-2],['fraction',1.5],['nan',null,'nan'],['infinity',null,'infinity'],['negative-infinity',null,'negative-infinity'],['exponent',1e21]])await capture({id:'count-'+id,width:320,height:320,count,countKind});
    for(const offset of [80,400,880])await capture({id:'scroll-'+offset,width:320,height:320,slots:'all',drag:true,count:12,offset,png:true});
    await page.evaluate(()=>window.showPanel({id:'live',key:'live',width:320,height:320,slots:'all',drag:true,live:true,count:12}));await settle();
    await page.locator('[data-input="filter"]').fill('Edited filter');await page.locator('[data-input="body"]').fill('Edited body');
    await page.evaluate(()=>{window.originalFilter=document.querySelector('[data-input="filter"]');window.originalBody=document.querySelector('[data-input="body"]');window.updatePanel({title:'Updated title',count:0,accent:'#407fa3'});});await settle();
    await page.locator('[data-action="drag"]').click();await page.locator('[data-action="toolbar"]').click();await page.locator('[data-input="filter"]').focus();await page.keyboard.press('Tab');
    const live=await page.evaluate(()=>({filterRetained:window.originalFilter===document.querySelector('[data-input="filter"]'),bodyRetained:window.originalBody===document.querySelector('[data-input="body"]'),filterValue:document.querySelector('[data-input="filter"]').value,bodyValue:document.querySelector('[data-input="body"]').value,tabToToolbar:document.activeElement===document.querySelector('[data-action="toolbar"]'),events:window.panelEvents}));
    const handlePaint=()=>page.locator('[data-action="drag"]').evaluate(node=>{const s=getComputedStyle(node.parentElement);return{color:s.color,cursor:s.cursor,duration:s.transitionDuration,timing:s.transitionTimingFunction};});
    await page.mouse.move(840,620);await settle();const handleIdle=await handlePaint();await page.locator('[data-action="drag"]').hover();await page.waitForTimeout(250);const handleHover=await handlePaint();await page.mouse.down();const handlePressed=await handlePaint();await page.mouse.up();
    await page.evaluate(()=>{const feed=document.querySelector('#host>div').children[1];feed.scrollTop=120;window.beforePanelScroll=feed.scrollTop;window.updatePanel({title:'Scrolled panel',count:7});});await settle();
    const liveScroll=await page.evaluate(()=>({before:window.beforePanelScroll,after:document.querySelector('#host>div').children[1].scrollTop}));assert.equal(liveScroll.before,liveScroll.after);
    interactions.push({scheme,...live,handleIdle,handleHover,handlePressed,liveScroll});assert.deepEqual(errors,[]);
    await page.emulateMedia({reducedMotion:'no-preference'});await page.evaluate(()=>window.updatePanel({accent:'#b84a50'}));await settle();
    await page.evaluate(()=>{for(const animation of document.querySelector('#host>div').getAnimations())animation.pause();});
    for(const time of [0,20,50,100,150,199,200]){await page.evaluate(time=>{for(const animation of document.querySelector('#host>div').getAnimations())animation.currentTime=time;},time);const sample=await measure();motion.push({scheme,time,left:sample.paint.left,duration:sample.transition.duration,timing:sample.transition.timing});}
    await page.evaluate(()=>window.showPanel({id:'keyboard',width:320,height:320}));await settle();await page.evaluate(()=>document.activeElement.blur());await page.keyboard.press('Tab');
    const focused=await page.evaluate(()=>document.activeElement===document.querySelector('#host>div').children[1]);await page.keyboard.press('PageDown');await page.waitForTimeout(400);const down=(await measure()).scroll.offset;
    await page.keyboard.press('Home');await page.waitForTimeout(400);keyboard.push({scheme,focused,pageDown:down,home:(await measure()).scroll.offset});
   }finally{await page.close();}
  }
 }finally{await browser.close();}
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify({format:'orchard-panel-reference-v1',repository:'gaulatti/bleecker',commit:pin,source:file,sourceSha256:dependencies[0].sha256,dependencies,scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(client)),cssSha256:hash(css),fonts,browser:browser.version(),cases,interactions,motion,keyboard},null,2)+'\n');
 console.log(JSON.stringify({cases:cases.length,pngs:cases.filter(row=>row.file).length,interactions:interactions.length,motion:motion.length}));
})().catch(error=>{console.error(error);process.exitCode=1;});
