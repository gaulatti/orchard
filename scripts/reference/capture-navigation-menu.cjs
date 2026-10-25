const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto'),{execFileSync}=require('node:child_process');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};
const source=option('--source-root'),tooling=option('--tooling-root'),playwright=option('--playwright-module'),output=option('--output'),pin='a86059013cd59684ce452849278ea81e734f2066';
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');

async function main(){
  await fs.mkdir(output,{recursive:true});const dependencies=[];
  for(const name of ['src/components/navigation-menu.tsx','src/components/nav-menu.tsx','src/utils/cn.ts','src/utils/hooks.ts','src/styles/global.css','src/styles/tokens.generated.css','package-lock.json','stories/components/NavigationMenu.stories.tsx']){
    const bytes=await fs.readFile(path.join(source,name));assert.deepEqual(bytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024}));dependencies.push({path:name,sha256:hash(bytes)});
    const target=path.join(output,'Source',name);await fs.mkdir(path.dirname(target),{recursive:true});await fs.writeFile(target,bytes);
  }
  assert.equal(dependencies[0].sha256,'937a1ff45c8a4058247968189c39a22bc444f1168c06b0b4b64200325e5a6d71');
  const runtime=path.join(source,'node_modules'),client=path.join(__dirname,'navigation-menu-client.tsx'),lock=JSON.parse(await fs.readFile(path.join(source,'package-lock.json'))),modules=[];
  for(const name of ['react','react-dom','lucide-react']){const bytes=await fs.readFile(path.join(runtime,name,'package.json')),pkg=JSON.parse(bytes);assert.equal(pkg.version,lock.packages['node_modules/'+name].version);modules.push({name,version:pkg.version,packageSha256:hash(bytes)});}
  const bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/navigation-menu':path.join(source,'src/components/navigation-menu.tsx'),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],metafile:true,define:{'process.env.NODE_ENV':'"production"'}});
  const slash=s=>s.replaceAll('\\','/'),input=(await fs.readFile(path.join(source,'src/styles/global.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'').replace('@import "tailwindcss" source(none);','@import "'+slash(path.join(tooling,'node_modules/tailwindcss/index.css'))+'" source(none);').replace('@import "./tokens.generated.css";','@import "'+slash(path.join(source,'src/styles/tokens.generated.css'))+'";').replace('@source "../**/*.{ts,tsx}";','@source "'+slash(path.join(source,'src/**/*.{ts,tsx}'))+'";');
  await fs.writeFile(path.join(output,'input.css'),input);execFileSync(process.execPath,[path.join(tooling,'node_modules/@tailwindcss/cli/dist/index.mjs'),'-i',path.join(output,'input.css'),'-o',path.join(output,'source.css'),'--minify']);
  const css=await fs.readFile(path.join(output,'source.css')),fonts=[],bundledFiles=[];let faces='';
  for(const [weight,name]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+name);fonts.push({weight,name,sha256:hash(bytes)});faces+=`@font-face{font-family:"Encode Sans";font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
  for(const file of Object.keys(bundle.metafile.inputs))bundledFiles.push({path:slash(path.resolve(file)),sha256:hash(await fs.readFile(path.resolve(file)))});
  await fs.writeFile(path.join(output,'bundle.js'),bundle.outputFiles[0].contents);
  const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[],histories=[],presence=[];
  try{for(const scheme of ['Light','Dark']){
    const page=await browser.newPage({viewport:{width:1000,height:760},locale:'en-US',deviceScaleFactor:1,reducedMotion:'reduce'}),errors=[],network=[];
    await page.route('**/*',route=>{network.push(route.request().url());return route.abort();});page.on('pageerror',error=>errors.push(error.message));
    try{
      await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0;overflow:hidden}${faces}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
      await page.evaluate(async()=>{for(const weight of [400,500,600])await document.fonts.load(weight+' 14px "Encode Sans"');await document.fonts.ready;});
      const settle=()=>page.waitForTimeout(260);
      const trigger=label=>page.locator('#navigation-host > div > div > button').filter({hasText:label});
      const direct=label=>page.locator('#navigation-host > div > a').filter({hasText:label});
      const child=label=>page.locator('#navigation-host .origin-top-left > a').filter({hasText:label});
      const show=async spec=>{await page.mouse.move(980,730);await page.evaluate(spec=>{document.documentElement.style.fontSize=(spec.rootFontSize??16)+'px';window.showNavigation(spec);},spec);await settle();};
      const measure=()=>page.evaluate(()=>{
        const box=node=>{const b=node.getBoundingClientRect();return{x:b.x,y:b.y,width:b.width,height:b.height};};
        const paint=node=>{const s=getComputedStyle(node);return{color:s.color,background:s.backgroundColor,border:s.borderColor,opacity:Number(s.opacity),radius:s.borderRadius,padding:[s.paddingLeft,s.paddingTop,s.paddingRight,s.paddingBottom].map(parseFloat),fontFamily:s.fontFamily,fontSize:s.fontSize,fontWeight:s.fontWeight,lineHeight:s.lineHeight,letterSpacing:s.letterSpacing,gap:s.gap,transitionDuration:s.transitionDuration,transitionTimingFunction:s.transitionTimingFunction,transform:s.transform,rotate:s.rotate,translate:s.translate,scale:s.scale};};
        const link=node=>({text:node.textContent,href:node.getAttribute('href'),target:node.getAttribute('target'),rel:node.getAttribute('rel'),marker:node.dataset.sourceMarker??null,custom:node.dataset.custom==='true',box:box(node),paint:paint(node),parts:[...node.children].filter(n=>n.tagName==='SPAN').map(n=>({text:n.textContent,box:box(n),paint:paint(n)})),input:[...node.querySelectorAll('input')].map(input=>({value:input.value,selectionStart:input.selectionStart,selectionEnd:input.selectionEnd}))});
        const host=document.querySelector('#navigation-host'),container=host.firstElementChild,focused=document.activeElement;
        const intrinsic=mode=>{const clone=container.cloneNode(true);clone.dir=getComputedStyle(container).direction;Object.assign(clone.style,{position:'absolute',left:'-100000px',top:'0',width:'max-content'});for(const child of clone.children)Object.assign(child.style,{width:mode,minWidth:mode,maxWidth:'none',flexShrink:'0'});document.body.append(clone);const widths=[...clone.children].map(child=>box(child).width);clone.remove();return widths;};
        return{host:box(host),container:{box:box(container),paint:paint(container),role:container.getAttribute('role')},intrinsic:{natural:intrinsic('max-content'),minimum:intrinsic('min-content')},links:[...container.children].filter(n=>n.tagName==='A').map(link),triggers:[...container.querySelectorAll(':scope > div > button')].map(node=>({text:node.textContent,expanded:node.getAttribute('aria-expanded'),hasPopup:node.getAttribute('aria-haspopup'),box:box(node),paint:paint(node),chevron:[...node.querySelectorAll('svg')].map(svg=>({box:box(svg),paint:paint(svg)}))})),surfaces:[...container.querySelectorAll('.origin-top-left')].map(node=>({box:box(node),paint:paint(node),visibleClass:node.classList.contains('opacity-100'),links:[...node.children].filter(n=>n.tagName==='A').map(link)})),focus:focused===document.body?'BODY':focused?.id||focused?.getAttribute('aria-label')||focused?.textContent||null,events:structuredClone(window.navigationEvents),renderCalls:structuredClone(window.navigationRenderCalls)};
      });
      const capture=async(id,spec={},open='Components',state=null)=>{
        await show(spec);if(open){await trigger(open).click();await settle();}
        if(state==='trigger-hover')await trigger('Components').hover();
        if(state==='link-hover')await child('Buttons').hover();
        if(state==='direct-hover')await direct('Home').hover();
        if(state==='trigger-focus')await trigger('Components').focus();
        if(state==='child-focus')await child('Buttons').focus();
        if(state==='trigger-pressed'){await trigger('Components').hover();await page.mouse.down();}
        if(!state)await page.mouse.move(980,730);await settle();
        const measurement=await measure(),file=`navigation-menu-${cases.length}-${scheme}.png`,bytes=await page.screenshot({path:path.join(output,file)});cases.push({id,scheme,spec,measurement,file,pngSha256:hash(bytes)});
        if(state==='trigger-pressed')await page.mouse.up();
      };
      await capture('closed',{},null);await capture('empty',{empty:true},null);await capture('links-only',{linksOnly:true},null);await capture('basic');await capture('empty-group',{emptyGroup:true});await capture('no-description',{noDescriptions:true});await capture('long-description',{description:'A deliberately long fictional description that wraps across several lines without hiding its original text.'});await capture('unicode',{label:'Straße 東京 é العربية',description:'Fictional multilingual components.'});await capture('narrow',{width:200});await capture('rtl',{rtl:true});await capture('bottom-right',{left:780,top:650,width:180});await capture('custom',{custom:true});await capture('custom-input',{custom:true,customInput:true});await capture('group-href-unused',{groupHref:'https://example.invalid/ignored'});await capture('layout-open',{},'Layout');await capture('trigger-hover',{},null,'trigger-hover');await capture('direct-hover',{},null,'direct-hover');await capture('link-hover',{},'Components','link-hover');await capture('trigger-focus',{},null,'trigger-focus');await capture('child-focus',{},'Components','child-focus');await capture('font200',{rootFontSize:32});
      await capture('trigger-pressed',{},null,'trigger-pressed');
      const history=async(id,spec,perform)=>{await show(spec);await trigger('Components').click();await settle();const before=await measure();await perform();await settle();histories.push({id,scheme,spec,before,after:await measure(),retainedReactInstance:true});};
      await history('default-child',{},async()=>await child('Buttons').click());
      await history('default-external',{},async()=>await child('External library').click());
      await history('direct-while-open',{},async()=>await direct('Home').click());
      await history('custom-child',{custom:true},async()=>await child('Buttons').click());
      await history('custom-direct-while-open',{custom:true},async()=>await direct('Home').click());
      await history('custom-ignored-close',{custom:true,ignoreClose:true},async()=>await child('Buttons').click());
      await history('custom-throw',{custom:true,throwCustom:true},async()=>await child('Buttons').click());
      await history('outside-dismiss',{},async()=>await page.locator('#outside-after').click());
      await history('inside-pointer',{},async()=>await page.locator('#navigation-host > div').dispatchEvent('pointerdown'));
      await history('hover-other-group',{},async()=>await trigger('Layout').hover());
      await history('toggle-root',{},async()=>await trigger('Components').click());
      await history('switch-root',{},async()=>await trigger('Layout').click());
      await history('escape',{},async()=>await page.keyboard.press('Escape'));
      await history('arrow-down',{},async()=>await page.keyboard.press('ArrowDown'));
      await history('live-label',{},async()=>await page.evaluate(()=>window.updateNavigation({label:'Updated original label'})));
      await history('live-description',{},async()=>await page.evaluate(()=>window.updateNavigation({description:'Updated original description.'})));
      await history('replace-trigger',{},async()=>await page.evaluate(()=>window.updateNavigation({trigger:'Renamed'})));
      await history('prepend-index',{},async()=>await page.evaluate(()=>window.updateNavigation({prepend:true})));
      await history('remove-active-index',{},async()=>await page.evaluate(()=>window.updateNavigation({onlyLayout:true})));
      await show({});await trigger('Components').focus();await page.keyboard.press('Enter');await settle();const before=await measure();await page.keyboard.press('Tab');await settle();const tab=await measure();await page.keyboard.press('Enter');await settle();histories.push({id:'tab-enter-child',scheme,spec:{},before,tab,after:await measure(),retainedReactInstance:true});
      await show({custom:true,customInput:true});await trigger('Components').click();await settle();const original=await page.locator('input[aria-label="Original original-buttons"]').elementHandle();await original.fill('caller retained custom text');await page.evaluate(()=>window.updateNavigation({retainedEntries:true}));await settle();const retained=await page.locator('input[aria-label="Original original-buttons"]').evaluate((node,original)=>node===original,original);histories.push({id:'custom-node-retained',scheme,spec:{custom:true,customInput:true},sameInputNode:retained,after:await measure(),retainedReactInstance:true});
      await show({});await trigger('Components').click();await settle();const samples=await page.evaluate(async()=>{const trigger=document.querySelector('#navigation-host > div > div > button'),original=document.querySelector('.origin-top-left'),sample=()=>{const node=document.querySelector('.origin-top-left');return{present:!!node,sameNode:node===original,visibleClass:node?.classList.contains('opacity-100')??false,expanded:trigger.getAttribute('aria-expanded')};};trigger.click();await new Promise(r=>setTimeout(r,50));const at50=sample();await new Promise(r=>setTimeout(r,130));return{at50,at180:sample()};});presence.push({scheme,...samples});
      assert.deepEqual(errors,['fictional navigation callback failed']);assert.deepEqual(network,[]);
      await page.close();
    }catch(error){await page.screenshot({path:path.join(output,'failed-'+scheme+'.png')}).catch(()=>{});throw error;}
  }
  const result={format:'orchard-navigation-menu-reference-v1',commit:pin,dependencies,modules,fonts,bundledFiles,browser:browser.version(),scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(client)),cssSha256:hash(css),bundleSha256:hash(bundle.outputFiles[0].contents),viewport:{width:1000,height:760},reducedMotion:'reduce',networkRequests:0,cases,histories,presence,nativeImplemented:false,nativeComponentAccepted:false};
  await fs.writeFile(path.join(output,'NavigationMenu.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,histories:histories.length,presence:presence.length,bundledFiles:bundledFiles.length,browser:browser.version()}));
  }finally{await browser.close();}
}
main().catch(error=>{console.error(error);process.exitCode=1;});
