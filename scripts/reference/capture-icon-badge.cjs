const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto'),{execFileSync}=require('node:child_process');
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const source=option('--source-root'),tooling=option('--tooling-root'),playwright=option('--playwright-module'),cssDirectory=option('--source-css'),output=option('--output');
const pin='a86059013cd59684ce452849278ea81e734f2066',hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});const file='src/components/icon-badge.tsx',bytes=await fs.readFile(path.join(source,file));assert.equal(hash(bytes),'45701e66553a6c9b57e79dc0ec339adf6c6583abf75432fced09261bc7fefd0a');
 for(const name of [file,'src/core/contracts.ts','src/styles/global.css','src/styles/tokens.generated.css','src/utils/cn.ts','package-lock.json'])assert.equal(Buffer.compare(await fs.readFile(path.join(source,name)),execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024})),0,`Frozen source differs: ${name}`);
 assert.equal(require(path.join(tooling,'node_modules/esbuild/package.json')).version,'0.25.12');assert.equal(require(path.join(playwright,'package.json')).version,'1.62.1');const runtime=path.join(source,'node_modules');for(const name of ['react','react-dom'])assert.equal(require(path.join(runtime,name,'package.json')).version,'19.2.4');
 const client=path.join(__dirname,'icon-badge-client.tsx'),clientBytes=await fs.readFile(client),bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/icon-badge':path.join(source,file),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const normal=JSON.parse(await fs.readFile('tests/Reference/Accordion.json','utf8')),cssBytes=await fs.readFile(path.join(cssDirectory,'source.css'));assert.equal(hash(cssBytes),normal.sourceCssSha256);let css=cssBytes.toString();const fonts=[];
 for(const [family,name] of [['Encode Sans','EncodeSans-Regular'],['Libre Franklin','LibreFranklin-Regular']]){const font=await fs.readFile(`src/Orchard/Assets/Fonts/${name}.ttf`);fonts.push({name,sha256:hash(font)});css+=`@font-face{font-family:"${family}";font-weight:400;src:url(data:font/ttf;base64,${font.toString('base64')})}`;}
 const specs=[...['md','lg'].flatMap(size=>['primary','subtle','outlined'].map(variant=>({id:`${size}-${variant}`,size,variant}))),
  {id:'defaults'},{id:'empty',child:'empty'},{id:'named',named:true},{id:'large-child',child:'large'},
  {id:'child-color',childColor:'rgb(180, 55, 90)'},{id:'rich',child:'rich'},
  {id:'text',child:'text',value:'A'},{id:'mixed-text',child:'text',value:'A B C D E F G H'},
  {id:'long-text',child:'text',value:'ConsumerBadgeOverflow'},{id:'whitespace',child:'text',value:'  A\t B\n C  '},
  {id:'narrow-block',parentWidth:40},{id:'narrow-flex',parentWidth:40,parentFlex:true},
  {id:'custom-style',size:'md',variant:'outlined',style:{width:88,height:40,borderWidth:2,borderRadius:6,padding:4,color:'rgb(180, 55, 90)',backgroundColor:'rgb(245, 237, 220)',borderColor:'rgb(33, 67, 99)'}},
  ...['0','-0','-1.5','1e-7','1e-6','1e20','1e21','NaN','Infinity','-Infinity'].map(value=>({id:`number-${value}`,child:'number',value})),
  ...['0','12345678901234567890123456789','-12345678901234567890123456789'].map(value=>({id:`bigint-${value}`,child:'bigint',value})),
  ...['false','true'].map(value=>({id:`boolean-${value}`,child:'boolean',value}))];
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'});const cases=[],interactions=[];
 try{assert.equal(browser.version(),'155.0.4283.62');for(const motion of ['reduce','no-preference'])for(const scheme of ['Light','Dark']){
  const page=await browser.newPage({viewport:{width:320,height:240},deviceScaleFactor:1,reducedMotion:motion}),errors=[];page.on('pageerror',error=>errors.push(error.message));const cdp=await page.context().newCDPSession(page);await cdp.send('DOM.enable');
  try{
   await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0}#root{position:absolute;left:20px;top:20px;width:280px}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);await page.evaluate(async()=>{await document.fonts.load('400 16px "Encode Sans"');await document.fonts.ready;});
   const settle=()=>page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
   for(const spec of specs){
    await page.mouse.move(0,0);await page.evaluate(spec=>window.showBadge(spec),spec);await settle();
    const measurement=await page.evaluate(()=>{const e=document.getElementById('badge'),s=getComputedStyle(e),box=n=>{const r=n.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};};const range=document.createRange();range.selectNodeContents(e);return{box:box(e),children:[...e.children].map(child=>({tag:child.tagName,box:box(child),color:getComputedStyle(child).color})),text:e.textContent,textBox:box(range),background:s.backgroundColor,border:s.borderColor,borderWidth:s.borderTopWidth,radius:s.borderTopLeftRadius,foreground:s.color,font:s.fontFamily,fontSize:s.fontSize,lineHeight:s.lineHeight,overflow:s.overflow,animations:e.getAnimations().length,tabIndex:e.tabIndex};});
    const {root}=await cdp.send('DOM.getDocument'),{nodeId}=await cdp.send('DOM.querySelector',{nodeId:root.nodeId,selector:'#badge'}),{nodes}=await cdp.send('Accessibility.getPartialAXTree',{nodeId,fetchRelatives:false});measurement.accessibility=nodes.map(node=>({role:node.role?.value,name:node.name?.value,ignored:node.ignored,ignoredReasons:node.ignoredReasons?.map(reason=>reason.name)}));
    const file=`icon-badge-${spec.id}-${scheme}-${motion}.png`,png=await page.screenshot({path:path.join(output,file)});cases.push({id:spec.id,scheme,motion,spec,measurement,file,pngSha256:hash(png)});
   }
   await page.evaluate(()=>window.showBadge({id:'live',child:'rich'}));await settle();await page.locator('#child-input').fill('Retained');await page.evaluate(()=>window.retainedBadgeChild=document.getElementById('child-input'));
   for(const [size,variant] of [['md','outlined'],['lg','subtle'],['lg','primary']]){await page.evaluate(props=>window.showBadge({id:'live',child:'rich',...props},false),{size,variant});await settle();interactions.push({scheme,motion,size,variant,...await page.evaluate(()=>({childRetained:window.retainedBadgeChild===document.getElementById('child-input'),value:document.getElementById('child-input').value}))});}
   await page.locator('#child-button').click();assert.deepEqual(await page.evaluate(()=>window.badgeEvents),['child','badge']);await page.locator('#child-button').focus();await page.keyboard.press('Tab');assert.equal(await page.evaluate(()=>document.activeElement.id),'child-input');assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const reference={format:'orchard-icon-badge-reference-v1',sourceCommit:pin,sourceFile:file,sourceSha256:hash(bytes),sourceCssSha256:hash(cssBytes),clientSha256:hash(clientBytes),scriptSha256:hash(await fs.readFile(__filename)),browser:browser.version(),fonts,cases,interactions};await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(reference,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,interactions:interactions.length,defaults:cases.find(row=>row.id==='defaults'&&row.scheme==='Light'&&row.motion==='reduce').measurement}));
})().catch(error=>{console.error(error);process.exitCode=1});
