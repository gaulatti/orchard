import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
export function verifySeparator(folder,hostAssembly,model){
 const r=model??JSON.parse(fs.readFileSync(path.join(folder,'separator.json'),'utf8'));
 assert.equal(r.format,'orchard-separator-native-v1');assert.equal(r.passed,true);
 assert.equal(r.states.length,32);assert.equal(r.contracts.length,8);assert(r.guards>100);
 for(const field of ['focus','activations'])assert.equal(r[field],0);
 for(const field of ['hardwareInputSent','osSettingsChanged','nativeARM64RuntimeVerified','displayScalingVerified','nativeComponentAccepted'])assert.equal(r[field],false);
 assert.equal(r.processArchitecture,'X64');assert.equal(r.osArchitecture,'X64');assert.equal(r.rasterizationScale,1);
 assert.equal(hash(path.join(folder,'app-identity','Orchard.dll')),r.librarySha256);assert.equal(hash(path.join(folder,'app-identity',hostAssembly)),r.hostSha256);
 const source=path.join(folder,'source-separator.tsx');assert.equal(hash(source),'c6fb193ba83ae952419e55819543f153c267af3a70d06c95dee3eda8a1b2155d');
 const expected=[];let pixels=0,linePixels=0;
 for(const theme of ['Light','Dark'])for(const flow of ['LeftToRight','RightToLeft'])for(const orientation of ['Horizontal','Vertical'])for(const decorative of [true,false])for(const enabled of [true,false])expected.push({theme,flow,orientation,decorative,enabled});
 assert.deepEqual(r.states.map(s=>Object.fromEntries(Object.keys(expected[0]).map(k=>[k,s[k]]))),expected);
 for(const s of r.states){
  const dark=s.theme==='Dark',horizontal=s.orientation==='Horizontal',foreground=dark?255:0,opacity=dark?.1:.05;
  assert.deepEqual(s.color,[foreground,foreground,foreground,255]);assert(Math.abs(s.Opacity-opacity)<.000001);assert.equal(s.tabStop,false);assert.equal(s.controlElement,!s.decorative);assert.equal(s.contentElement,!s.decorative);assert.equal(s.role,'Separator');assert.equal(s.name,'Fictional workspace sections');
  assert.equal(s.box.width,horizontal?320:1);assert.equal(s.box.height,horizontal?1:64);assert.deepEqual(s.line,s.box);
  for(const value of Object.values(s.box))assert(Number.isInteger(value));assert(s.box.x>=0&&s.box.y>=0&&s.box.x+s.box.width<=400&&s.box.y+s.box.height<=160);
  const file=path.join(folder,s.image.file);assert.equal(hash(file),s.image.sha256);const png=readRgbaPng(fs.readFileSync(file));assert.equal(png.width,400);assert.equal(png.height,160);
  // UNORM8 alpha can be quantized before compositing. Bound the result from
  // the two adjacent alpha values; outside the actual line require exact canvas.
  const background=[68,85,102],alphas=[Math.floor(opacity*255)/255,Math.ceil(opacity*255)/255];
  const bounds=background.map(b=>{const values=alphas.map(a=>foreground*a+b*(1-a));return[Math.floor(Math.min(...values)),Math.ceil(Math.max(...values))];});
  for(let y=0;y<160;y++)for(let x=0;x<400;x++){
   const inside=x>=s.box.x&&x<s.box.x+s.box.width&&y>=s.box.y&&y<s.box.y+s.box.height,at=(y*400+x)*4;
   assert.equal(png.rgba[at+3],255);
   for(let c=0;c<3;c++)if(inside)assert(png.rgba[at+c]>=bounds[c][0]&&png.rgba[at+c]<=bounds[c][1],`Wrong line pixel ${s.image.file} ${x},${y},${c}`);else assert.equal(png.rgba[at+c],background[c],`Unexpected paint outside separator: ${s.image.file}`);
   pixels++;if(inside)linePixels++;
  }
 }
 const ids=['caller-bindings','live-caller-values','retemplate','remount'];assert.deepEqual(r.contracts.map(c=>c.theme+'/'+c.id),['Light','Dark'].flatMap(t=>ids.map(id=>t+'/'+id)));
 for(const c of r.contracts){assert(c.passed&&c.originalBrushRetained&&c.bindingsRetained);const index=ids.indexOf(c.id);assert.equal(c.box.width,[137,5,5,7][index]);assert.equal(c.box.height,[3,64,64,48][index]);}
 assert.equal(r.catalogs.length,hostAssembly==='Orchard.Catalog.dll'?2:0);for(const c of r.catalogs)assert(c.passed&&c.controls===4);
 return{passed:true,scope:'native source recipe, complete separator pixels, UIA metadata, live resize/orientation/semantics, caller bindings and template/remount; no hardware input or system settings',states:32,callerContracts:8,pngs:32,pixels,linePixels,librarySha256:r.librarySha256,hostSha256:r.hostSha256,guards:r.guards,focus:0,activations:0,displayScalingVerified:false,nativeARM64RuntimeVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const [folder,host,output]=process.argv.slice(2);const result=verifySeparator(path.resolve(folder),host);fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(result));}
