const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto');
const{isDeepStrictEqual}=require('node:util');
const args=process.argv.slice(2),option=name=>{const index=args.indexOf(name);if(index<0||!args[index+1])throw Error('Pass '+name+' <absolute module directory>.');return path.resolve(args[index+1]);};
const esbuild=require(option('--esbuild-module')),{chromium}=require(option('--playwright-module'));
const hash=value=>crypto.createHash('sha256').update(value).digest('hex');
const samples=require('./data-table-toolbar-cases.cjs');

(async()=>{
 const dir=path.resolve(__dirname,'../../artifacts/data-table-toolbar-browser'),source=path.resolve(dir,'../line-chart-browser/frozen-source/src/components/data-table-toolbar.tsx');
 const sourceSha256=hash(await fs.readFile(source));if(sourceSha256!=='a2beb795287fd027857d9f4d12df9d0e76406b0fa0703a9fcf20aaa23064a0ef')throw Error('Frozen toolbar source differs.');
 const runtime=path.join(dir,'node_modules');
 const build=await esbuild.build({entryPoints:[path.join(__dirname,'data-table-toolbar-client.tsx')],bundle:true,platform:'browser',outfile:path.join(dir,'bundle.js'),metafile:true,alias:{'@frozen/data-table-toolbar':source,react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom'),'@tanstack/react-table':path.join(runtime,'@tanstack/react-table')},nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const lock=JSON.parse(await fs.readFile(path.resolve(dir,'../chart-theme-reference/bleecker-package-lock.json'),'utf8')),packages=new Map();
 for(const input of Object.keys(build.metafile.inputs)){
  const full=path.resolve(input).replaceAll('\\','/'),at=full.lastIndexOf('/node_modules/');if(at<0)continue;
  const suffix=full.slice(at+14).split('/'),name=suffix[0].startsWith('@')?suffix.slice(0,2).join('/'):suffix[0],packageRoot=full.slice(0,at+14)+name;if(packages.has(packageRoot))continue;
  const manifest=JSON.parse(await fs.readFile(path.join(packageRoot,'package.json'),'utf8')),first=full.indexOf('/node_modules/'),actual=JSON.parse(await fs.readFile(full.slice(0,first+14)+'.package-lock.json','utf8')).packages[packageRoot.slice(first+1)];
  if(!actual||manifest.version!==actual.version||!Object.entries(lock.packages).some(([key,value])=>(key==='node_modules/'+name||key.endsWith('/node_modules/'+name))&&value.version===actual.version&&value.integrity===actual.integrity))throw Error('Bundled runtime differs: '+name);
  packages.set(packageRoot,{name,version:actual.version,integrity:actual.integrity});
 }
 for(const[name,version]of[['react','19.2.4'],['react-dom','19.2.4'],['@tanstack/react-table','8.21.3'],['@tanstack/table-core','8.21.3']])if([...packages.values()].filter(p=>p.name===name).length!==1||![...packages.values()].some(p=>p.name===name&&p.version===version))throw Error('One pinned runtime is required: '+name);
 await fs.writeFile(path.join(dir,'build-meta.json'),JSON.stringify(build.metafile,null,2));
 const dependencies=[];for(const input of Object.keys(build.metafile.inputs)){const full=path.resolve(input);if(full.includes('frozen-source'+path.sep)){dependencies.push({file:path.relative(path.resolve(dir,'../line-chart-browser/frozen-source'),full).replaceAll('\\','/'),sha256:hash(await fs.readFile(full))});}}
 const css=(await fs.readFile(path.resolve(dir,'../line-chart-browser/styles.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'');
 const fontFiles=[];let fonts='';for(const[weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){const bytes=await fs.readFile(path.resolve(dir,'../../src/Orchard/Assets/Fonts/'+file));fontFiles.push({file,weight,sha256:hash(bytes)});fonts+=`@font-face{font-family:'Encode Sans';font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const toolVersions={esbuild:require(path.join(option('--esbuild-module'),'package.json')).version,playwright:require(path.join(option('--playwright-module'),'package.json')).version};
 if(toolVersions.esbuild!=='0.25.12'||toolVersions.playwright!=='1.62.1')throw Error('Capture tooling differs.');
 const probe=args.includes('--probe'),output=probe?'probe-v2.json':'reference-v2.json',selected=probe?samples.filter(s=>['default','column-filter','global-filter','initial-filter','missing-column','at-breakpoint','small-viewport','live-source','column-number','column-array','column-object'].includes(s.id)):samples;
 let cases=[];const browser=await chromium.launch({headless:true,channel:'msedge'});
 const provenance={captureVersion:2,sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256,compiledCssSha256:hash(css),clientSha256:hash(await fs.readFile(path.join(__dirname,'data-table-toolbar-client.tsx'))),captureScriptSha256:hash(await fs.readFile(__filename)),fixtureSignatureSha256:hash(JSON.stringify(samples)),dependencies:dependencies.sort((a,b)=>a.file.localeCompare(b.file)),fontFiles,toolVersions,bundledRuntime:[...packages.values()].sort((a,b)=>a.name.localeCompare(b.name)),browser:browser.version()};
 try{
  if(!probe){try{const previous=JSON.parse(await fs.readFile(path.join(dir,output),'utf8'));const{cases:old,...oldProvenance}=previous;if(!isDeepStrictEqual(oldProvenance,provenance))throw Error('Cannot resume changed toolbar capture provenance.');cases=old;}catch(error){if(error.code!=='ENOENT')throw error;}}
  for(const scheme of probe?['Light']:['Light','Dark'])for(const sample of selected){if(cases.some(c=>c.id===sample.id&&c.scheme===scheme))continue;
   const{id,viewportWidth=1000,hostWidth=800,direction='ltr',customCss='',...props}=sample;
   const page=await browser.newPage({viewport:{width:viewportWidth,height:700},deviceScaleFactor:1,reducedMotion:'reduce'}),errors=[],consoleMessages=[];
   page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{if(['error','warning'].includes(message.type()))consoleMessages.push({type:message.type(),text:message.text()});});
   try{
    await page.setContent(`<html class="${scheme==='Dark'?'dark':''}" dir="${direction}"><head><style>${css}${fonts}body{margin:0;background:var(--color-background)}#root{width:${hostWidth}px}${customCss}</style></head><body><button id="before">Before</button><div id="root"></div><button id="after">After</button><output id="data-probe"></output><script>${await fs.readFile(path.join(dir,'bundle.js'),'utf8')}</script></body></html>`);
    await page.evaluate(async()=>{await Promise.all([400,500,600].map(weight=>document.fonts.load(weight+' 16px "Encode Sans"')));await document.fonts.ready;});
    await page.evaluate(value=>window.showToolbar(value),props);
    const settle=async()=>{
     await page.evaluate(()=>window.__settled=undefined);
     const result=await page.waitForFunction(()=>{
      const root=document.querySelector('#root');if(!root?.firstElementChild||!window.snapshotTable)return false;
      const paint=[...root.querySelectorAll('*')].map(node=>{const r=node.getBoundingClientRect(),s=getComputedStyle(node);return[r.x,r.y,r.width,r.height,s.color,s.backgroundColor,s.borderColor,s.boxShadow,s.opacity,s.transform,s.translate,s.scale,node instanceof HTMLInputElement?node.value:null];});
      const signature=JSON.stringify([root.innerHTML,paint,window.snapshotTable(),document.querySelector('#data-probe').textContent]),prior=window.__settled;
      if(prior?.signature===signature){prior.stable++;prior.frames++;}
      else window.__settled={signature,stable:0,frames:(prior?.frames??0)+1,start:prior?.start??performance.now()};
      const current=window.__settled;return current.stable>=3?{stable:current.stable,frames:current.frames,milliseconds:performance.now()-current.start}:false;
     },null,{polling:'raf',timeout:2000});return await result.jsonValue();
    };
    const normalPresentation=await settle();
    const read=async()=>({...await page.locator('#root').evaluate(root=>{
     const origin=root.getBoundingClientRect(),attrs=node=>Object.fromEntries([...node.attributes].map(a=>[a.name,a.value])),box=node=>{const r=node.getBoundingClientRect();return{x:r.x-origin.x,y:r.y-origin.y,width:r.width,height:r.height};};
     const shape=node=>{if(node.nodeType===3){const range=document.createRange();range.selectNode(node);return{tag:'#text',text:node.textContent,rects:[...range.getClientRects()].map(r=>({x:r.x-origin.x,y:r.y-origin.y,width:r.width,height:r.height}))};}const s=getComputedStyle(node);return{tag:node.tagName,text:node.textContent,attributes:attrs(node),box:box(node),style:Object.fromEntries(['display','flexDirection','flexGrow','flexShrink','flexBasis','alignItems','justifyContent','width','height','minWidth','maxWidth','minHeight','maxHeight','padding','margin','gap','fontFamily','fontSize','fontWeight','lineHeight','letterSpacing','color','backgroundColor','borderColor','borderWidth','borderRadius','opacity','pointerEvents','boxShadow','transform','translate','scale','transformOrigin','transitionProperty','transitionDuration','transitionTimingFunction','overflowX','overflowY','whiteSpace','cursor','outlineColor','outlineStyle','outlineWidth','outlineOffset','stroke','strokeWidth'].map(key=>[key,s[key]])),focused:document.activeElement===node,...(node instanceof HTMLInputElement?{value:node.value,placeholder:node.placeholder,selectionStart:node.selectionStart,selectionEnd:node.selectionEnd}:{}),...(node instanceof HTMLButtonElement?{disabled:node.disabled}:{}),children:[...node.childNodes].map(shape)};};
     return{surface:shape(root.firstElementChild),table:window.snapshotTable(),visibleRows:document.querySelector('#data-probe').textContent,events:[...window.events],inputEvents:[...window.inputEvents],focused:document.activeElement===document.body?'body':document.activeElement.id||document.activeElement.getAttribute('data-slot')||document.activeElement.tagName,rootBox:box(root),scrollWidth:root.scrollWidth,clientWidth:root.clientWidth};
    }),aria:await page.locator('#root').ariaSnapshot()});
    const normal=await read(),states=[],screenshots=[];
    if(['default','column-filter','small-viewport','at-breakpoint','constrained-host','multilingual','rtl'].includes(id)){
     const before=await read();if(!isDeepStrictEqual(before,normal))throw Error('Screenshot initial state differs: '+id);
     const file=id+'-'+scheme+'-v2-initial.png';await page.locator('#root').screenshot({path:path.join(dir,file)});
     screenshots.push({file,sha256:hash(await fs.readFile(path.join(dir,file))),normalMatchesFullCapture:true});
    }
    const step=async(action,work)=>{await work();const presentation=await settle();states.push({action,presentation,state:await read()});};
    const interactive=['default','column-filter','other-column-filter','global-filter','both-filters','initial-filter','initial-and-global','missing-column','disabled-slots','live-source'];
    if(interactive.includes(id)){
     const search=page.locator('#root input');if(await search.count()){
      await step('search-focus',()=>search.focus());await step('search-fill',()=>search.fill('Harbor'));
      await step('search-clear',()=>search.fill(''));
      await step('search-keyboard',async()=>{await search.focus();await page.keyboard.type('Reed');});
     }
     const reset=page.getByRole('button',{name:'Reset',exact:true});if(await reset.count()){
      await step('reset-hover',()=>reset.hover());await step('reset-held',()=>page.mouse.down());await step('reset-release',()=>page.mouse.up());
      if(await reset.count())await step('reset-keyboard',async()=>{await reset.focus();await page.keyboard.press('Enter');});
     }
     const filter=page.locator('[data-slot="filters"]').first();if(await filter.count()&&!await filter.isDisabled())await step('filter-slot',()=>filter.click());
     const action=page.locator('[data-slot="actions"]').first();if(await action.count()&&!await action.isDisabled())await step('action-slot',()=>action.click());
     await page.locator('#before').focus();for(let index=0;index<Math.min(normal.surface.children.flatMap(n=>n.children??[]).length+3,7);index++)await step('Tab',()=>page.keyboard.press('Tab'));
    }
    if(id==='live-source'){
     await step('external-column-filter',()=>page.evaluate(()=>window.setColumnFilters([{id:'title',value:'Vale'}])));
     await step('external-global-filter',()=>page.evaluate(()=>window.setGlobalFilter('Vale')));
     await step('search-column-change',()=>page.evaluate(()=>window.updateToolbar({searchColumn:'status',searchPlaceholder:'Status search'})));
     await step('search-column-hidden',()=>page.evaluate(()=>window.updateToolbar({searchColumn:''})));
     await step('replace-table',()=>page.evaluate(()=>window.updateToolbar({secondTable:true,searchColumn:'category'})));
     await step('second-table-search',()=>page.locator('#root input').fill('Reed'));
     await step('restore-table',()=>page.evaluate(()=>window.updateToolbar({secondTable:false,searchColumn:'title'})));
     await step('replace-slots',()=>page.evaluate(()=>window.updateToolbar({filters:{kind:'buttons',labels:['Replacement filter'],width:180},actions:{kind:'buttons',labels:['Replacement action'],width:150}})));
    }
    if(!interactive.includes(id)){
     const reset=page.getByRole('button',{name:'Reset',exact:true});if(await reset.count())await step('reset-click',()=>reset.click());
     if(await reset.count())await step('reset-Enter',async()=>{await reset.focus();await page.keyboard.press('Enter');});
    }
    const entry={id,scheme,viewportWidth,hostWidth,direction,props,customCss,normal,normalPresentation,states,screenshots,errors,consoleMessages};cases.push(entry);
    await fs.writeFile(path.join(dir,output),JSON.stringify({...provenance,cases},null,2)+'\n');
    console.log(JSON.stringify({id,scheme,width:normal.surface.box.width,height:normal.surface.box.height,direction:normal.surface.style.flexDirection,searchValue:normal.surface.children.flatMap(n=>n.children??[]).flatMap(n=>n.children??[]).find(n=>n.tag==='INPUT')?.value,filtered:normal.table.state,states:states.length,errors,consoleMessages:consoleMessages.length}));
   }finally{await page.close();}
  }
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
