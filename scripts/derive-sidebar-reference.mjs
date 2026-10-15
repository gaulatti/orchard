import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {isDeepStrictEqual} from 'node:util';
import {fileURLToPath} from 'node:url';
import path from 'node:path';

if(!process.argv[2])throw Error('Pass the unchanged Sidebar browser capture JSON.');
const bytes=await readFile(process.argv[2]),capture=JSON.parse(bytes),dir=path.dirname(path.resolve(process.argv[2]));
const sha=value=>createHash('sha256').update(value).digest('hex');
if(capture.captureVersion!==4||capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='2c06422943eedda2232a04c087b8f8562f614829f2b77229ecc7941488894856'||capture.react!=='19.2.4'||capture.collapsible!=='1.1.14'||capture.cases.length!==82||new Set(capture.cases.map(c=>c.id+'/'+c.scheme)).size!==82)throw Error('Frozen Sidebar capture differs.');
const source=path.resolve(dir,'../line-chart-browser/frozen-source/src/components/sidebar.tsx');
if(sha(await readFile(source))!==capture.sourceSha256)throw Error('Frozen Sidebar source differs.');
const css=(await readFile(path.resolve(dir,'../line-chart-browser/styles.css'),'utf8')).replace(/@import url\([^\r\n]*\);/g,'');
if(sha(css)!==capture.compiledCssSha256)throw Error('Capture stylesheet differs.');
for(const font of capture.fontFiles)if(sha(await readFile(new URL('../src/Orchard/Assets/Fonts/'+font.file,import.meta.url)))!==font.sha256)throw Error('Capture font differs: '+font.file);
const lock=JSON.parse(await readFile(path.resolve(dir,'../chart-theme-reference/bleecker-package-lock.json'),'utf8'));
const meta=JSON.parse(await readFile(path.join(dir,'build-meta.json'),'utf8')),packages=new Map();
if(!Object.keys(meta.inputs).some(input=>path.resolve(input)===source)||!Object.keys(meta.inputs).some(input=>path.resolve(input)===fileURLToPath(new URL('reference/sidebar-client.tsx',import.meta.url))))throw Error('Bundle does not contain the frozen Sidebar and checked-in client.');
for(const input of Object.keys(meta.inputs)){
 const full=path.resolve(input).replaceAll('\\','/'),at=full.lastIndexOf('/node_modules/');if(at<0)continue;
 const suffix=full.slice(at+14).split('/'),name=suffix[0].startsWith('@')?suffix.slice(0,2).join('/'):suffix[0],root=full.slice(0,at+14)+name;
 if(packages.has(root))continue;
 const manifest=JSON.parse(await readFile(path.join(root,'package.json'),'utf8')),first=full.indexOf('/node_modules/');
 const installed=JSON.parse(await readFile(full.slice(0,first+14)+'.package-lock.json','utf8')).packages[root.slice(first+1)];
 if(!installed||installed.version!==manifest.version||!Object.entries(lock.packages).some(([key,value])=>(key==='node_modules/'+name||key.endsWith('/node_modules/'+name))&&value.version===installed.version&&value.integrity===installed.integrity))throw Error('Bundled runtime differs: '+root);
 packages.set(root,{name,version:installed.version,integrity:installed.integrity});
}
if(!isDeepStrictEqual([...packages.values()].sort((a,b)=>a.name.localeCompare(b.name)),capture.bundledRuntime))throw Error('Capture runtime does not match its actual bundle.');
for(const [name,version]of[['react','19.2.4'],['react-dom','19.2.4'],['lucide-react','0.563.0'],['@radix-ui/react-collapsible','1.1.14']])if(capture.bundledRuntime.filter(p=>p.name===name&&p.version===version).length!==1||capture.bundledRuntime.filter(p=>p.name===name).length!==1)throw Error('Unpinned or duplicate runtime: '+name);
for(const sample of capture.cases){
 if(sample.errors.length||!['Light','Dark'].includes(sample.scheme)||!capture.cases.some(c=>c.id===sample.id&&c.scheme!==sample.scheme)||sample.normal.surface.tag!=='ASIDE'||sample.normal.nav.tag!=='NAV')throw Error('Source case failed or lost its surface: '+sample.id);
 for(const state of [sample.normal,...sample.states.map(s=>s.state)]){
  if(state.events.some(event=>event.kind!=='slot'&&event.same!==true)||state.renderCalls.some(call=>!call.same||!call.iconSame||!call.badgeSame||!call.labelSame)||state.inputEvents.some(event=>event.trusted!==true))throw Error('Source identity or actual input receipt failed: '+sample.id);
 }
 if(sample.motion&&(!sample.motionFrames.some(frame=>frame.width>64&&frame.width<256)||sample.motionFrames.at(-1).width!==64))throw Error('Source width motion was not presented: '+sample.id);
}
// Intern repeated computed-style objects without discarding any captured field.
// Reconstruct and compare the entire capture before publishing the reference.
const styles=[],indices=new Map();
function encode(value){
 if(Array.isArray(value))return value.map(encode);
 if(!value||typeof value!=='object')return value;
 if(Object.hasOwn(value,'styleIndex'))throw Error('Capture collides with the style-table format.');
 return Object.fromEntries(Object.entries(value).map(([key,item])=>{
  if(key!=='style'||!item||typeof item!=='object')return[key,encode(item)];
  const signature=JSON.stringify(item);if(!indices.has(signature)){indices.set(signature,styles.length);styles.push(item);}
  return['styleIndex',indices.get(signature)];
 }));
}
function decode(value){
 if(Array.isArray(value))return value.map(decode);
 if(!value||typeof value!=='object')return value;
 return Object.fromEntries(Object.entries(value).map(([key,item])=>key==='styleIndex'?['style',styles[item]]:[key,decode(item)]));
}
const encoded=encode(capture);if(!isDeepStrictEqual(capture,decode(encoded)))throw Error('Style interning changed the captured contract.');
const result=JSON.stringify({format:'orchard-sidebar-reference-v1',fullCaptureSha256:sha(bytes),...encoded,styles},null,2)+'\n';
await writeFile(new URL('../tests/Reference/SidebarLayout.json',import.meta.url),result);
console.log(JSON.stringify({cases:capture.cases.length,styles:styles.length,fullCaptureSha256:sha(bytes),sha256:sha(result),bytes:Buffer.byteLength(result)}));
