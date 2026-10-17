const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const option=n=>{const i=process.argv.indexOf(n);assert(i>=0);return path.resolve(process.argv[i+1]);};
const source=option('--source-root'),tooling=option('--tooling-root'),output=option('--output'),playwright=option('--playwright-module');
const pin='a86059013cd59684ce452849278ea81e734f2066',hash=b=>crypto.createHash('sha256').update(b).digest('hex'),slash=s=>s.replaceAll('\\','/');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const file='src/components/radio-group.tsx',bytes=await fs.readFile(path.join(source,file));
 assert.equal(hash(bytes),'1af7eb200200683a11a1cae62291f462a711f89bf0a7bfaf67d9f7d2f1fb33fb');
 assert.deepEqual(bytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+file]));
 const esbuild=require(path.join(tooling,'node_modules/esbuild')),runtime=path.join(source,'node_modules');
 assert.equal(require(path.join(tooling,'node_modules/esbuild/package.json')).version,'0.25.12');
 assert.equal(require(path.join(tooling,'node_modules/@tailwindcss/cli/package.json')).version,'4.2.2');
 for(const name of ['react','react-dom'])assert.equal(JSON.parse(await fs.readFile(path.join(runtime,name,'package.json'))).version,'19.2.4');
 const bundle=await esbuild.build({entryPoints:[path.join(__dirname,'radio-group-client.tsx')],alias:{'frozen/radio-group':path.join(source,file),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const globalBytes=await fs.readFile(path.join(source,'src/styles/global.css'));
 for(const name of ['src/styles/global.css','src/styles/tokens.generated.css','src/utils/cn.ts','package-lock.json'])
  assert.deepEqual(await fs.readFile(path.join(source,name)),execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024}),name);
 const input=globalBytes.toString().replace(/@import url\([^\r\n]*\);/g,'').replace('@import "tailwindcss" source(none);','@import "'+slash(path.join(tooling,'node_modules/tailwindcss/index.css'))+'" source(none);').replace('@import "./tokens.generated.css";','@import "'+slash(path.join(source,'src/styles/tokens.generated.css'))+'";').replace('@source "../**/*.{ts,tsx}";','@source "'+slash(path.join(source,'src/**/*.{ts,tsx}'))+'";');
 await fs.writeFile(path.join(output,'input.css'),input);
 execFileSync(process.execPath,[path.join(tooling,'node_modules/@tailwindcss/cli/dist/index.mjs'),'-i',path.join(output,'input.css'),'-o',path.join(output,'source.css'),'--minify']);
 let css=await fs.readFile(path.join(output,'source.css'),'utf8');
 for(const [weight,name]of [[400,'Regular'],[500,'Medium']])css+='@font-face{font-family:"Encode Sans";font-weight:'+weight+';src:url(data:font/ttf;base64,'+(await fs.readFile('src/Orchard/Assets/Fonts/EncodeSans-'+name+'.ttf')).toString('base64')+')}';
 const common=[{value:'daily',label:'Daily',description:'A morning digest.'},{value:'weekly',label:'Weekly',description:'A weekly overview.'},{value:'paused',label:'Paused',disabled:true}];
 const specs=[{id:'selected',options:common,value:'daily'},{id:'unselected',options:common},{id:'disabled',options:common,value:'daily',disabled:true},{id:'disabled-selected',options:common,value:'paused'},{id:'horizontal',options:common,value:'weekly',orientation:'horizontal'},{id:'unknown-value',options:common,value:'unknown'},{id:'empty',options:[]},{id:'long',options:[{value:'long',label:'Keep notifications enabled while reviewing the editorial queue',description:'Send alerts when a device starts playback.'}],value:'long'},{id:'unicode',options:[{value:'ja',label:'通知を有効にする',description:'تمكين الإشعارات'}],value:'ja'},{id:'empty-string',options:[{value:'',label:'Empty value'},{value:'b',label:'Second'}],value:''}];
 specs.push({id:'rtl',options:common,value:'weekly',orientation:'horizontal',dir:'rtl'},{id:'hover',options:common,value:'daily',hover:'weekly'},{id:'disabled-hover',options:common,value:'daily',disabled:true,hover:'weekly'},{id:'selected-hover',options:common,value:'daily',hover:'daily'});
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[],interactions=[];
 try{for(const width of [320,640])for(const scheme of ['Light','Dark']){
  const page=await browser.newPage({viewport:{width,height:640},deviceScaleFactor:1,reducedMotion:'reduce',locale:'en-US'});
  const cdp=await page.context().newCDPSession(page);await cdp.send('DOM.enable');await cdp.send('CSS.enable');
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.setContent('<!doctype html><html class="'+(scheme==='Dark'?'dark':'')+'"><head><style>'+css+'body{margin:0}#root{position:absolute;left:20px;top:20px;width:'+(width-40)+'px}</style></head><body><div id="root"></div><script>'+bundle.outputFiles[0].text+'</script></body></html>');
  await page.evaluate(async()=>{for(const w of [400,500])await document.fonts.load(w+' 14px "Encode Sans"');await document.fonts.ready;});
  const settle=()=>page.evaluate(async()=>{for(const a of document.getAnimations())a.finish();await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));});
  for(const spec of specs){await page.mouse.move(0,0);await page.evaluate(s=>{document.getElementById('root').dir=s.dir??'ltr';window.show(s);},spec);await settle();if(spec.hover){await page.locator('label').filter({has:page.locator('input[value='+spec.hover+']')}).hover();await settle();}const file='radio-'+spec.id+'-'+width+'-'+scheme+'.png',png=await page.screenshot({path:path.join(output,file)});let platformFonts=[];if(spec.id==='unicode'){const {root}=await cdp.send('DOM.getDocument');const {nodeIds}=await cdp.send('DOM.querySelectorAll',{nodeId:root.nodeId,selector:'label > span:last-child > span'});for(const nodeId of nodeIds)platformFonts.push((await cdp.send('CSS.getPlatformFontsForNode',{nodeId})).fonts);}const textRegions=spec.id==="rtl"?await page.evaluate(()=>Array.from(document.querySelectorAll("label > span:last-child > span")).map(el=>{const r=document.createRange();r.selectNodeContents(el);const c=document.createElement("canvas").getContext("2d"),s=getComputedStyle(el);c.font=s.fontWeight+" "+s.fontSize+" "+s.fontFamily;c.direction=s.direction;c.fontKerning=s.fontKerning;return{text:el.textContent,regions:Array.from(r.getClientRects()).map(b=>({x:b.x,width:b.width})),canvasWidth:c.measureText(el.textContent).width,kerning:s.fontKerning};})):[];cases.push({id:spec.id,width,scheme,spec,measurement:await page.evaluate(()=>window.measure()),file,pngSha256:hash(png),platformFonts,textRegions});}
  for(const accept of [true,false]){await page.evaluate(spec=>window.show(spec),{options:common,value:'daily',accept});await settle();await page.locator('label').nth(1).click();await settle();interactions.push({width,scheme,accept,requests:await page.evaluate(()=>window.requests),measurement:await page.evaluate(()=>window.measure())});}
  assert.deepEqual(errors,[]);await page.close();
 }}finally{await browser.close();}
 const reference={format:'orchard-radio-group-reference-v1',sourceCommit:pin,sourceFile:file,sourceSha256:hash(bytes),scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(path.join(__dirname,'radio-group-client.tsx'))),browser:browser.version(),globalCssSha256:hash(globalBytes),cases,interactions};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(reference,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,interactions:interactions.length}));
})().catch(error=>{console.error(error.message);process.exitCode=1;});
