const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const option=name=>{const index=process.argv.indexOf(name);if(index<0||!process.argv[index+1])throw Error('Pass '+name+' <absolute directory>.');return path.resolve(process.argv[index+1]);};
const modulePath=option('--playwright-module'),output=option('--output');
if(require(path.join(modulePath,'package.json')).version!=='1.62.1')throw Error('Pinned Playwright differs.');
const {chromium}=require(modulePath),hash=value=>crypto.createHash('sha256').update(value).digest('hex');
const canonical=value=>Array.isArray(value)?value.map(canonical):value&&typeof value==='object'?Object.fromEntries(Object.keys(value).sort().map(key=>[key,canonical(value[key])])):value;
const digest=value=>hash(JSON.stringify(canonical(value)));
(async()=>{
 const scriptSha256=hash(await fs.readFile(__filename)),referenceBytes=await fs.readFile('tests/Reference/DataTableToolbarLayout.json'),reference=JSON.parse(referenceBytes);
 const resetProbe=JSON.parse(await fs.readFile('tests/Reference/DataTableToolbarReset.json','utf8'));
 const sourceRoot='artifacts/line-chart-browser/frozen-source';
 for(const dependency of reference.dependencies)assert.equal(hash(await fs.readFile(path.join(sourceRoot,dependency.file))),dependency.sha256);
 assert.equal(hash(await fs.readFile('scripts/reference/data-table-toolbar-client.tsx')),reference.clientSha256);
 const bundle=await fs.readFile('artifacts/data-table-toolbar-browser/bundle.js','utf8');assert.equal(hash(bundle),resetProbe.bundleSha256);
 const css=(await fs.readFile('artifacts/line-chart-browser/styles.css','utf8')).replace(/@import url\([^\r\n]*\);/g,'');assert.equal(hash(css),reference.compiledCssSha256);
 let fonts='';for(const [weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){
  const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);assert.equal(hash(bytes),reference.fontFiles.find(item=>item.file===file).sha256);
  fonts+="@font-face{font-family:'Encode Sans';font-weight:"+weight+";src:url(data:font/ttf;base64,"+bytes.toString('base64')+")}"
 }
 const expand=value=>Array.isArray(value)?value.map(expand):value&&typeof value==='object'?Object.fromEntries(Object.entries(value).map(([key,item])=>key==='styleIndex'?['style',reference.styles[item]]:[key,expand(item)])):value;
 const normalize=value=>({...value,inputEvents:value.inputEvents.map(({time,...receipt})=>receipt)});
 await fs.mkdir(output,{recursive:true});const captures=[],cases=[],browser=await chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),reference.browser);
 try{for(const sample of reference.cases.filter(item=>item.states.length||item.screenshots.length)){
  const {id,scheme}=sample;const page=await browser.newPage({viewport:{width:sample.viewportWidth,height:700},deviceScaleFactor:1,reducedMotion:'reduce'});
  const errors=[];page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{if(['error','warning'].includes(message.type()))errors.push(message.type()+': '+message.text());});
  try{
   await page.setContent('<html class="'+(scheme==='Dark'?'dark':'')+'" dir="'+sample.direction+'"><head><style>'+css+fonts+'body{margin:0;background:var(--color-background)}#root{width:'+sample.hostWidth+'px}'+sample.customCss+'</style></head><body><button id="before">Before</button><div id="root"></div><button id="after">After</button><output id="data-probe"></output><script>'+bundle+'</script></body></html>');
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(weight=>document.fonts.load(weight+' 16px "Encode Sans"')));await document.fonts.ready;});
   await page.evaluate(props=>window.showToolbar(props),sample.props);
   const settle=async()=>{
    await page.evaluate(()=>window.__pixelSettled=undefined);
    const handle=await page.waitForFunction(()=>{
     const root=document.querySelector('#root');if(!root?.firstElementChild||!window.snapshotTable)return false;
     const paint=[...root.querySelectorAll('*')].map(node=>{const r=node.getBoundingClientRect(),s=getComputedStyle(node);return[r.x,r.y,r.width,r.height,s.color,s.backgroundColor,s.borderColor,s.boxShadow,s.opacity,s.transform,s.translate,s.scale,node instanceof HTMLInputElement?node.value:null];});
     const signature=JSON.stringify([root.innerHTML,paint,window.snapshotTable(),document.querySelector('#data-probe').textContent]),prior=window.__pixelSettled;
     window.__pixelSettled={signature,stable:prior?.signature===signature?prior.stable+1:0,frames:(prior?.frames??0)+1,start:prior?.start??performance.now()};
     const current=window.__pixelSettled;return current.stable>=3?{stable:current.stable,frames:current.frames,milliseconds:performance.now()-current.start}:false;
    },null,{polling:'raf',timeout:2000});return handle.jsonValue();
   };
   const read=async()=>({...await page.locator('#root').evaluate(root=>{
    const origin=root.getBoundingClientRect(),box=node=>{const r=node.getBoundingClientRect();return{x:r.x-origin.x,y:r.y-origin.y,width:r.width,height:r.height};};
    const shape=node=>{if(node.nodeType===3){const range=document.createRange();range.selectNode(node);return{tag:'#text',text:node.textContent,rects:[...range.getClientRects()].map(r=>({x:r.x-origin.x,y:r.y-origin.y,width:r.width,height:r.height}))};}const style=getComputedStyle(node);return{tag:node.tagName,text:node.textContent,attributes:Object.fromEntries([...node.attributes].map(item=>[item.name,item.value])),box:box(node),style:Object.fromEntries(['display','flexDirection','flexGrow','flexShrink','flexBasis','alignItems','justifyContent','width','height','minWidth','maxWidth','minHeight','maxHeight','padding','margin','gap','fontFamily','fontSize','fontWeight','lineHeight','letterSpacing','color','backgroundColor','borderColor','borderWidth','borderRadius','opacity','pointerEvents','boxShadow','transform','translate','scale','transformOrigin','transitionProperty','transitionDuration','transitionTimingFunction','overflowX','overflowY','whiteSpace','cursor','outlineColor','outlineStyle','outlineWidth','outlineOffset','stroke','strokeWidth'].map(key=>[key,style[key]])),focused:document.activeElement===node,...(node instanceof HTMLInputElement?{value:node.value,placeholder:node.placeholder,selectionStart:node.selectionStart,selectionEnd:node.selectionEnd}:{}),...(node instanceof HTMLButtonElement?{disabled:node.disabled}:{}),children:[...node.childNodes].map(shape)};};
    return{surface:shape(root.firstElementChild),table:window.snapshotTable(),visibleRows:document.querySelector('#data-probe').textContent,events:[...window.events],inputEvents:[...window.inputEvents],focused:document.activeElement===document.body?'body':document.activeElement.id||document.activeElement.getAttribute('data-slot')||document.activeElement.tagName,rootBox:box(root),scrollWidth:root.scrollWidth,clientWidth:root.clientWidth};
   }),aria:await page.locator('#root').ariaSnapshot()});
   const verify=async(expected,phase)=>{
    const actual=await read();try{assert.deepEqual(normalize(actual),normalize(expand(expected)));}catch(error){await fs.writeFile(path.join(output,'mismatch.json'),JSON.stringify({id,scheme,phase,actual,expected:expand(expected)},null,2));throw Error(`${id}/${scheme}/${phase} differs from the frozen state: ${error.message.slice(0,600)}`);}
    assert.ok(actual.inputEvents.every(receipt=>receipt.trusted));return actual;
   };
   const capture=async(expected,step,action,presentation)=>{
    const before=await verify(expected,action);const file=`${id}-${String(step).padStart(2,'0')}-${action}-${scheme}.png`,repeatFile=file.replace(/\.png$/,'.repeat.png');
    const first=await page.locator('#root').screenshot({path:path.join(output,file),caret:'initial',animations:'allow'});
    await page.evaluate(()=>new Promise(requestAnimationFrame));
    const repeat=await page.locator('#root').screenshot({path:path.join(output,repeatFile),caret:'initial',animations:'allow'});
    const after=await verify(expected,action+'-after-capture');assert.deepEqual(normalize(after),normalize(before));
    const repeatPixelStable=first.equals(repeat);
    if(step===0){assert.equal(hash(first),sample.screenshots[0].sha256,'Original initial pixels differ.');assert.equal(repeatPixelStable,true,'Original initial repeat pixels differ.');}
    captures.push({id,scheme,step,action,file,repeatFile,width:first.readUInt32BE(16),height:first.readUInt32BE(20),sha256:hash(first),repeatSha256:hash(repeat),repeatPixelStable,stateSha256:digest(normalize(before)),expectedStateSha256:digest(normalize(expand(expected))),postCaptureStateSha256:digest(normalize(after)),inputReceipts:before.inputEvents.length,trustedInputReceipts:before.inputEvents.filter(receipt=>receipt.trusted).length,focused:before.focused,presentation});
   };
   const normalPresentation=await settle();await verify(sample.normal,'normal');let originalInitialReplay=null;
   if(sample.screenshots.length){
    await capture(sample.normal,0,'initial',normalPresentation);
    // Replay the original producer capture at its original point. Playwright's
    // default caret cleanup leaves an empty style attribute on unfocused inputs;
    // that attribute is present in the frozen subsequent DOM states.
    assert.equal(sample.normal.focused,'body');
    const original=await page.locator('#root').screenshot({caret:'hide',animations:'allow'});assert.equal(hash(original),sample.screenshots[0].sha256);
    originalInitialReplay={caret:'hide',unfocused:true,sha256:hash(original)};
   }
   let step=0,tabStarted=false;
   for(const recorded of sample.states){
    const action=recorded.action,search=page.locator('#root input'),reset=page.getByRole('button',{name:'Reset',exact:true});
    switch(action){
     case'search-focus':await search.focus();break;case'search-fill':await search.fill('Harbor');break;case'search-clear':await search.fill('');break;
     case'search-keyboard':await search.focus();await page.keyboard.type('Reed');break;
     case'reset-hover':await reset.hover();break;case'reset-held':await page.mouse.down();break;case'reset-release':await page.mouse.up();break;
     case'reset-click':await reset.click();break;case'reset-keyboard':case'reset-Enter':await reset.focus();await page.keyboard.press('Enter');break;
     case'filter-slot':await page.locator('[data-slot="filters"]').first().click();break;case'action-slot':await page.locator('[data-slot="actions"]').first().click();break;
     case'Tab':if(!tabStarted){await page.locator('#before').focus();tabStarted=true;}await page.keyboard.press('Tab');break;
     case'external-column-filter':await page.evaluate(()=>window.setColumnFilters([{id:'title',value:'Vale'}]));break;
     case'external-global-filter':await page.evaluate(()=>window.setGlobalFilter('Vale'));break;
     case'search-column-change':await page.evaluate(()=>window.updateToolbar({searchColumn:'status',searchPlaceholder:'Status search'}));break;
     case'search-column-hidden':await page.evaluate(()=>window.updateToolbar({searchColumn:''}));break;
     case'replace-table':await page.evaluate(()=>window.updateToolbar({secondTable:true,searchColumn:'category'}));break;
     case'second-table-search':await search.fill('Reed');break;
     case'restore-table':await page.evaluate(()=>window.updateToolbar({secondTable:false,searchColumn:'title'}));break;
     case'replace-slots':await page.evaluate(()=>window.updateToolbar({filters:{kind:'buttons',labels:['Replacement filter'],width:180},actions:{kind:'buttons',labels:['Replacement action'],width:150}}));break;
     default:throw Error('Unknown frozen action: '+action);
    }
    await capture(recorded.state,++step,action,await settle());
   }
   assert.deepEqual(errors,[]);const final=await read();cases.push({id,scheme,states:step,inputReceipts:final.inputEvents.length,trustedInputReceipts:final.inputEvents.filter(receipt=>receipt.trusted).length,originalInitialReplay,errors});
   console.log(JSON.stringify({id,scheme,states:step,captures:captures.length}));
  }finally{await page.close();}
 }}finally{await browser.close();}
 assert.equal(captures.filter(item=>item.step>0).length,376);assert.equal(captures.filter(item=>item.step===0).length,14);assert.equal(cases.length,62);
 assert.equal(hash(await fs.readFile(__filename)),scriptSha256,'Capture script changed during execution.');
 const report={format:'orchard-data-table-toolbar-state-pixels-v1',captureComplete:true,nativeComponentAccepted:false,repeatPixelStable:captures.every(item=>item.repeatPixelStable),sourceCommit:reference.sourceCommit,sourceSha256:reference.sourceSha256,compiledCssSha256:reference.compiledCssSha256,referenceSha256:hash(referenceBytes),bundleSha256:hash(bundle),clientSha256:reference.clientSha256,probeScriptSha256:scriptSha256,browser:reference.browser,playwright:'1.62.1',deviceScaleFactor:1,viewportHeight:700,reducedMotion:'reduce',caret:'initial',animations:'allow',masks:0,tolerance:0,cases,captures};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');await fs.writeFile('tests/Reference/DataTableToolbarStatePixels.json',JSON.stringify(report,null,2)+'\n');
 console.log(JSON.stringify({cases:cases.length,states:376,initials:14,distinctTrustedReceipts:cases.reduce((sum,item)=>sum+item.trustedInputReceipts,0),changedRepeats:captures.filter(item=>!item.repeatPixelStable).length,output}));
})().catch(error=>{console.error(error);process.exit(1)});
