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
 const labels=[
  {id:'mixed',label:'Révision 日本語 العربية',width:280},
  {id:'mixed-rtl',label:'Révision 日本語 العربية',width:160,rtl:true},
  {id:'cjk-short',label:'日本語公開',width:60},
  {id:'cjk-zero',label:'日本語公開',width:0},
  {id:'cjk-narrow',label:'日本語の公開ワークフロー',width:100},
  {id:'hiragana',label:'ひらがなとカタカナ',width:80},
  {id:'hangul',label:'한글 공개 작업',width:80},
  {id:'arabic',label:'حالة النشر والمراجعة',width:80,rtl:true},
  {id:'hebrew',label:'מצב פרסום ובדיקה',width:80,rtl:true},
  {id:'combining',label:'Re\u0301vision cafe\u0301 A\u030Aland',width:100},
  {id:'emoji',label:'Review 👩🏽‍💻 🏳️‍🌈 family 👨‍👩‍👧',width:100},
  {id:'zero-width-space',label:'Consumer\u200bowned\u200bpublication',width:70},
  {id:'word-joiner',label:'Review\u2060queue',width:70},
  {id:'nonbreaking',label:'Review\u00a0queue',width:70},
  {id:'soft-hyphen',label:'publi\u00adcation\u00adworkflow',width:100},
  {id:'punctuation',label:'Consumer-owned/Review—queue',width:100},
  {id:'cjk-punctuation',label:'「日本語」、公開。レビュー',width:80},
  {id:'long-latin',label:'Consumer owned publication workflow',width:100}
 ];
 const specs=labels.map(({id,label,...props})=>({id,...props,items:[{...basic[0],label},basic[1]]}));
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[],updates=[];
 try{assert.equal(browser.version(),'155.0.4283.62');for(const motion of ['reduce','no-preference'])for(const scheme of ['Light','Dark']){
  const page=await browser.newPage({viewport:{width:640,height:400},deviceScaleFactor:1,reducedMotion:motion}),errors=[];page.on('pageerror',error=>errors.push(error.message));
  try{
   await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0}#root{position:absolute;left:20px;top:20px;width:280px}.custom-stack{margin:0;}.custom-track{height:20px;border-radius:3px;background:rgb(245,237,220)}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);await page.evaluate(async()=>{for(const weight of [400,600])await document.fonts.load(`${weight} 12px "Encode Sans"`);await document.fonts.ready;});
   const settle=()=>page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
   const session=await page.context().newCDPSession(page);await session.send('DOM.enable');await session.send('CSS.enable');
   const measure=()=>page.evaluate(()=>{
    const root=document.querySelector('#root>div'),box=n=>{const r=n.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};};
    const node=(n,text=false)=>{const s=getComputedStyle(n),result={box:box(n),text:n.textContent,color:s.color,background:s.backgroundColor,radius:s.borderTopLeftRadius,weight:s.fontWeight,fontSize:s.fontSize,lineHeight:s.lineHeight,title:n.title};
     if(text){const clone=n.cloneNode(true);Object.assign(clone.style,{position:'absolute',left:'-20000px',top:'0',display:'block',minWidth:'0',maxWidth:'none',width:'max-content'});n.parentElement.appendChild(clone);try{const maximum=clone.getBoundingClientRect().width;clone.style.width='min-content';result.intrinsic={maximum,minimum:clone.getBoundingClientRect().width};}finally{clone.remove();}const element=n.firstChild,segments=[...new Intl.Segmenter('en',{granularity:'grapheme'}).segment(n.textContent)],lineHeight=parseFloat(s.lineHeight);result.graphemes=segments.map(({index,segment})=>{const range=document.createRange();range.setStart(element,index);range.setEnd(element,index+segment.length);const rects=[...range.getClientRects()].map(rect=>({x:rect.x,y:rect.y,width:rect.width,height:rect.height}));const visible=rects.filter(rect=>rect.width>0),position=visible.at(-1)??rects.at(-1);return{index,text:segment,rects,line:position?Math.round((position.y-result.box.y)/lineHeight):null};});}
     return result;
    };
    return{root:node(root),bar:node(root.children[0]),segments:[...root.children[0].children].map(n=>node(n)),legend:root.children[1]?node(root.children[1]):null,items:root.children[1]?[...root.children[1].children].map(n=>({box:box(n),swatch:node(n.children[0]),label:node(n.children[1],true),value:node(n.children[2],true)})):[],animations:root.getAnimations({subtree:true}).length,tabIndex:root.tabIndex};
   });
   for(const spec of specs){await page.mouse.move(0,0);await page.evaluate(spec=>window.showProgressStack(spec),spec);await settle();const measurement=await measure(),file=`progress-stack-text-${spec.id}-${scheme}-${motion}.png`,png=await page.screenshot({path:path.join(output,file)});const {root:dom}=await session.send('DOM.getDocument');const {nodeIds}=await session.send('DOM.querySelectorAll',{nodeId:dom.nodeId,selector:'#root>div>div:nth-child(2)>div>span:not(:first-child)'});const platformFonts=[];for(const nodeId of nodeIds)platformFonts.push(await session.send('CSS.getPlatformFontsForNode',{nodeId}));cases.push({id:spec.id,scheme,motion,spec,measurement,platformFonts,file,pngSha256:hash(png)});}
   for(const[id,items,showLegend]of [['reorder',[...basic].reverse(),true],['remove',basic.slice(1),true],['hide',basic,false],['clear',[],true]]){await page.evaluate(spec=>window.showProgressStack(spec),{id,items,showLegend});await settle();updates.push({id,scheme,motion,measurement:await measure()});}assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const reference={format:'orchard-progress-stack-text-reference-v1',sourceCommit:pin,sourceFile:file,sourceSha256:hash(bytes),sourceCssSha256:hash(cssBytes),clientSha256:hash(clientBytes),scriptSha256:hash(await fs.readFile(__filename)),browser:browser.version(),fonts,cases,updates};await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(reference,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,updates:updates.length}));
})().catch(error=>{console.error(error);process.exitCode=1});
