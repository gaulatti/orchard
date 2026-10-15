import{readFile,writeFile}from'node:fs/promises';
import{createHash}from'node:crypto';
import{isDeepStrictEqual}from'node:util';
import{createRequire}from'node:module';
import{fileURLToPath}from'node:url';
import path from'node:path';
const require=createRequire(import.meta.url),samples=require('./reference/data-table-toolbar-cases.cjs');
const sha=value=>createHash('sha256').update(value).digest('hex');
const sourcePins={
 'src/components/data-table-toolbar.tsx':'a2beb795287fd027857d9f4d12df9d0e76406b0fa0703a9fcf20aaa23064a0ef',
 'src/components/input.tsx':'01bce586edd4c1ad4aed30415b5aa9cbc6e620a874e4751cf7299bfa54ccc192',
 'src/components/button.tsx':'119925584b3ec53c790fb0b762631894cf70eed12d488d5e9ae6743b7a67f929',
 'src/utils/cn.ts':'74e8fe9d0d680c442ed6adb13e7d119d6c210c19ae6c114313b2a72552be0883'
};
const runtimePins={'react':'19.2.4','react-dom':'19.2.4','scheduler':'0.27.0','lucide-react':'0.563.0','clsx':'2.1.1','tailwind-merge':'3.5.0','@tanstack/react-table':'8.21.3','@tanstack/table-core':'8.21.3'};
function requireValue(condition,message){if(!condition)throw Error(message);}
function* nodes(node){yield node;for(const child of node.children??[])yield*nodes(child);}
function decode(value){
 if(value&&typeof value==='object'&&value.$undefined===true)return undefined;
 if(value&&typeof value==='object'&&Object.hasOwn(value,'$number')){switch(value.$number){case'NaN':return NaN;case'-0':return-0;case'Infinity':return Infinity;case'-Infinity':return-Infinity;default:throw Error('Unknown number descriptor.');}}
 if(Array.isArray(value))return value.map(decode);
 return value;
}
export async function validateCapture(capture,dir){
 requireValue(capture.captureVersion===2&&capture.sourceCommit==='a86059013cd59684ce452849278ea81e734f2066'&&capture.sourceSha256===sourcePins['src/components/data-table-toolbar.tsx'],'Frozen toolbar capture differs.');
 requireValue(capture.cases.length===samples.length*2&&new Set(capture.cases.map(c=>c.id+'/'+c.scheme)).size===samples.length*2,'Toolbar cases are missing or duplicated.');
 requireValue(capture.toolVersions.esbuild==='0.25.12'&&capture.toolVersions.playwright==='1.62.1'&&typeof capture.browser==='string'&&capture.browser.length>0,'Capture tooling differs.');
 requireValue(capture.fixtureSignatureSha256===sha(JSON.stringify(samples)),'Frozen fixture matrix differs.');
 for(const[file,key]of[['reference/data-table-toolbar-client.tsx','clientSha256'],['reference/capture-data-table-toolbar.cjs','captureScriptSha256']])requireValue(sha(await readFile(new URL(file,import.meta.url)))===capture[key],'Capture code differs: '+key);
 const frozen=path.resolve(dir,'../line-chart-browser/frozen-source');
 const dependencies=[];for(const[file,pin]of Object.entries(sourcePins)){requireValue(sha(await readFile(path.join(frozen,file)))===pin,'Frozen dependency differs: '+file);dependencies.push({file,sha256:pin});}
 requireValue(isDeepStrictEqual(dependencies.sort((a,b)=>a.file.localeCompare(b.file)),capture.dependencies),'Bundled producer dependencies differ.');
 const css=(await readFile(path.resolve(dir,'../line-chart-browser/styles.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'');requireValue(sha(css)===capture.compiledCssSha256,'Capture stylesheet differs.');
 requireValue(capture.fontFiles.length===3,'Capture fonts are incomplete.');
 for(const font of capture.fontFiles)requireValue(sha(await readFile(new URL('../src/Orchard/Assets/Fonts/'+font.file,import.meta.url)))===font.sha256,'Capture font differs: '+font.file);
 const lock=JSON.parse(await readFile(path.resolve(dir,'../chart-theme-reference/bleecker-package-lock.json'),'utf8')),meta=JSON.parse(await readFile(path.join(dir,'build-meta.json'),'utf8')),packages=new Map();
 for(const[file]of Object.entries(sourcePins))requireValue(Object.keys(meta.inputs).some(input=>path.resolve(input)===path.join(frozen,file)),'Bundle omits producer dependency '+file);
 requireValue(Object.keys(meta.inputs).some(input=>path.resolve(input)===fileURLToPath(new URL('reference/data-table-toolbar-client.tsx',import.meta.url))),'Bundle omits the real-table consumer.');
 for(const input of Object.keys(meta.inputs)){
  const full=path.resolve(input).replaceAll('\\','/'),at=full.lastIndexOf('/node_modules/');if(at<0)continue;
  const suffix=full.slice(at+14).split('/'),name=suffix[0].startsWith('@')?suffix.slice(0,2).join('/'):suffix[0],root=full.slice(0,at+14)+name;if(packages.has(root))continue;
  const manifest=JSON.parse(await readFile(path.join(root,'package.json'),'utf8')),first=full.indexOf('/node_modules/'),installed=JSON.parse(await readFile(full.slice(0,first+14)+'.package-lock.json','utf8')).packages[root.slice(first+1)];
  requireValue(installed&&manifest.version===installed.version&&Object.entries(lock.packages).some(([key,value])=>(key==='node_modules/'+name||key.endsWith('/node_modules/'+name))&&value.version===installed.version&&value.integrity===installed.integrity),'Bundled runtime differs: '+name);
  packages.set(root,{name,version:installed.version,integrity:installed.integrity});
 }
 const runtime=[...packages.values()].sort((a,b)=>a.name.localeCompare(b.name));requireValue(isDeepStrictEqual(runtime,capture.bundledRuntime),'Actual bundle does not match capture runtime.');
 requireValue(runtime.length===Object.keys(runtimePins).length,'Unexpected runtime package count.');
 for(const[name,version]of Object.entries(runtimePins))requireValue(runtime.filter(p=>p.name===name&&p.version===version).length===1,'Unpinned or duplicate runtime: '+name);
 let stateCount=0,screenshotCount=0,receipts=0,uniqueReceipts=0;
 for(const sample of samples)for(const scheme of['Light','Dark']){
  const entry=capture.cases.find(c=>c.id===sample.id&&c.scheme===scheme);requireValue(!!entry,'Missing source case: '+sample.id+'/'+scheme);
  const{id,viewportWidth=1000,hostWidth=800,direction='ltr',customCss='',...props}=sample;
  requireValue(isDeepStrictEqual(entry.props,props)&&entry.viewportWidth===viewportWidth&&entry.hostWidth===hostWidth&&entry.direction===direction&&entry.customCss===customCss,'Source fixture props differ: '+id);
  requireValue(entry.errors.length===0&&entry.consoleMessages.length===0,'Source runtime failed: '+id);
  let column=props.searchColumn;
  for(const{action,state,presentation}of[{action:'normal',state:entry.normal,presentation:entry.normalPresentation},...entry.states]){
   requireValue(presentation.stable>=3&&presentation.frames>=4&&presentation.milliseconds<2000,'Source paint did not settle: '+id+'/'+action);
   if(action==='search-column-change')column='status';if(action==='search-column-hidden')column='';if(action==='replace-table')column='category';if(action==='restore-table')column='title';
   requireValue(state.surface.tag==='DIV'&&state.table.sameTable&&state.table.columns.every(c=>c.same)&&state.table.rows.every(row=>row.same),'Source table/column/row identity was lost: '+id);
   requireValue(state.visibleRows===state.table.rows.map(row=>row.title).join('|'),'Filtered rows did not reach the consumer UI: '+id);
   requireValue(state.table.globalTruthy===Boolean(decode(state.table.state.globalFilter)),'Global-filter truthiness differs: '+id);
   const all=[...nodes(state.surface)],inputs=all.filter(n=>n.tag==='INPUT'),reset=all.filter(n=>n.tag==='BUTTON'&&n.text==='Reset');
   requireValue(reset.length===Number(state.table.state.columnFilters.length>0||state.table.globalTruthy),'Reset visibility differs: '+id+'/'+action);
   requireValue(inputs.length===Number(Boolean(column)),'Search-column visibility differs: '+id+'/'+action);
   if(inputs.length){const value=decode(state.table.columns.find(c=>c.id===column)?.value);requireValue(inputs[0].value===String(value??''),'Controlled input value differs: '+id+'/'+action);}
   requireValue(state.inputEvents.every(event=>event.trusted===true),'Untrusted source input: '+id+'/'+action);receipts+=state.inputEvents.length;
   requireValue(state.events.filter(event=>event.kind==='slot').every(event=>event.sameTable&&event.sameRows),'Consumer slot lost original data/table: '+id);
   if(['reset-release','reset-keyboard','reset-click','reset-Enter'].includes(action))requireValue(isDeepStrictEqual(state.table.state.globalFilter,entry.normal.table.state.globalFilter)&&isDeepStrictEqual(state.table.state.columnFilters,entry.normal.table.initialColumnFilters),'Reset did not preserve global/restore initial columns: '+id+'/'+action);
  }
  stateCount+=entry.states.length;
  uniqueReceipts+=Math.max(entry.normal.inputEvents.length,...entry.states.map(s=>s.state.inputEvents.length));
  for(const screenshot of entry.screenshots){requireValue(screenshot.normalMatchesFullCapture===true&&sha(await readFile(path.join(dir,screenshot.file)))===screenshot.sha256,'Initial screenshot differs: '+id);screenshotCount++;}
 }
 requireValue(stateCount===376&&screenshotCount===14&&uniqueReceipts>1000,'Input/state/screenshot matrix is incomplete.');
 return{cases:capture.cases.length,states:stateCount,screenshots:screenshotCount,receipts:uniqueReceipts,receiptSnapshots:receipts};
}
export function internCapture(capture){
 const styles=[],indices=new Map();
 function encode(value){if(Array.isArray(value))return value.map(encode);if(!value||typeof value!=='object')return value;requireValue(!Object.hasOwn(value,'styleIndex'),'Style-table key collision.');return Object.fromEntries(Object.entries(value).map(([key,item])=>{if(key!=='style'||!item||typeof item!=='object')return[key,encode(item)];const signature=JSON.stringify(item);if(!indices.has(signature)){indices.set(signature,styles.length);styles.push(item);}return['styleIndex',indices.get(signature)];}));}
 function decode(value){if(Array.isArray(value))return value.map(decode);if(!value||typeof value!=='object')return value;return Object.fromEntries(Object.entries(value).map(([key,item])=>key==='styleIndex'?['style',styles[item]]:[key,decode(item)]));}
 const encoded=encode(capture);requireValue(isDeepStrictEqual(capture,decode(encoded)),'Style interning changed the capture.');return{encoded,styles};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 if(!process.argv[2])throw Error('Pass the unchanged full toolbar browser capture JSON.');
 const bytes=await readFile(process.argv[2]),capture=JSON.parse(bytes),evidence=await validateCapture(capture,path.dirname(path.resolve(process.argv[2]))),{encoded,styles}=internCapture(capture);
 const result=JSON.stringify({format:'orchard-data-table-toolbar-reference-v1',fullCaptureSha256:sha(bytes),...encoded,styles},null,2)+'\n';
 await writeFile(new URL('../tests/Reference/DataTableToolbarLayout.json',import.meta.url),result);console.log(JSON.stringify({...evidence,styles:styles.length,fullCaptureSha256:sha(bytes),sha256:sha(result),bytes:Buffer.byteLength(result)}));
}
