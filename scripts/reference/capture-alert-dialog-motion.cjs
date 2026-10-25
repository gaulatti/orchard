// Replay reversals of the pinned original component. Presence timers stay in
// an isolated browser realm; CSS presentation is paused and sampled directly.
const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};
const source=option('--source-root'),tooling=option('--tooling-root'),output=option('--output'),playwright=option('--playwright-module'),cssFile=option('--css-file');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const referenceFile=path.resolve('tests/Reference/AlertDialog.json'),referenceBytes=await fs.readFile(referenceFile),reference=JSON.parse(referenceBytes);
 for(const row of [...reference.dependencies,...reference.cssSourceInputs,...reference.localBundleInputs])assert.equal(hash(await fs.readFile(path.join(source,row.path))),row.sha256,row.path);
 for(const row of reference.modules){const packageFile=path.join(source,row.lockPath,'package.json');assert.equal(hash(await fs.readFile(packageFile)),row.packageSha256,row.name);for(const file of row.files)assert.equal(hash(await fs.readFile(path.join(source,'node_modules',file.path))),file.sha256,file.path);}
 for(const row of reference.toolchain)assert.equal(hash(await fs.readFile(path.join(tooling,'node_modules',row.name,'package.json'))),row.packageSha256,row.name);
 const css=await fs.readFile(cssFile);assert.equal(hash(css),reference.cssSha256,'Original compiled CSS differs.');
 const client=path.join(__dirname,'alert-dialog-client.tsx');assert.equal(hash(await fs.readFile(client)),reference.clientSha256);
 const runtime=path.join(source,'node_modules');
 const motionClient=path.join(__dirname,'alert-dialog-motion-client.tsx');
 const bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[motionClient],alias:Object.fromEntries([['alert-dialog','alert-dialog'],['button','button'],['modal','modal'],['drawer','drawer'],['popover','popover']].map(([alias,file])=>['frozen/'+alias,path.join(source,'src/components',file+'.tsx')]).concat([['react',path.join(runtime,'react')],['react-dom',path.join(runtime,'react-dom')]])),bundle:true,metafile:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const inputFiles=[];for(const file of Object.keys(bundle.metafile.inputs)){const absolute=path.resolve(file);inputFiles.push({path:absolute,sha256:hash(await fs.readFile(absolute))});}
 const expected=new Map(reference.modules.flatMap(row=>row.files.map(file=>[path.resolve(source,'node_modules',file.path),file.sha256])).concat(reference.localBundleInputs.map(row=>[path.resolve(source,row.path),row.sha256]),[[client,reference.clientSha256],[motionClient,hash(await fs.readFile(motionClient))]]));
 assert.equal(inputFiles.length,expected.size,'Bundle input count changed.');for(const row of inputFiles)assert.equal(row.sha256,expected.get(row.path),'Unexpected bundle input '+row.path);
 let faces='';for(const row of reference.fonts){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+row.name);assert.equal(hash(bytes),row.sha256,row.name);faces+=`@font-face{font-family:"${row.family}";font-weight:${row.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 assert.equal(JSON.parse(await fs.readFile(path.join(playwright,'package.json'))).version,reference.playwrightVersion);
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge',ignoreDefaultArgs:['--hide-scrollbars']}),histories=[];
 async function poll(page,predicate,message){for(let i=0;i<100;i++){if(await page.evaluate(predicate))return;await new Promise(resolve=>setTimeout(resolve,5));}throw new Error(message);}
 try{for(const scheme of ['Light','Dark'])for(const enterInterrupt of [25,75,125])for(const exitInterrupt of [7,25,75]){
  const page=await browser.newPage({viewport:{width:800,height:600},deviceScaleFactor:1,reducedMotion:'no-preference'}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{
   await page.clock.install({time:0});await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}html,body{margin:0;width:100%;height:100%;overflow:hidden}body{background:var(--color-background)}${faces}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(()=>document.fonts.ready);await page.clock.pauseAt(await page.evaluate(()=>Date.now()));
   await page.evaluate(()=>window.showAlertDialog({isOpen:false}));await poll(page,()=>!!document.querySelector('#opener'),'Original fixture did not mount.');
   await page.evaluate(()=>window.updateAlertDialog({isOpen:true}));await poll(page,()=>!!document.querySelector('[role="alertdialog"]'),'Original dialog did not mount.');
   await page.evaluate(()=>void getComputedStyle(document.querySelector('[role="alertdialog"]')).transform);
   await page.clock.runFor(20);await poll(page,()=>document.querySelector('[role="alertdialog"]')?.dataset.state==='open','Original entry delay did not finish.');
   const stages=[];let logicalTime=0;
   async function stage(entering,times){
    const animations=await page.evaluate(async()=>{
     const surface=document.querySelector('[role="alertdialog"]'),backdrop=surface.closest('[data-bleecker-modal-overlay]').firstElementChild;
     // Flushing styles creates transitions from the preceding paused value.
     void getComputedStyle(surface).opacity;void getComputedStyle(backdrop).opacity;
     window.reversalAnimations=document.getAnimations().filter(a=>(a.effect?.target===surface||a.effect?.target===backdrop)&&a.transitionProperty);
     for(const a of window.reversalAnimations)a.pause();await Promise.all(window.reversalAnimations.map(a=>a.ready));
     return window.reversalAnimations.map(a=>{a.currentTime=0;return{target:a.effect.target===surface?'surface':'backdrop',property:a.transitionProperty,...a.effect.getTiming()};}).sort((a,b)=>(a.target+a.property).localeCompare(b.target+b.property));
    });
    assert.equal(animations.length,3,JSON.stringify({scheme,enterInterrupt,exitInterrupt,entering,animations,previous:stages.at(-1)}));
    const samples=[];for(const milliseconds of times)samples.push(await page.evaluate(milliseconds=>{
     for(const a of window.reversalAnimations)a.currentTime=milliseconds;
     const surface=document.querySelector('[role="alertdialog"]'),backdrop=surface.closest('[data-bleecker-modal-overlay]').firstElementChild,s=getComputedStyle(surface),b=getComputedStyle(backdrop),m=new DOMMatrix(s.transform),box=surface.getBoundingClientRect();
     return{milliseconds,opacity:+s.opacity,backdropOpacity:+b.opacity,scale:m.a,translateY:m.f,surface:{x:box.x,y:box.y,width:box.width,height:box.height},state:surface.dataset.state,inert:surface.inert,rootInert:document.querySelector('#root').inert};
    },milliseconds));
    stages.push({entering,logicalTime,animations,samples});logicalTime+=times.at(-1);
   }
   async function setVisible(visible){
    await page.evaluate(open=>{
     window.pendingReversalFreeze=new Promise(resolve=>{
      const observer=new MutationObserver(()=>{
       const surface=document.querySelector('[role="alertdialog"]');if(surface?.dataset.state!==(open?'open':'closing'))return;
       const backdrop=surface.closest('[data-bleecker-modal-overlay]').firstElementChild;
       void getComputedStyle(surface).opacity;void getComputedStyle(backdrop).opacity;
       for(const a of document.getAnimations().filter(a=>(a.effect?.target===surface||a.effect?.target===backdrop)&&a.transitionProperty)){a.pause();a.currentTime=0;}
       observer.disconnect();resolve();
      });observer.observe(document.body,{subtree:true,attributes:true,attributeFilter:['data-state','class']});
     });
     window.commitAlertDialog(()=>window.updateAlertDialog({isOpen:open}));
    },visible);
    if(visible){await poll(page,()=>document.querySelector('#root').inert,'Controlled reentry did not acquire ownership.');await new Promise(resolve=>setTimeout(resolve,20));await page.clock.runFor(20);await poll(page,()=>document.querySelector('[role="alertdialog"]')?.dataset.state==='open','Reentry delay did not finish.');}
    else await poll(page,()=>!document.querySelector('#root').inert&&document.querySelector('[role="alertdialog"]')?.dataset.state==='closing','Original exit did not release ownership.');
    await page.evaluate(()=>window.pendingReversalFreeze);
   }
   await stage(true,[0,enterInterrupt]);await setVisible(false);await stage(false,[0,exitInterrupt]);await setVisible(true);await stage(true,[0,10,40]);await setVisible(false);await stage(false,[0,7]);await setVisible(true);await stage(true,[0,10,40,190]);
   histories.push({scheme,enterInterrupt,exitInterrupt,stages});assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-alert-dialog-reversal-reference-v1',commit:reference.commit,sourceReference:'tests/Reference/AlertDialog.json',sourceReferenceSha256:hash(referenceBytes),sourceSha256:reference.sourceSha256,cssSha256:hash(css),clientSha256:reference.clientSha256,scriptSha256:hash(await fs.readFile(__filename)),browser:browser.version(),playwrightVersion:reference.playwrightVersion,headless:true,driver:'paused-original-css-transitions-with-isolated-realm-presence-timers',scope:'CSS presentation reversals; real-time timer/CSS integration and native input remain unverified',inputFiles,histories};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({histories:histories.length,stages:histories.reduce((n,h)=>n+h.stages.length,0),samples:histories.reduce((n,h)=>n+h.stages.reduce((s,v)=>s+v.samples.length,0),0)}));
})().catch(error=>{console.error(error);process.exitCode=1;});
