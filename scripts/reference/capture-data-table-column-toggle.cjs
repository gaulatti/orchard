const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const arg=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],'Pass '+name+' <path>.');return path.resolve(process.argv[at+1]);};
const output=arg('--output'),esbuildPath=arg('--esbuild-module'),playwrightPath=arg('--playwright-module'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const samples=[
 {id:'default',actions:['open','click-first','space','external-status-off','tab','rename','escape','open','outside']},
 {id:'replacement',actions:['open','replace','click-first','old-notification','escape']},
 {id:'empty',kind:'empty',actions:['open','tab','reverse-tab','escape']},
 {id:'custom',label:'Fields',visibility:{status:false,rendered:false,blank:false,locked:false},actions:['open','click-first','escape']},
 {id:'group',kind:'group',actions:['open','click-first','escape']},
 {id:'long',kind:'long',actions:['open','reverse-tab','space','tab','escape']},
 {id:'narrow',viewportWidth:400,hostWidth:368,actions:['open','click-first','escape']},
 {id:'rtl',direction:'rtl',actions:['open','click-first','escape']},
 {id:'bottom',top:640,actions:['open','click-first','escape']},
 {id:'collision',viewportWidth:240,hostWidth:208,actions:['open','escape']},
 {id:'all-hidden',visibility:{title:false,status:false,rendered:false,blank:false},actions:['open','space','escape']},
 {id:'empty-label',label:'',actions:['open','escape']},
 {id:'no-rows',rows:0,actions:['open','click-first','escape']},
 {id:'eligibility',actions:['open','hide-eligibility','restore-eligibility','escape']},
];
(async()=>{
 await fs.mkdir(output,{recursive:true});const scriptSha256=hash(await fs.readFile(__filename));
 const sourceRoot=path.resolve('artifacts/line-chart-browser/frozen-source'),runtime=path.resolve('artifacts/data-table-column-toggle-browser/node_modules'),shared=path.resolve('artifacts/data-table-pagination-browser/node_modules');
 const source=path.join(sourceRoot,'src/components/data-table-column-toggle.tsx');assert.equal(hash(await fs.readFile(source)),'9082ca90691d40083e9bb4b94f687daa56a8e3a064e356ab5dd9afa67ed89df7');
 const lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lockBlob=crypto.createHash('sha1').update(Buffer.concat([Buffer.from('blob '+lockBytes.length+'\0'),lockBytes])).digest('hex');assert.equal(lockBlob,'0b6113dd13d74b1023208cbf96e080915f3b47a2');const lock=JSON.parse(lockBytes);
 const baseline=JSON.parse(await fs.readFile('tests/Reference/DataTablePagination.json'));assert.equal(require(esbuildPath).version,baseline.toolVersions.esbuild);assert.equal(require(path.join(playwrightPath,'package.json')).version,baseline.toolVersions.playwright);
 const aliases=Object.fromEntries(['react','react-dom','lucide-react','clsx','tailwind-merge','@tanstack/react-table'].map(name=>[name,path.join(shared,name)]));aliases['@radix-ui/react-popover']=path.join(runtime,'@radix-ui/react-popover');
 const build=await require(esbuildPath).build({entryPoints:['scripts/reference/data-table-column-toggle-client.tsx'],bundle:true,write:false,metafile:true,platform:'browser',alias:{'@frozen/data-table-column-toggle':source,...aliases},nodePaths:[runtime,shared],define:{'process.env.NODE_ENV':'"production"'}});
 const packages=new Map(),dependencies=[];
 for(const input of Object.keys(build.metafile.inputs)){
  const full=path.resolve(input),normalized=full.replaceAll('\\','/');if(full.startsWith(sourceRoot+path.sep))dependencies.push({file:path.relative(sourceRoot,full).replaceAll('\\','/'),sha256:hash(await fs.readFile(full))});
  const at=normalized.lastIndexOf('/node_modules/');if(at<0)continue;const parts=normalized.slice(at+14).split('/'),name=parts[0].startsWith('@')?parts.slice(0,2).join('/'):parts[0],root=normalized.slice(0,at+14)+name;if(packages.has(root))continue;
  const metadata=JSON.parse(await fs.readFile(path.join(root,'package.json'))),first=normalized.indexOf('/node_modules/'),installed=JSON.parse(await fs.readFile(normalized.slice(0,first+14)+'.package-lock.json')),entry=installed.packages[root.slice(first+1)];
  assert.ok(entry&&metadata.version===entry.version&&Object.entries(lock.packages).some(([key,value])=>(key==='node_modules/'+name||key.endsWith('/node_modules/'+name))&&value.version===entry.version&&value.integrity===entry.integrity),'Frozen dependency differs: '+name+' '+metadata.version);packages.set(root,{name,version:entry.version,integrity:entry.integrity});
 }
 const css=(await fs.readFile('artifacts/line-chart-browser/styles.css','utf8')).replace(/@import url\([^\r\n]*\);/g,'');assert.equal(hash(css),baseline.compiledCssSha256);let fonts='';for(const item of baseline.fontFiles){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+item.file);assert.equal(hash(bytes),item.sha256);fonts+=`@font-face{font-family:'Encode Sans';font-weight:${item.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),baseline.browser);const reports=[];
 try{for(const scheme of ['Light','Dark'])for(const sample of samples){
  const {id,viewportWidth=1000,hostWidth=520,direction='ltr',top=16,actions,...props}=sample,page=await browser.newPage({viewport:{width:viewportWidth,height:745},deviceScaleFactor:1,reducedMotion:'reduce'}),errors=[];page.setDefaultTimeout(2000);page.on('pageerror',error=>errors.push(error.message));
  try{
   await page.setContent(`<html dir="${direction}" class="${scheme==='Dark'?'dark':''}"><head><style>${css}${fonts}body{margin:0;background:var(--color-background)}#root{display:flex;justify-content:flex-end;width:${hostWidth}px;margin:${top}px 16px 16px}#data-probe{display:block;margin:16px}</style></head><body><div id="root"></div><button id="outside">Outside</button><output id="data-probe"></output><script>${build.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(weight=>document.fonts.load(weight+' 16px "Encode Sans"')));await document.fonts.ready;});await page.evaluate(props=>window.showColumns(props),props);await page.waitForFunction(()=>window.snapshotColumns&&document.querySelector('#root button'));
   const read=()=>page.evaluate(()=>{
    const trigger=document.querySelector('#root button'),popup=document.querySelector('[role=dialog]'),checks=[...(popup?.querySelectorAll('input[type=checkbox]')??[])],eligible=window.table.getAllColumns().filter(column=>typeof column.accessorFn!=='undefined'&&column.getCanHide()),focus=document.activeElement;
    const box=node=>{const r=node.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};},style=node=>{const s=getComputedStyle(node);return{background:s.backgroundColor,color:s.color,border:s.border,borderRadius:s.borderRadius,padding:s.padding,font:s.font,lineHeight:s.lineHeight,boxShadow:s.boxShadow,transform:s.transform,opacity:s.opacity};};
    const viewport=popup?.querySelector('div.max-h-60');return{table:window.snapshotColumns(),events:[...window.events],inputEvents:[...window.inputEvents],outsideClicks:window.outsideClicks,visibleCells:document.querySelector('#data-probe').textContent,label:trigger.textContent,focused:focus===trigger?'trigger':checks.includes(focus)?'column:'+eligible[checks.indexOf(focus)].id:focus===popup?'dialog':focus?.id==='outside'?'outside':focus?.tagName,popupOpen:!!popup,trigger:{box:box(trigger),style:style(trigger),expanded:trigger.getAttribute('aria-expanded'),icon:trigger.querySelector('svg')?.outerHTML},popup:popup?{box:box(popup),style:style(popup),role:popup.getAttribute('role'),tabIndex:popup.tabIndex}:null,viewport:viewport?{box:box(viewport),scrollTop:viewport.scrollTop,scrollHeight:viewport.scrollHeight,clientHeight:viewport.clientHeight}:null,options:checks.map((check,index)=>({id:eligible[index].id,checked:check.checked,label:check.closest('label').textContent,box:box(check.closest('label')),input:box(check),style:style(check.closest('label'))}))};
   });
   const settle=async()=>{await page.evaluate(()=>window.columnSettled=undefined);const value=await page.waitForFunction(()=>{const popup=document.querySelector('[role=dialog]'),nodes=[document.querySelector('#root button'),...(popup?[popup,...popup.querySelectorAll('*')]:[])],signature=JSON.stringify([window.snapshotColumns(),window.events,window.outsideClicks,document.activeElement?.outerHTML,nodes.map(node=>{const r=node.getBoundingClientRect(),s=getComputedStyle(node);return[r.x,r.y,r.width,r.height,s.opacity,s.transform,s.backgroundColor];})]),prior=window.columnSettled;const next=window.columnSettled={signature,stable:prior?.signature===signature?prior.stable+1:0,frames:(prior?.frames??0)+1,start:prior?.start??performance.now()};return next.stable>=3?{stable:next.stable,frames:next.frames,milliseconds:performance.now()-next.start}:false;},null,{polling:'raf',timeout:2000});return value.jsonValue();};
   const snapshots=[];const capture=async(phase,action=null)=>{const presentation=await settle(),before=await read(),file=`${id}-${snapshots.length}-${phase}-${scheme}.png`,bytes=await page.screenshot({caret:'initial',animations:'allow'});await fs.writeFile(path.join(output,file),bytes);assert.deepEqual(await read(),before);assert.ok(before.table.sameTable&&before.table.sameRows&&before.table.columns.every(column=>column.same)&&before.table.rows.every(row=>row.same&&row.cells.every(cell=>cell.same)));assert.ok(before.inputEvents.every(event=>event.trusted));const accessibility=before.popupOpen?await page.locator('[role=dialog]').ariaSnapshot():null;snapshots.push({phase,action,presentation,state:before,accessibility,file,sha256:hash(bytes),width:viewportWidth,height:745});};
   await capture('closed');
   for(const action of actions){
    if(action==='open')await page.locator('#root button').click();
    else if(action==='click-first')await page.locator('[role=dialog] label').first().click();
    else if(action==='space')await page.keyboard.press('Space');
    else if(action==='tab')await page.keyboard.press('Tab');
    else if(action==='reverse-tab')await page.keyboard.press('Shift+Tab');
    else if(action==='escape')await page.keyboard.press('Escape');
    else if(action==='outside')await page.locator('#outside').click();
    else if(action==='external-status-off')await page.evaluate(()=>window.setVisibility({...window.table.getState().columnVisibility,status:false}));
    else if(action==='rename')await page.evaluate(()=>window.updateColumns({label:'Fields now'}));
    else if(action==='replace')await page.evaluate(()=>window.updateColumns({secondTable:true}));
    else if(action==='old-notification')await page.evaluate(()=>window.setOldVisibility({title:false,status:false}));
    else if(action==='hide-eligibility')await page.evaluate(()=>window.updateColumns({enableHiding:false}));
    else if(action==='restore-eligibility')await page.evaluate(()=>window.updateColumns({enableHiding:true}));
    else throw Error('Unknown source action '+action);
    await capture(action,action);
   }
   assert.deepEqual(errors,[]);reports.push({id,scheme,props,viewportWidth,hostWidth,direction,top,snapshots});console.log(JSON.stringify({id,scheme,states:snapshots.length}));
  }catch(error){await fs.writeFile(path.join(output,'failure.json'),JSON.stringify({id,scheme,error:String(error),errors},null,2));throw error;}finally{await page.close();}
 }}finally{await browser.close();}
 assert.equal(hash(await fs.readFile(__filename)),scriptSha256);
 const report={format:'orchard-data-table-column-toggle-v1',sourceCommit:baseline.sourceCommit,sourceSha256:hash(await fs.readFile(source)),sourceLockGitBlob:lockBlob,scriptSha256,clientSha256:hash(await fs.readFile('scripts/reference/data-table-column-toggle-client.tsx')),compiledCssSha256:hash(css),browser:baseline.browser,toolVersions:baseline.toolVersions,fontFiles:baseline.fontFiles,dependencies:dependencies.sort((a,b)=>a.file.localeCompare(b.file)),bundledRuntime:[...packages.values()].sort((a,b)=>a.name.localeCompare(b.name)),cases:reports};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');await fs.writeFile('tests/Reference/DataTableColumnToggle.json',JSON.stringify(report,null,2)+'\n');
})().catch(error=>{console.error(error);process.exit(1)});
