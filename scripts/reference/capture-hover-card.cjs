const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};
const source=option('--source-root'),tooling=option('--tooling-root'),output=option('--output'),playwright=option('--playwright-module'),pin='a86059013cd59684ce452849278ea81e734f2066',hash=b=>crypto.createHash('sha256').update(b).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});const dependencies=[];
 for(const name of ['src/components/hover-card.tsx','stories/components/HoverCard.stories.tsx','src/utils/cn.ts','src/styles/global.css','src/styles/tokens.generated.css','package-lock.json']){const bytes=await fs.readFile(path.join(source,name));assert.deepEqual(bytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024}));dependencies.push({path:name,sha256:hash(bytes)});}
 assert.equal(dependencies[0].sha256,'f8af0dfd5c71926359b269f15d8782bd97fbd3fd18579346f59b28a9bfd0fcb6');
 const runtime=path.join(source,'node_modules'),lock=JSON.parse(await fs.readFile(path.join(source,'package-lock.json'))),modules=[];
 const client=path.join(__dirname,'hover-card-client.tsx');const bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/hover-card':path.join(source,'src/components/hover-card.tsx'),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,metafile:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const resolvedPackages=new Map();
 for(const inputPath of Object.keys(bundle.metafile.inputs)){
  const absolute=path.resolve(inputPath);if(!absolute.startsWith(runtime+path.sep))continue;
  let directory=path.dirname(absolute),packageFile;
  while(directory.startsWith(runtime)){try{await fs.access(path.join(directory,'package.json'));packageFile=path.join(directory,'package.json');break;}catch{directory=path.dirname(directory);}}
  assert(packageFile,'No package owns '+absolute);const lockPath=path.relative(source,directory).replaceAll('\\','/');const bytes=await fs.readFile(packageFile),pkg=JSON.parse(bytes);assert.equal(typeof pkg.name,'string');assert.equal(typeof pkg.version,'string');assert(lock.packages[lockPath],lockPath);assert.equal(pkg.version,lock.packages[lockPath].version,lockPath);
  let row=resolvedPackages.get(directory);if(!row){row={name:pkg.name,version:pkg.version,lockPath,packageSha256:hash(bytes),files:[]};resolvedPackages.set(directory,row);}
  row.files.push({path:path.relative(runtime,absolute).replaceAll('\\','/'),sha256:hash(await fs.readFile(absolute))});
 }
 for(const row of [...resolvedPackages.values()].sort((a,b)=>a.lockPath.localeCompare(b.lockPath))){row.files.sort((a,b)=>a.path.localeCompare(b.path));modules.push(row);}
 const slash=s=>s.replaceAll('\\','/');const input=(await fs.readFile(path.join(source,'src/styles/global.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'').replace('@import "tailwindcss" source(none);','@import "'+slash(path.join(tooling,'node_modules/tailwindcss/index.css'))+'" source(none);').replace('@import "./tokens.generated.css";','@import "'+slash(path.join(source,'src/styles/tokens.generated.css'))+'";').replace('@source "../**/*.{ts,tsx}";','@source "'+slash(path.join(source,'src/**/*.{ts,tsx}'))+'";');
 await fs.writeFile(path.join(output,'input.css'),input+'\n@source inline("w-96 p-6 rounded-none");');execFileSync(process.execPath,[path.join(tooling,'node_modules/@tailwindcss/cli/dist/index.mjs'),'-i',path.join(output,'input.css'),'-o',path.join(output,'source.css'),'--minify']);const css=await fs.readFile(path.join(output,'source.css'));
 const fonts=[];let faces='';for(const [name,family,weight]of[['EncodeSans-Regular.ttf','Encode Sans',400],['EncodeSans-Medium.ttf','Encode Sans',500],['EncodeSans-SemiBold.ttf','Encode Sans',600],['LibreFranklin-Regular.ttf','Libre Franklin',400]]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+name);fonts.push({name,family,weight,sha256:hash(bytes)});faces+=`@font-face{font-family:"${family}";font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),browserVersion=browser.version(),cases=[],interactions=[],transitions=[];
 try{for(const scheme of ['Light','Dark']){
  const page=await browser.newPage({viewport:{width:800,height:600},deviceScaleFactor:1,reducedMotion:'reduce',locale:'en-US',timezoneId:'America/New_York'}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}html,body{margin:0;width:100%;height:100%;overflow:hidden}body{background:var(--color-background)}${faces}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
  await page.evaluate(async()=>{await Promise.all([['Encode Sans',400],['Encode Sans',500],['Encode Sans',600],['Libre Franklin',400]].map(([family,weight])=>document.fonts.load(weight+' 16px "'+family+'"')));await document.fonts.ready;});
  const settle=()=>page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
  const finish=()=>page.evaluate(()=>document.querySelector('[role="tooltip"]').getAnimations().forEach(a=>a.finish()));
  const measure=()=>page.evaluate(()=>{
   const trigger=document.querySelector('#trigger'),surface=document.querySelector('[role="tooltip"]'),wrapper=surface.parentElement,style=getComputedStyle(surface),content=surface.querySelector('#original-content'),box=node=>{const r=node.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};};
   const r=surface.getBoundingClientRect(),x=Math.max(0,Math.min(innerWidth-1,r.x+r.width/2)),y=Math.max(0,Math.min(innerHeight-1,r.y+r.height/2)),hit=document.elementFromPoint(x,y),token=getComputedStyle(document.documentElement);
   return{trigger:box(trigger),wrapper:box(wrapper),following:box(document.querySelector('#following')),surface:box(surface),content:content?box(content):null,hovered:wrapper.matches(':hover'),focused:document.activeElement?.id??'',disabled:trigger.disabled,role:surface.getAttribute('role'),ariaExpanded:trigger.getAttribute('aria-expanded'),ariaDescribedBy:trigger.getAttribute('aria-describedby'),parentIsOriginalWrapper:surface.parentElement===trigger.parentElement,pointerEvents:style.pointerEvents,opacity:parseFloat(style.opacity),visibility:style.visibility,display:style.display,hitInside:hit===surface||surface.contains(hit),clipHost:getComputedStyle(document.querySelector('#anchor-host')).overflow,paint:{background:style.backgroundColor,border:style.borderColor,color:style.color,shadow:style.boxShadow,radius:parseFloat(style.borderRadius),padding:[style.paddingLeft,style.paddingTop,style.paddingRight,style.paddingBottom].map(parseFloat),borderWidth:parseFloat(style.borderTopWidth),font:style.fontFamily,fontSize:parseFloat(style.fontSize),lineHeight:parseFloat(style.lineHeight),zIndex:style.zIndex},motion:{duration:style.transitionDuration,properties:style.transitionProperty,easing:style.transitionTimingFunction,translate:style.translate,transform:style.transform,enter:token.getPropertyValue('--motion-surface').trim(),ease:token.getPropertyValue('--ease-premium').trim()}};
  });
  const show=async spec=>{await page.mouse.move(0,0);await page.setViewportSize({width:spec.viewportWidth??800,height:spec.viewportHeight??600});await page.evaluate(value=>window.showHoverCard(value),spec);await settle();if(spec.hover)await page.locator('#trigger').hover();await finish();await settle();};
  const specs=[];for(const rtl of [false,true])for(const side of ['top','bottom'])for(const hover of [false,true])specs.push({id:`${rtl?'rtl':'ltr'}-${side}-${hover?'open':'closed'}`,side,rtl,hover});
  for(const[id,patch]of [['default-side',{}],['wide-trigger',{triggerWidth:320}],['fractional',{x:320.375,y:240.625,triggerWidth:80.5,triggerHeight:32.25}],['top-edge',{side:'top',y:8}],['bottom-edge',{y:560}],['left-edge',{x:4}],['right-edge',{x:712}],['small-viewport',{viewportWidth:200,viewportHeight:160,x:60,y:60}],['empty',{empty:true}],['tall-content',{contentHeight:420}],['wide-content',{contentWidth:340}],['disabled',{disabled:true}],['custom-surface',{custom:true}],['clipped',{clip:true}]])specs.push({id,hover:true,...patch});
  for(const spec of specs){await show(spec);const measurement=await measure(),file='hover-card-'+scheme+'-'+spec.id+'.png',png=await page.screenshot({path:path.join(output,file)});cases.push({id:spec.id,scheme,spec,measurement,file,pngSha256:hash(png)});}
  await show({editor:true});const samples=[];const sample=async label=>samples.push({label,measurement:await measure(),clicks:await page.evaluate(()=>window.hoverCardClicks)});
  await sample('initial');await page.locator('#trigger').focus();await page.keyboard.press('Enter');await sample('keyboard-trigger');await page.keyboard.press('Tab');await sample('closed-content-tab');await page.keyboard.press('Escape');await sample('closed-escape');
  await page.locator('#trigger').hover();await finish();await settle();await sample('trigger-hover');await page.keyboard.press('Escape');await sample('hovered-escape');
  await page.locator('#editor').fill('Caller edit');await page.locator('#editor').evaluate(node=>{node.setSelectionRange(2,5);window.originalHoverCardEditor=node;});await page.evaluate(()=>window.updateHoverCard({side:'top',contentHeight:80}));await settle();await finish();
  const retained=await page.evaluate(()=>{const node=document.querySelector('#editor');return{original:node===window.originalHoverCardEditor,text:node.value,start:node.selectionStart,end:node.selectionEnd};});await sample('live-content-and-side');
  await page.mouse.move(0,0);await finish();await sample('outside');interactions.push({id:'keyboard-edit-and-hover',scheme,samples,retained});
  await show({hover:true});const pointer=[];const point=async label=>pointer.push({label,measurement:await measure()});await point('trigger');
  let r=await page.locator('[role="tooltip"]').boundingBox();await page.mouse.move(r.x+20,r.y+20);await finish();await settle();await point('direct-content');
  await page.locator('#trigger').hover();await finish();await settle();const anchor=await page.locator('#trigger').boundingBox();await page.mouse.move(anchor.x+anchor.width/2,anchor.y+anchor.height+4);await finish();await settle();await point('gap');
  r=await page.locator('[role="tooltip"]').boundingBox();await page.mouse.move(r.x+20,r.y+20);await finish();await settle();await point('closed-content');interactions.push({id:'pointer-ownership-and-gap',scheme,samples:pointer});
  // Freeze this isolated browser page's animation clock before trusted hover
  // input, so even the source's 0.01-ms reduced transition remains observable.
  const animationClock=await page.context().newCDPSession(page);await animationClock.send('Animation.enable');await animationClock.send('Animation.setPlaybackRate',{playbackRate:0});
  for(const reduced of [false,true])for(const side of ['top','bottom']){
   await page.emulateMedia({reducedMotion:reduced?'reduce':'no-preference'});await show({side});
   for(const kind of ['enter','leave']){
    const before=await measure();if(kind==='enter')await page.locator('#trigger').hover();else await page.mouse.move(0,0);
    const timeline=await page.locator('[role="tooltip"]').evaluate(async node=>{
     const animations=node.getAnimations();animations.forEach(a=>a.pause());await Promise.all(animations.map(a=>a.ready));
     const definitions=animations.map(a=>({property:a.transitionProperty,timing:a.effect.getTiming(),frames:a.effect.getKeyframes()})),samples=[];
     for(const milliseconds of [0,55,110,165,220,260]){for(const a of animations)a.currentTime=milliseconds;const s=getComputedStyle(node),r=node.getBoundingClientRect();samples.push({milliseconds,opacity:parseFloat(s.opacity),translate:s.translate,transform:s.transform,x:r.x,y:r.y,pointerEvents:s.pointerEvents});}
     animations.forEach(a=>a.finish());return{definitions,samples};
    });transitions.push({scheme,reduced,side,kind,before,...timeline,after:await measure()});
   }
  }
  await animationClock.detach();assert.deepEqual(errors,[]);await page.close();
 }}finally{await browser.close();}
 const report={format:'orchard-hover-card-reference-v1',repository:'gaulatti/bleecker',commit:pin,source:'src/components/hover-card.tsx',sourceSha256:dependencies[0].sha256,dependencies,modules,fonts,browser:browserVersion,scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(client)),cssSha256:hash(css),cases,interactions,transitions};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({states:cases.length,histories:interactions.length,transitions:transitions.length,pngs:cases.length}));
})().catch(error=>{console.error(error);process.exitCode=1;});
