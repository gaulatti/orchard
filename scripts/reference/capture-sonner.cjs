const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto'),{execFileSync}=require('node:child_process');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};
const source=option('--source-root'),tooling=option('--tooling-root'),playwright=option('--playwright-module'),output=option('--output'),pin='a86059013cd59684ce452849278ea81e734f2066';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
async function main(){
 assert(!await fs.stat(path.join(output,'Sonner.json')).catch(()=>null),'Never overwrite a captured receipt');
 await fs.mkdir(output,{recursive:true});const dependencies=[];
 for(const name of ['src/components/sonner.tsx','src/components/button.tsx','src/utils/cn.ts','src/core/index.ts','src/core/contracts.ts','src/styles/global.css','src/styles/tokens.generated.css','package-lock.json','stories/components/Sonner.stories.tsx']){
  const bytes=await fs.readFile(path.join(source,name));assert.deepEqual(bytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024}));
  dependencies.push({path:name,sha256:hash(bytes)});const target=path.join(output,'Source',name);await fs.mkdir(path.dirname(target),{recursive:true});await fs.writeFile(target,bytes);
 }
 assert.equal(dependencies[0].sha256,'9abd2f6c1f907708e499170c6368e20f8400afb0c0c5dc07f60fb4635289154d');
 const runtime=path.join(source,'node_modules'),client=path.join(__dirname,'sonner-client.tsx'),lock=JSON.parse(await fs.readFile(path.join(source,'package-lock.json'))),modules=[];
 for(const name of ['react','react-dom','lucide-react']){const bytes=await fs.readFile(path.join(runtime,name,'package.json')),pkg=JSON.parse(bytes);assert.equal(pkg.version,lock.packages['node_modules/'+name].version);modules.push({name,version:pkg.version,packageSha256:hash(bytes)});}
 const toolingModules=[];for(const name of ['tailwindcss','@tailwindcss/cli','esbuild']){const bytes=await fs.readFile(path.join(tooling,'node_modules',name,'package.json')),pkg=JSON.parse(bytes);if(name!=='esbuild')assert.equal(pkg.version,lock.packages['node_modules/'+name].version);toolingModules.push({name,version:pkg.version,packageSha256:hash(bytes)});}
 const bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/sonner':path.join(source,'src/components/sonner.tsx'),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],metafile:true,define:{'process.env.NODE_ENV':'"production"'}});
 const slash=s=>s.replaceAll('\\','/');
 const input=(await fs.readFile(path.join(source,'src/styles/global.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'').replace('@import "tailwindcss" source(none);','@import "'+slash(path.join(tooling,'node_modules/tailwindcss/index.css'))+'" source(none);').replace('@import "./tokens.generated.css";','@import "'+slash(path.join(source,'src/styles/tokens.generated.css'))+'";').replace('@source "../**/*.{ts,tsx}";','@source "'+slash(path.join(source,'src/**/*.{ts,tsx}'))+'";');
 await fs.writeFile(path.join(output,'input.css'),input);execFileSync(process.execPath,[path.join(tooling,'node_modules/@tailwindcss/cli/dist/index.mjs'),'-i',path.join(output,'input.css'),'-o',path.join(output,'source.css'),'--minify']);
 const css=await fs.readFile(path.join(output,'source.css')),fonts=[],bundledFiles=[];let faces='';
 for(const [weight,name]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+name);fonts.push({weight,name,sha256:hash(bytes)});faces+=`@font-face{font-family:"Encode Sans";font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 for(const file of Object.keys(bundle.metafile.inputs))bundledFiles.push({path:slash(path.resolve(file)),sha256:hash(await fs.readFile(path.resolve(file)))});
 await fs.writeFile(path.join(output,'bundle.js'),bundle.outputFiles[0].contents);
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[],histories=[];
 const html=scheme=>`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0;overflow:hidden}${faces}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`;
 async function prepare(page,scheme){
  const errors=[],network=[];await page.route('**/*',route=>{network.push(route.request().url());return route.abort();});page.on('pageerror',e=>errors.push(e.message));
  await page.setContent(html(scheme));await page.evaluate(async()=>{for(const weight of [400,500,600])await document.fonts.load(weight+' 14px "Encode Sans"');await document.fonts.ready;});return{errors,network};
 }
 const measure=page=>page.evaluate(()=>{
  const box=node=>{const b=node.getBoundingClientRect();return{x:b.x,y:b.y,width:b.width,height:b.height};};
  const paint=node=>{const s=getComputedStyle(node);return{color:s.color,background:s.backgroundColor,border:s.borderColor,borderWidth:s.borderWidth,radius:s.borderRadius,padding:[s.paddingLeft,s.paddingTop,s.paddingRight,s.paddingBottom].map(parseFloat),fontSize:s.fontSize,fontWeight:s.fontWeight,lineHeight:s.lineHeight,letterSpacing:s.letterSpacing,gap:s.gap,opacity:Number(s.opacity),translate:s.translate,scale:s.scale,transform:s.transform,transitionDuration:s.transitionDuration,transitionTimingFunction:s.transitionTimingFunction,shadow:s.boxShadow,pointerEvents:s.pointerEvents};};
  const hosts=[...document.querySelectorAll('body > div[aria-label="Notifications"]')];
  return{hosts:hosts.map(host=>({box:box(host),paint:paint(host),label:host.getAttribute('aria-label'),live:host.getAttribute('aria-live'),items:[...host.querySelectorAll('[role="alert"]')].map(item=>({box:box(item),paint:paint(item),visibleClass:item.classList.contains('opacity-100'),role:item.getAttribute('role'),title:{text:item.querySelector('p').textContent,box:box(item.querySelector('p')),paint:paint(item.querySelector('p'))},description:[...item.querySelectorAll('p')].slice(1).map(p=>({text:p.textContent,box:box(p),paint:paint(p)})),icons:[...item.querySelectorAll('svg')].map(svg=>({box:box(svg),paint:paint(svg),paths:[...svg.children].map(n=>({tag:n.tagName,attributes:Object.fromEntries([...n.attributes].map(a=>[a.name,a.value]))}))})),buttons:[...item.querySelectorAll('button')].map(button=>({text:button.textContent,name:button.getAttribute('aria-label'),box:box(button),paint:paint(button)}))}))})),focus:document.activeElement===document.body?'BODY':document.activeElement?.getAttribute('aria-label')||document.activeElement?.textContent||null,events:structuredClone(window.sonnerEvents??[])};
 });
 try{for(const scheme of ['Light','Dark']){
  const page=await browser.newPage({viewport:{width:1000,height:760},deviceScaleFactor:1,locale:'en-US',reducedMotion:'reduce'}),guard=await prepare(page,scheme);
  const capture=async(id,spec={},toasts=[{}],state=null)=>{
   await page.setViewportSize({width:spec.viewportWidth??1000,height:spec.viewportHeight??760});await page.mouse.move(500,20);
   await page.evaluate(({spec,toasts})=>{window.showSonner(spec);for(const t of toasts)window.invokeSonner({duration:0,...t});},{spec,toasts});await page.waitForTimeout(280);
   const button=state?.startsWith('action')?page.locator('[role="alert"] button').first():page.locator('button[aria-label="Dismiss"]').first();
   if(state?.endsWith('hover'))await button.hover();if(state?.endsWith('focus'))await button.focus();if(state?.endsWith('pressed')){await button.hover();await page.mouse.down();}
   await page.waitForTimeout(240);const measurement=await measure(page),file=`sonner-${cases.length}-${scheme}.png`,bytes=await page.screenshot({path:path.join(output,file)});
   cases.push({id,scheme,spec,toasts,state,measurement,file,pngSha256:hash(bytes)});if(state?.endsWith('pressed'))await page.mouse.up();
  };
  await capture('empty',{},[]);
  for(const variant of ['default','success','error','warning','info'])await capture('variant-'+variant,{},[{variant,description:'A fictional local update.'}]);
  for(const position of ['top-left','top-center','top-right','bottom-left','bottom-center','bottom-right'])await capture('position-'+position,{position});
  for(const actionVariant of [undefined,'primary','secondary','outline','subtle','ghost','destructive','link'])await capture('action-'+(actionVariant??'default'),{},[{hasAction:true,...(actionVariant?{actionVariant}:{}),description:'A caller-owned fictional action.'}]);
  await capture('empty-title',{},[{title:''}]);await capture('empty-description',{},[{description:''}]);await capture('long',{},[{title:'A deliberately long fictional title that wraps onto multiple lines',description:'Fictional local content that remains visible and wraps without truncating the original description.'}]);
  await capture('unicode',{},[{title:'Straße 東京 é العربية',description:'Fictional 日本語 descrição — שלום.'}]);await capture('rtl',{rtl:true},[{description:'A fictional local update.',hasAction:true,actionVariant:'link'}]);
  await capture('narrow',{viewportWidth:200},[{description:'A fictional local update.',hasAction:true}]);await capture('font200',{rootFontSize:32},[{description:'A fictional local update.',hasAction:true}]);
  await capture('stack',{},[{title:'First original',variant:'success'},{title:'Second original',variant:'error',description:'Second description.'},{title:'Third original',hasAction:true,actionVariant:'link'}]);
  await capture('unbounded-stack',{},Array.from({length:12},(_,i)=>({title:'Original item '+i})));
  for(const target of ['action','dismiss'])for(const state of ['hover','focus','pressed'])await capture(target+'-'+state,{},[{hasAction:true,actionVariant:'link'}],target+'-'+state);
  assert.deepEqual(guard.errors,[]);assert.deepEqual(guard.network,[]);await page.close();
  // Deterministic source JS deadlines; CSS tween/raster motion is a separate gate.
  const clock=await browser.newPage({viewport:{width:1000,height:760},reducedMotion:'reduce'});await clock.clock.install({time:new Date('2026-10-18T12:00:00Z')});await clock.clock.pauseAt(new Date('2026-10-18T12:00:00Z'));const clockGuard=await prepare(clock,scheme);
  const snapshot=async()=>{const m=await measure(clock);return{hosts:m.hosts.map(h=>({label:h.label,live:h.live,items:h.items.map(i=>({title:i.title.text,description:i.description.map(d=>d.text),visible:i.visibleClass,buttons:i.buttons.map(b=>({text:b.text,name:b.name}))}))})),events:m.events};};
  const reset=spec=>clock.evaluate(spec=>window.showSonner(spec??{}),spec??{}),add=spec=>clock.evaluate(spec=>window.invokeSonner(spec??{}),spec??{}),advance=ms=>clock.clock.runFor(ms);
  const history=async(id,perform)=>{await reset({});const steps=[];const take=async label=>steps.push({label,value:await snapshot()});const extra=await perform(take);histories.push({id,scheme,steps,...extra,issuedIds:await clock.evaluate(()=>window.sonnerIds),retainedReactInstance:true});};
  await history('default-timeout',async take=>{await add({});await take('added');await advance(16);await take('frame');await advance(3983);await take('before-timeout');await advance(1);await take('hidden-at-4000');await advance(299);await take('retained-299');await advance(1);await take('removed-300');});
  await history('button-dismiss',async take=>{await add({duration:0});await advance(16);await take('before');await clock.evaluate(()=>document.querySelector('button[aria-label="Dismiss"]').click());await take('hidden');await advance(299);await take('retained-299');await advance(1);await take('removed-300');});
  await history('api-dismiss',async take=>{const id=await add({duration:0});await advance(16);await take('before');await clock.evaluate(id=>window.dismissSonner(id),id);await take('removed-immediately');});
  await history('action-retained',async take=>{await add({duration:0,hasAction:true});await advance(16);await clock.evaluate(()=>document.querySelector('[role="alert"] button').click());await take('after-action');});
  await history('action-adds',async take=>{await add({duration:0,hasAction:true,actionAdds:true});await advance(16);await clock.evaluate(()=>document.querySelector('[role="alert"] button').click());await advance(16);await take('after-action');});
  await history('action-dismisses',async take=>{await add({duration:0,hasAction:true,actionDismisses:true});await advance(16);await clock.evaluate(()=>document.querySelector('[role="alert"] button').click());await take('after-action');});
  await history('action-throws',async take=>{await add({duration:0,hasAction:true,throwAction:true});await advance(16);await clock.evaluate(()=>document.querySelector('[role="alert"] button').click());await take('after-action');});
  await history('persistent-values',async take=>{for(const spec of [{duration:0},{duration:-1},{duration:null},{durationKind:'undefined'},{durationKind:'nan'}])await add(spec);await advance(5000);await take('after-5000');});
  await history('default-change',async take=>{await reset({duration:100});await add({title:'Original timer'});await advance(16);await clock.evaluate(()=>window.updateSonner({duration:8000}));await add({title:'Later timer'});await advance(84);await take('old-hidden');await advance(300);await take('old-removed');await advance(7615);await take('new-before');await advance(1);await take('new-hidden');});
  await history('duration-override',async take=>{await reset({duration:100});await add({title:'Persistent override',duration:0});await add({title:'Explicit timer',duration:50});await advance(16);await take('frame');await advance(34);await take('explicit-hidden');await advance(300);await take('persistent-only');});
  await history('short-timeout-before-frame',async take=>{await add({duration:1});await take('added');await advance(1);await take('timed-out');await advance(16);await take('late-frame');await advance(284);await take('removed');});
  await history('button-before-frame',async take=>{await add({duration:0});await clock.evaluate(()=>document.querySelector('button[aria-label="Dismiss"]').click());await take('hidden');await advance(16);await take('late-frame');await advance(284);await take('removed');});
  await history('retained-action',async take=>{await add({duration:0,hasAction:true});await advance(16);await clock.evaluate(()=>document.querySelector('button[aria-label="Dismiss"]').click());await take('hidden');await clock.evaluate(()=>document.querySelector('[role="alert"] button').click());await take('action-while-retained');await advance(300);await take('removed');});
  await history('unknown-dismiss',async take=>{await add({duration:0});await advance(16);await clock.evaluate(()=>window.dismissSonner('fictional-nonexistent-id'));await take('retained');});
  await history('escape',async take=>{await add({duration:0});await advance(16);await clock.keyboard.press('Escape');await take('retained');});
  await history('hover-focus-no-pause',async take=>{await add({duration:100});await advance(16);await clock.evaluate(()=>{const b=document.querySelector('button[aria-label="Dismiss"]');b.dispatchEvent(new MouseEvent('mouseover',{bubbles:true}));b.focus();});await advance(84);await take('hidden-at-deadline');});
  await history('convenience-methods',async take=>{for(const method of ['success','error','warning','info'])await add({method,title:method,duration:0});await advance(16);await take('ordered');});
  await history('unmounted-api',async take=>{await clock.evaluate(()=>window.updateSonner({unmounted:true}));const id=await add({});await take('unmounted');return{returnedId:id};});
  await history('multiple-host-cleanup',async take=>{await reset({providers:[{key:'first',duration:0,position:'top-left'},{key:'last',duration:0,position:'bottom-right'}]});await add({title:'Last mounted'});await advance(16);await take('last-host');await clock.evaluate(()=>window.updateSonner({providers:[{key:'last',duration:0,position:'bottom-right'}]}));const id=await add({title:'After earlier unmount'});await take('earlier-unmounted');return{returnedId:id};});
  assert.deepEqual(clockGuard.errors,['fictional sonner action failed']);assert.deepEqual(clockGuard.network,[]);await clock.close();
  console.log(JSON.stringify({scheme,states:cases.filter(c=>c.scheme===scheme).length,histories:histories.filter(c=>c.scheme===scheme).length}));
 }
 const result={format:'orchard-sonner-reference-v1',commit:pin,dependencies,modules,toolingModules,fonts,bundledFiles,browser:browser.version(),scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(client)),cssSha256:hash(css),bundleSha256:hash(bundle.outputFiles[0].contents),viewport:{width:1000,height:760},reducedMotion:'reduce',networkRequests:0,cases,histories,nativeImplemented:false,nativeComponentAccepted:false};
 await fs.writeFile(path.join(output,'Sonner.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({states:cases.length,histories:histories.length,bundledFiles:bundledFiles.length}));
 }finally{await browser.close();}
}
main().catch(e=>{console.error(e);process.exitCode=1;});
