const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};
const source=option('--source-root'),tooling=option('--tooling-root'),output=option('--output'),playwright=option('--playwright-module'),pin='a86059013cd59684ce452849278ea81e734f2066',hash=b=>crypto.createHash('sha256').update(b).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});const dependencies=[];
 for(const name of ['src/components/scroll-area.tsx','stories/components/ScrollArea.stories.tsx','src/utils/cn.ts','src/styles/global.css','src/styles/tokens.generated.css','package-lock.json']){const bytes=await fs.readFile(path.join(source,name));assert.deepEqual(bytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024}));dependencies.push({path:name,sha256:hash(bytes)});}
 assert.equal(dependencies[0].sha256,'20c254f224ec44aa8e965674435f40e7f5fc05ce4bdd8abe3c4dc443df5cd78f');
 const runtime=path.join(source,'node_modules'),lock=JSON.parse(await fs.readFile(path.join(source,'package-lock.json'))),modules=[];
 const client=path.join(__dirname,'scroll-area-client.tsx');const bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/scroll-area':path.join(source,'src/components/scroll-area.tsx'),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,metafile:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
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
 await fs.writeFile(path.join(output,'input.css'),input+'\n@source inline("border border-sand/20 p-2");');execFileSync(process.execPath,[path.join(tooling,'node_modules/@tailwindcss/cli/dist/index.mjs'),'-i',path.join(output,'input.css'),'-o',path.join(output,'source.css'),'--minify']);const css=await fs.readFile(path.join(output,'source.css'));
 const fonts=[];let faces='';for(const [name,family,weight]of[['EncodeSans-Regular.ttf','Encode Sans',400],['EncodeSans-Medium.ttf','Encode Sans',500],['EncodeSans-SemiBold.ttf','Encode Sans',600],['LibreFranklin-Regular.ttf','Libre Franklin',400]]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+name);fonts.push({name,family,weight,sha256:hash(bytes)});faces+=`@font-face{font-family:"${family}";font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge',ignoreDefaultArgs:['--hide-scrollbars']}),browserVersion=browser.version(),cases=[],paints=[],interactions=[],motionStyles=[];
 try{for(const scheme of ['Light','Dark']){
  const page=await browser.newPage({viewport:{width:800,height:700},deviceScaleFactor:1,reducedMotion:'reduce',locale:'en-US',timezoneId:'America/New_York'}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}html,body{margin:0;width:100%;height:100%;overflow:hidden}body{background:var(--color-background)}${faces}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
  await page.evaluate(async()=>{await document.fonts.load('400 16px "Encode Sans"');await document.fonts.ready;});
  const settle=()=>page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
  const stable=()=>page.evaluate(()=>new Promise((resolve,reject)=>{let last='',same=0,ticks=0;function sample(){const el=document.querySelector('#area'),next=el.scrollLeft+','+el.scrollTop;if(next===last)same++;else same=0;last=next;if(same>=8)resolve(null);else if(++ticks>150)reject(new Error('Source scroll offsets did not settle.'));else requestAnimationFrame(sample);}requestAnimationFrame(sample);}));
  const measure=()=>page.evaluate(()=>{
   const area=document.querySelector('#area'),content=document.querySelector('#original-content'),s=getComputedStyle(area),box=node=>{const r=node.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};},pseudo=name=>{const p=getComputedStyle(area,name);return{width:p.width,height:p.height,background:p.backgroundColor,radius:p.borderRadius,display:p.display};};
   return{area:box(area),following:box(document.querySelector('#following')),content:content?box(content):null,client:{width:area.clientWidth,height:area.clientHeight,left:area.clientLeft,top:area.clientTop},scroll:{width:area.scrollWidth,height:area.scrollHeight,left:area.scrollLeft,top:area.scrollTop},overflow:{x:s.overflowX,y:s.overflowY},maxHeight:s.maxHeight,height:s.height,boxSizing:s.boxSizing,direction:s.direction,padding:[s.paddingLeft,s.paddingTop,s.paddingRight,s.paddingBottom].map(parseFloat),border:[s.borderLeftWidth,s.borderTopWidth,s.borderRightWidth,s.borderBottomWidth].map(parseFloat),scrollbar:pseudo('::-webkit-scrollbar'),track:pseudo('::-webkit-scrollbar-track'),thumb:pseudo('::-webkit-scrollbar-thumb'),scrollBehavior:s.scrollBehavior,role:area.getAttribute('role'),name:area.getAttribute('aria-label'),marker:area.getAttribute('data-marker'),tabIndex:area.tabIndex,focused:document.activeElement?.id??'',originalPresent:content!==null};
  });
  const show=async spec=>{await page.mouse.move(0,0);await page.setViewportSize({width:800,height:700});await page.evaluate(value=>window.showScrollArea(value),spec);await settle();await stable();};
  const specs=[];
  for(const orientation of ['vertical','horizontal','both'])for(const rtl of [false,true]){
   for(const[id,patch]of[['overflow',{}],['fitting',{contentWidth:120,contentHeight:80}],['empty',{empty:true}],['natural-width',{contentWidth:null,wrap:true}],['unbounded',{maxHeight:null}],['small-maximum',{maxHeight:'64px'}]])specs.push({id:`${orientation}-${rtl?'rtl':'ltr'}-${id}`,orientation,rtl,...patch});
  }
  for(const[id,patch]of[['default',{}],['zero-maximum',{maxHeight:'0px'}],['fractional',{width:256.5,maxHeight:'173.25px',contentWidth:520.75,contentHeight:480.5}],['relative-maximum',{parentHeight:300,maxHeight:'50%'}],['rem-maximum',{maxHeight:'10rem'}],['style-override',{maxHeight:'240px',styleMaxHeight:'160px'}],['style-clear-maximum',{styleMaxHeight:null}],['caller-overflow',{orientation:'vertical',styleOverflow:'auto'}],['caller-box',{orientation:'both',custom:true}],['explicit-height',{orientation:'both',height:150}],['small-content-max',{maxHeight:'300px',contentHeight:80,contentWidth:120}]])specs.push({id,...patch});
  for(const spec of specs){await show(spec);const measurement=await measure(),file='scroll-area-'+scheme+'-'+spec.id+'.png',png=await page.screenshot({path:path.join(output,file)});cases.push({id:spec.id,scheme,spec,measurement,file,pngSha256:hash(png)});}
  for(const orientation of ['vertical','horizontal','both']){
   await show({orientation});const area=(await measure()).area,point=orientation==='horizontal'?{x:area.x+30,y:area.y+area.height-7}:{x:area.x+area.width-3,y:area.y+30};
   for(const hover of [false,true]){if(hover)await page.mouse.move(point.x,point.y);await settle();const measurement=await measure(),file='scroll-area-'+scheme+'-'+orientation+'-thumb-'+(hover?'hover':'rest')+'.png',png=await page.screenshot({path:path.join(output,file)});paints.push({scheme,orientation,hover,point,measurement,file,pngSha256:hash(png)});}
  }
  for(const orientation of ['vertical','horizontal','both'])for(const rtl of [false,true]){
   await show({orientation,rtl,editor:true,tabIndex:0});const samples=[];const sample=async label=>samples.push({label,measurement:await measure()});await sample('initial');
   await page.locator('#area').evaluate(el=>el.scrollTo({left:80,top:120,behavior:'instant'}));await stable();await sample('programmatic-positive');
   await page.locator('#area').evaluate(el=>el.scrollTo({left:-80,top:120,behavior:'instant'}));await stable();await sample('programmatic-negative');
   await page.locator('#area').evaluate(el=>el.scrollTo({left:1e6,top:1e6,behavior:'instant'}));await stable();await sample('end-positive');
   await page.locator('#area').evaluate(el=>el.scrollTo({left:-1e6,top:1e6,behavior:'instant'}));await stable();await sample('end-negative');
   await page.locator('#area').evaluate(el=>el.scrollTo({left:0,top:0,behavior:'instant'}));await stable();await page.locator('#area').hover();await page.mouse.wheel(0,120);await stable();await sample('vertical-wheel');
   await page.mouse.move(0,0);await page.locator('#area').focus();await page.keyboard.press('Home');await stable();await page.keyboard.press('ArrowDown');await stable();await sample('arrow-down');await page.keyboard.press('End');await stable();await sample('keyboard-end');
   await page.locator('#area').evaluate(el=>el.scrollTo({left:0,top:0,behavior:'instant'}));await stable();await page.locator('#editor').fill('Caller edit');await page.locator('#editor').evaluate(el=>{el.setSelectionRange(2,5);window.originalScrollEditor=el;});
   await page.evaluate(()=>window.updateScrollArea({orientation:'both',styleMaxHeight:'160px'}));await settle();await stable();const retained=await page.evaluate(()=>{const el=document.querySelector('#editor');return{original:el===window.originalScrollEditor,text:el.value,start:el.selectionStart,end:el.selectionEnd};});await sample('orientation-and-maximum-change');
   await page.evaluate(()=>window.updateScrollArea({contentWidth:120,contentHeight:80}));await settle();await stable();await sample('content-shrink');
   interactions.push({id:orientation+'-'+(rtl?'rtl':'ltr'),scheme,orientation,rtl,samples,retained,scrollCallbackObserved:await page.evaluate(()=>window.scrollAreaEventCount>0)});
  }
  for(const orientation of ['vertical','horizontal']){
   await show({orientation});const area=(await measure()).area,vertical=orientation==='vertical',point=vertical?{x:area.x+area.width-3,y:area.y+30}:{x:area.x+30,y:area.y+area.height-7};
   const before=await measure();await page.mouse.move(point.x,point.y);await page.mouse.down();await page.mouse.move(point.x+(vertical?0:60),point.y+(vertical?60:0),{steps:6});await page.mouse.up();await stable();interactions.push({id:'thumb-drag-'+orientation,scheme,orientation,delta:60,before,after:await measure()});
  }
  for(const reduced of [false,true]){await page.emulateMedia({reducedMotion:reduced?'reduce':'no-preference'});await show({});motionStyles.push({scheme,reduced,scrollBehavior:(await measure()).scrollBehavior});}
  assert.deepEqual(errors,[]);await page.close();
 }}finally{await browser.close();}
 const report={format:'orchard-scroll-area-reference-v1',repository:'gaulatti/bleecker',commit:pin,source:'src/components/scroll-area.tsx',sourceSha256:dependencies[0].sha256,dependencies,modules,fonts,browser:browserVersion,headless:true,ignoredDefaultArgs:['--hide-scrollbars'],scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(client)),cssSha256:hash(css),cases,paints,interactions,motionStyles};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({states:cases.length,scrollbarPaintStates:paints.length,histories:interactions.length,pngs:cases.length+paints.length}));
})().catch(error=>{console.error(error);process.exitCode=1;});
