const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 const output=option('--output'),bytes=await fs.readFile('tests/Reference/ProgressStack.json'),source=JSON.parse(bytes);assert.equal(source.sourceSha256,'101df66952162d978a45935604e468e7b465e8a57afdf873a48e0867a46b6254');
 await fs.mkdir(output,{recursive:true});const browser=await require(option('--playwright-module')).chromium.launch({headless:true,channel:'msedge'}),cases=[];
 try{
  assert.equal(browser.version(),'155.0.4283.62');const page=await browser.newPage({viewport:{width:640,height:400},deviceScaleFactor:1});
  for(const scheme of ['Light','Dark'])for(const[id,backdrop]of [['transparent',[0,0,0,0]],['white',[255,255,255,255]],['black',[0,0,0,255]],['dark',[13,24,33,255]],['blue',[44,87,132,255]]]){
   const paint=source.cases.find(row=>row.id==='empty'&&row.scheme===scheme&&row.motion==='reduce').measurement.bar.background;
   await page.setContent(`<style>html,body{margin:0;background:transparent}body{background:rgba(${backdrop[0]},${backdrop[1]},${backdrop[2]},${backdrop[3]/255})}#square{position:absolute;left:20px;top:20px;width:40px;height:40px}#paint{width:100%;height:100%;background:${paint}}</style><div id="square"><div id="paint"></div></div>`);
   const computed=await page.locator('#paint').evaluate(node=>getComputedStyle(node).backgroundColor);assert.equal(computed,paint);
   const file=`progress-stack-paint-${id}-${scheme}.png`,png=await page.screenshot({path:path.join(output,file),omitBackground:true});cases.push({id,scheme,backdrop,paint,width:40,height:40,viewportWidth:640,viewportHeight:400,file,pngSha256:hash(png)});
  }
 }finally{await browser.close();}
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify({format:'orchard-progress-stack-paint-reference-v1',scope:'square paint-only diagnostic using unchanged frozen track CSS in the canonical 640x400 context; not component raster acceptance',sourceCommit:source.sourceCommit,sourceSha256:source.sourceSha256,trackReferenceSha256:hash(bytes),scriptSha256:hash(await fs.readFile(__filename)),browser:browser.version(),cases},null,2)+'\n');console.log(JSON.stringify({cases:cases.length}));
})().catch(error=>{console.error(error);process.exitCode=1;});
