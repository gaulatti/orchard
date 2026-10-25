const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const arg=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return path.resolve(process.argv[i+1]);};
const input=arg('--source-artifact'),playwright=arg('--playwright-module'),output=arg('--output');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
(async()=>{
 const referenceBytes=await fs.readFile('tests/Reference/Menubar.json'),reference=JSON.parse(referenceBytes);
 const cssBytes=await fs.readFile(path.join(input,'source.css')),bundle=await fs.readFile(path.join(input,'bundle.js'));
 assert.equal(hash(cssBytes),reference.cssSha256);assert.equal(hash(bundle),reference.bundleSha256);
 let css=cssBytes.toString();for(const font of reference.fonts){const b=await fs.readFile('src/Orchard/Assets/Fonts/'+font.name);assert.equal(hash(b),font.sha256);css+=`@font-face{font-family:"Encode Sans";font-weight:${font.weight};src:url(data:font/ttf;base64,${b.toString('base64')})}`;}
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),transitions=[];
 try{for(const scheme of ['Light','Dark'])for(const reduced of [false,true]){
  const page=await browser.newPage({viewport:{width:900,height:760},deviceScaleFactor:1,reducedMotion:reduced?'reduce':'no-preference'}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));await page.route('**/*',route=>route.abort());
  try{
   await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0;overflow:hidden}</style></head><body><div id="root"></div><script>${bundle}</script></body></html>`);
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(w=>document.fonts.load(w+' 14px "Encode Sans"')));await document.fonts.ready;});
   const target=role=>role==='trigger'?page.locator('[role="menubar"] > div > button').filter({hasText:'File'}):page.locator('[role="menu"] button').filter({hasText:{action:'New fictional draft',checkbox:'Show fictional guides',radio:'Fictional Alpha mode',submenu:'More fictional choices'}[role]});
   const finish=()=>page.evaluate(()=>document.querySelector('[role="menubar"]').getAnimations({subtree:true}).forEach(a=>a.finish()));
   async function setup(role,hover=false,open=false){await page.mouse.move(880,730);await page.evaluate(()=>window.showMenubar({}));await page.waitForTimeout(35);if(role!=='trigger'||open){await target('trigger').evaluate(n=>n.click());await page.waitForTimeout(35);}if(hover)await target(role).hover();await finish();}
   async function capture(role,kind,trigger){
    const before=await target(role).evaluate(n=>{const s=getComputedStyle(n);return{background:s.backgroundColor,foreground:s.color};});await trigger();
    const timeline=await target(role).evaluate(async n=>{const animations=n.getAnimations();animations.forEach(a=>a.pause());await Promise.all(animations.map(a=>a.ready));
     const definitions=animations.map(a=>({property:a.transitionProperty,timing:a.effect.getTiming(),frames:a.effect.getKeyframes()})),samples=[];
     for(const milliseconds of [0,47.5,95,142.5,190,220]){animations.forEach(a=>a.currentTime=milliseconds);const s=getComputedStyle(n);samples.push({milliseconds,background:s.backgroundColor,foreground:s.color});}
     animations.forEach(a=>a.finish());return{definitions,samples};});transitions.push({scheme,reduced,role,kind,before,...timeline});
   }
   for(const role of ['trigger','action','checkbox','radio','submenu']){
    await setup(role);await capture(role,'hover',()=>target(role).hover());
    await setup(role,true);await capture(role,'leave',()=>page.mouse.move(880,730));
   }
   await setup('trigger');await capture('trigger','open',()=>target('trigger').evaluate(n=>n.click()));
   await setup('trigger',false,true);await capture('trigger','close',()=>target('trigger').evaluate(n=>n.click()));
   assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 assert.equal(transitions.length,48);
 for(const t of transitions){if(!t.reduced)assert(t.definitions.length>0,t.scheme+'/'+t.role+'/'+t.kind);for(const d of t.definitions){assert.equal(d.timing.duration,t.reduced?.01:190);assert.equal(d.timing.easing,'cubic-bezier(0.22, 1, 0.36, 1)');assert(['color','background-color'].includes(d.property));}}
 const result={format:'orchard-menubar-row-motion-reference-v1',commit:reference.commit,referenceSha256:hash(referenceBytes),cssSha256:reference.cssSha256,bundleSha256:reference.bundleSha256,scriptSha256:hash(await fs.readFile(__filename)),browser:browser.version(),originalCaptureBrowser:reference.browser,osSettingsChanged:false,nativeMotionVerified:false,transitions};
 await fs.writeFile(output,JSON.stringify(result,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify({transitions:48,samples:transitions.reduce((n,t)=>n+t.samples.length,0),browser:browser.version(),originalCaptureBrowser:reference.browser}));
})().catch(e=>{console.error(e);process.exitCode=1;});
