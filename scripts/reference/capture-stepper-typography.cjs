const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module'),hash=value=>crypto.createHash('sha256').update(value).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});const referenceBytes=await fs.readFile('tests/Reference/Stepper.json'),reference=JSON.parse(referenceBytes);
 const source=path.resolve('artifacts/line-chart-browser/frozen-source/src/components/stepper.tsx');assert.equal(hash(await fs.readFile(source)),reference.sourceSha256);
 const runtime=path.resolve('artifacts/line-chart-browser/node_modules'),icons=path.resolve('artifacts/error-state-browser/node_modules');
 for(const name of ['react','react-dom'])assert.equal(require(path.join(runtime,name,'package.json')).version,reference.react);
 assert.equal(require(path.join(icons,'lucide-react/package.json')).version,reference.lucide);
 assert.equal(require(path.join(esbuildPath,'package.json')).version,reference.esbuild);assert.equal(require(path.join(playwrightPath,'package.json')).version,reference.playwright);
 const bundle=await require(esbuildPath).build({stdin:{contents:`import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{Stepper}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));window.show=p=>flushSync(()=>root.render(<Stepper {...p} onStepClick={()=>{}}/>));`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',nodePaths:[runtime,icons],alias:{react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile('artifacts/line-chart-browser/styles.css');assert.equal(hash(cssBytes),reference.cssSha256);
 let fonts='';for(const font of reference.fontFiles){const bytes=await fs.readFile('src/Orchard/Assets/Fonts/'+font.file);assert.equal(hash(bytes),font.sha256);fonts+=`@font-face{font-family:'Encode Sans';font-weight:${font.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;}
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),reference.browser);const reports=[];
 try{for(const test of reference.cases){
  const page=await browser.newPage({viewport:{width:1024,height:745},deviceScaleFactor:1,reducedMotion:'reduce'});
  try{
   await page.setContent(`<html class="${test.scheme==='Dark'?'dark':''}"><head><style>${cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'')}${fonts}body{padding:20px;margin:0}#root{width:${test.width}px;direction:${test.direction}}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
   await page.evaluate(p=>window.show(p),test.props);await page.evaluate(async()=>{await Promise.all([400,500,600].map(w=>document.fonts.load(w+' 14px "Encode Sans"')));});
   const rows=await page.locator('#root').evaluate(root=>{
    const rootBox=root.getBoundingClientRect();
    const text=p=>{
     if(!p)return null;const style=getComputedStyle(p),box=p.getBoundingClientRect();
     const range=document.createRange();range.selectNodeContents(p);const rects=[...range.getClientRects()].map(r=>({x:r.x-rootBox.x,y:r.y-rootBox.y,width:r.width,height:r.height}));
     if(box.height===0)return{text:p.textContent,lineHeight:style.lineHeight,firstBaseline:null,rects};
     const node=p.firstChild,index=node.textContent.search(/[^ \t\r\n\f]/);if(node.nodeType!==Node.TEXT_NODE||index<0)throw Error('Expected a nonempty original text node.');
     const character=String.fromCodePoint(node.textContent.codePointAt(index));range.setStart(node,index);range.setEnd(node,index+character.length);const first=range.getBoundingClientRect();
     const context=document.createElement('canvas').getContext('2d');context.font=style.font;const metrics=context.measureText(character);
     if(Math.abs(first.height-metrics.fontBoundingBoxAscent-metrics.fontBoundingBoxDescent)>.001)throw Error('DOM range and measured font line boxes disagree.');
     return{text:p.textContent,lineHeight:style.lineHeight,lines:Math.round(box.height/parseFloat(style.lineHeight)),firstBaseline:first.y-rootBox.y+metrics.fontBoundingBoxAscent,rangeAscent:metrics.fontBoundingBoxAscent,rangeDescent:metrics.fontBoundingBoxDescent,rects,font:style.font,fontWeight:style.fontWeight};
    };
    const buttonTypography=button=>button.querySelector('svg')?null:{...text(button),lines:1};
    return[...root.firstElementChild.children].map(row=>({label:text(row.lastElementChild.children[0]),description:text(row.lastElementChild.children[1]),button:buttonTypography(row.querySelector('button'))}));
   });
   reports.push({id:test.id,scheme:test.scheme,rows});
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-stepper-typography-reference-v1',sourceCommit:reference.sourceCommit,sourceSha256:reference.sourceSha256,normalReferenceSha256:hash(referenceBytes),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),scriptNormalization:'lf',browser:reference.browser,fontFiles:reference.fontFiles,reports};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:reports.length,defaultLight:reports[0]}));
})().catch(error=>{console.error(error);process.exitCode=1});
