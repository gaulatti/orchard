const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto');
const moduleIndex=process.argv.indexOf('--playwright-module');
if(moduleIndex<0||!process.argv[moduleIndex+1])throw Error('Pass --playwright-module <absolute package directory>.');
const playwrightPath=path.resolve(process.argv[moduleIndex+1]);
if(require(path.join(playwrightPath,'package.json')).version!=='1.62.1')throw Error('Pinned Playwright differs.');
const {chromium}=require(playwrightPath);
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
(async()=>{
 const ref=JSON.parse(await fs.readFile('tests/Reference/DataTableToolbarLayout.json','utf8'));
 const css=(await fs.readFile('artifacts/line-chart-browser/styles.css','utf8')).replace(/@import url\([^\r\n]*\);/g,'');
 if(hash(css)!==ref.compiledCssSha256)throw Error('Frozen CSS differs.');
 let fonts='';for(const [weight,file]of[[400,'EncodeSans-Regular.ttf'],[500,'EncodeSans-Medium.ttf'],[600,'EncodeSans-SemiBold.ttf']]){
  const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+file);
  fonts+="@font-face{font-family:'Encode Sans';font-weight:"+weight+";src:url(data:font/ttf;base64,"+bytes.toString('base64')+")}"
 }
 const bundle=await fs.readFile('artifacts/data-table-toolbar-browser/bundle.js','utf8');
 const browser=await chromium.launch({headless:true,channel:'msedge'}),results=[];
 if(browser.version()!==ref.browser){await browser.close();throw Error('Frozen browser differs.')}
 try{for(const scheme of ['Light','Dark']){
  const sample=ref.cases.find(c=>c.id==='column-filter'&&c.scheme===scheme);
  const page=await browser.newPage({viewport:{width:1000,height:745},reducedMotion:'reduce',deviceScaleFactor:1});
  await page.setContent('<html class="'+(scheme==='Dark'?'dark':'')+'" dir="ltr"><head><style>'+css+fonts+'body{margin:0;background:var(--color-background)}#root{width:800px}</style></head><body><button id="before">Before</button><div id="root"></div><button id="after">After</button><output id="data-probe"></output><script>'+bundle+'</script></body></html>');
  await page.evaluate(model=>window.showToolbar(model),sample.props);
  await page.evaluate(async()=>{await document.fonts.ready;for(let i=0;i<4;i++)await new Promise(requestAnimationFrame)});
  const before=await page.locator('#root').screenshot();
  if(hash(before)!==sample.screenshots[0].sha256)throw Error('Initial source screenshot differs: '+scheme);
  const metric=await page.evaluate(()=>{
   const root=document.querySelector('#root'),button=[...root.querySelectorAll('button')].find(b=>b.textContent==='Reset');
   const text=[...button.childNodes].find(n=>n.nodeType===Node.TEXT_NODE&&n.textContent.trim()==='Reset');
   const range=document.createRange();range.selectNodeContents(text);const originalRange=range.getBoundingClientRect();
   const span=document.createElement('span');button.insertBefore(span,text);span.append(text);
   const marker=document.createElement('i');marker.style.cssText='display:inline-block;width:0;height:0;padding:0;margin:0;border:0;vertical-align:baseline';span.append(marker);
   const origin=root.getBoundingClientRect(),point=marker.getBoundingClientRect(),style=getComputedStyle(button);
   return {baselineY:point.y-origin.y,textRange:{x:originalRange.x-origin.x,y:originalRange.y-origin.y,width:originalRange.width,height:originalRange.height},fontFamily:style.fontFamily,fontSize:style.fontSize,fontWeight:style.fontWeight,lineHeight:style.lineHeight};
  });
  const after=await page.locator('#root').screenshot();if(!before.equals(after))throw Error('Baseline marker changed source pixels: '+scheme);
  results.push({scheme,initialScreenshotSha256:hash(before),markerScreenshotSha256:hash(after),...metric});await page.close();
 }}finally{await browser.close()}
 const report={format:'orchard-data-table-toolbar-caption-v1',browser:ref.browser,playwright:'1.62.1',probeScriptSha256:hash(await fs.readFile(__filename)),sourceCommit:ref.sourceCommit,sourceSha256:ref.sourceSha256,compiledCssSha256:ref.compiledCssSha256,results};
 await fs.writeFile('tests/Reference/DataTableToolbarCaption.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
})().catch(e=>{console.error(e.message);process.exit(1)});
