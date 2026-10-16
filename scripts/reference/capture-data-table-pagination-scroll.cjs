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
   await page.evaluate(()=>{
    window.scrollReceipts=[];
    for(const type of ['pointermove','pointerdown','pointerup','scroll'])document.addEventListener(type,event=>window.scrollReceipts.push({type,trusted:event.isTrusted,target:event.target.getAttribute?.('data-radix-select-viewport')!==null?'viewport':event.target.tagName}),true);
    window.showPagination({rows:50,pageSize:10,pageSizeOptions:[1,2,3,4,5,10,20,30,40,50,100,200,500]});
   });
   const read=()=>page.evaluate(()=>{
    const list=document.querySelector('[role=listbox]'),viewport=list?.querySelector('[data-radix-select-viewport]'),box=node=>{const r=node.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};};
    const indicator=node=>{if(!node)return null;const style=getComputedStyle(node);return{box:box(node),ariaHidden:node.getAttribute('aria-hidden'),background:style.backgroundColor,color:style.color,borderTop:style.borderTop,borderBottom:style.borderBottom,icon:node.querySelector('svg')?.outerHTML};};
    const up=list?.querySelector('svg.lucide-chevron-up')?.closest('div[aria-hidden="true"]'),down=list?.querySelector('svg.lucide-chevron-down')?.closest('div[aria-hidden="true"]');
    return{table:window.snapshotPagination(),events:[...window.events],inputEvents:[...window.inputEvents],receipts:[...window.scrollReceipts],visibleRows:document.querySelector('#data-probe').textContent,popupOpen:!!list,displayed:document.querySelector('[role=combobox]').textContent,focused:document.activeElement===list?'listbox':document.activeElement?.getAttribute('role')==='option'?'option:'+document.activeElement.textContent:document.activeElement?.getAttribute('role'),popup:list?box(list):null,viewport:viewport?{box:box(viewport),scrollTop:viewport.scrollTop,clientHeight:viewport.clientHeight,scrollHeight:viewport.scrollHeight}:null,up:indicator(up),down:indicator(down)};
   });
   const settle=async()=>{await page.evaluate(()=>window.scrollSettled=undefined);const value=await page.waitForFunction(()=>{
    const list=document.querySelector('[role=listbox]'),nodes=[document.querySelector('[role=combobox]'),...(list?[list,...list.querySelectorAll('*')]:[])],signature=JSON.stringify([window.snapshotPagination(),window.events,document.activeElement?.outerHTML,nodes.map(node=>{const r=node.getBoundingClientRect(),s=getComputedStyle(node);return[r.x,r.y,r.width,r.height,node.scrollTop,s.color,s.backgroundColor,s.opacity,s.transform];})]),prior=window.scrollSettled;
    const next=window.scrollSettled={signature,start:prior?.start??performance.now(),stable:prior?.signature===signature?prior.stable+1:0,frames:(prior?.frames??0)+1};return next.stable>=3?{stable:next.stable,frames:next.frames,milliseconds:performance.now()-next.start}:false;
   },null,{polling:'raf',timeout:2000});return value.jsonValue();};
   const capture=async phase=>{const presentation=await settle(),state=await read(),file=`scroll-${phase}-${scheme}.png`,image=await page.screenshot({caret:'initial',animations:'allow'});await fs.writeFile(path.join(output,file),image);assert.deepEqual(await read(),state);assert.ok(state.table.sameTable&&state.table.sameRows&&state.table.rows.every(row=>row.same));assert.equal(state.events.length,0);assert.ok(state.receipts.every(event=>event.trusted));reports.push({scheme,phase,state,presentation,file,sha256:hash(image)});console.log(JSON.stringify({scheme,phase,focus:state.focused,offset:state.viewport?.scrollTop,height:state.popup?.height,up:!!state.up,down:!!state.down}));};
   await page.getByRole('combobox').focus();await page.keyboard.press('Space');await capture('opened');
   await page.keyboard.press('End');await capture('end');await page.keyboard.press('Home');await capture('home');
   const hover=async(direction,boundary=true)=>{
    const point=await page.evaluate(direction=>{const node=document.querySelector('[role=listbox] svg.lucide-chevron-'+direction)?.closest('div[aria-hidden="true"]');if(!node)throw Error('Missing source '+direction+' indicator.');const box=node.getBoundingClientRect();return{x:box.x+box.width/2,y:box.y+box.height/2};},direction);
    await page.mouse.move(point.x,point.y);await page.waitForFunction(({direction,boundary})=>{const viewport=document.querySelector('[data-radix-select-viewport]');return direction==='down'?(boundary?Math.ceil(viewport.scrollTop)>=viewport.scrollHeight-viewport.clientHeight:viewport.scrollTop>0):viewport.scrollTop===0;},{direction,boundary},{polling:'raf',timeout:2000});
   };
   await hover('down',false);await capture('hover-down-step');await hover('down');await capture('hover-bottom');await hover('up');await capture('hover-top');
   await page.mouse.move(950,700);await capture('left-indicator');
   await page.evaluate(()=>window.updatePagination({pageSizeOptions:[10,20]}));await capture('shrink-open');
   await page.keyboard.press('Escape');await capture('closed');assert.deepEqual(errors,[]);
  }catch(error){await fs.writeFile(path.join(output,'failure-'+scheme+'.json'),JSON.stringify({error:String(error),state:await page.evaluate(()=>({list:document.querySelector('[role=listbox]')?.outerHTML,focus:document.activeElement?.outerHTML,hovered:[...document.querySelectorAll(':hover')].map(node=>node.outerHTML.slice(0,400)),receipts:window.scrollReceipts?.slice(-60)}))},null,2));throw error;}finally{await page.close();}
 }}finally{await browser.close();}
 assert.equal(hash(await fs.readFile(__filename)),scriptSha256);
 const result={format:'orchard-data-table-pagination-scroll-v1',scriptSha256,keyboardReferenceSha256:hash(referenceBytes),bundleSha256:reference.bundleSha256,browser:reference.browser,reports};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(result,null,2)+'\n');await fs.writeFile('tests/Reference/DataTablePaginationScroll.json',JSON.stringify(result,null,2)+'\n');
})().catch(error=>{console.error(error);process.exit(1)});
