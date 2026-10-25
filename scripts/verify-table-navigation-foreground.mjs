import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export const navigationForegroundStages=['first','middle','last','single-page','source-restored','host-disabled','host-enabled','live-theme','theme-restored','unloaded','reloaded','template-cleared','template-restored','caller-template','caller-presenter-change','button-template-restored','retired-presenter-change','direct-content-template','direct-button-change','direct-template-restored','caller-foreground','caller-foreground-change','caller-bound-disabled','caller-bound-restored'];
const detached=new Set(['unloaded','template-cleared']);
function brush(b){assert.equal(b.rgba.length,4);for(const c of b.rgba)assert(Number.isInteger(c)&&c>=0&&c<=255);assert(Number.isFinite(b.opacity)&&b.opacity>=0&&b.opacity<=1);}
export function verifyTableNavigationForegroundReport(n){
 assert.equal(n.format,'orchard-table-navigation-foreground-native-v1');assert.equal(n.executionCompleted,true);assert.equal(typeof n.passed,'boolean');assert.equal(n.states.length,96);assert.equal(n.pngs.length,88);assert(n.guards>=n.pngs.length*8);assert.equal(n.focus,0);assert.equal(n.activations,0);assert.equal(n.publicApisOnly,true);
 for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(n[key],false,key);
 const expected=[];for(const scheme of ['Light','Dark'])for(const kind of ['data-table','pagination'])for(const name of navigationForegroundStages)expected.push([scheme,kind,name]);assert.deepEqual(n.states.map(s=>[s.scheme,s.kind,s.name]),expected);
 let observations=0,disabled=0,brushFailures=0,releaseFailures=0;const files=[];
 for(const s of n.states){assert.equal(s.loaded,!detached.has(s.name));if(!s.loaded){assert.equal(s.retiredGlyphs,s.kind==='data-table'?2:4);assert.equal(s.retiredGlyphsUnloaded,true);assert.equal(typeof s.ownedBindingsReleased,'boolean');releaseFailures+=s.ownedBindingsReleased?0:1;continue;}
  assert.equal(s.actualTheme,s.name==='live-theme'?(s.scheme==='Light'?'Dark':'Light'):s.scheme);assert.equal(s.pageCount,s.name==='single-page'?1:5);assert.equal(s.pageIndex,['first','single-page'].includes(s.name)?0:s.name==='last'?s.pageCount-1:1);assert.equal(s.hostEnabled,!['host-disabled','caller-bound-disabled'].includes(s.name));assert.equal(s.callerBindingExpected,['caller-foreground','caller-foreground-change','caller-bound-disabled','caller-bound-restored'].includes(s.name));assert.equal(s.directContentTemplate,['direct-content-template','direct-button-change'].includes(s.name));assert.equal(s.callerTemplate,['caller-template','caller-presenter-change','direct-content-template','direct-button-change'].includes(s.name));
  const names=s.kind==='data-table'?['PART_Previous','PART_Next']:['PART_First','PART_Previous','PART_Next','PART_Last'];assert.deepEqual(s.buttons.map(b=>b.name),names);
  const file=`table-navigation-foreground-${s.scheme}-${s.kind}-${s.name}.png`;assert.equal(s.file,file);files.push(file);
  for(const [index,b]of s.buttons.entries()){observations++;assert.equal(b.expectedEnabled,s.hostEnabled&&(['PART_Previous','PART_First'].includes(b.name)?s.pageIndex>0:s.pageIndex<s.pageCount-1));assert.equal(b.enabled,b.expectedEnabled);assert.equal(typeof b.enabled,'boolean');assert.equal(typeof b.brushMatches,'boolean');brush(b.foreground);brush(b.stroke);const same=JSON.stringify(b.foreground)===JSON.stringify(b.stroke);assert.equal(b.brushMatches,same);brushFailures+=same?0:1;disabled+=b.enabled?0:1;
   for(const box of [b.glyph,b.button]){for(const key of ['x','y','width','height'])assert(Number.isFinite(box[key]));assert(box.width>0&&box.height>0);}assert.equal(b.glyph.width,16);assert.equal(b.glyph.height,16);assert.equal(b.button.width,32);assert.equal(b.button.height,32);
   if(!b.enabled)assert(Math.abs(b.foreground.opacity-.45)<1e-7,'Native disabled foreground must retain the pinned45percent recipe');
   if(s.callerBindingExpected&&index===0)assert.equal(b.callerForegroundBindingRetained,true);
   assert.equal(b.producerKind,s.directContentTemplate&&index===0?'button':'content-presenter');
   if(['caller-template','caller-presenter-change'].includes(s.name)&&index===0)assert.equal(b.presenterName,'CallerPresenter');
   if(s.directContentTemplate&&index===0)assert.equal(b.presenterName,b.name);
  }
 }
 assert.equal(observations,264);assert(disabled>0);assert.equal(new Set(files).size,88);assert.deepEqual(n.pngs.map(p=>p.file).sort(),files.sort());assert.equal(n.failures.length,brushFailures+releaseFailures);assert.equal(n.passed,brushFailures+releaseFailures===0);
 return{captureValid:true,states:96,frames:88,observations,disabledObservations:disabled,brushFailures,retiredBindingFailures:releaseFailures};
}
export function verifyTableNavigationForegroundPaint(s,png){
 const violations=[];for(const b of s.buttons){const box=b.glyph,x=Math.floor(box.x+1),y=Math.floor(box.y+1);assert(x>=0&&y>=0&&x<png.width&&y<png.height);const offset=(y*png.width+x)*4,bg=[...png.rgba.subarray(offset,offset+4)];assert.equal(bg[3],255);
  const a=b.foreground.opacity*b.foreground.rgba[3]/255,composite=bg.slice(0,3).map((v,c)=>v*(1-a)+b.foreground.rgba[c]*a),lo=composite.map((v,c)=>Math.floor(Math.min(v,bg[c]))-1),hi=composite.map((v,c)=>Math.ceil(Math.max(v,bg[c]))+1);let pixels=0,max=0,inkPixels=0;
  for(let py=Math.ceil(box.y);py<Math.floor(box.y+box.height);py++)for(let px=Math.ceil(box.x);px<Math.floor(box.x+box.width);px++){assert(px>=0&&py>=0&&px<png.width&&py<png.height);const i=(py*png.width+px)*4;let excess=false,ink=false;for(let c=0;c<3;c++){const d=Math.max(lo[c]-png.rgba[i+c],png.rgba[i+c]-hi[c],0);max=Math.max(max,d);excess||=d>0;ink||=Math.abs(png.rgba[i+c]-bg[c])>1;}pixels+=excess?1:0;inkPixels+=ink?1:0;}
  if(pixels||inkPixels<8)violations.push({scheme:s.scheme,kind:s.kind,stage:s.name,button:b.name,enabled:b.enabled,pixels,maximumExcess:max,inkPixels,missingGlyph:inkPixels<8,background:bg,expectedMinimumRgb:lo,expectedMaximumRgb:hi});
 }
 return violations;
}
export async function auditTableNavigationForeground({native,hostAssembly}){
 const bytes=await fs.readFile(path.join(native,'table-navigation-foreground.json')),n=JSON.parse(bytes),summary=verifyTableNavigationForegroundReport(n);assert.equal(hash(await fs.readFile(path.join(native,'app-identity','Orchard.dll'))),n.librarySha256);assert.equal(hash(await fs.readFile(path.join(native,'app-identity',hostAssembly))),n.hostSha256);
 const exit=JSON.parse(await fs.readFile(path.join(native,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.timedOut,false);assert.equal(exit.exitCode,n.passed?0:1);
 const paintFailures=[];let pixels=0;for(const entry of n.pngs){const p=await fs.readFile(path.join(native,entry.file));assert.equal(hash(p),entry.sha256);const png=readRgbaPng(p);assert.equal(png.width,1000);assert.equal(png.height,1100);assert.equal(entry.width,1000);assert.equal(entry.height,1100);const state=n.states.find(s=>s.file===entry.file);assert(state);paintFailures.push(...verifyTableNavigationForegroundPaint(state,png));pixels+=png.width*png.height;}
 return{format:'orchard-table-navigation-foreground-audit-v1',...summary,passed:n.passed&&paintFailures.length===0,paintFailures:paintFailures.length,paintFailureDetails:paintFailures,rawPixelsVerified:pixels,librarySha256:n.librarySha256,hostSha256:n.hostSha256,reportSha256:hash(bytes),fullRasterParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};const result=await auditTableNavigationForeground({native:option('--native'),hostAssembly:option('--host-assembly')});await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({...result,paintFailureDetails:undefined}));process.exitCode=result.passed?0:1;}
