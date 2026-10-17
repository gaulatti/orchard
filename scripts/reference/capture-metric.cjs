const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const output=option('--output'),esbuildPath=option('--esbuild-module'),playwrightPath=option('--playwright-module');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const base=path.resolve('artifacts/line-chart-browser'),source=path.join(base,'frozen-source/src/components/metric.tsx'),formatter=path.join(base,'frozen-source/src/utils/format.ts'),runtime=path.join(base,'node_modules');
 assert.equal(hash(await fs.readFile(source)),'2aeb702dc6a115a9b24ff62842d564a9fcd92f1069925b3e07929532daca47c7'); assert.equal(hash(await fs.readFile(formatter)),'e7c2b863087ac5e0c06973e27abbcab597f745b0540cd8698def589a8fe7129c');
 const lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes),installed=JSON.parse(await fs.readFile(path.join(base,'package-lock.json')));
 for(const[key,value]of Object.entries(installed.packages))if(key){assert.equal(value.version,lock.packages[key]?.version);assert.equal(value.integrity,lock.packages[key]?.integrity);}
 assert.equal(require(path.join(esbuildPath,'package.json')).version,'0.25.12');assert.equal(require(path.join(playwrightPath,'package.json')).version,'1.62.1');
 const bundle=await require(esbuildPath).build({alias:{react:path.join(runtime,'react'),'react-dom':path.join(runtime,'react-dom')},stdin:{contents:`import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{Metric}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));window.show=p=>flushSync(()=>root.render(<Metric {...p}/>));`,loader:'tsx',resolveDir:process.cwd()},bundle:true,write:false,platform:'browser',jsx:'automatic',nodePaths:[runtime],define:{'process.env.NODE_ENV':'"production"'}});
 const cssBytes=await fs.readFile(path.join(base,'styles.css')),font=await fs.readFile('src/Orchard/Assets/Fonts/EncodeSans-Regular.ttf');
 const css=cssBytes.toString().replace(/@import url\([^\r\n]*\);/g,'')+`@font-face{font-family:'Encode Sans';font-weight:400;src:url(data:font/ttf;base64,${font.toString('base64')})}html,body{background:var(--color-background)}body{margin:0;font-family:'Encode Sans';font-size:16px;line-height:24px}#root{margin:20px}#root>span{color:var(--color-foreground)}`;
 const values=[null,'NaN',0,'-0',1,-1,1.005,-1.005,12.3456,999.5,9999,1234567.89,'Infinity','-Infinity'];
 const specs=values.flatMap((value,i)=>['number','currency','percent','compact'].map(format=>({id:`${format}-${i}`,value,format}))).concat([
  ...['USD','EUR','JPY','KWD','ZZZ'].flatMap(currency=>[undefined,0,3].map(decimals=>({id:`currency-${currency}-${decimals??'default'}`,value:1234.567,currency,format:'currency',decimals}))),
  ...['number','percent','compact'].flatMap(format=>[0,2,6].map(decimals=>({id:`${format}-fixed-${decimals}`,value:1234.567,format,decimals}))),
  {id:'slots',value:1248,prefix:'~ ',suffix:' reviews'},{id:'empty-slots',value:null,prefix:'[',suffix:']'},
  {id:'long',value:1234567890123456,suffix:' consumer-owned reviews and local checks'},
  {id:'negative-half',value:-2.5,decimals:0},{id:'positive-half',value:2.5,decimals:0}
 ]);
 const browser=await require(playwrightPath).chromium.launch({headless:true,channel:'msedge'});assert.equal(browser.version(),'155.0.4283.45');const cases=[];
 try{for(const locale of['en-US','de-DE','ja-JP','ar-EG']){
  const page=await browser.newPage({locale,viewport:{width:320,height:120},deviceScaleFactor:1,reducedMotion:'reduce'});const errors=[];page.on('pageerror',e=>errors.push(e.message));
  try{await page.setContent(`<html><head><style>${css}</style></head><body><div id='root'></div><script>${bundle.outputFiles[0].text}</script></body></html>`);await page.evaluate(async()=>{await document.fonts.load('400 16px "Encode Sans"');await document.fonts.ready;});
   for(const scheme of['Light','Dark']){await page.evaluate(s=>document.documentElement.classList.toggle('dark',s==='Dark'),scheme);
    for(const spec of specs){await page.evaluate(p=>{const value=p.value==='NaN'?NaN:p.value==='Infinity'?Infinity:p.value==='-Infinity'?-Infinity:p.value==='-0'?-0:p.value;window.show({...p,value});},spec);
     const state=await page.evaluate(()=>{const e=document.querySelector('#root>span'),s=getComputedStyle(e),b=e.getBoundingClientRect();return{text:e.textContent,box:{x:b.x,y:b.y,width:b.width,height:b.height},font:s.fontFamily,fontSize:s.fontSize,lineHeight:s.lineHeight,spacing:s.letterSpacing,numerals:s.fontVariantNumeric,color:s.color};});
     let file,pngSha256;if(locale==='en-US'&&['number-8','currency-8','compact-11','slots','empty-slots','long'].includes(spec.id)){file=`metric-${spec.id}-${scheme}.png`;await page.screenshot({path:path.join(output,file)});pngSha256=hash(await fs.readFile(path.join(output,file)));}
     cases.push({locale,scheme,...spec,state,...(file?{file,pngSha256}:{})});
    }
   }assert.deepEqual(errors,[]);
  }finally{await page.close();}
 }}finally{await browser.close();}
 const report={format:'orchard-metric-v1',sourceCommit:'a86059013cd59684ce452849278ea81e734f2066',sourceSha256:hash(await fs.readFile(source)),formatterSha256:hash(await fs.readFile(formatter)),scriptSha256:hash((await fs.readFile(__filename,'utf8')).replace(/\r\n/g,'\n')),cssSha256:hash(cssBytes),frozenLockSha256:hash(lockBytes),fontSha256:hash(font),browser:browser.version(),cases};
 await fs.writeFile(path.join(output,'reference.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,formatterSha256:report.formatterSha256}));
})().catch(e=>{console.error(e);process.exitCode=1});
