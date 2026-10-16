const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const arg=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],'Pass '+name+' <path>.');return path.resolve(process.argv[at+1]);};
const output=arg('--output'),esbuildPath=arg('--esbuild-module'),playwrightPath=arg('--playwright-module'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const samples=[
 {id:'default',actions:['open','draft','open','all','open','same-all','open','escape','open','outside']},
 {id:'selected',filters:[{id:'status',value:'Draft'}],actions:['open','tab','reverse-tab','down','enter','open','home','enter']},
 {id:'custom',placeholder:'Any status',actions:['open','draft','external-published','open','escape']},
 {id:'empty-placeholder',placeholder:'',actions:['open','published','open','all']},
 {id:'empty-options',options:[],actions:['open','enter']},
 {id:'missing',columnId:'missing',actions:['restore-column','open','draft','remove-column','restore-column']},
 {id:'unknown',filters:[{id:'status',value:'Unknown'}],actions:['open','escape','external-draft','open','all']},
 {id:'empty-filter',filters:[{id:'status',value:''}],actions:['open','same-all','open','draft']},
 {id:'disabled',options:[{label:'Draft',value:'Draft',disabled:true},{label:'Published',value:'Published'}],actions:['open','down','enter']},
 {id:'all-disabled',options:[{label:'Draft',value:'Draft',disabled:true},{label:'Published',value:'Published',disabled:true}],actions:['open','end','enter']},
 {id:'replacement',filters:[{id:'status',value:'Draft'}],secondFilters:[{id:'status',value:'Published'}],actions:['open','replace','all','old-notification','open','escape']},
 {id:'external-open',actions:['open','external-draft','escape','open','external-published','escape']},
 {id:'column-change',actions:['open','title-column','escape','open','all']},
 {id:'typeahead',actions:['focus','type-p','type-d','open','type-p','enter']},
 {id:'long',options:Array.from({length:20},(_,index)=>({label:'Status '+index,value:'Status '+index})),actions:['open','end','enter','open','home','enter']},
 {id:'rtl',direction:'rtl',actions:['open','draft','open','escape']},
 {id:'bottom',top:640,actions:['open','published','open','escape']},
 {id:'narrow',viewportWidth:400,hostWidth:368,actions:['open','draft','open','escape']},
 {id:'below-breakpoint',viewportWidth:639,hostWidth:300,actions:['open','escape']},
 {id:'at-breakpoint',viewportWidth:640,hostWidth:300,actions:['open','escape']},
 {id:'constrained-host',hostWidth:120,actions:['open','escape']},
 {id:'duplicate-empty',options:[{label:'Explicit empty',value:''},{label:'Draft',value:'Draft'}],actions:['open','escape']},
 {id:'sentinel-collision',options:[{label:'Literal sentinel',value:'__bleecker_empty_option__'},{label:'Draft',value:'Draft'}],actions:['open','escape']},
 {id:'numeric-zero',columnId:'title',filters:[{id:'title',value:0}],actions:['open','escape']},
 {id:'boolean-false',columnId:'title',filters:[{id:'title',value:false}],actions:['open','escape']},
 {id:'numeric-value',columnId:'title',filters:[{id:'title',value:42}],actions:['open','escape']},
 {id:'array-value',columnId:'title',filters:[{id:'title',value:['Harbor']}],actions:['open','escape']},
 {id:'null-value',columnId:'title',filters:[{id:'title',value:null}],actions:['open','escape']},
];
(async()=>{
 await fs.mkdir(output,{recursive:true});const scriptSha256=hash(await fs.readFile(__filename)),sourceRoot=path.resolve('artifacts/line-chart-browser/frozen-source'),runtime=path.resolve('artifacts/data-table-pagination-browser/node_modules'),source=path.join(sourceRoot,'src/components/data-table-filters.tsx');
 assert.equal(hash(await fs.readFile(source)),'83a7e5de0d5b0a63d42591e3d45b230243d0ee97d9797a2f9b6b87d4369334bb');
 const lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lockBlob=crypto.createHash('sha1').update(Buffer.concat([Buffer.from('blob '+lockBytes.length+'\0'),lockBytes])).digest('hex');assert.equal(lockBlob,'0b6113dd13d74b1023208cbf96e080915f3b47a2');const lock=JSON.parse(lockBytes),baseline=JSON.parse(await fs.readFile('tests/Reference/DataTablePagination.json','utf8'));
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const aliases=Object.fromEntries(['react','react-dom','lucide-react','clsx','tailwind-merge','@tanstack/react-table','@radix-ui/react-select'].map(name=>[name,path.join(runtime,name)]));
 const client=path.resolve('scripts/reference/data-table-filters-client.tsx'),build=await require(esbuildPath).build({entryPoints:[client],bundle:true,platform:'browser',write:false,metafile:true,alias:{'@frozen/data-table-filters':source,...aliases},nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}}),dependencies=[],packages=new Map();
 for(const input of Object.keys(build.metafile.inputs)){
  const full=path.resolve(input),normalized=full.replaceAll('\\','/');if(full.startsWith(sourceRoot+path.sep))dependencies.push({file:path.relative(sourceRoot,full).replaceAll('\\','/'),sha256:hash(await fs.readFile(full))});
  const at=normalized.lastIndexOf('/node_modules/');if(at<0)continue;const parts=normalized.slice(at+14).split('/'),name=parts[0].startsWith('@')?parts.slice(0,2).join('/'):parts[0],root=normalized.slice(0,at+14)+name;if(packages.has(root))continue;
  const meta=JSON.parse(await fs.readFile(path.join(root,'package.json'),'utf8')),first=normalized.indexOf('/node_modules/'),installed=JSON.parse(await fs.readFile(normalized.slice(0,first+14)+'.package-lock.json','utf8')),entry=installed.packages[root.slice(first+1)];
  assert.ok(entry&&entry.version===meta.version&&Object.entries(lock.packages).some(([id,value])=>(id==='node_modules/'+name||id.endsWith('/node_modules/'+name))&&value.version===entry.version&&value.integrity===entry.integrity),'Bundled dependency differs: '+name);packages.set(root,{name,version:entry.version,integrity:entry.integrity});
 }
 for(const[name,version]of[['react','19.2.4'],['react-dom','19.2.4'],['@tanstack/react-table','8.21.3'],['@radix-ui/react-select','2.3.7']])assert.ok([...packages.values()].some(item=>item.name===name&&item.version===version));
 const css=(await fs.readFile('artifacts/line-chart-browser/styles.css','utf8')).replace(/@import url\([^\r\n]*\);/g,'');assert.equal(hash(css),baseline.compiledCssSha256);let fonts='';for(const item of baseline.fontFiles){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+item.file);assert.equal(hash(bytes),item.sha256);fonts+=`@font-face{font-family:'Encode Sans';font-weight:${item.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),baseline.browser);const cases=[];
 try{for(const scheme of ['Light','Dark'])for(const sample of samples){
  const{id,viewportWidth=1000,hostWidth=520,direction='ltr',top=16,actions,...props}=sample,page=await browser.newPage({viewport:{width:viewportWidth,height:745},deviceScaleFactor:1,reducedMotion:'reduce'}),errors=[];page.setDefaultTimeout(2000);page.on('pageerror',error=>errors.push(error.message));
  try{
   await page.setContent(`<html dir="${direction}" class="${scheme==='Dark'?'dark':''}"><head><style>${css}${fonts}body{display:flow-root;min-height:100vh;margin:0;background:var(--color-background)}#root{width:${hostWidth}px;margin:${top}px 16px 16px}#outside{position:fixed;right:16px;bottom:16px}#data-probe{display:block;margin:16px}</style></head><body><div id="root"></div><button id="outside">Outside</button><output id="data-probe"></output><script>${build.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(weight=>document.fonts.load(weight+' 16px "Encode Sans"')));await document.fonts.ready;});await page.evaluate(props=>window.showFilters(props),props);await page.waitForFunction(()=>!!window.filterSnapshot);
   const read=()=>page.evaluate(()=>{
    const trigger=document.querySelector('#root button'),popup=document.querySelector('[role=listbox]'),options=[...(popup?.querySelectorAll('[role=option]')??[])],focus=document.activeElement;
    const box=node=>{const r=node.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};},style=node=>{const s=getComputedStyle(node);return{background:s.backgroundColor,color:s.color,border:s.border,borderRadius:s.borderRadius,padding:s.padding,font:s.font,lineHeight:s.lineHeight,boxShadow:s.boxShadow,transform:s.transform,opacity:s.opacity};};
    const viewport=popup?.querySelector('[data-radix-select-viewport]');return{table:window.filterSnapshot(),events:[...window.events],inputEvents:[...window.inputEvents],outsideClicks:window.outsideClicks,visibleRows:document.getElementById('data-probe').textContent,focused:focus===trigger?'trigger':options.includes(focus)?'option:'+options.indexOf(focus):focus===popup?'listbox':focus?.id==='outside'?'outside':focus?.tagName,popupOpen:!!popup,trigger:trigger?{box:box(trigger),wrapper:box(trigger.parentElement),style:style(trigger),text:trigger.textContent,placeholder:trigger.hasAttribute('data-placeholder'),name:trigger.getAttribute('aria-label'),expanded:trigger.getAttribute('aria-expanded')}:null,popup:popup?{box:box(popup),style:style(popup),role:popup.getAttribute('role')}:null,viewport:viewport?{box:box(viewport),scrollTop:viewport.scrollTop,scrollHeight:viewport.scrollHeight,clientHeight:viewport.clientHeight}:null,options:options.map((option,index)=>({index,label:option.textContent,checked:option.getAttribute('aria-selected'),disabled:option.getAttribute('aria-disabled'),state:option.getAttribute('data-state'),box:box(option),style:style(option)}))};
   });
   const settle=async()=>{await page.evaluate(()=>window.filterSettled=undefined);const handle=await page.waitForFunction(()=>{const nodes=[document.querySelector('#root'),...document.querySelectorAll('#root *,[role=listbox],[role=listbox] *')],signature=JSON.stringify([window.filterSnapshot(),window.events,window.outsideClicks,nodes.map(node=>{const r=node.getBoundingClientRect(),s=getComputedStyle(node);return[r.x,r.y,r.width,r.height,s.opacity,s.transform,s.backgroundColor];})]),old=window.filterSettled,next=window.filterSettled={signature,focus:document.activeElement,stable:old?.signature===signature&&old?.focus===document.activeElement?old.stable+1:0,frames:(old?.frames??0)+1,start:old?.start??performance.now()};return next.stable>=3?{stable:next.stable,frames:next.frames,milliseconds:performance.now()-next.start}:false;},null,{polling:'raf',timeout:2000});return handle.jsonValue();};
   const snapshots=[];const capture=async phase=>{const presentation=await settle(),before=await read(),file=`${id}-${snapshots.length}-${phase}-${scheme}.png`,bytes=await page.screenshot({caret:'initial',animations:'allow'});await fs.writeFile(path.join(output,file),bytes);assert.deepEqual(await read(),before);assert.ok(before.table.sameTable&&before.table.sameRows&&before.table.columns.every(item=>item.same)&&before.table.rows.every(row=>row.same));assert.ok(before.inputEvents.every(event=>event.trusted));snapshots.push({phase,presentation,state:before,accessibility:before.popupOpen?await page.locator('[role=listbox]').ariaSnapshot():null,file,sha256:hash(bytes),width:viewportWidth,height:745});};
   await capture('closed');
   for(const action of actions){
    if(action==='open')await page.locator('#root button').click();
    else if(['draft','published','all','same-all'].includes(action))await page.getByRole('option',{name:action==='draft'?'Draft':action==='published'?'Published':props.placeholder??'All',exact:true}).click();
    else if(action==='focus')await page.keyboard.press('Tab');
    else if(['down','home','end','enter','escape','tab','reverse-tab'].includes(action))await page.keyboard.press({down:'ArrowDown',home:'Home',end:'End',enter:'Enter',escape:'Escape',tab:'Tab','reverse-tab':'Shift+Tab'}[action]);
    else if(action==='type-p'||action==='type-d')await page.keyboard.press(action==='type-p'?'p':'d');
    else if(action==='outside'){const box=await page.locator('#outside').boundingBox();assert.ok(box);await page.mouse.click(box.x+box.width/2,box.y+box.height/2);}
    else if(action==='external-draft'||action==='external-published')await page.evaluate(value=>window.setFilters([{id:'status',value}]),action==='external-draft'?'Draft':'Published');
    else if(action==='replace')await page.evaluate(()=>window.updateFilters({second:true}));
    else if(action==='old-notification')await page.evaluate(()=>window.setOldFilters([{id:'status',value:'Published'}]));
    else if(action==='restore-column')await page.evaluate(()=>window.updateFilters({columnId:'status'}));
    else if(action==='remove-column')await page.evaluate(()=>window.updateFilters({columnId:'missing'}));
    else if(action==='title-column')await page.evaluate(()=>window.updateFilters({columnId:'title'}));
    else throw Error('Unknown source action: '+action);
    await capture(action);
   }
   assert.deepEqual(errors,[]);cases.push({id,scheme,props,viewportWidth,hostWidth,direction,top,snapshots});console.log(JSON.stringify({id,scheme,states:snapshots.length}));
  }catch(error){await fs.writeFile(path.join(output,'failure.json'),JSON.stringify({id,scheme,error:String(error),errors},null,2));throw error;}finally{await page.close();}
 }}finally{await browser.close();}
 assert.equal(hash(await fs.readFile(__filename)),scriptSha256);const report={format:'orchard-data-table-filters-v1',sourceCommit:baseline.sourceCommit,sourceSha256:hash(await fs.readFile(source)),sourceLockGitBlob:lockBlob,scriptSha256,clientSha256:hash(await fs.readFile(client)),compiledCssSha256:hash(css),browser:baseline.browser,toolVersions:baseline.toolVersions,fontFiles:baseline.fontFiles,dependencies:dependencies.sort((a,b)=>a.file.localeCompare(b.file)),bundledRuntime:[...packages.values()].sort((a,b)=>a.name.localeCompare(b.name)),cases};await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');await fs.writeFile('tests/Reference/DataTableFilters.json',JSON.stringify(report,null,2)+'\n');
})().catch(error=>{console.error(error);process.exit(1)});
