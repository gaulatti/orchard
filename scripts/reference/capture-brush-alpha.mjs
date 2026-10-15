import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';

const option=name=>{const i=process.argv.indexOf(name);if(i<0||!process.argv[i+1])throw Error('Pass '+name+' <path>.');return path.resolve(process.argv[i+1]);};
const modulePath=option('--playwright-module'),output=option('--output'),require=createRequire(import.meta.url);
assert.equal(require(path.join(modulePath,'package.json')).version,'1.62.1');
const {chromium}=require(modulePath),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const referenceBytes=await fs.readFile('tests/Reference/DataTableToolbarLayout.json'),reference=JSON.parse(referenceBytes);
for(const dependency of reference.dependencies)assert.equal(hash(await fs.readFile(path.join('artifacts/line-chart-browser/frozen-source',dependency.file))),dependency.sha256);
const bundle=await fs.readFile('artifacts/data-table-toolbar-browser/bundle.js','utf8');
assert.equal(hash(bundle),JSON.parse(await fs.readFile('tests/Reference/DataTableToolbarReset.json','utf8')).bundleSha256);
const css=(await fs.readFile('artifacts/line-chart-browser/styles.css','utf8')).replace(/@import url\([^\r\n]*\);/g,'');
assert.equal(hash(css),reference.compiledCssSha256);
let fonts='';
for(const [weight,file]of [[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){
 const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);assert.equal(hash(bytes),reference.fontFiles.find(item=>item.file===file).sha256);
 fonts+="@font-face{font-family:'Encode Sans';font-weight:"+weight+";src:url(data:font/ttf;base64,"+bytes.toString('base64')+")}"
}
await fs.mkdir(output,{recursive:true});
const browser=await chromium.launch({headless:true,channel:'msedge'}),reports=[];
assert.equal(browser.version(),reference.browser);
try{
 for(const scheme of ['Light','Dark']){
  const sample=reference.cases.find(item=>item.id==='column-filter'&&item.scheme===scheme);
  const page=await browser.newPage({viewport:{width:1024,height:745},deviceScaleFactor:1,reducedMotion:'reduce'});
  const errors=[];page.on('pageerror',error=>errors.push(error.message));
  try{
   const settle=async selector=>{
    await page.evaluate(()=>window.__alphaPaint=undefined);
    const handle=await page.waitForFunction(selector=>{
     const root=document.querySelector(selector);if(!root)return false;
     const signature=JSON.stringify([root,...root.querySelectorAll('*')].map(node=>{const r=node.getBoundingClientRect(),s=getComputedStyle(node);return[r.x,r.y,r.width,r.height,s.backgroundColor,s.borderColor,s.color,s.opacity,s.transform];}));
     const prior=window.__alphaPaint;window.__alphaPaint={signature,stable:prior?.signature===signature?prior.stable+1:0,frames:(prior?.frames??0)+1,start:prior?.start??performance.now()};
     const current=window.__alphaPaint;return current.stable>=3?{stable:current.stable,frames:current.frames,milliseconds:performance.now()-current.start}:false;
    },selector,{polling:'raf',timeout:2000});return handle.jsonValue();
   };
   await page.setContent('<html class="'+(scheme==='Dark'?'dark':'')+'"><head><style>'+css+fonts+'body{margin:0;background:var(--color-background)}#root{width:800px}</style></head><body><div id="root"></div><output id="data-probe"></output><script>'+bundle+'</script></body></html>');
   await page.evaluate(async()=>{await Promise.all([400,500,600].map(weight=>document.fonts.load(weight+' 16px "Encode Sans"')));await document.fonts.ready;});
   await page.evaluate(props=>window.showToolbar(props),sample.props);
   const reset=page.getByRole('button',{name:'Reset',exact:true});await reset.hover();
   const originalPresentation=await settle('#root');
   const source=await reset.evaluate(button=>{
    const receipt=window.inputEvents.findLast(item=>item.type==='pointermove'),hit=receipt&&document.elementFromPoint(receipt.x,receipt.y);
    const token=document.documentElement.classList.contains('dark')?'dark:hover:bg-white/[0.06]':'hover:bg-sand/12',selector='.'+CSS.escape(token),declarations=[];
    const visit=(rules,selectors='')=>{for(const rule of rules){const context=selectors+' '+(rule.selectorText??'');if(context.includes(selector)&&rule.style?.backgroundColor.startsWith('color-mix(in oklab,'))declarations.push({value:rule.style.backgroundColor,selectors:context});if(rule.cssRules)visit(rule.cssRules,context);}};
    for(const sheet of document.styleSheets)visit(sheet.cssRules);
    if(declarations.length!==1)throw Error('Expected one original Oklab hover declaration.');
    return{background:getComputedStyle(button).backgroundColor,parentBackground:getComputedStyle(document.body).backgroundColor,pointerOver:button.matches(':hover'),receipt,ownsHit:!!hit&&button.contains(hit),declaration:declarations[0],bounds:button.getBoundingClientRect().toJSON()};
   });
   const nodes=node=>[node,...(node.children??[]).flatMap(nodes)];
   const recorded=nodes(sample.states.find(item=>item.action==='reset-hover').state.surface).find(node=>node.tag==='BUTTON'&&node.text==='Reset');
   await fs.writeFile(path.join(output,'observed-'+scheme+'.json'),JSON.stringify({source,expected:reference.styles[recorded.styleIndex].backgroundColor,bounds:await reset.boundingBox()},null,2));
   assert.equal(source.background,reference.styles[recorded.styleIndex].backgroundColor);assert.equal(source.receipt.trusted,true);assert.equal(source.ownsHit,true);
   const originalFile=`toolbar-${scheme.toLowerCase()}.png`,original=await page.locator('#root').screenshot({caret:'initial',animations:'allow'}),originalRepeat=await page.locator('#root').screenshot({caret:'initial',animations:'allow'});assert.equal(hash(original),hash(originalRepeat));await fs.writeFile(path.join(output,originalFile),original);
   const cells=await page.evaluate(({source,scheme})=>{
    const match=source.declaration.value.match(/([\d.]+)%/);if(!match)throw Error('Unrecognized original color mix: '+source.declaration.value);
    const alpha=Number(match[1])/100,rounded=Math.round(alpha*255),floor=Math.floor(alpha*255);
    const backgrounds=[source.parentBackground,'rgb(230, 213, 184)','rgb(0, 0, 0)'];
    const panel=document.createElement('div');panel.id='alpha-probe';panel.style.cssText='position:fixed;inset:0;width:1024px;height:745px;background:white;pointer-events:none';document.body.append(panel);
    const cells=[];
    backgrounds.forEach((background,row)=>[alpha,rounded/255,floor/255,1].forEach((opacity,column)=>{
     const x=16+column*64,y=40+row*80,cell=document.createElement('div'),paint=document.createElement('div');
     cell.style.cssText=`position:absolute;left:${x}px;top:${y}px;width:32px;height:32px;background:${background}`;
     paint.style.cssText=`width:100%;height:100%;background:${source.declaration.value.replace(/[\d.]+%/,opacity*100+'%')}`;
     cell.append(paint);panel.append(cell);cells.push({row,column,x,y,width:32,height:32,background,paint:getComputedStyle(paint).backgroundColor});
    }));return{alpha,rounded,floor,cells};
   },{source,scheme});
   const swatchPresentation=await settle('#alpha-probe');
   const file=`catalog-${scheme.toLowerCase()}.png`,first=await page.locator('#alpha-probe').screenshot({caret:'initial',animations:'allow'}),repeat=await page.locator('#alpha-probe').screenshot({caret:'initial',animations:'allow'});
   assert.equal(hash(first),hash(repeat));await fs.writeFile(path.join(output,file),first);assert.deepEqual(errors,[]);
   reports.push({scheme,file,source,...cells,sha256:hash(first),repeatSha256:hash(repeat),originalFile,originalSha256:hash(original),originalRepeatSha256:hash(originalRepeat),originalPresentation,swatchPresentation,errors});
  }finally{await page.close();}
 }
}finally{await browser.close();}
await fs.writeFile(path.join(output,'result.json'),JSON.stringify({format:'orchard-brush-alpha-source-v1',sourceCommit:reference.sourceCommit,referenceSha256:hash(referenceBytes),compiledCssSha256:reference.compiledCssSha256,browser:reference.browser,scriptSha256:hash(await fs.readFile(new URL(import.meta.url))),masks:0,tolerance:0,reports},null,2)+'\n');
console.log(JSON.stringify({output,reports:reports.length}));
