const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const {execFileSync} = require('node:child_process');
const option = name => {const index=process.argv.indexOf(name);assert(index>=0 && process.argv[index+1],name);return path.resolve(process.argv[index+1]);};
const source=option('--source-root'), tooling=option('--tooling-root'), playwright=option('--playwright-module'), output=option('--output');
const pin='a86059013cd59684ce452849278ea81e734f2066';
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');

async function main() {
  await fs.mkdir(output,{recursive:true});
  const dependencies=[];
  for(const name of ['src/components/menubar.tsx','src/utils/cn.ts','src/utils/hooks.ts','src/styles/global.css','src/styles/tokens.generated.css','package-lock.json']) {
    const bytes=await fs.readFile(path.join(source,name));
    assert.deepEqual(bytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024}));
    dependencies.push({path:name,sha256:hash(bytes)});
    const frozen=path.join(output,'Source',name);
    await fs.mkdir(path.dirname(frozen),{recursive:true});
    await fs.writeFile(frozen,bytes);
  }
  assert.equal(dependencies[0].sha256,'372c40d54f5cf711426d2042b3a1f00d71221f157ab16643d8f42f8883cc2122');
  const runtime=path.join(source,'node_modules'),client=path.join(__dirname,'menubar-client.tsx');
  const lock=JSON.parse(await fs.readFile(path.join(source,'package-lock.json'))),modules=[];
  for(const name of ['react','react-dom','lucide-react']) {
    const file=path.join(runtime,name,'package.json'),bytes=await fs.readFile(file),pkg=JSON.parse(bytes);
    assert.equal(pkg.version,lock.packages['node_modules/'+name].version);
    modules.push({name,version:pkg.version,packageSha256:hash(bytes)});
  }
  const bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/menubar':path.join(source,'src/components/menubar.tsx'),react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],metafile:true,define:{'process.env.NODE_ENV':'"production"'}});
  const slash=value=>value.replaceAll('\\','/');
  const input=(await fs.readFile(path.join(source,'src/styles/global.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'').replace('@import "tailwindcss" source(none);','@import "'+slash(path.join(tooling,'node_modules/tailwindcss/index.css'))+'" source(none);').replace('@import "./tokens.generated.css";','@import "'+slash(path.join(source,'src/styles/tokens.generated.css'))+'";').replace('@source "../**/*.{ts,tsx}";','@source "'+slash(path.join(source,'src/**/*.{ts,tsx}'))+'";');
  await fs.writeFile(path.join(output,'input.css'),input);
  execFileSync(process.execPath,[path.join(tooling,'node_modules/@tailwindcss/cli/dist/index.mjs'),'-i',path.join(output,'input.css'),'-o',path.join(output,'source.css'),'--minify']);
  const css=await fs.readFile(path.join(output,'source.css')),fonts=[],bundledFiles=[];
  let faces='';
  for(const [weight,name] of [[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]) {
    const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+name);
    fonts.push({weight,name,sha256:hash(bytes)});
    faces+=`@font-face{font-family:"Encode Sans";font-weight:${weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;
  }
  for(const file of Object.keys(bundle.metafile.inputs)) bundledFiles.push({path:slash(path.resolve(file)),sha256:hash(await fs.readFile(path.resolve(file)))});
  await fs.writeFile(path.join(output,'bundle.js'),bundle.outputFiles[0].contents);
  const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[],histories=[],presence=[];
  try {
    for(const scheme of ['Light','Dark']) {
      const page=await browser.newPage({viewport:{width:900,height:760},locale:'en-US',deviceScaleFactor:1,reducedMotion:'reduce'}),errors=[],network=[];
      await page.route('**/*',route=>{network.push(route.request().url());return route.abort();});
      page.on('pageerror',error=>errors.push(error.message));
      try {
        await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0;overflow:hidden}${faces}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
        await page.evaluate(async()=>{for(const weight of [400,500,600])await document.fonts.load(weight+' 14px "Encode Sans"');await document.fonts.ready;});
        const settle=()=>page.waitForTimeout(260);
        const top=label=>page.locator('[role="menubar"] > div > button').filter({hasText:label});
        const row=label=>page.locator('[role="menu"] button').filter({hasText:label});
        const show=async spec=>{await page.mouse.move(880,730);await page.evaluate(spec=>{document.documentElement.style.fontSize=(spec.rootFontSize ?? 16)+'px';window.showMenubar(spec);},spec);await settle();};
        const measure=()=>page.evaluate(()=>{
          const box=node=>{const b=node.getBoundingClientRect();return{x:b.x,y:b.y,width:b.width,height:b.height};};
          const paint=node=>{const s=getComputedStyle(node);return{background:s.backgroundColor,border:s.borderColor,color:s.color,opacity:Number(s.opacity),radius:s.borderRadius,fontSize:s.fontSize,lineHeight:s.lineHeight,padding:[s.paddingLeft,s.paddingTop,s.paddingRight,s.paddingBottom].map(parseFloat),fontWeight:s.fontWeight,gap:s.gap,transitionDuration:s.transitionDuration};};
          const item=node=>({text:node.textContent,role:node.getAttribute('role'),box:box(node),paint:paint(node),disabled:node.disabled===true,checked:node.getAttribute('aria-checked'),expanded:node.getAttribute('aria-expanded'),hasPopup:node.getAttribute('aria-haspopup'),tabIndex:node.tabIndex,icons:[...node.querySelectorAll('svg')].map(icon=>({box:box(icon),paint:paint(icon)}))});
          const bar=document.querySelector('[role="menubar"]'),surfaces=[...bar.querySelectorAll('div')].filter(node=>node.classList.contains('origin-top-left'));
          const focused=document.activeElement;
          return{bar:{box:box(bar),paint:paint(bar)},triggers:[...bar.querySelectorAll(':scope > div > button')].map(item),surfaces:surfaces.map(surface=>({role:surface.getAttribute('role'),box:box(surface),paint:paint(surface),visibleClass:surface.classList.contains('opacity-100'),rows:[...surface.querySelectorAll('button,[role="separator"]')].filter(node=>node.closest('.origin-top-left')===surface).map(item)})),focus:focused===document.body?'BODY':focused?.id||focused?.textContent||null,events:structuredClone(window.menubarEvents)};
        });
        const capture=async(id,spec={},open='File',hover=[])=>{
          await show(spec);
          if(open){await top(open).click();await settle();}
          for(const label of hover){await row(label).hover();await settle();}
          if(!hover.length){await page.mouse.move(880,730);await settle();}
          const measurement=await measure(),file=`menubar-${cases.length}-${scheme}.png`,bytes=await page.screenshot({path:path.join(output,file)});
          cases.push({id,scheme,spec,measurement,file,pngSha256:hash(bytes)});
        };
        await capture('closed',{},null);
        await capture('empty-bar',{emptyBar:true},null);
        await capture('basic');
        await capture('rich-action',{rich:true});
        await capture('checked-beta',{checked:true,radio:'b'});
        await capture('empty-menu',{emptyMenu:true});
        await capture('edit-menu',{},'Edit');
        await capture('submenu',{},'File',['More fictional choices']);
        await capture('deep-submenu',{},'File',['More fictional choices','Deeper fictional choices']);
        await capture('disabled-submenu',{},'File',['Unavailable submenu']);
        await capture('unicode',{label:'Straße 東京 é العربية'});
        await capture('long-label',{label:'A fictional action with a deliberately long label across the menu'});
        await capture('narrow',{width:140});
        await capture('rtl',{rtl:true});
        await capture('bottom-right',{left:680,top:650,width:180});
        await capture('font200',{rootFontSize:32});

        const history=async(id,setup,perform)=>{
          await show(setup);await top('File').click();await settle();
          const before=await measure();await perform();await settle();const after=await measure();
          histories.push({id,scheme,setup,before,after,retainedReactInstance:true});
          return histories[histories.length-1];
        };
        for(const accept of [false,true]) for(const action of ['checkbox','radio']) {
          const h=await history(`${action}-${accept?'accepted':'refused'}`,{accept},async()=>await row(action==='checkbox'?'Show fictional guides':'Fictional Beta mode').click());
          await top('File').click();await settle();h.reopened=await measure();
        }
        await history('action',{},async()=>await row('New fictional draft').click());
        await history('disabled-action',{},async()=>await row('Unavailable action').evaluate(button=>button.click()));
        await history('disabled-radio',{},async()=>await row('Unavailable mode').evaluate(button=>button.click()));
        await history('toggle-trigger',{},async()=>await top('File').click());
        await history('switch-trigger',{},async()=>await top('Edit').click());
        await history('hover-other-trigger',{},async()=>await top('Edit').hover());
        await history('outside-dismiss',{},async()=>await page.locator('#outside-after').click());
        await history('inside-bar-pointer',{},async()=>await page.locator('[role="menubar"]').dispatchEvent('pointerdown'));
        await history('escape',{},async()=>await page.keyboard.press('Escape'));
        await history('arrow-down',{},async()=>await page.keyboard.press('ArrowDown'));
        await history('shortcut-display-only',{},async()=>await page.keyboard.press('Control+n'));
        await history('submenu-click',{},async()=>await row('More fictional choices').evaluate(button=>button.click()));
        await history('submenu-action',{},async()=>{await row('More fictional choices').hover();await settle();await row('Nested original action').click();});
        await history('live-selection',{},async()=>await page.evaluate(()=>window.updateMenubar({checked:true,radio:'b'})));
        await history('replace-items',{},async()=>await page.evaluate(()=>window.updateMenubar({label:'Updated original label'})));
        await history('replace-trigger',{},async()=>await page.evaluate(()=>window.updateMenubar({trigger:'Renamed'})));
        await history('remove-active-index',{},async()=>await page.evaluate(()=>window.updateMenubar({emptyBar:true})));
        await history('active-index-now-edit',{},async()=>await page.evaluate(()=>window.updateMenubar({onlyEdit:true})));
        await history('prepend-index',{},async()=>await page.evaluate(()=>window.updateMenubar({prepend:true})));
        await history('callback-throws',{throwAction:true},async()=>await row('New fictional draft').click());

        await show({});await top('File').focus();await page.keyboard.press('Enter');await settle();
        const keyboardBefore=await measure();await page.keyboard.press('Tab');await settle();
        const keyboardAfter=await measure();await page.keyboard.press('Enter');await settle();
        histories.push({id:'tab-and-enter',scheme,setup:{},before:keyboardBefore,after:keyboardAfter,selected:await measure(),retainedReactInstance:true});

        // Browser event timers continue under reduced motion: CSS exits instantly,
        // while usePresence retains the same original menu subtree for 150ms.
        await show({});await top('File').click();await settle();
        const samples=await page.evaluate(async()=>{
          const trigger=document.querySelector('[role="menubar"] > div > button');
          const original=document.querySelector('[role="menu"]');
          const sample=()=>{const menu=document.querySelector('[role="menu"]');return{present:!!menu,sameNode:menu===original,visibleClass:menu?.classList.contains('opacity-100')??false,expanded:trigger.getAttribute('aria-expanded')};};
          trigger.click();await new Promise(resolve=>setTimeout(resolve,50));const at50=sample();
          await new Promise(resolve=>setTimeout(resolve,130));const at180=sample();
          return{at50,at180};
        });
        presence.push({scheme,...samples});
        assert.deepEqual(errors,['fictional menubar callback failed']);
        assert.deepEqual(network,[]);
        await page.close();
      } catch(error) {await page.screenshot({path:path.join(output,'failed-'+scheme+'.png')}).catch(()=>{});throw error;}
    }
    const result={format:'orchard-menubar-reference-v1',commit:pin,dependencies,modules,fonts,bundledFiles,browser:browser.version(),scriptSha256:hash(await fs.readFile(__filename)),clientSha256:hash(await fs.readFile(client)),cssSha256:hash(css),bundleSha256:hash(bundle.outputFiles[0].contents),viewport:{width:900,height:760},reducedMotion:'reduce',networkRequests:0,cases,histories,presence,nativeImplemented:false,nativeComponentAccepted:false};
    await fs.writeFile(path.join(output,'Menubar.json'),JSON.stringify(result,null,2)+'\n');
    console.log(JSON.stringify({cases:cases.length,histories:histories.length,presence:presence.length,bundledFiles:bundledFiles.length,browser:browser.version()}));
  } finally {await browser.close();}
}
main().catch(error=>{console.error(error);process.exitCode=1;});
