const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const source=path.resolve('artifacts/line-chart-browser/frozen-source/src/components/textarea.tsx'),sourceBytes=await fs.readFile(source),runtime=path.resolve('artifacts/line-chart-browser/node_modules');
 assert.equal(hash(sourceBytes),'b523ba0962a02492a5e5954957eede4e1ad683ee7ce7ac21c04097325eb82082');
 const lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes),installed=JSON.parse(await fs.readFile('artifacts/line-chart-browser/package-lock.json'));
 for(const[key,value]of Object.entries(installed.packages))if(key){assert.equal(value.version,lock.packages[key]?.version);assert.equal(value.integrity,lock.packages[key]?.integrity);}
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const bundle=await require(esbuildPath).build({alias:{react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},stdin:{contents:`import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{Textarea}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));window.changes=[];window.show=p=>flushSync(()=>root.render(<Textarea key="editor" aria-label="Editorial notes" defaultValue="First\nSecond" {...p} onChange={e=>window.changes.push(e.target.value)}/>));`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile('artifacts/line-chart-browser/styles.css');let css=cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'');const fonts=[];
 for(const[weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);fonts.push({weight,file,sha256:hash(bytes)});css+=`@font-face{font-family:'Encode Sans';font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),'155.0.4283.45');const cases=[];
 try{for(const width of[320,640])for(const scheme of['Light','Dark']){
  const context=await browser.newContext({viewport:{width,height:600},deviceScaleFactor:1,reducedMotion:'reduce',permissions:['clipboard-read','clipboard-write']});const page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{
   await context.route('http://localhost/**',route=>route.fulfill({contentType:'text/html',body:`<html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0;background:${scheme==='Dark'?'#0D1821':'white'}}#root{position:absolute;left:20px;top:20px;width:${width-40}px}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text};window.events=[];for(const type of['keydown','keyup','input','copy','cut','paste'])document.addEventListener(type,e=>window.events.push({type,trusted:e.isTrusted,key:e.key??null}),true);window.show({});</script></body></html>`}));
   await page.goto('http://localhost/textarea-commands');await page.evaluate(async()=>{await document.fonts.load('14px "Encode Sans"');await document.fonts.ready;await navigator.clipboard.writeText('Command clipboard');});const editor=page.getByRole('textbox',{name:'Editorial notes'});await editor.click();
   for(const[id,key]of[['select-all','Control+a'],['copy','Control+c'],['cut','Control+x'],['undo','Control+z'],['redo','Control+y'],['paste','Control+v'],['readonly-copy','Control+c'],['readonly-cut','Control+x'],['readonly-paste','Control+v']]){
    if(id==='readonly-copy'){await page.evaluate(()=>window.show({readOnly:true}));await page.keyboard.press('Control+a');}
    if(id==='readonly-paste')await page.evaluate(()=>navigator.clipboard.writeText('Rejected'));
    await page.evaluate(()=>window.events=[]);await page.keyboard.press(key);
    await page.evaluate(async()=>{for(const a of document.getAnimations())a.finish();await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));});
    const actual=await editor.evaluate(e=>({text:e.value,selection:[e.selectionStart,e.selectionEnd],readOnly:e.readOnly,focused:document.activeElement===e}));
    const clipboard=await page.evaluate(async()=>({text:await navigator.clipboard.readText(),types:(await navigator.clipboard.read()).flatMap(item=>item.types)}));
    clipboard.normalizedText=clipboard.text.replace(/\r\n?/g,'\n');
    assert.equal(actual.text,id==='cut'||id==='redo'?'':'First\nSecond');if(id==='copy'||id==='readonly-copy'){assert.equal(clipboard.normalizedText,'First\nSecond');assert.deepEqual(clipboard.types,['text/plain']);}
    const events=await page.evaluate(()=>window.events);assert(events.length>=4&&events.every(e=>e.trusted));
    const file=`textarea-commands-${id}-${width}-${scheme}.png`;await page.screenshot({path:path.join(output,file)});cases.push({id,width,scheme,key,actual,clipboard,events,file,pngSha256:hash(await fs.readFile(path.join(output,file)))});
   }
   assert.deepEqual(errors,[]);
  }finally{await context.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-textarea-commands-reference-v1',sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256:hash(sourceBytes),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),frozenLockSha256:hash(lockBytes),cssSha256:hash(cssBytes),fontFiles:fonts,browser:browser.version(),realClipboard:true,cases};await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:cases.length}));
})().catch(error=>{console.error(error);process.exitCode=1;});
