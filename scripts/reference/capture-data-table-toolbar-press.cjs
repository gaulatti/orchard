const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const option=name=>{const i=process.argv.indexOf(name);if(i<0||!process.argv[i+1])throw Error('Pass '+name+' <directory>.');return path.resolve(process.argv[i+1]);};
const modulePath=option('--playwright-module'),output=option('--output');
assert.equal(require(path.join(modulePath,'package.json')).version,'1.62.1');
const {chromium}=require(modulePath),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 const scriptSha256=hash(await fs.readFile(__filename)),referenceBytes=await fs.readFile('tests/Reference/DataTableToolbarLayout.json'),reference=JSON.parse(referenceBytes);
 for(const dependency of reference.dependencies)assert.equal(hash(await fs.readFile(path.join('artifacts/line-chart-browser/frozen-source',dependency.file))),dependency.sha256);
 const bundle=await fs.readFile('artifacts/data-table-toolbar-browser/bundle.js','utf8');assert.equal(hash(bundle),JSON.parse(await fs.readFile('tests/Reference/DataTableToolbarReset.json','utf8')).bundleSha256);
 const css=(await fs.readFile('artifacts/line-chart-browser/styles.css','utf8')).replace(/@import url\([^\r\n]*\);/g,'');assert.equal(hash(css),reference.compiledCssSha256);
 let fonts='';for(const [weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){
  const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);assert.equal(hash(bytes),reference.fontFiles.find(item=>item.file===file).sha256);
  fonts+="@font-face{font-family:'Encode Sans';font-weight:"+weight+";src:url(data:font/ttf;base64,"+bytes.toString('base64')+")}"
 }
 await fs.mkdir(output,{recursive:true});const browser=await chromium.launch({headless:true,channel:'msedge'}),results=[];assert.equal(browser.version(),reference.browser);
 try{for(const scheme of ['Light','Dark'])for(const reducedMotion of ['reduce','no-preference'])for(const id of ['space-outside','space-hover','enter-outside','drag-out','drag-return','space-tab-cancel']){
  const sample=reference.cases.find(item=>item.id==='global-filter'&&item.scheme===scheme),page=await browser.newPage({viewport:{width:1000,height:745},deviceScaleFactor:1,reducedMotion}),errors=[];
  page.on('pageerror',error=>errors.push(error.message));let keyHeld=false,mouseHeld=false;
  try{
   await page.setContent('<html class="'+(scheme==='Dark'?'dark':'')+'"><head><style>'+css+fonts+'body{margin:0;background:var(--color-background)}#root{width:800px}</style></head><body><button id="before">Before</button><div id="root"></div><button id="after">After</button><output id="data-probe"></output><script>'+bundle+'</script></body></html>');
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(weight=>document.fonts.load(weight+' 16px "Encode Sans"')));await document.fonts.ready;});
   await page.evaluate(props=>window.showToolbar(props),sample.props);
   const search=page.locator('#root input'),reset=page.getByRole('button',{name:'Reset',exact:true});await search.click();await page.keyboard.press('Tab');await page.keyboard.press('Tab');
   assert.equal(await reset.evaluate(button=>document.activeElement===button),true);
   await page.evaluate(()=>{
    const button=[...document.querySelectorAll('#root button')].find(item=>item.textContent==='Reset');window.pressClicks=[];
    button.addEventListener('click',event=>window.pressClicks.push({trusted:event.isTrusted,detail:event.detail,pointerType:event.pointerType??null}));
    window.readPress=phase=>{const origin=document.querySelector('#root').getBoundingClientRect(),r=button.getBoundingClientRect(),s=getComputedStyle(button),pointer=window.inputEvents.findLast(item=>item.type==='pointermove'),hit=pointer&&document.elementFromPoint(pointer.x,pointer.y);
     return{phase,box:{x:r.x-origin.x,y:r.y-origin.y,width:r.width,height:r.height},scale:s.scale,translate:s.translate,transform:s.transform,background:s.backgroundColor,color:s.color,border:s.borderColor,transitionProperty:s.transitionProperty,transitionDuration:s.transitionDuration,transitionTimingFunction:s.transitionTimingFunction,focused:document.activeElement===button,focusRole:document.activeElement===button?'reset':document.activeElement instanceof HTMLInputElement?'search':document.activeElement.getAttribute('data-slot')??document.activeElement.id??document.activeElement.tagName,activeQuery:button.matches(':active'),hoverQuery:button.matches(':hover'),pointer,pointerOwnsReset:!!hit&&button.contains(hit),clicks:[...window.pressClicks],events:[...window.events],table:window.snapshotTable(),visibleRows:document.querySelector('#data-probe').textContent};};
   });
   const states=[];
   const capture=async phase=>{
    await page.evaluate(()=>window.pressSettled=undefined);
    const handle=await page.waitForFunction(phase=>{const next=window.readPress(phase),signature=JSON.stringify(next),prior=window.pressSettled;window.pressSettled={signature,stable:prior?.signature===signature?prior.stable+1:0,frames:(prior?.frames??0)+1,start:prior?.start??performance.now()};const current=window.pressSettled;return current.stable>=3?{state:next,presentation:{stable:current.stable,frames:current.frames,milliseconds:performance.now()-current.start}}:false;},phase,{polling:'raf',timeout:2000});
    const point=await handle.jsonValue(),file=`${id}-${scheme}-${reducedMotion}-${phase}.png`,bytes=await page.locator('#root').screenshot({caret:'initial',animations:'allow'});await fs.writeFile(path.join(output,file),bytes);
    assert.deepEqual(await page.evaluate(phase=>window.readPress(phase),phase),point.state);states.push({...point,file,sha256:hash(bytes)});
   };
   if(id==='space-hover'||id.startsWith('drag-'))await reset.hover();
   await capture('normal');
   if(id.startsWith('drag-')){
    await page.mouse.down();mouseHeld=true;await capture('held');const box=await search.boundingBox();await page.mouse.move(box.x+box.width/2,box.y+box.height/2);await capture('outside');
    if(id==='drag-return'){await reset.hover();await capture('returned');}
    await page.mouse.up();mouseHeld=false;await capture('released');
   }else{
    const key=id==='enter-outside'?'Enter':'Space';await page.keyboard.down(key);keyHeld=true;await capture('held');
    if(id==='space-tab-cancel'){await page.keyboard.press('Shift+Tab');await capture('blurred');}
    await page.keyboard.up(key);keyHeld=false;await capture('released');
   }
   const receipts=await page.evaluate(()=>window.inputEvents);assert.equal(receipts.every(item=>item.trusted),true);assert.deepEqual(errors,[]);
   results.push({id,scheme,reducedMotion,props:sample.props,states,receipts,errors});console.log(JSON.stringify({id,scheme,reducedMotion,states:states.length}));
  }finally{if(keyHeld)await page.keyboard.up(id==='enter-outside'?'Enter':'Space');if(mouseHeld)await page.mouse.up();await page.close();}
 }}finally{await browser.close();}
 assert.equal(hash(await fs.readFile(__filename)),scriptSha256,'Capture script changed during collection.');
 const report={format:'orchard-data-table-toolbar-press-v1',browser:reference.browser,playwright:'1.62.1',sourceCommit:reference.sourceCommit,sourceSha256:reference.sourceSha256,compiledCssSha256:reference.compiledCssSha256,bundleSha256:hash(bundle),referenceSha256:hash(referenceBytes),scriptSha256,caret:'initial',animations:'allow',masks:0,tolerance:0,results};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');await fs.writeFile('tests/Reference/DataTableToolbarPress.json',JSON.stringify(report,null,2)+'\n');
})().catch(error=>{console.error(error);process.exit(1)});
