const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto'),{execFileSync}=require('node:child_process');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const source=option('--source-root'),tooling=option('--tooling-root'),output=option('--output'),playwright=option('--playwright-module'),pin='a86059013cd59684ce452849278ea81e734f2066',hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const dependencies=[];
 for(const file of ['src/components/tooltip.tsx','src/components/modal.tsx','src/components/dialog-close-button.tsx','src/utils/cn.ts','src/utils/hooks.ts','src/styles/global.css','src/styles/tokens.generated.css','package-lock.json']) {
  const bytes=await fs.readFile(path.join(source,file));assert.deepEqual(bytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+file],{maxBuffer:8*1024*1024}));dependencies.push({path:file,sha256:hash(bytes)});
 }
 const runtime=path.join(source,'node_modules'),lock=JSON.parse(await fs.readFile(path.join(source,'package-lock.json'))),modules=[];
 for(const name of ['react','react-dom','@radix-ui/react-tooltip','lucide-react']){const file=path.join(runtime,name,'package.json'),pkg=JSON.parse(await fs.readFile(file));assert.equal(pkg.version,lock.packages['node_modules/'+name].version);modules.push({name,version:pkg.version,packageSha256:hash(await fs.readFile(file))});}
 const client=path.join(__dirname,'tooltip-client.tsx'),bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],bundle:true,write:false,platform:'browser',jsx:'automatic',metafile:true,nodePaths:[runtime],alias:{'frozen/tooltip':path.join(source,'src/components/tooltip.tsx'),'frozen/modal':path.join(source,'src/components/modal.tsx'),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},define:{'process.env.NODE_ENV':'"production"'}});
 const slash=s=>s.replaceAll('\\','/');
 const input=(await fs.readFile(path.join(source,'src/styles/global.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'').replace('@import "tailwindcss" source(none);','@import "'+slash(path.join(tooling,'node_modules/tailwindcss/index.css'))+'" source(none);').replace('@import "./tokens.generated.css";','@import "'+slash(path.join(source,'src/styles/tokens.generated.css'))+'";').replace('@source "../**/*.{ts,tsx}";','@source "'+slash(path.join(source,'src/**/*.{ts,tsx}'))+'";');
 await fs.writeFile(path.join(output,'input.css'),input);execFileSync(process.execPath,[path.join(tooling,'node_modules/@tailwindcss/cli/dist/index.mjs'),'-i',path.join(output,'input.css'),'-o',path.join(output,'source.css'),'--minify']);const css=await fs.readFile(path.join(output,'source.css')),fonts=[];let faces='';
 for(const [weight,name] of [[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+name);fonts.push({weight,name,sha256:hash(bytes)});faces+=`@font-face{font-family:"Encode Sans";font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const bundledFiles=[];for(const file of Object.keys(bundle.metafile.inputs)){const full=path.resolve(file);bundledFiles.push({path:slash(full),sha256:hash(await fs.readFile(full))});}await fs.writeFile(path.join(output,'bundle.js'),bundle.outputFiles[0].contents);
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[],histories=[];
 try {
  for(const scheme of ['Light','Dark']) {
   const page=await browser.newPage({viewport:{width:900,height:760},locale:'en-US',deviceScaleFactor:1,reducedMotion:'reduce'}),errors=[];page.on('pageerror',e=>errors.push(e.message));
   try {
    await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0;overflow:hidden}${faces}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
    await page.evaluate(async()=>{for(const weight of [400,500,600])await document.fonts.load(weight+' 12px "Encode Sans"');await document.fonts.ready;});
    const settle=()=>page.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r))));
    const show=async spec=>{await page.evaluate(s=>window.showTooltip(s),spec);await settle();if(spec.scope==='modal')await page.waitForTimeout(35);await page.locator('#tip-trigger').focus();await page.locator('[data-radix-popper-content-wrapper]>[data-side]').waitFor({state:'visible'});await settle();};
    const measure=()=>page.evaluate(()=>{
     const box=n=>{const b=n.getBoundingClientRect();return{x:b.x,y:b.y,width:b.width,height:b.height};};
     const tooltip=[...document.querySelectorAll('[data-radix-popper-content-wrapper]>[data-side]')].find(n=>n.dataset.state!=='closed'),wrapper=tooltip?.parentElement,arrow=tooltip?.querySelector('svg');
     const paint=n=>{const s=getComputedStyle(n);return{background:s.backgroundColor,color:s.color,fontFamily:s.fontFamily,fontSize:s.fontSize,lineHeight:s.lineHeight,padding:[s.paddingLeft,s.paddingTop,s.paddingRight,s.paddingBottom].map(parseFloat),radius:s.borderRadius};};
     return{trigger:document.querySelector('#tip-trigger')?box(document.querySelector('#tip-trigger')):null,tooltip:tooltip?{box:box(tooltip),side:tooltip.dataset.side,state:tooltip.dataset.state,paint:paint(tooltip),zIndex:Number(getComputedStyle(wrapper).zIndex),inert:tooltip.inert||wrapper.inert,arrow:arrow?{box:box(arrow),fill:getComputedStyle(arrow).fill}:null}:null,dialogs:document.querySelectorAll('[role="dialog"]').length,focused:document.activeElement?.id??null,events:window.tooltipEvents};
    });
    for(const side of ['top','bottom','left','right'])for(const edge of [false,true])for(const slot of ['fixed','text']){
     const spec={scope:'background',side,edge,slot,x:edge?(side==='left'?0:side==='right'?780:390):390,y:edge?(side==='top'?0:side==='bottom'?724:360):360};await show(spec);const measurement=await measure(),file=`tooltip-${cases.length}-${scheme}.png`,bytes=await page.screenshot({path:path.join(output,file)});cases.push({scheme,spec,measurement,file,pngSha256:hash(bytes)});
    }
    for(const side of ['top','bottom','left','right']){const spec={scope:'modal',side,slot:'fixed'};await show(spec);const measurement=await measure(),file=`tooltip-${cases.length}-${scheme}.png`,bytes=await page.screenshot({path:path.join(output,file)});cases.push({scheme,spec,measurement,file,pngSha256:hash(bytes)});}
    for(const scope of ['background','modal']){await show({scope,side:'top',slot:'fixed',x:390,y:360});const before=await measure();await page.keyboard.press('Escape');await page.waitForTimeout(180);await settle();histories.push({scheme,scope,before,after:await measure()});}
    assert.deepEqual(errors,[]);await page.close();
   }catch(error){await page.screenshot({path:path.join(output,'failed-'+scheme+'.png')}).catch(()=>{});throw error;}
  }
  const result={format:'orchard-tooltip-reference-v1',commit:pin,dependencies,modules,fonts,bundledFiles,browser:browser.version(),viewport:{width:900,height:760,scale:1},scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(client)),cssSha256:hash(css),bundleSha256:hash(bundle.outputFiles[0].contents),cases,histories};await fs.writeFile(path.join(output,'Tooltip.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,histories:histories.length,bundledFiles:bundledFiles.length}));
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
