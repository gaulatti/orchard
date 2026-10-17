const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const source=path.resolve('artifacts/line-chart-browser/frozen-source/src/components/textarea.tsx');
 const sourceBytes=await fs.readFile(source),runtime=path.resolve('artifacts/line-chart-browser/node_modules');
 assert.equal(hash(sourceBytes),'b523ba0962a02492a5e5954957eede4e1ad683ee7ce7ac21c04097325eb82082');
 const lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes);
 const installed=JSON.parse(await fs.readFile('artifacts/line-chart-browser/package-lock.json'));
 for(const[key,value]of Object.entries(installed.packages))if(key){assert.equal(value.version,lock.packages[key]?.version);assert.equal(value.integrity,lock.packages[key]?.integrity);}
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const bundle=await require(esbuildPath).build({alias:{react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},
  stdin:{contents:`import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{Textarea}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));window.actions=[];window.show=p=>flushSync(()=>root.render(<Textarea key={p.id} aria-label="Editorial notes" {...p} onChange={e=>window.actions.push(e.target.value)}/>));`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile('artifacts/line-chart-browser/styles.css');let css=cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'');const fonts=[];
 for(const[weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);fonts.push({weight,file,sha256:hash(bytes)});css+=`@font-face{font-family:'Encode Sans';font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const specs=[
  {id:'typing',limit:5,initial:'',keys:['a','b','c','d','e','f']},
  {id:'newline',limit:5,initial:'',keys:['a','b','Enter','c','d','e']},
  {id:'paste',limit:5,initial:'',paste:'abcdef'},
  {id:'paste-crlf',limit:5,initial:'',paste:'ab\r\ncde'},
  {id:'surrogate-fit',limit:3,initial:'',paste:'A😀B'},
  {id:'surrogate-boundary',limit:2,initial:'',paste:'A😀B'},
  {id:'selection',limit:5,initial:'ABCDE',selection:[1,4],paste:'uvwxyz'},
  {id:'full',limit:3,initial:'abc',paste:'x'},
  {id:'programmatic-overflow',limit:3,initial:'ABCDE',paste:'x'},
  {id:'overflow-replacement',limit:3,initial:'ABCDE',selection:[0,5],paste:'uvwxyz'},
  {id:'zero-capacity-selection',limit:3,initial:'ABCDE',selection:[3,5],paste:'x'},
  {id:'live-limit',limit:5,liveLimit:3,initial:'ab',paste:'cde'},
  {id:'unlimited',limit:null,initial:'',paste:'Unlimited\r\nnotes 😀'},
  {id:'readonly',limit:3,initial:'Read-only',readOnly:true,paste:'x'}
 ];
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),'155.0.4283.45');const cases=[];
 try{for(const width of[320,640])for(const scheme of['Light','Dark']){
  const context=await browser.newContext({viewport:{width,height:600},deviceScaleFactor:1,reducedMotion:'reduce',permissions:['clipboard-read','clipboard-write']});
  const page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{
   await context.route('http://localhost/**',route=>route.fulfill({contentType:'text/html',body:`<html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0;background:${scheme==='Dark'?'#0D1821':'white'}}#root{position:absolute;left:20px;top:20px;width:${width-40}px}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text};window.events=[];for(const type of['keydown','keyup','input','paste'])document.addEventListener(type,e=>window.events.push({type,trusted:e.isTrusted,key:e.key??null,inputType:e.inputType??null}),true);</script></body></html>`}));
   await page.goto('http://localhost/textarea-limits');await page.evaluate(async()=>{await document.fonts.load('14px "Encode Sans"');await document.fonts.ready;});
   for(const spec of specs){
    await page.evaluate(p=>{window.events=[];window.actions=[];window.show(p);},{id:spec.id,defaultValue:spec.initial,...(spec.limit===null?{}:{maxLength:spec.limit}),readOnly:!!spec.readOnly});
    const editor=page.getByRole('textbox',{name:'Editorial notes'});await editor.click();
    await editor.evaluate((e,selection)=>e.setSelectionRange(...selection),spec.selection??[spec.initial.length,spec.initial.length]);
    if(spec.liveLimit!==undefined)await page.evaluate(p=>window.show(p),{id:spec.id,defaultValue:spec.initial,maxLength:spec.liveLimit});
    if(spec.keys)for(const key of spec.keys)await page.keyboard.press(key);
    else{await page.evaluate(text=>navigator.clipboard.writeText(text),spec.paste);await page.keyboard.press('Control+v');}
    await page.evaluate(async()=>{for(const a of document.getAnimations())a.finish();await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));});
    const actual=await editor.evaluate(e=>({text:e.value,length:e.value.length,selection:[e.selectionStart,e.selectionEnd],maxLength:e.maxLength,readOnly:e.readOnly,ariaInvalid:e.getAttribute('aria-invalid'),valid:e.validity.valid,tooLong:e.validity.tooLong}));
    const file=`textarea-limits-${spec.id}-${width}-${scheme}.png`;await page.screenshot({path:path.join(output,file)});
    const changes=await page.evaluate(()=>window.actions);
    let undo=null;
    if(spec.paste){await page.keyboard.press('Control+z');undo=await editor.inputValue();}
    const events=await page.evaluate(()=>window.events);assert(events.length>=2&&events.every(e=>e.trusted));
    if(spec.paste)assert(events.some(e=>e.type==='paste'),'Paste must come from the real clipboard and keyboard.');
    cases.push({id:spec.id,width,scheme,spec,actual,undo,changes,events,file,pngSha256:hash(await fs.readFile(path.join(output,file)))});
   }
   assert.deepEqual(errors,[]);
  }finally{await context.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-textarea-limits-reference-v1',sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256:hash(sourceBytes),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),frozenLockSha256:hash(lockBytes),cssSha256:hash(cssBytes),fontFiles:fonts,browser:browser.version(),playwright:'1.62.1',esbuild:'0.25.12',freshMountPerCase:true,realClipboard:true,cases};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(cases.filter(c=>c.width===320&&c.scheme==='Light').map(({id,actual})=>({id,...actual}))));
})().catch(error=>{console.error(error);process.exitCode=1;});
