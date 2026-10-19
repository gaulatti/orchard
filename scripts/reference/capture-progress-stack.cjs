const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto'),{execFileSync}=require('node:child_process');
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const source=option('--source-root'),tooling=option('--tooling-root'),playwright=option('--playwright-module'),cssDirectory=option('--source-css'),output=option('--output');
const pin='a86059013cd59684ce452849278ea81e734f2066',hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});const file='src/components/progress-stack.tsx',bytes=await fs.readFile(path.join(source,file));assert.equal(hash(bytes),'101df66952162d978a45935604e468e7b465e8a57afdf873a48e0867a46b6254');
 for(const name of [file,'src/styles/global.css','src/styles/tokens.generated.css','src/utils/cn.ts','package-lock.json'])assert.equal(Buffer.compare(await fs.readFile(path.join(source,name)),execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024})),0,`Frozen source differs: ${name}`);
 assert.equal(require(path.join(tooling,'node_modules/esbuild/package.json')).version,'0.25.12');assert.equal(require(path.join(playwright,'package.json')).version,'1.62.1');const runtime=path.join(source,'node_modules');for(const name of ['react','react-dom'])assert.equal(require(path.join(runtime,name,'package.json')).version,'19.2.4');
 const client=path.join(__dirname,'progress-stack-client.tsx'),clientBytes=await fs.readFile(client),bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/progress-stack':path.join(source,file),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const normal=JSON.parse(await fs.readFile('tests/Reference/Accordion.json','utf8')),cssBytes=await fs.readFile(path.join(cssDirectory,'source.css'));assert.equal(hash(cssBytes),normal.sourceCssSha256);let css=cssBytes.toString();const fonts=[];
 for(const [family,name,weight]of [['Encode Sans','EncodeSans-Regular',400],['Encode Sans','EncodeSans-SemiBold',600]]){const font=await fs.readFile(`src/Orchard/Assets/Fonts/${name}.ttf`);fonts.push({name,weight,sha256:hash(font)});css+=`@font-face{font-family:"${family}";font-weight:${weight};src:url(data:font/ttf;base64,${font.toString('base64')})}`;}
 const basic=[{label:'Published',value:'6',color:'#2c5784'},{label:'Draft',value:'3',color:'#bd7067'},{label:'Review',value:'1',color:'#b38b4d'}];
 const specs=[{id:'default',items:basic},{id:'empty',items:[]},{id:'single',items:[basic[0]]},{id:'single-zero',items:[{...basic[0],value:'0'}]},
 {id:'fractions',items:basic.map((item,i)=>({...item,value:String((i+1)/10)}))},
 {id:'nonbreaking-space',items:[{...basic[0],label:'\u00a0Published\u00a0'},basic[1]]},
 {id:'hidden',items:basic,showLegend:false},{id:'max-20',items:basic,max:'20'},{id:'max-5',items:basic,max:'5'},
 ...['0','-1','NaN','Infinity','-Infinity'].map(max=>({id:'max-'+max,items:basic,max})),
 {id:'zeros',items:basic.map(item=>({...item,value:'0'}))},{id:'negative',items:[{...basic[0],value:'-4'},{...basic[1],value:'8'}]},
 {id:'negative-total',items:[{...basic[0],value:'-4'},{...basic[1],value:'2'}]},
 ...['NaN','Infinity','-Infinity','-0','1e-7','1e+21'].map(value=>({id:'value-'+value,items:[{...basic[0],value},basic[1]]})),
 {id:'uncolored',items:basic.map(({color,...item})=>item)},{id:'transparent',items:[{...basic[0],color:'rgba(44,87,132,0.25)'},basic[1]]},
 {id:'empty-label',items:basic.map(item=>({...item,label:''}))},{id:'whitespace',items:[{...basic[0],label:'  Some\t records\n here '},basic[1]]},
 {id:'long-label',items:[{...basic[0],label:'Consumer-owned publication workflow'},basic[1]]},
 {id:'rtl',items:basic,rtl:true},{id:'custom',items:basic,custom:true},
 ...[0,40,100,160,600].map(width=>({id:'width-'+width,items:basic,width}))];
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[],updates=[];
 try{assert.equal(browser.version(),'155.0.4283.62');for(const motion of ['reduce','no-preference'])for(const scheme of ['Light','Dark']){
  const page=await browser.newPage({viewport:{width:640,height:400},deviceScaleFactor:1,reducedMotion:motion}),errors=[];page.on('pageerror',error=>errors.push(error.message));
  try{
   await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0}#root{position:absolute;left:20px;top:20px;width:280px}.custom-stack{margin:0;}.custom-track{height:20px;border-radius:3px;background:rgb(245,237,220)}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);await page.evaluate(async()=>{for(const weight of [400,600])await document.fonts.load(`${weight} 12px "Encode Sans"`);await document.fonts.ready;});
   const settle=()=>page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
   const measure=()=>page.evaluate(()=>{const root=document.querySelector('#root>div'),box=n=>{const r=n.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};},node=n=>{const s=getComputedStyle(n);return{box:box(n),text:n.textContent,color:s.color,background:s.backgroundColor,radius:s.borderTopLeftRadius,weight:s.fontWeight,fontSize:s.fontSize,lineHeight:s.lineHeight,title:n.title};};return{root:node(root),bar:node(root.children[0]),segments:[...root.children[0].children].map(node),legend:root.children[1]?node(root.children[1]):null,items:root.children[1]?[...root.children[1].children].map(n=>({box:box(n),swatch:node(n.children[0]),label:node(n.children[1]),value:node(n.children[2])})):[],animations:root.getAnimations({subtree:true}).length,tabIndex:root.tabIndex};});
   for(const spec of specs){await page.mouse.move(0,0);await page.evaluate(spec=>window.showProgressStack(spec),spec);await settle();const measurement=await measure(),file=`progress-stack-${spec.id}-${scheme}-${motion}.png`,png=await page.screenshot({path:path.join(output,file)});cases.push({id:spec.id,scheme,motion,spec,measurement,file,pngSha256:hash(png)});}
   for(const[id,items,showLegend]of [['reorder',[...basic].reverse(),true],['remove',basic.slice(1),true],['hide',basic,false],['clear',[],true]]){await page.evaluate(spec=>window.showProgressStack(spec),{id,items,showLegend});await settle();updates.push({id,scheme,motion,measurement:await measure()});}assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const reference={format:'orchard-progress-stack-reference-v1',sourceCommit:pin,sourceFile:file,sourceSha256:hash(bytes),sourceCssSha256:hash(cssBytes),clientSha256:hash(clientBytes),scriptSha256:hash(await fs.readFile(__filename)),browser:browser.version(),fonts,cases,updates};await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(reference,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,updates:updates.length,first:cases[0].measurement}));
})().catch(error=>{console.error(error);process.exitCode=1});
