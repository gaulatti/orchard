const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const option=name=>{const i=process.argv.indexOf(name);if(i<0||!process.argv[i+1])throw Error('Pass '+name+' <directory>.');return path.resolve(process.argv[i+1]);};
const esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module'),output=option('--output'),esbuild=require(esbuildPath),{chromium}=require(playwrightPath),samples=require('./data-table-pagination-cases.cjs');
const hash=value=>crypto.createHash('sha256').update(value).digest('hex');
(async()=>{
 const scriptSha256=hash(await fs.readFile(__filename)),dir=path.resolve('artifacts/data-table-pagination-browser'),sourceRoot=path.resolve('artifacts/line-chart-browser/frozen-source'),source=path.join(sourceRoot,'src/components/data-table-pagination.tsx'),runtime=path.join(dir,'node_modules');await fs.mkdir(output,{recursive:true});
 assert.equal(hash(await fs.readFile(source)),'385a8ae19a8b052be43ae29e7c43b3662ac35ccf892f20c4ef1fc0b893d10897');
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const client=path.resolve('scripts/reference/data-table-pagination-client.tsx');
 const aliases=Object.fromEntries(['react','react-dom','lucide-react','clsx','tailwind-merge','@tanstack/react-table','@radix-ui/react-select'].map(name=>[name,path.join(runtime,name)]));
 const build=await esbuild.build({entryPoints:[client],bundle:true,platform:'browser',outfile:path.join(dir,'bundle.js'),metafile:true,alias:{'@frozen/data-table-pagination':source,...aliases},nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const lock=JSON.parse(await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json','utf8')),packages=new Map(),dependencies=[];
 for(const input of Object.keys(build.metafile.inputs)){
  const full=path.resolve(input),normalized=full.replaceAll('\\','/');if(full.startsWith(sourceRoot+path.sep))dependencies.push({file:path.relative(sourceRoot,full).replaceAll('\\','/'),sha256:hash(await fs.readFile(full))});
  const at=normalized.lastIndexOf('/node_modules/');if(at<0)continue;const suffix=normalized.slice(at+14).split('/'),name=suffix[0].startsWith('@')?suffix.slice(0,2).join('/'):suffix[0],root=normalized.slice(0,at+14)+name;if(packages.has(root))continue;
  const metadata=JSON.parse(await fs.readFile(path.join(root,'package.json'),'utf8')),first=normalized.indexOf('/node_modules/'),installed=JSON.parse(await fs.readFile(normalized.slice(0,first+14)+'.package-lock.json','utf8')),entry=installed.packages[root.slice(first+1)];
  assert.ok(entry&&entry.version===metadata.version&&Object.entries(lock.packages).some(([id,value])=>(id==='node_modules/'+name||id.endsWith('/node_modules/'+name))&&value.version===entry.version&&value.integrity===entry.integrity),'Bundled dependency differs: '+name);
  packages.set(root,{name,version:entry.version,integrity:entry.integrity});
 }
 for(const [name,version]of[['react','19.2.4'],['react-dom','19.2.4'],['@tanstack/react-table','8.21.3'],['@tanstack/table-core','8.21.3'],['@radix-ui/react-select','2.3.7']])assert.equal([...packages.values()].filter(item=>item.name===name&&item.version===version).length,1);
 const previous=JSON.parse(await fs.readFile('tests/Reference/DataTableToolbarLayout.json','utf8')),css=(await fs.readFile('artifacts/line-chart-browser/styles.css','utf8')).replace(/@import url\([^\r\n]*\);/g,'');assert.equal(hash(css),previous.compiledCssSha256);
 let fonts='';const fontFiles=[];for(const [weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);assert.equal(hash(bytes),previous.fontFiles.find(item=>item.file===file).sha256);fontFiles.push({file,weight,sha256:hash(bytes)});fonts+="@font-face{font-family:'Encode Sans';font-weight:"+weight+";src:url(data:font/ttf;base64,"+bytes.toString('base64')+")}";}
 const browser=await chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),previous.browser);const cases=[],styles=[],styleMap=new Map();
 const intern=value=>{const key=JSON.stringify(value);if(!styleMap.has(key)){styleMap.set(key,styles.length);styles.push(value);}return styleMap.get(key);};
 const compact=value=>Array.isArray(value)?value.map(compact):value&&typeof value==='object'?Object.fromEntries(Object.entries(value).map(([key,item])=>key==='style'?['styleIndex',intern(item)]:[key,compact(item)])):value;
 try{for(const scheme of ['Light','Dark'])for(const sample of samples){
  const {id,viewportWidth=1000,hostWidth=800,direction='ltr',actions=[],...props}=sample,page=await browser.newPage({viewport:{width:viewportWidth,height:745},deviceScaleFactor:1,reducedMotion:'reduce'}),errors=[];
  page.setDefaultTimeout(2000);page.on('pageerror',error=>errors.push(error.message));
  try{
   await page.setContent('<html class="'+(scheme==='Dark'?'dark':'')+'" dir="'+direction+'"><head><style>'+css+fonts+'body{margin:0;background:var(--color-background)}#root{width:'+hostWidth+'px}</style></head><body><button id="before">Before</button><div id="root"></div><button id="after">After</button><output id="data-probe"></output><script>'+await fs.readFile(path.join(dir,'bundle.js'),'utf8')+'</script></body></html>');
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(weight=>document.fonts.load(weight+' 16px "Encode Sans"')));await document.fonts.ready;});await page.evaluate(model=>window.showPagination(model),props);
   const settle=async()=>{await page.evaluate(()=>window.paginationSettled=undefined);const handle=await page.waitForFunction(()=>{const root=document.querySelector('#root');if(!root?.firstElementChild||!window.snapshotPagination)return false;const nodes=[...root.querySelectorAll('*'),...document.querySelectorAll('[role=listbox] *')];const paint=nodes.map(node=>{const r=node.getBoundingClientRect(),s=getComputedStyle(node);return[r.x,r.y,r.width,r.height,s.backgroundColor,s.color,s.opacity,s.transform,s.borderColor,s.boxShadow];});const signature=JSON.stringify([root.innerHTML,paint,window.snapshotPagination(),window.events,document.querySelector('#data-probe').textContent]),prior=window.paginationSettled;window.paginationSettled={signature,stable:prior?.signature===signature?prior.stable+1:0,frames:(prior?.frames??0)+1,start:prior?.start??performance.now()};const next=window.paginationSettled;return next.stable>=3?{stable:next.stable,frames:next.frames,milliseconds:performance.now()-next.start}:false;},null,{polling:'raf',timeout:2000});return handle.jsonValue();};
   const read=async()=>page.locator('#root').evaluate(root=>{
    const origin=root.getBoundingClientRect(),box=node=>{const r=node.getBoundingClientRect();return{x:r.x-origin.x,y:r.y-origin.y,width:r.width,height:r.height};};
    const shape=node=>{if(node.nodeType===3){const range=document.createRange();range.selectNode(node);return{tag:'#text',text:node.textContent,rects:[...range.getClientRects()].map(r=>({x:r.x-origin.x,y:r.y-origin.y,width:r.width,height:r.height}))};}const s=getComputedStyle(node);return{tag:node.tagName,text:node.textContent,attributes:Object.fromEntries([...node.attributes].map(item=>[item.name,item.value])),box:box(node),style:Object.fromEntries(['display','flexDirection','flexGrow','flexShrink','alignItems','justifyContent','width','height','minWidth','minHeight','maxHeight','padding','margin','gap','fontFamily','fontSize','fontWeight','lineHeight','letterSpacing','color','backgroundColor','borderColor','borderWidth','borderRadius','boxShadow','opacity','overflowX','overflowY','whiteSpace','textOverflow','outlineColor','outlineWidth','outlineStyle','transform','scale','translate'].map(key=>[key,s[key]])),focused:document.activeElement===node,...(node instanceof HTMLButtonElement?{disabled:node.disabled}:{}),children:[...node.childNodes].map(shape)};};
    return{surface:shape(root.firstElementChild),popup:[...document.querySelectorAll('[role=listbox]')].map(shape),table:window.snapshotPagination(),events:[...window.events],inputEvents:[...window.inputEvents],visibleRows:document.querySelector('#data-probe').textContent,focused:document.activeElement===document.body?'body':document.activeElement.getAttribute('role')||document.activeElement.id||document.activeElement.tagName,rootBox:box(root),scrollWidth:root.scrollWidth,clientWidth:root.clientWidth};
   });
   const snapshots=[];const capture=async action=>{const presentation=await settle(),before=await read(),file=`${id}-${snapshots.length}-${action}-${scheme}.png`,image=await page.locator('#root').screenshot({caret:'initial',animations:'allow'});await fs.writeFile(path.join(output,file),image);assert.deepEqual(await read(),before);assert.ok(before.table.sameTable&&before.table.sameRows&&before.table.columns.every(item=>item.same)&&before.table.rows.every(item=>item.same));assert.ok(before.inputEvents.every(item=>item.trusted));snapshots.push({action,state:compact(before),presentation,file,sha256:hash(image),aria:await page.locator('#root').ariaSnapshot()});};
   await capture('initial');const buttons=()=>page.locator('#root button:not([role=combobox])'),select=()=>page.getByRole('combobox');
   for(const action of actions){
    if(['first','previous','next','last'].includes(action)){await buttons().nth(['first','previous','next','last'].indexOf(action)).click();}
    else if(action==='disabled-previous'){const target=buttons().nth(1);assert.equal(await target.isDisabled(),true);const box=await target.boundingBox();await page.mouse.click(box.x+box.width/2,box.y+box.height/2);}
    else if(action.startsWith('size-')){await select().click();await page.getByRole('option',{name:action.slice(5),exact:true}).click();}
    else if(action==='open-escape'){await select().click();await capture('open');await page.keyboard.press('Escape');}
    else if(action==='keyboard-size-20'||action==='keyboard-last'){
     await select().focus();await page.keyboard.press('ArrowDown');
     await page.waitForFunction(()=>document.activeElement?.getAttribute('role')==='option',null,{polling:'raf',timeout:2000});
     await page.keyboard.press(action==='keyboard-last'?'End':'ArrowDown');
     const label=action==='keyboard-last'?'500':'20';await page.waitForFunction(label=>document.activeElement?.textContent===label,label,{polling:'raf',timeout:2000});await page.keyboard.press('Enter');
    }
    else if(action==='Tab')await page.keyboard.press('Tab');
    else if(action==='sort-desc')await page.evaluate(()=>window.setSorting([{id:'title',desc:true}]));
    else if(action==='filter-draft')await page.evaluate(()=>window.setFilters([{id:'status',value:'Draft'}]));
    else if(action==='clear-filter')await page.evaluate(()=>window.setFilters([]));
    else if(action==='external-page')await page.evaluate(()=>window.setPagination({pageIndex:2,pageSize:7}));
    else if(action==='external-selection')await page.evaluate(()=>window.setSelection({'row-003':true,'row-040':true}));
    else if(action==='external-options')await page.evaluate(()=>window.updatePagination({pageSizeOptions:[7,14,21]}));
    else if(action==='replace-table')await page.evaluate(()=>window.updatePagination({secondTable:true}));
    else if(action==='stale-table')await page.evaluate(()=>window.setFirstPageIndex(1));
    else if(action==='restore-table')await page.evaluate(()=>window.updatePagination({secondTable:false}));
    else throw Error('Unknown pagination action '+action);
    await capture(action);
   }
   assert.deepEqual(errors,[]);cases.push({id,scheme,viewportWidth,hostWidth,direction,props,snapshots,errors});console.log(JSON.stringify({id,scheme,states:snapshots.length}));
  }catch(error){await fs.writeFile(path.join(output,'failure.json'),JSON.stringify({id,scheme,error:String(error),errors},null,2));throw error;}finally{await page.close();}
 }}finally{await browser.close();}
 assert.equal(hash(await fs.readFile(__filename)),scriptSha256);
 const report={format:'orchard-data-table-pagination-v1',sourceCommit:previous.sourceCommit,sourceSha256:hash(await fs.readFile(source)),compiledCssSha256:hash(css),clientSha256:hash(await fs.readFile(client)),captureScriptSha256:scriptSha256,casesScriptSha256:hash(await fs.readFile(path.resolve(__dirname,'data-table-pagination-cases.cjs'))),browser:previous.browser,toolVersions:{esbuild:'0.25.12',playwright:'1.62.1'},dependencies:dependencies.sort((a,b)=>a.file.localeCompare(b.file)),bundledRuntime:[...packages.values()].sort((a,b)=>a.name.localeCompare(b.name)),fontFiles,styles,cases};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');await fs.writeFile('tests/Reference/DataTablePagination.json',JSON.stringify(report,null,2)+'\n');
})().catch(error=>{console.error(error);process.exit(1)});
