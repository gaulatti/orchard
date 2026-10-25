const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto'),{execFileSync}=require('node:child_process');
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const source=option('--source-root'),tooling=option('--tooling-root'),playwright=option('--playwright-module'),cssDirectory=option('--source-css'),output=option('--output');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 const referenceBytes=await fs.readFile('tests/Reference/ProgressStackText.json'),reference=JSON.parse(referenceBytes),pin=reference.sourceCommit;
 const sourceFile=path.join(source,reference.sourceFile),sourceBytes=await fs.readFile(sourceFile);
 assert.equal(hash(sourceBytes),reference.sourceSha256);
 assert.equal(Buffer.compare(sourceBytes,execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+reference.sourceFile])),0);
 for(const name of ['src/styles/global.css','src/styles/tokens.generated.css','src/utils/cn.ts','package-lock.json'])assert.equal(Buffer.compare(await fs.readFile(path.join(source,name)),execFileSync('git',['--git-dir',path.resolve(source,'../bleecker.git'),'show',pin+':'+name],{maxBuffer:8*1024*1024})),0,`Frozen dependency differs: ${name}`);
 const client=path.join(__dirname,'progress-stack-client.tsx'),clientBytes=await fs.readFile(client); assert.equal(hash(clientBytes),reference.clientSha256);
 const runtime=path.join(source,'node_modules');
 assert.equal(require(path.join(tooling,'node_modules/esbuild/package.json')).version,'0.25.12');
 assert.equal(require(path.join(playwright,'package.json')).version,'1.62.1');
 for(const name of ['react','react-dom'])assert.equal(require(path.join(runtime,name,'package.json')).version,'19.2.4');
 const bundle=await require(path.join(tooling,'node_modules/esbuild')).build({entryPoints:[client],alias:{'frozen/progress-stack':sourceFile,react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile(path.join(cssDirectory,'source.css')); assert.equal(hash(cssBytes),reference.sourceCssSha256); let css=cssBytes.toString();
 for(const font of reference.fonts){const bytes=await fs.readFile(`src/Orchard/Assets/Fonts/${font.name}.ttf`);assert.equal(hash(bytes),font.sha256);css+=`@font-face{font-family:"Encode Sans";font-weight:${font.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await require(playwright).chromium.launch({headless:true,channel:'msedge'}),cases=[];
 try {
  assert.equal(browser.version(),reference.browser);
  for(const scheme of ['Light','Dark']){
   const page=await browser.newPage({viewport:{width:640,height:400},deviceScaleFactor:1,reducedMotion:'reduce'});
   try {
    await page.setContent(`<!doctype html><html class="${scheme==='Dark'?'dark':''}"><head><style>${css}body{margin:0}#root{position:absolute;left:20px;top:20px;width:280px}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
    await page.evaluate(async()=>{for(const weight of [400,600])await document.fonts.load(`${weight} 12px "Encode Sans"`);await document.fonts.ready;});
    const session=await page.context().newCDPSession(page); await session.send('Accessibility.enable');
    for(const row of reference.cases.filter(row=>row.scheme===scheme&&row.motion==='reduce')){
     await page.evaluate(spec=>window.showProgressStack(spec),row.spec);
     await page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
     const {nodes}=await session.send('Accessibility.getFullAXTree');
     const texts=nodes.filter(node=>!node.ignored&&node.role?.value==='StaticText').map(node=>node.name.value);
     assert.deepEqual(texts,row.spec.items.flatMap(item=>[item.label,String(Number(item.value))]));
     cases.push({id:row.id,scheme,texts,sourcePngSha256:row.pngSha256,paintedLineBoxes:nodes.filter(node=>!node.ignored&&node.role?.value==='InlineTextBox').map(node=>node.name?.value)});
    }
   } finally {await page.close();}
  }
 } finally {await browser.close();}
 const report={format:'orchard-progress-stack-accessibility-reference-v1',sourceCommit:pin,sourceFile:reference.sourceFile,sourceSha256:hash(sourceBytes),textReferenceSha256:hash(referenceBytes),clientSha256:hash(clientBytes),sourceCssSha256:hash(cssBytes),scriptSha256:hash(await fs.readFile(__filename)),browser:reference.browser,cases};
 await fs.mkdir(output,{recursive:true});await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({passed:true,cases:cases.length,staticTexts:cases.reduce((sum,row)=>sum+row.texts.length,0)}));
})().catch(error=>{console.error(error);process.exitCode=1;});
