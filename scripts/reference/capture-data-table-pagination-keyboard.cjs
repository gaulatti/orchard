const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const option=name=>{const index=process.argv.indexOf(name);assert.ok(index>=0&&process.argv[index+1],'Pass '+name+' <path>.');return path.resolve(process.argv[index+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const cases=[
 {id:'first',open:'ArrowDown',navigate:'ArrowDown',commit:'Enter',trapTab:true,reverseTab:true},
 {id:'middle',pageIndex:2,open:'ArrowUp',navigate:'ArrowDown',commit:'Enter'},
 {id:'last',pageIndex:4,open:'Enter',navigate:'ArrowDown',commit:'Space'},
 {id:'long-end',pageSizeOptions:[1,2,3,4,5,10,20,30,40,50,100,200,500],open:'Space',navigate:'End',commit:'Enter'},
 {id:'long-home',pageIndex:2,pageSize:20,pageSizeOptions:[1,2,3,4,5,10,20,30,40,50,100,200,500],open:'ArrowUp',navigate:'Home',commit:'Enter'},
 {id:'narrow',viewportWidth:400,hostWidth:368,open:'ArrowDown',navigate:'ArrowDown',commit:'Enter'},
 {id:'rtl',direction:'rtl',open:'ArrowDown',navigate:'ArrowDown',commit:'Enter'},
 {id:'cancel',open:'ArrowDown',navigate:'ArrowDown',commit:'Escape'},
];
(async()=>{
 await fs.mkdir(output,{recursive:true});const scriptSha256=hash(await fs.readFile(__filename));
 const baselineBytes=await fs.readFile('tests/Reference/DataTablePagination.json'),baseline=JSON.parse(baselineBytes),sourceRoot=path.resolve('artifacts/line-chart-browser/frozen-source'),runtime=path.resolve('artifacts/data-table-pagination-browser/node_modules');
 for(const item of baseline.dependencies)assert.equal(hash(await fs.readFile(path.join(sourceRoot,item.file))),item.sha256);
 assert.equal(hash(await fs.readFile('scripts/reference/data-table-pagination-client.tsx')),baseline.clientSha256);
 const esbuild=require(esbuildPath),{chromium}=require(playwrightPath);assert.equal(esbuild.version,baseline.toolVersions.esbuild);assert.equal(require(path.join(playwrightPath,'package.json')).version,baseline.toolVersions.playwright);
 const aliases=Object.fromEntries(['react','react-dom','lucide-react','clsx','tailwind-merge','@tanstack/react-table','@radix-ui/react-select'].map(name=>[name,path.join(runtime,name)]));
 const bundle=await esbuild.build({entryPoints:['scripts/reference/data-table-pagination-client.tsx'],bundle:true,platform:'browser',write:false,metafile:true,alias:{'@frozen/data-table-pagination':path.join(sourceRoot,'src/components/data-table-pagination.tsx'),...aliases},nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const packages=new Map();
 for(const input of Object.keys(bundle.metafile.inputs)){
  const normalized=path.resolve(input).replaceAll('\\','/'),at=normalized.lastIndexOf('/node_modules/');if(at<0)continue;
  const parts=normalized.slice(at+14).split('/'),name=parts[0].startsWith('@')?parts.slice(0,2).join('/'):parts[0],root=normalized.slice(0,at+14)+name;if(packages.has(root))continue;
  const metadata=JSON.parse(await fs.readFile(path.join(root,'package.json'))),first=normalized.indexOf('/node_modules/'),installed=JSON.parse(await fs.readFile(normalized.slice(0,first+14)+'.package-lock.json')),entry=installed.packages[root.slice(first+1)];
  assert.equal(metadata.version,entry.version);assert.ok(baseline.bundledRuntime.some(item=>item.name===name&&item.version===entry.version&&item.integrity===entry.integrity));packages.set(root,{name,version:entry.version,integrity:entry.integrity});
 }
 assert.deepEqual([...packages.values()].sort((a,b)=>a.name.localeCompare(b.name)),baseline.bundledRuntime);
 const css=(await fs.readFile('artifacts/line-chart-browser/styles.css','utf8')).replace(/@import url\([^\r\n]*\);/g,'');assert.equal(hash(css),baseline.compiledCssSha256);
 let fonts='';for(const item of baseline.fontFiles){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+item.file);assert.equal(hash(bytes),item.sha256);fonts+=`@font-face{font-family:'Encode Sans';font-weight:${item.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),baseline.browser);const reports=[];
 try{for(const scheme of ['Light','Dark'])for(const sample of cases){
  const {id,open,navigate,commit,trapTab=false,reverseTab=false,viewportWidth=1000,hostWidth=800,direction='ltr',...props}=sample;
  const page=await browser.newPage({viewport:{width:viewportWidth,height:745},deviceScaleFactor:1,reducedMotion:'reduce'});page.setDefaultTimeout(2000);const errors=[];page.on('pageerror',error=>errors.push(error.message));
  try{
   await page.setContent(`<html class="${scheme==='Dark'?'dark':''}" dir="${direction}"><head><style>${css}${fonts}body{margin:0;background:var(--color-background)}#root{width:${hostWidth}px}</style></head><body><button id="before">Before</button><div id="root"></div><button id="after">After</button><output id="data-probe"></output><script>${bundle.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(weight=>document.fonts.load(weight+' 16px "Encode Sans"')));await document.fonts.ready;});await page.evaluate(props=>window.showPagination(props),props);
   const state=()=>page.evaluate(()=>{
    const focus=document.activeElement,combo=document.querySelector('[role=combobox]'),buttons=[...document.querySelectorAll('#root button:not([role=combobox])')],list=document.querySelector('[role=listbox]'),box=node=>{const r=node.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};};
    return{table:window.snapshotPagination(),events:[...window.events],inputEvents:[...window.inputEvents],visibleRows:document.querySelector('#data-probe').textContent,focused:focus===combo?'combo':focus?.getAttribute('role')==='option'?'option:'+focus.textContent:focus?.id==='before'?'before':focus?.id==='after'?'after':buttons.includes(focus)?['first','previous','next','last'][buttons.indexOf(focus)]:focus?.tagName,popupOpen:!!list,surface:box(document.querySelector('#root')),popup:list?box(list):null,options:[...document.querySelectorAll('[role=option]')].map(item=>({text:item.textContent,focused:item===focus,selected:item.getAttribute('aria-selected')}))};
   });
   const settle=async()=>{await page.evaluate(()=>window.keyboardSettled=undefined);const value=await page.waitForFunction(()=>{
    const focus=document.activeElement,nodes=[...document.querySelectorAll('#root *'),...document.querySelectorAll('[role=listbox] *')],signature=JSON.stringify([window.snapshotPagination(),window.events,focus?.outerHTML,nodes.map(node=>{const r=node.getBoundingClientRect(),s=getComputedStyle(node);return[r.x,r.y,r.width,r.height,s.color,s.backgroundColor,s.opacity,s.transform,s.boxShadow];})]),prior=window.keyboardSettled;
    const next=window.keyboardSettled={signature,start:prior?.start??performance.now(),stable:prior?.signature===signature?prior.stable+1:0,frames:(prior?.frames??0)+1};return next.stable>=3?{stable:next.stable,frames:next.frames,milliseconds:performance.now()-next.start}:false;
   },null,{polling:'raf',timeout:2000});return value.jsonValue();};
   const snapshots=[];const capture=async(phase,key=null)=>{const presentation=await settle(),before=await state(),file=`${id}-${snapshots.length}-${phase}-${scheme}.png`,image=await page.screenshot({caret:'initial',animations:'allow'});await fs.writeFile(path.join(output,file),image);assert.deepEqual(await state(),before);assert.ok(before.table.sameTable&&before.table.sameRows&&before.table.columns.every(item=>item.same)&&before.table.rows.every(item=>item.same));assert.ok(before.inputEvents.every(item=>item.trusted));snapshots.push({phase,key,state:before,presentation,file,sha256:hash(image)});};
   await page.locator('#before').click();await capture('before');await page.keyboard.press('Tab');await capture('combo','Tab');
   await page.keyboard.press(open);await page.waitForFunction(()=>document.activeElement?.getAttribute('role')==='option',null,{polling:'raf',timeout:2000});await capture('opened',open);
   if(trapTab){await page.keyboard.press('Tab');await capture('popup-tab','Tab');}
   await page.keyboard.press(navigate);const options=props.pageSizeOptions??[10,20,30,50,100],size=props.pageSize??10,target=navigate==='End'?options.at(-1):navigate==='Home'?options[0]:options[options.indexOf(size)+1];
   await page.waitForFunction(label=>document.activeElement?.getAttribute('role')==='option'&&document.activeElement.textContent===label,String(target),{polling:'raf',timeout:2000});await capture('highlighted',navigate);
   await page.keyboard.press(commit);await page.waitForFunction(()=>!document.querySelector('[role=listbox]')&&document.activeElement?.getAttribute('role')==='combobox',null,{polling:'raf',timeout:2000});await capture('committed',commit);
   for(let index=0;index<5;index++){
    await page.keyboard.press('Tab');await capture('tab-'+index,'Tab');if((await state()).focused==='after')break;assert.ok(index<4,'Tab did not leave original pagination.');
    if(reverseTab&&index===0){await page.keyboard.press('Shift+Tab');await capture('reverse-tab','Shift+Tab');await page.keyboard.press('Tab');await capture('forward-again','Tab');}
   }
   assert.deepEqual(errors,[]);reports.push({id,scheme,props,viewportWidth,viewportHeight:745,hostWidth,direction,snapshots,errors});console.log(JSON.stringify({id,scheme,states:snapshots.length}));
  }catch(error){await fs.writeFile(path.join(output,'failure.json'),JSON.stringify({id,scheme,error:String(error),errors},null,2));throw error;}finally{await page.close();}
 }}finally{await browser.close();}
 assert.equal(hash(await fs.readFile(__filename)),scriptSha256);
 const report={format:'orchard-data-table-pagination-keyboard-v1',baselineSha256:hash(baselineBytes),sourceSha256:baseline.sourceSha256,scriptSha256,clientSha256:baseline.clientSha256,bundleSha256:hash(bundle.outputFiles[0].contents),browser:baseline.browser,bundledRuntime:baseline.bundledRuntime,fontFiles:baseline.fontFiles,cases:reports};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');await fs.writeFile('tests/Reference/DataTablePaginationKeyboard.json',JSON.stringify(report,null,2)+'\n');
})().catch(error=>{console.error(error);process.exit(1)});
