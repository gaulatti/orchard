const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const arg=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],'Pass '+name+' <path>.');return path.resolve(process.argv[at+1]);};
const output=arg('--output'),playwrightPath=arg('--playwright-module'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});const scriptSha256=hash(await fs.readFile(__filename));
 const referenceBytes=await fs.readFile('tests/Reference/DataTablePaginationKeyboard.json'),reference=JSON.parse(referenceBytes),baseline=JSON.parse(await fs.readFile('tests/Reference/DataTablePagination.json'));
 const bundle=await fs.readFile('artifacts/data-table-pagination-browser/bundle.js');assert.equal(hash(bundle),reference.bundleSha256);
 assert.equal(hash(await fs.readFile('scripts/reference/data-table-pagination-client.tsx')),reference.clientSha256);
 const css=(await fs.readFile('artifacts/line-chart-browser/styles.css','utf8')).replace(/@import url\([^\r\n]*\);/g,'');assert.equal(hash(css),baseline.compiledCssSha256);
 let fonts='';for(const font of reference.fontFiles){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+font.file);assert.equal(hash(bytes),font.sha256);fonts+=`@font-face{font-family:'Encode Sans';font-weight:${font.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const {chromium}=require(playwrightPath);assert.equal(require(path.join(playwrightPath,'package.json')).version,baseline.toolVersions.playwright);
 const browser=await chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),reference.browser);const reports=[];
 try{for(const scheme of ['Light','Dark']){
  const page=await browser.newPage({viewport:{width:1000,height:745},deviceScaleFactor:1,reducedMotion:'reduce'});page.setDefaultTimeout(2000);const errors=[];page.on('pageerror',error=>errors.push(error.message));
  try{
   await page.setContent(`<html class="${scheme==='Dark'?'dark':''}"><head><style>${css}${fonts}body{margin:0;background:var(--color-background)}#root{width:800px}</style></head><body><div id="root"></div><output id="data-probe"></output><script>${bundle.toString()}</script></body></html>`);
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(weight=>document.fonts.load(weight+' 16px "Encode Sans"')));await document.fonts.ready;});
   await page.evaluate(()=>window.showPagination({rows:8,pageIndex:1,pageSize:4,pageSizeOptions:[2,4,8]}));await page.getByRole('combobox').click();
   const read=()=>page.evaluate(()=>({table:window.snapshotPagination(),events:[...window.events],inputEvents:[...window.inputEvents],displayed:document.querySelector('[role=combobox]').textContent,popupOpen:!!document.querySelector('[role=listbox]'),options:[...document.querySelectorAll('[role=option]')].map(item=>item.textContent),visibleRows:document.querySelector('#data-probe').textContent}));
   for(const phase of ['opened','external-size','remove-current','restore-current']){
    if(phase==='external-size')await page.evaluate(()=>window.setPagination({pageIndex:0,pageSize:8}));
    if(phase==='remove-current')await page.evaluate(()=>window.updatePagination({pageSizeOptions:[2,4]}));
    if(phase==='restore-current')await page.evaluate(()=>{window.updatePagination({pageSizeOptions:[2,4,8]});window.setPagination({pageIndex:1,pageSize:4});});
    await page.evaluate(()=>window.optionsSettled=undefined);const settled=await page.waitForFunction(()=>{
     const nodes=[...document.querySelectorAll('#root *'),...document.querySelectorAll('[role=listbox] *')],signature=JSON.stringify([window.snapshotPagination(),window.events,nodes.map(node=>{const r=node.getBoundingClientRect(),s=getComputedStyle(node);return[node.outerHTML,r.x,r.y,r.width,r.height,s.color,s.backgroundColor,s.opacity,s.transform];})]),prior=window.optionsSettled;
     const value=window.optionsSettled={signature,start:prior?.start??performance.now(),stable:prior?.signature===signature?prior.stable+1:0,frames:(prior?.frames??0)+1};return value.stable>=3?{stable:value.stable,frames:value.frames,milliseconds:performance.now()-value.start}:false;
    },null,{polling:'raf',timeout:2000});
    const state=await read(),file=`open-options-${phase}-${scheme}.png`,image=await page.screenshot({caret:'initial',animations:'allow'});await fs.writeFile(path.join(output,file),image);assert.deepEqual(await read(),state);
    assert.ok(state.table.sameTable&&state.table.sameRows&&state.table.columns.every(item=>item.same)&&state.table.rows.every(item=>item.same));assert.ok(state.inputEvents.every(item=>item.trusted));
    assert.equal(state.events.length,0);assert.equal(state.popupOpen,true);assert.equal(state.displayed,phase==='remove-current'?'':phase==='external-size'?'8':'4');
    reports.push({scheme,phase,state,presentation:await settled.jsonValue(),file,sha256:hash(image)});
   }
   assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 assert.equal(hash(await fs.readFile(__filename)),scriptSha256);
 const result={format:'orchard-data-table-pagination-open-options-v1',scriptSha256,keyboardReferenceSha256:hash(referenceBytes),bundleSha256:reference.bundleSha256,reports};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(result,null,2)+'\n');await fs.writeFile('tests/Reference/DataTablePaginationOpenOptions.json',JSON.stringify(result,null,2)+'\n');
 console.log(JSON.stringify({cases:2,states:reports.length,passed:true}));
})().catch(error=>{console.error(error);process.exit(1)});
