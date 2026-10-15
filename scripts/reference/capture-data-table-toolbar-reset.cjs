const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto');
const at=process.argv.indexOf('--playwright-module');
if(at<0||!process.argv[at+1])throw Error('Pass --playwright-module <absolute package directory>.');
const modulePath=path.resolve(process.argv[at+1]);
if(require(path.join(modulePath,'package.json')).version!=='1.62.1')throw Error('Pinned Playwright differs.');
const {chromium}=require(modulePath),hash=value=>crypto.createHash('sha256').update(value).digest('hex');
(async()=>{
 const reference=JSON.parse(await fs.readFile('tests/Reference/DataTableToolbarLayout.json','utf8'));
 const source=await fs.readFile('artifacts/line-chart-browser/frozen-source/src/components/data-table-toolbar.tsx');
 if(hash(source)!==reference.sourceSha256)throw Error('Frozen source differs.');
 const css=(await fs.readFile('artifacts/line-chart-browser/styles.css','utf8')).replace(/@import url\([^\r\n]*\);/g,'');
 if(hash(css)!==reference.compiledCssSha256)throw Error('Frozen CSS differs.');
 let fonts='';for(const [weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){
  const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);
  if(hash(bytes)!==reference.fontFiles.find(item=>item.file===file).sha256)throw Error('Frozen font differs.');
  fonts+="@font-face{font-family:'Encode Sans';font-weight:"+weight+";src:url(data:font/ttf;base64,"+bytes.toString('base64')+")}"
 }
 const bundle=await fs.readFile('artifacts/data-table-toolbar-browser/bundle.js','utf8');
 const browser=await chromium.launch({headless:true,channel:'msedge'}),results=[];
 if(browser.version()!==reference.browser){await browser.close();throw Error('Frozen browser differs.');}
 try{for(const scheme of ['Light','Dark'])for(const reducedMotion of ['reduce','no-preference']){
  const sample=reference.cases.find(item=>item.id==='global-filter'&&item.scheme===scheme);
  const page=await browser.newPage({viewport:{width:1000,height:745},deviceScaleFactor:1,reducedMotion});
  await page.setContent('<html class="'+(scheme==='Dark'?'dark':'')+'"><head><style>'+css+fonts+'body{margin:0;background:var(--color-background)}#root{width:800px}</style></head><body><button id="before">Before</button><div id="root"></div><button id="after">After</button><output id="data-probe"></output><script>'+bundle+'</script></body></html>');
  await page.evaluate(model=>window.showToolbar(model),sample.props);
  await page.evaluate(async()=>{await document.fonts.ready;window.resetFrames=[];window.resetReceipts=[];
   const root=document.querySelector('#root'),button=[...root.querySelectorAll('button')].find(item=>item.textContent==='Reset');
   window.readReset=phase=>{const r=button.getBoundingClientRect(),o=root.getBoundingClientRect(),s=getComputedStyle(button);return{phase,time:performance.now(),box:{x:r.x-o.x,y:r.y-o.y,width:r.width,height:r.height},scale:s.scale,translate:s.translate,transform:s.transform,background:s.backgroundColor,color:s.color,border:s.borderColor,transitionProperty:s.transitionProperty,transitionDuration:s.transitionDuration,transitionTimingFunction:s.transitionTimingFunction,focused:document.activeElement===button,active:button.matches(':active')};};
   for(const name of ['pointerdown','pointerup','click'])button.addEventListener(name,event=>{window.resetReceipts.push({name,trusted:event.isTrusted,pointerType:event.pointerType,button:event.button});window.resetFrames.push(window.readReset(name));});
  });
  const settle=async phase=>{await page.evaluate(()=>window.resetSettled=undefined);
   const value=await page.waitForFunction(phase=>{const next=window.readReset(phase);window.resetFrames.push(next);const signature=JSON.stringify({...next,time:0}),old=window.resetSettled;
    window.resetSettled={signature,stable:old?.signature===signature?old.stable+1:0,frames:(old?.frames??0)+1,start:old?.start??performance.now()};
    const current=window.resetSettled;return current.stable>=3?{...next,stable:current.stable,frames:current.frames,milliseconds:performance.now()-current.start}:false;
   },phase,{polling:'raf',timeout:2000});return value.jsonValue();};
  const normal=await settle('normal');const reset=page.getByRole('button',{name:'Reset',exact:true});
  await reset.hover();const hover=await settle('hover');await page.mouse.down();const held=await settle('held');
  await page.mouse.up();const released=await settle('released');
  const detail=await page.evaluate(()=>({frames:window.resetFrames,receipts:window.resetReceipts,table:window.snapshotTable()}));
  if(detail.receipts.length!==3||detail.receipts.some(item=>!item.trusted||item.pointerType!=='mouse'||item.button!==0))throw Error('Original Reset lacks exact trusted mouse receipts.');
  await reset.focus();await page.evaluate(()=>{window.resetKeys=[];const button=[...document.querySelectorAll('#root button')].find(item=>item.textContent==='Reset');
   for(const name of ['keydown','keyup'])button.addEventListener(name,event=>window.resetKeys.push({name,trusted:event.isTrusted,key:event.key,repeat:event.repeat}));});
  await page.keyboard.down('Space');const spaceHeld=await settle('space-held');
  await page.keyboard.up('Space');const spaceReleased=await settle('space-released');
  const keyboard=await page.evaluate(()=>({receipts:window.resetKeys,table:window.snapshotTable()}));
  if(keyboard.receipts.length!==2||keyboard.receipts.some(item=>!item.trusted||item.key!==' '||item.repeat))throw Error('Original Reset lacks exact trusted Space receipts.');
  results.push({scheme,reducedMotion,props:sample.props,normal,hover,held,released,...detail,spaceHeld,spaceReleased,keyboard});await page.close();
 }}finally{await browser.close();}
 const report={format:'orchard-data-table-toolbar-reset-v1',browser:reference.browser,playwright:'1.62.1',sourceCommit:reference.sourceCommit,sourceSha256:reference.sourceSha256,compiledCssSha256:reference.compiledCssSha256,bundleSha256:hash(bundle),probeScriptSha256:hash(await fs.readFile(__filename)),results};
 await fs.writeFile('tests/Reference/DataTableToolbarReset.json',JSON.stringify(report,null,2)+'\n');
 console.log(JSON.stringify(results.map(({scheme,reducedMotion,held,spaceHeld,spaceReleased})=>({scheme,reducedMotion,heldScale:held.scale,spaceHeldScale:spaceHeld.scale,spaceHeldTranslate:spaceHeld.translate,spaceReleasedScale:spaceReleased.scale}))));
})().catch(error=>{console.error(error);process.exit(1)});
