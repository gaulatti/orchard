const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto');
const{isDeepStrictEqual}=require('node:util');
const args=process.argv.slice(2),option=name=>{const index=args.indexOf(name);if(index<0||!args[index+1])throw Error('Pass '+name+' <absolute module path>.');return path.resolve(args[index+1]);};
const esbuild=require(option('--esbuild-module'));
const{chromium}=require(option('--playwright-module'));
(async()=>{
 const dir=path.resolve(__dirname,'../../artifacts/sidebar-browser'),source=path.resolve(dir,'../line-chart-browser/frozen-source/src/components/sidebar.tsx'),sourceSha256=crypto.createHash('sha256').update(await fs.readFile(source)).digest('hex');
 if(sourceSha256!=='2c06422943eedda2232a04c087b8f8562f614829f2b77229ecc7941488894856')throw Error('Pinned Sidebar differs.');
 const runtime=path.join(dir,'node_modules');
 const build=await esbuild.build({entryPoints:[path.join(__dirname,'sidebar-client.tsx')],bundle:true,platform:'browser',outfile:path.join(dir,'bundle.js'),metafile:true,alias:{'@frozen/sidebar':source,react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const locked=JSON.parse(await fs.readFile(path.resolve(dir,'../chart-theme-reference/bleecker-package-lock.json'),'utf8')),packages=new Map();
 for(const input of Object.keys(build.metafile.inputs)){
  const full=path.resolve(input).replaceAll('\\','/'),at=full.lastIndexOf('/node_modules/');if(at<0)continue;
  const suffix=full.slice(at+14).split('/'),name=suffix[0].startsWith('@')?suffix.slice(0,2).join('/'):suffix[0],root=full.slice(0,at+14)+name;if(packages.has(root))continue;
  const manifest=JSON.parse(await fs.readFile(path.join(root,'package.json'),'utf8')),first=full.indexOf('/node_modules/'),actual=JSON.parse(await fs.readFile(full.slice(0,first+14)+'.package-lock.json','utf8')).packages[root.slice(first+1)];
  if(!actual||actual.version!==manifest.version||!Object.entries(locked.packages).some(([key,v])=>(key==='node_modules/'+name||key.endsWith('/node_modules/'+name))&&v.version===actual.version&&v.integrity===actual.integrity))throw Error('Bundled dependency differs: '+name);
  packages.set(root,{name,version:actual.version,integrity:actual.integrity});
 }
 if([...packages.values()].filter(p=>p.name==='react').length!==1)throw Error('One canonical React is required.');
 await fs.writeFile(path.join(dir,'build-meta.json'),JSON.stringify(build.metafile,null,2));
 const bundle=await fs.readFile(path.join(dir,'bundle.js'),'utf8'),css=(await fs.readFile(path.resolve(dir,'../line-chart-browser/styles.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'');
 const fontFiles=[];let fonts='';for(const[weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){const bytes=await fs.readFile(path.resolve(dir,'../../src/Orchard/Assets/Fonts/'+file));fontFiles.push({file,weight,sha256:crypto.createHash('sha256').update(bytes).digest('hex')});fonts+=`@font-face{font-family:'Encode Sans';font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const icon={kind:'icon'},badge={kind:'badge',text:'3'},slot=text=>({kind:'slot',text});
 const link=(id,label,extra={})=>({id,label,href:'#'+id,...extra});
 const defaults=[link('overview','Overview',{icon,active:true,badge:{kind:'badge',text:'12'}}),{id:'coverage',label:'Coverage',icon,items:[link('drafts','Drafts',{badge}),link('published','Published',{active:true}),{id:'hidden-child',label:'Hidden child',items:[link('grandchild','Hidden grandchild',{active:true})]}]}, {id:'settings',label:'Settings',icon,href:'#settings',items:[link('profile','Profile'),link('security','Security',{disabled:true})]}, {id:'ignored',label:'No destination'}];
 const samples=[
  {id:'default',items:defaults,header:slot('Fictional workspace'),footer:slot('Local only')},
  {id:'collapsed',items:defaults,collapsed:true,header:slot('F'),footer:slot('L')},
  {id:'empty',items:[]},{id:'empty-slots',items:[],header:slot('Header'),footer:slot('Footer')},
  {id:'links',items:[link('a','Alpha'),link('b','Beta'),link('c','Gamma')]},
  {id:'closed',items:[{id:'group',label:'Group',icon,items:[link('a','Alpha'),link('b','Beta')]}]},
  {id:'active-group',items:[{id:'group',label:'Group',active:true,icon,badge,items:[link('a','Alpha')]}]},
  {id:'disabled-group',items:[{id:'group',label:'Disabled group',disabled:true,items:[link('a','Alpha')]}]},
  {id:'disabled-link',items:[link('disabled','Disabled',{disabled:true,icon,badge}),link('enabled','Enabled')]},
  {id:'disabled-active',items:[link('disabled','Disabled active',{disabled:true,active:true,icon,badge})]},
  {id:'group-route',items:[{id:'group',label:'Group with route',href:'#group',items:[link('child','Child')]}]},
  {id:'collapsed-group-route',collapsed:true,items:[{id:'group',label:'Group with route',href:'#group',icon,items:[link('child','Child',{active:true})]}]},
  {id:'collapsed-no-route',collapsed:true,items:[{id:'group',label:'Group without route',icon,items:[link('child','Child',{active:true})]}]},
  {id:'empty-group',items:[{id:'group',label:'Empty group',items:[]},link('leaf','Empty group route',{items:[]})]},
  {id:'empty-href',items:[{id:'empty',label:'Empty href',href:''},link('hash','Hash route'),{id:'group',label:'Group',href:'',items:[link('child','Child')]}]},
  {id:'empty-label',items:[link('empty','',{icon}),link('plain','')]},
  {id:'zero-content',items:[link('zero','Zero',{icon:0,badge:0})],header:0,footer:0},
  {id:'false-content',items:[link('false','False',{icon:false,badge:false})],header:false,footer:false},
  {id:'true-content',items:[link('true','True',{icon:true,badge:true})],header:true,footer:true},
  {id:'empty-string-slots',items:[link('a','Alpha')],header:'',footer:''},
  {id:'plain-slots',items:[link('a','Alpha')],header:'Plain header',footer:'Plain footer'},
  {id:'icon-small',items:[link('a','Small icon',{icon:{kind:'icon',size:12}})]},
  {id:'icon-large',items:[link('a','Large icon',{icon:{kind:'icon',size:28}})]},
  {id:'long-labels',items:[link('long','A very long fictional workspace destination that truncates',{icon,badge}),{id:'group',label:'A very long fictional expandable group title that truncates',icon,badge,items:[link('child','A very long fictional nested destination that truncates',{active:true,badge})]}]},
  {id:'multilingual',items:[link('jp','港口 日本語',{icon,active:true}),{id:'ar',label:'العربية',icon,items:[link('ar-child','مراجعة المحتوى',{active:true})]},link('greek','Δημοσίευση')]},
  {id:'dense',items:Array.from({length:35},(_,i)=>link('item-'+i,'Destination '+i,{icon,active:i===34})),header:slot('Workspace'),footer:slot('Footer')},
  {id:'short',height:140,items:defaults,header:slot('Workspace'),footer:slot('Footer')},
  {id:'tiny-height',height:40,items:defaults,header:slot('Workspace'),footer:slot('Footer')},
  {id:'constrained',hostWidth:180,flex:true,items:defaults},
  {id:'fractional',height:244.5,items:defaults},
  {id:'custom-width',className:'consumer-sidebar',items:defaults,customCss:'.consumer-sidebar{width:320px;padding-left:16px;padding-right:16px}'},
  {id:'custom-link',customLink:true,items:defaults},
  {id:'custom-link-collapsed',customLink:true,collapsed:true,items:defaults},
  {id:'deep-active',items:[{id:'group',label:'Group',items:[{id:'middle',label:'Middle',items:[link('deep','Deep active',{active:true})]},link('visible','Visible child')]}]},
  {id:'state-retention',items:[{id:'group',label:'Retained group',items:[link('child','Child',{active:true})]}]},
  {id:'state-closed',items:[{id:'group',label:'Initially closed',items:[link('child','Child')]}]},
  {id:'slot-buttons',items:[link('a','Alpha')],header:{kind:'button',text:'Header action'},footer:{kind:'button',text:'Footer action'}},
  {id:'state-reorder',items:[{id:'open',label:'Open group',items:[link('open-child','Open child',{active:true})]},{id:'closed',label:'Closed group',items:[link('closed-child','Closed child')]},link('leaf','Leaf')]},
  {id:'state-shape-closed',items:[link('shape','Initial leaf')]},
  {id:'state-shape-open',items:[{id:'shape',label:'Initial group',items:[link('shape-child','Initial child',{active:true})]}]},
  {id:'motion',motion:true,items:defaults}
 ];
 const screenshotsOnly=process.argv.includes('--screenshots-only'),visualIds=['default','collapsed','long-labels','multilingual','short','dense'],snapshots=[];
 const probe=process.argv.includes('--probe'),selected=screenshotsOnly?samples.filter(s=>visualIds.includes(s.id)):probe?samples.filter(s=>['default','collapsed','disabled-link','disabled-group','zero-content','state-retention','state-closed'].includes(s.id)):samples;
 if(probe&&screenshotsOnly)throw Error('Choose --probe or --screenshots-only.');
 const output=probe?'probe-v4.json':'reference-v4.json';let cases=[],previousCapture;
 if(!probe){try{
  previousCapture=JSON.parse(await fs.readFile(path.join(dir,output),'utf8'));
  const expected={captureVersion:4,sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256,react:'19.2.4',collapsible:'1.1.14',compiledCssSha256:crypto.createHash('sha256').update(css).digest('hex'),fontFiles,bundledRuntime:[...packages.values()].sort((a,b)=>a.name.localeCompare(b.name))};
  for(const[key,value]of Object.entries(expected))if(!isDeepStrictEqual(previousCapture[key],value))throw Error('Cannot resume different capture provenance: '+key);
  cases=previousCapture.cases;
 }catch(e){if(e.code!=='ENOENT')throw e;}}
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 async function settle(page){await page.waitForTimeout(260);await page.waitForFunction(()=>{const node=document.querySelector('aside');if(!node)return false;const s=node.getBoundingClientRect(),signature=[s.x,s.y,s.width,s.height,node.innerHTML].join('|'),old=window.__settled;if(old?.signature===signature)old.frames++;else window.__settled={signature,frames:0};return window.__settled.frames>=3},null,{polling:'raf',timeout:2000});}
 try{if(previousCapture&&previousCapture.browser!==browser.version())throw Error('Cannot resume a different browser version.');for(const scheme of probe?['Light']:['Light','Dark'])for(const sample of selected){if(!screenshotsOnly&&cases.some(c=>c.id===sample.id&&c.scheme===scheme))continue;
  const{id,height=480,hostWidth=800,flex=false,customCss='',motion=false,...props}=sample,page=await browser.newPage({viewport:{width:1000,height:700},deviceScaleFactor:1,reducedMotion:motion?'no-preference':'reduce'}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{
   await page.setContent(`<html class="${scheme==='Dark'?'dark':''}"><head><style>${css}${fonts}body{margin:0;background:var(--color-background)}#root{height:${height}px;width:${hostWidth}px;${flex?'display:flex;':''}}${customCss}</style></head><body><button id="before">Before</button><div id="root"></div><button id="after">After</button><script>${bundle}</script></body></html>`);
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(w=>document.fonts.load(w+' 16px "Encode Sans"')));await document.fonts.ready});await page.evaluate(p=>window.showSidebar(p),props);await settle(page);
   const read=()=>page.locator('#root').evaluate(root=>{
    const base=root.getBoundingClientRect(),attrs=e=>Object.fromEntries([...e.attributes].map(a=>[a.name,a.value])),box=e=>{const r=e.getBoundingClientRect();return{x:r.x-base.x,y:r.y-base.y,width:r.width,height:r.height}},style=e=>{const s=getComputedStyle(e);return Object.fromEntries(['display','width','height','padding','margin','gap','fontFamily','fontSize','fontWeight','lineHeight','color','backgroundColor','borderColor','borderWidth','borderRadius','opacity','pointerEvents','boxShadow','transform','translate','rotate','scale','transformOrigin','transitionProperty','transitionDuration','transitionTimingFunction','overflowX','overflowY','textOverflow','whiteSpace','cursor','outlineColor','outlineStyle','outlineWidth','outlineOffset'].map(k=>[k,s[k]]))},shape=e=>({tag:e.tagName,text:e.textContent,attributes:attrs(e),box:box(e),style:style(e)});
    const aside=root.querySelector('aside'),nav=aside.querySelector('nav');return{surface:shape(aside),slots:[...aside.children].filter(e=>e!==nav).map(shape),anonymous:[...aside.childNodes].filter(n=>n.nodeType===3&&n.textContent).map(n=>n.textContent),nav:{...shape(nav),scrollTop:nav.scrollTop,scrollHeight:nav.scrollHeight,clientHeight:nav.clientHeight},rows:[...nav.querySelectorAll('a,button')].map(e=>({...shape(e),spans:[...e.children].map(shape),svg:[...e.querySelectorAll('svg')].map(shape),focused:document.activeElement===e})),groups:[...nav.querySelectorAll('[data-state]')].map(shape),focused:document.activeElement===document.querySelector('#before')?'before':document.activeElement===document.querySelector('#after')?'after':document.activeElement===document.body?'body':document.activeElement?.textContent??'',events:[...window.events],inputEvents:[...window.inputEvents],renderCalls:[...window.renderCalls],url:location.href};
   });
   const normal=await read(),states=[],motionFrames=[];
   if(visualIds.includes(id)){
    if(screenshotsOnly){const original=cases.find(c=>c.id===id&&c.scheme===scheme);if(!original||!isDeepStrictEqual(original.normal,normal))throw Error('Initial screenshot differs from the captured normal state: '+id+'/'+scheme);}
    const file=id+'-'+scheme+'-initial.png';await page.locator('aside').screenshot({path:path.join(dir,file)});
    snapshots.push({id,scheme,file,normalMatchesFullCapture:screenshotsOnly,sha256:crypto.createHash('sha256').update(await fs.readFile(path.join(dir,file))).digest('hex')});
   }
   if(screenshotsOnly)continue;
   async function step(action,work){await work();await settle(page);states.push({action,state:await read()});}
   if(['default','collapsed','disabled-link','disabled-group','group-route','custom-link','custom-link-collapsed','slot-buttons','motion'].includes(id)){
    await page.locator('#before').focus();for(let i=0;i<Math.min(normal.rows.length+2,8);i++)await step('Tab',()=>page.keyboard.press('Tab'));
    const group=page.locator('nav button').first();if(await group.count()){await step('group-click',()=>group.click());await step('group-Enter',()=>page.keyboard.press('Enter'));await step('group-Space',()=>page.keyboard.press('Space'));}
    const anchor=page.locator('nav a').first();if(await anchor.count()){await step('link-Enter',async()=>{await anchor.focus();await page.keyboard.press('Enter')});const r=await anchor.boundingBox();if(r)await step('link-pointer',()=>page.mouse.click(r.x+r.width/2,r.y+r.height/2));}
   }
   if(['default','custom-link','motion'].includes(id)){
    const anchor=page.locator('nav a').first();await step('link-hover',()=>anchor.hover());
    await step('link-pressed',()=>page.mouse.down());await step('link-released',()=>page.mouse.up());
    await step('link-exit',()=>page.locator('#before').hover());
   }
   if(id==='slot-buttons'){
    await step('header-click',()=>page.locator('[data-consumer-slot="header"]').click());
    await step('footer-Space',async()=>{await page.locator('[data-consumer-slot="footer"]').focus();await page.keyboard.press('Space')});
   }
   if(id==='dense')await step('scroll-last',()=>page.locator('nav a').last().focus());
   if(id==='state-retention'||id==='state-closed'){
    await step('active-change',()=>page.evaluate(active=>window.updateSidebar({active}),{child:id==='state-closed'}));
    await step('collapse',()=>page.evaluate(()=>window.updateSidebar({collapsed:true})));
    await step('expand',()=>page.evaluate(()=>window.updateSidebar({collapsed:false})));
    await step('same-id-replacement',()=>page.evaluate(()=>window.updateSidebar({replace:[{id:'group',label:'Replacement group',items:[{id:'replacement',label:'Replacement child',href:'#replacement',active:false}]}]})));
    await step('remove',()=>page.evaluate(()=>window.updateSidebar({remove:['group']})));
    await step('readd',()=>page.evaluate(()=>window.updateSidebar({append:[{id:'group',label:'Readded group',items:[{id:'replacement',label:'Replacement child',href:'#replacement',active:true}]}]})));
   }
   if(id==='state-reorder'){
    await step('reorder',()=>page.evaluate(()=>window.updateSidebar({reorder:['closed','leaf','open']})));
    await step('active-change',()=>page.evaluate(()=>window.updateSidebar({active:{'open-child':false,'closed-child':true}})));
    await step('reorder-back',()=>page.evaluate(()=>window.updateSidebar({reorder:['open','closed','leaf']})));
   }
   if(id==='state-shape-open'||id==='state-shape-closed'){
    await step('to-group',()=>page.evaluate(()=>window.updateSidebar({replace:[{id:'shape',label:'Group stage',items:[{id:'shape-child',label:'Active child',href:'#shape-child',active:true}]}]})));
    await step('to-leaf',()=>page.evaluate(()=>window.updateSidebar({replace:[{id:'shape',label:'Leaf stage',href:'#shape'}]})));
    await step('to-group-inactive',()=>page.evaluate(()=>window.updateSidebar({replace:[{id:'shape',label:'Inactive group stage',items:[{id:'shape-child',label:'Inactive child',href:'#shape-child',active:false}]}]})));
   }
   if(motion){
    motionFrames.push(...await page.evaluate(async()=>{const frames=[],aside=document.querySelector('aside'),start=performance.now();window.updateSidebar({collapsed:true});await new Promise(resolve=>{const frame=time=>{const s=getComputedStyle(aside);frames.push({time:time-start,width:aside.getBoundingClientRect().width,paddingLeft:parseFloat(s.paddingLeft),paddingRight:parseFloat(s.paddingRight),animations:aside.getAnimations().map(a=>({property:a.transitionProperty,time:a.currentTime,start:a.startTime,duration:a.effect.getTiming().duration,easing:a.effect.getTiming().easing}))});if(time-start<300)requestAnimationFrame(frame);else resolve(null)};requestAnimationFrame(frame)});return frames}));
    await step('collapse-motion',async()=>{});
    await step('expand-motion',()=>page.evaluate(()=>window.updateSidebar({collapsed:false})));
   }
   if(visualIds.includes(id)){await page.mouse.move(950,650);await page.locator('#before').focus();await settle(page);await page.locator('aside').screenshot({path:path.join(dir,id+'-'+scheme+'.png')});}
   cases.push({id,scheme,props,height,hostWidth,flex,customCss,motion,normal,states,motionFrames,errors});await fs.writeFile(path.join(dir,output),JSON.stringify({captureVersion:4,sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256,react:'19.2.4',collapsible:'1.1.14',browser:browser.version(),compiledCssSha256:crypto.createHash('sha256').update(css).digest('hex'),fontFiles,bundledRuntime:[...packages.values()].sort((a,b)=>a.name.localeCompare(b.name)),cases},null,2)+'\n');console.log(JSON.stringify({id,scheme,width:normal.surface.box.width,height:normal.surface.box.height,rows:normal.rows.length,actions:states.map(s=>({action:s.action,expanded:s.state.rows.filter(r=>r.tag==='BUTTON').map(r=>r.attributes['aria-expanded']),events:s.state.events})),errors}));
  }finally{await page.close();}
 }}finally{await browser.close();}
 if(screenshotsOnly)await fs.writeFile(path.join(dir,'screenshots-report.json'),JSON.stringify({sourceSha256,fullCaptureSha256:crypto.createHash('sha256').update(await fs.readFile(path.join(dir,output))).digest('hex'),snapshots},null,2)+'\n');
})().catch(e=>{console.error(e);process.exitCode=1});
