const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const arg=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const source=arg('--source-root'),tooling=arg('--tooling-root'),output=arg('--output'),playwright=arg('--playwright-module'),hash=b=>crypto.createHash('sha256').update(b).digest('hex'),pin='a86059013cd59684ce452849278ea81e734f2066';
(async()=>{
 await fs.mkdir(output,{recursive:true});const file='src/components/theme-toggle.tsx',dependencies=[];
 for(const name of[file,'src/components/icon-button.tsx','src/theme/theme-provider.tsx','src/theme/theme-script.tsx','src/theme/types.ts','src/utils/cn.ts','src/styles/global.css','src/styles/tokens.generated.css','package-lock.json']){
  const bytes=await fs.readFile(path.join(source,name));assert.deepEqual(bytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024}));dependencies.push({path:name,sha256:hash(bytes)});
 }assert.equal(dependencies[0].sha256,'57e3f4181d2b50fc0aa7dcd56205ef62e94955d4067d5e20c58254c34bd638d9');
 const runtime=path.join(source,'node_modules'),client=path.join(__dirname,'theme-toggle-client.tsx'),bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/theme-toggle':path.join(source,file),'frozen/theme-provider':path.join(source,'src/theme/theme-provider.tsx'),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const slash=s=>s.replaceAll('\\','/');const input=(await fs.readFile(path.join(source,'src/styles/global.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'').replace('@import "tailwindcss" source(none);','@import "'+slash(path.join(tooling,'node_modules/tailwindcss/index.css'))+'" source(none);').replace('@import "./tokens.generated.css";','@import "'+slash(path.join(source,'src/styles/tokens.generated.css'))+'";').replace('@source "../**/*.{ts,tsx}";','@source "'+slash(path.join(source,'src/**/*.{ts,tsx}'))+'";');
 await fs.writeFile(path.join(output,'input.css'),input);execFileSync(process.execPath,[path.join(tooling,'node_modules/@tailwindcss/cli/dist/index.mjs'),'-i',path.join(output,'input.css'),'-o',path.join(output,'source.css'),'--minify']);const css=await fs.readFile(path.join(output,'source.css'));
 const icons=[];for(const name of['sun','moon','monitor']){const bytes=await fs.readFile(path.join(runtime,'lucide-react/dist/esm/icons',name+'.js'));icons.push({name,sha256:hash(bytes)});}assert.equal(JSON.parse(await fs.readFile(path.join(runtime,'lucide-react/package.json'))).version,'0.563.0');
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[],interactions=[];
 try{for(const scheme of['Light','Dark']){
  const context=await browser.newContext({viewport:{width:240,height:160},deviceScaleFactor:1,reducedMotion:'reduce',colorScheme:scheme.toLowerCase()});
  // A normal origin gives the original provider its real localStorage boundary.
  await context.route('http://orchard-reference.local/**',route=>route.fulfill({contentType:'text/html',body:`<!doctype html><html><head><style>${css}body{margin:0;background:var(--color-background)}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`}));
  const page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{await page.goto('http://orchard-reference.local/');const settle=()=>page.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r))));
   const setup=async spec=>{await page.mouse.move(210,130);await page.evaluate(s=>window.showThemeToggle(s),spec);await settle();await page.evaluate(d=>document.documentElement.classList.toggle('dark',d),scheme==='Dark');await settle();};
   const capture=async spec=>{
    await setup(spec);const button=page.locator('#source-toggle');
    if(spec.state==='hover'||spec.state==='pressed')await button.hover();
    if(spec.state==='pressed')await page.mouse.down();
    if(spec.state==='focus')await button.focus();
    await settle();
    const measurement=await page.evaluate(()=>{
     const b=document.querySelector('#source-toggle'),svg=b.querySelector('svg'),s=getComputedStyle(b),i=getComputedStyle(svg),box=n=>{const r=n.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};};
     const canvas=document.createElement('canvas');canvas.width=canvas.height=1;const c=canvas.getContext('2d');c.fillStyle=i.stroke;c.fillRect(0,0,1,1);
     return{mode:window.themeContext.theme,label:b.getAttribute('aria-label'),title:b.getAttribute('title'),disabled:b.disabled,box:box(b),icon:box(svg),stroke:i.stroke,strokeRgba:[...c.getImageData(0,0,1,1).data],strokeWidth:i.strokeWidth,paths:[...svg.children].map(n=>({tag:n.tagName,attributes:Object.fromEntries([...n.attributes].map(a=>[a.name,a.value]))})),paint:{background:s.backgroundColor,border:s.borderTopColor,borderWidth:s.borderTopWidth,radius:s.borderTopLeftRadius,opacity:s.opacity,transform:s.transform,shadow:s.boxShadow},focused:document.activeElement===b};
    });const file=`theme-toggle-${spec.id}-${scheme}.png`;const pngSha256=hash(await page.screenshot({path:path.join(output,file)}));cases.push({id:spec.id,scheme,spec,measurement,file,pngSha256});
    if(spec.state==='pressed')await page.mouse.up();
   };
   for(const mode of['light','dark','system'])for(const size of['sm','md','lg'])for(const variant of['default','subtle','ghost'])for(const disabled of[false,true])await capture({id:`${mode}-${size}-${variant}-${disabled}`,mode,size,variant,disabled,state:'normal'});
   for(const mode of['light','dark','system'])for(const state of['hover','pressed','focus'])await capture({id:`${mode}-${state}`,mode,state});
   for(const[id,extra]of[['defaults',{}],['custom',{mode:'light',name:'Appearance — 東京',title:'Consumer tooltip'}],['empty',{mode:'dark',name:'',title:''}],['rtl',{mode:'system',rtl:true}]])await capture({id,state:'normal',...extra});
   for(const mode of['light','dark','system'])for(const overrideClick of[false,true]){
    await setup({mode,overrideClick});const samples=[];
    for(const action of['click','Space','Enter']){
     if(action==='click')await page.locator('#source-toggle').click();else await page.locator('#source-toggle').press(action);
     await settle();samples.push(await page.evaluate(action=>({action,mode:window.themeContext.theme,resolved:window.themeContext.resolvedTheme,label:document.querySelector('#source-toggle').getAttribute('aria-label'),stored:localStorage.getItem('fixture-theme'),overrideClicks:window.overrideClicks}),action));
    }interactions.push({mode,scheme,overrideClick,samples});
   }assert.deepEqual(errors,[]);
  }finally{await page.close();await context.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-theme-toggle-reference-v1',repository:'gaulatti/bleecker',commit:pin,source:file,sourceSha256:dependencies[0].sha256,dependencies,icons,scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(client)),cssSha256:hash(css),browser:browser.version(),cases,interactions};await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,pngs:cases.length,interactions:interactions.length,iconPaint:[...new Set(cases.map(r=>JSON.stringify({scheme:r.scheme,rgba:r.measurement.strokeRgba})))].map(JSON.parse)}));
})().catch(e=>{console.error(e);process.exitCode=1;});
