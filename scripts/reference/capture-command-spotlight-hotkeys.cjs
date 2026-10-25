const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const arg=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};
const base=arg('--reference-folder'),output=arg('--output'),playwright=arg('--playwright-module');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
async function main(){
 assert(!await fs.stat(output).catch(()=>null),'Use fresh capture output');await fs.mkdir(output,{recursive:true});
 const refBytes=await fs.readFile('tests/Reference/CommandSpotlight.json');assert.equal(hash(refBytes),'12b264b46566b389eff73d6b21e7f32feb465cdc52f2c86ab8379555a49a075e');const reference=JSON.parse(refBytes);
 const bundle=await fs.readFile(path.join(base,'bundle.js')),css=await fs.readFile(path.join(base,'source.css'));assert.equal(hash(bundle),reference.bundleSha256);assert.equal(hash(css),reference.cssSha256);
 const fonts=[];let faces='';for(const f of reference.fonts){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+f.name);assert.equal(hash(bytes),f.sha256);fonts.push(f);faces+=`@font-face{font-family:"${f.family}";font-weight:${f.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),reference.browser,'Source runtime must match the canonical browser');
 const cases=[],images=[],unexpected=[],errors=[];let casing;
 try{
  for(const theme of ['Light','Dark']){
   const page=await browser.newPage({viewport:{width:1000,height:240},deviceScaleFactor:1,colorScheme:theme==='Dark'?'dark':'light',locale:'en-US',timezoneId:'America/New_York',reducedMotion:'reduce'});let bootstrap=true;
   page.on('pageerror',e=>errors.push(e.message));page.on('popup',()=>unexpected.push('popup'));
   await page.route('**/*',route=>{if(bootstrap&&route.request().isNavigationRequest()){bootstrap=false;return route.fulfill({contentType:'text/html',body:`<!doctype html><html class="${theme==='Dark'?'dark':''}"><head><style>${css}body{margin:0}${faces}</style></head><body><div id="root"></div><script>${bundle}</script></body></html>`});}unexpected.push(route.request().url());return route.abort();});
   await page.goto('https://fixtures.orchard.test/command-spotlight-hotkeys');await page.evaluate(async()=>{await document.fonts.load('500 13px "Encode Sans"');await document.fonts.ready;});
   if(!casing)casing=await page.evaluate(require('./collect-command-spotlight-casing.cjs'));
   const keys=['k','K','1','!',';',':','/','?','[','{',']','}','\\','|','`','~',"'",'"',',','<','.','>','=','+','-','_','é','É','ß','ẞ','İ','ı','ſ','ﬃ','Σ','ς','σ','𐐀','𐐨','ArrowUp','ArrowDown','ArrowLeft','ArrowRight','F1','F24','Escape','Backspace','Delete','PageUp','PageDown','Home','End','Tab','CapsLock','Shift','Control','Alt','Meta','Enter',' ','Dead','Unidentified','','ΟΣ','control+k'];
   const specs=keys.map((hotkey,i)=>({id:'literal-'+i,hotkey,key:hotkey,ctrlKey:true})).concat([
    {id:'shift-mismatch',hotkey:'/',key:'?',ctrlKey:true,shiftKey:true},
    {id:'shift-match',hotkey:'?',key:'?',ctrlKey:true,shiftKey:true},
    {id:'meta-match',hotkey:'é',key:'É',metaKey:true},
    {id:'no-modifier',hotkey:'k',key:'k'},
    {id:'wrong-key',hotkey:'k',key:'j',ctrlKey:true},
    {id:'no-casefold-expansion',hotkey:'ß',key:'SS',ctrlKey:true},
    {id:'dotted-i-expansion',hotkey:'İ',key:'i\u0307',ctrlKey:true},
    {id:'sigma-context-match',hotkey:'ΟΣ',key:'ος',ctrlKey:true},
    {id:'sigma-context-mismatch',hotkey:'ΟΣ',key:'οσ',ctrlKey:true},
    ...['input','textarea','select','editable','button'].map(editor=>({id:'closed-'+editor,hotkey:'k',key:'k',ctrlKey:true,editor})),
    ...['input','textarea','select','editable','button'].map(editor=>({id:'open-'+editor,hotkey:'k',key:'k',ctrlKey:true,editor,open:true}))
   ]);
   for(const spec of specs){
    await page.evaluate(({spec,theme})=>{document.documentElement.className=theme==='Dark'?'dark':'';window.showSpotlight({hotkey:spec.hotkey,open:!!spec.open,acceptOpen:true,editor:spec.editor});},{spec,theme});await page.waitForTimeout(spec.open?45:25);
    const before=await page.evaluate(()=>({hint:[...document.querySelectorAll('[aria-label="Open command spotlight"] kbd')].map(n=>n.textContent),open:window.spotlightSpec().open}));
    if(['ß','ﬃ','İ','𐐀'].includes(spec.hotkey)&&spec.id.startsWith('literal-')){const file=`command-spotlight-hotkeys-${theme}-${spec.id}.png`;await page.screenshot({path:path.join(output,file),animations:'disabled'});images.push({theme,id:spec.id,file,sha256:hash(await fs.readFile(path.join(output,file)))});}
    const actual=await page.evaluate(spec=>{const target=spec.open?document.querySelector('[role="dialog"] input'):document.getElementById('original-background-editor')??document.body;const event=new KeyboardEvent('keydown',{key:spec.key,ctrlKey:!!spec.ctrlKey,metaKey:!!spec.metaKey,shiftKey:!!spec.shiftKey,altKey:!!spec.altKey,bubbles:true,cancelable:true});target.dispatchEvent(event);return{open:window.spotlightSpec().open,defaultPrevented:event.defaultPrevented,requests:window.spotlightEvents.filter(e=>e.kind==='open').map(e=>e.value),normalizedHotkey:spec.hotkey.toLowerCase(),normalizedKey:spec.key.toLowerCase()};},spec);
    const r={theme,id:spec.id,spec,before,...actual};cases.push(r);
   }
   await page.close();console.log(JSON.stringify({theme,states:specs.length}));
  }
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);const result={format:'orchard-command-spotlight-hotkeys-source-v1',commit:reference.commit,sourceReferenceSha256:hash(refBytes),bundleSha256:hash(bundle),cssSha256:hash(css),browser:browser.version(),scriptSha256:hash(await fs.readFile(__filename)),casingScriptSha256:hash(await fs.readFile(path.join(__dirname,"collect-command-spotlight-casing.cjs"))),fonts,cases,images,casing,errors,unexpected,syntheticDomEvents:true,physicalKeyboardVerified:false,externalNetworkRequests:0,nativeComponentAccepted:false};
  await fs.writeFile(path.join(output,'CommandSpotlightHotkeys.json'),JSON.stringify(result,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify({states:cases.length,images:images.length,scalars:casing.scalars,upperMappings:casing.upper.length,lowerMappings:casing.lower.length}));
 }finally{await browser.close();}
}
main().catch(e=>{console.error(e);process.exitCode=1;});
