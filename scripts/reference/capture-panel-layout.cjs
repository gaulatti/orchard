const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto'),{execFileSync}=require('node:child_process');
const arg=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const source=arg('--source-root'),tooling=arg('--tooling-root'),output=arg('--output'),playwright=arg('--playwright-module');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex'),pin='a86059013cd59684ce452849278ea81e734f2066';
(async()=>{
 await fs.mkdir(output,{recursive:true});const file='src/layout/panel-layout.tsx',dependencies=[];
 for(const name of [file,'src/utils/cn.ts','src/styles/global.css','src/styles/tokens.generated.css','package-lock.json']){
  const bytes=await fs.readFile(path.join(source,name));assert.deepEqual(bytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024}));dependencies.push({path:name,sha256:hash(bytes)});
 }
 assert.equal(dependencies[0].sha256,'fb7bb7fa4505a3572509d87e3004191e7be8167163abd158850a7f07885c024a');
 const client=path.join(__dirname,'panel-layout-client.tsx'),runtime=path.join(source,'node_modules');
 const bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/panel-layout':path.join(source,file),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const slash=value=>value.replaceAll('\\','/');
 let input=(await fs.readFile(path.join(source,'src/styles/global.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'')
  .replace('@import "tailwindcss" source(none);','@import "'+slash(path.join(tooling,'node_modules/tailwindcss/index.css'))+'" source(none);')
  .replace('@import "./tokens.generated.css";','@import "'+slash(path.join(source,'src/styles/tokens.generated.css'))+'";')
  .replace('@source "../**/*.{ts,tsx}";','@source "'+slash(path.join(source,'src/**/*.{ts,tsx}'))+'";');
 input+='\n@source "'+slash(client)+'";'; await fs.writeFile(path.join(output,'input.css'),input);
 execFileSync(process.execPath,[path.join(tooling,'node_modules/@tailwindcss/cli/dist/index.mjs'),'-i',path.join(output,'input.css'),'-o',path.join(output,'source.css'),'--minify']);
 const css=await fs.readFile(path.join(output,'source.css'));const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[];
 try{for(const scheme of ['Light','Dark']){
  const page=await browser.newPage({viewport:{width:900,height:680},deviceScaleFactor:1,reducedMotion:'reduce'}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{
   await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
   const specs=[];const block=(id,width,grow=false,height)=>({id,width,grow,height});
   for(const width of [180,500.375,800]) for(const configuration of [
    ['fixed',[block('a',120),block('b',200)]],['fixed-grow',[block('a',120),block('b',undefined,true)]],
    ['grow-fixed-grow',[block('a',undefined,true),block('b',120),block('c',undefined,true)]],
    ['three-grow',[block('a',undefined,true),block('b',undefined,true),block('c',undefined,true)]],
    ['columns',[{id:'column-a',items:[block('a',180,false,80),block('b',180,true)]},{id:'column-b',grow:true,items:[block('c',undefined,true),block('d',undefined,true)]}]],
    ['overflow',[block('a',240),block('b',320),block('c',400)]]])
     for(const rtl of [false,true]) specs.push({id:`${configuration[0]}-${width}-${rtl?'rtl':'ltr'}`,width,height:360,rtl,items:configuration[1]});
   specs.push({id:'empty',width:500,height:360,items:[]},
    {id:'padding',width:500,height:360,padding:true,items:[block('a',120),block('b',undefined,true)]},
    {id:'short-column',column:true,width:320,height:40,items:[block('a',undefined,false,80),block('b',undefined,true)]},
    {id:'column-three-grow',column:true,width:320,height:500.375,items:[block('a',undefined,true),block('b',undefined,true),block('c',undefined,true)]},
    {id:'column-fixed-overflow',column:true,width:320,height:100,items:[block('a',undefined,false,80),block('b',undefined,false,120)]},
    {id:'column-empty',column:true,width:320,height:360,items:[]},
    {id:'hidden',width:500,height:360,items:[{...block('a',100),hidden:true},block('b',undefined,true)]});
   for(const spec of specs){
    await page.evaluate(spec=>window.showPanelLayout(spec),spec);await page.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r))));
    const measurement=await page.evaluate(()=>{
     const box=node=>{const b=node.getBoundingClientRect();return{x:b.x,y:b.y,width:b.width,height:b.height};};
     const frame=document.querySelector('#host').firstElementChild;
     const elements=[...document.querySelectorAll('[data-item]')];
     return{frame:box(frame),items:elements.map(node=>({id:node.dataset.item,box:box(node),wrapper:box(node.parentElement)})),scrollWidth:frame.scrollWidth,clientWidth:frame.clientWidth,scrollHeight:frame.scrollHeight,clientHeight:frame.clientHeight};
    });
    const row={id:spec.id,scheme,spec,measurement};
    if(spec.width===500.375||['padding','short-column','column-three-grow','column-fixed-overflow','empty'].includes(spec.id)){
     row.file=`panel-layout-${spec.id}-${scheme}.png`;row.pngSha256=hash(await page.screenshot({path:path.join(output,row.file)}));}
    cases.push(row);
   }assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-panel-layout-reference-v1',repository:'gaulatti/bleecker',commit:pin,source:file,sourceSha256:dependencies[0].sha256,dependencies,scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(client)),cssSha256:hash(css),browser:browser.version(),cases};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,pngs:cases.filter(r=>r.file).length}));
})().catch(e=>{console.error(e);process.exitCode=1;});
