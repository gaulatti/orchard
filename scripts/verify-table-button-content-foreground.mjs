import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {verifyTableNavigationForegroundPaint} from './verify-table-navigation-foreground.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const contract=JSON.parse(await fs.readFile(new URL('../tests/Reference/DataTableNavigationPaint.json',import.meta.url)));
const source=await fs.readFile(new URL('../'+contract.buttonSource,import.meta.url));
assert.equal(hash(source),contract.buttonSourceSha256);assert.equal(Number(source.toString().match(/disabled:opacity-(\d+)/)[1])/100,contract.disabledOpacity);
export const buttonContentStages=['normal','disabled','reenabled','caller-presenter','template-restored','unloaded'];
function brush(v){assert.equal(v.rgba.length,4);assert(v.rgba.every(c=>Number.isInteger(c)&&c>=0&&c<=255));assert(Number.isFinite(v.opacity)&&v.opacity>=0&&v.opacity<=1);}
export function verifyTableButtonContentForegroundReport(n){
 assert.equal(n.format,'orchard-table-button-content-foreground-native-v1');assert.equal(n.executionCompleted,true);assert.equal(typeof n.passed,'boolean');assert.equal(n.states.length,24);assert.equal(n.pngs.length,20);assert(n.guards>=100);assert.equal(n.focus,0);assert.equal(n.activations,0);assert.equal(n.publicApisOnly,true);
 for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(n[key],false,key);
 const expected=[];for(const scheme of ['Light','Dark'])for(const kind of ['toolbar','column-toggle'])for(const name of buttonContentStages)expected.push([scheme,kind,name]);assert.deepEqual(n.states.map(s=>[s.scheme,s.kind,s.name]),expected);
 let observations=0,brushFailures=0,releaseFailures=0;const files=[];
 for(const s of n.states){assert.equal(s.loaded,s.name!=='unloaded');if(!s.loaded){assert.equal(s.retiredContentUnloaded,true);assert.equal(typeof s.ownedForegroundBindingsReleased,'boolean');releaseFailures+=s.ownedForegroundBindingsReleased?0:1;continue;}
  assert.equal(s.actualTheme,s.scheme);assert.equal(s.enabled,s.name!=='disabled');assert.equal(s.caption,s.kind==='toolbar'?'Reset':'Columns');assert.equal(s.fontBindingRetained,true);
  const roles=['caption',...Array.from({length:s.kind==='toolbar'?1:3},(_,i)=>'glyph-'+i)];assert.deepEqual(s.observations.map(v=>v.role),roles);
  for(const box of [s.captionBox,s.glyph,s.button]){for(const key of ['x','y','width','height'])assert(Number.isFinite(box[key]));assert(box.width>0&&box.height>0);}assert.equal(s.glyph.width,14);assert.equal(s.glyph.height,14);
  if(s.name==='caller-presenter')assert.equal(s.presenterName,'CallerPresenter');
  for(const v of s.observations){observations++;brush(v.foreground);if(v.actual!==null)brush(v.actual);const same=v.actual!==null&&JSON.stringify(v.foreground)===JSON.stringify(v.actual);assert.equal(v.brushMatches,same);brushFailures+=same?0:1;if(!s.enabled)assert(Math.abs(v.foreground.opacity-contract.disabledOpacity)<1e-7);if(s.name==='caller-presenter'){assert.deepEqual(v.foreground.rgba,[145,112,184,255]);assert.equal(v.foreground.opacity,1);}}
  const file=`table-button-content-foreground-${s.scheme}-${s.kind}-${s.name}.png`;assert.equal(s.file,file);files.push(file);
 }
 assert.equal(observations,60);assert.equal(new Set(files).size,20);assert.deepEqual(n.pngs.map(p=>p.file).sort(),files.sort());assert.equal(n.failures.length,brushFailures+releaseFailures);assert.equal(n.passed,brushFailures+releaseFailures===0);
 return{captureValid:true,states:24,frames:20,observations,brushFailures,retiredBindingFailures:releaseFailures};
}
export function verifyTableButtonContentForegroundPaint(s,png){
 return verifyTableNavigationForegroundPaint({...s,buttons:[{name:'caption',enabled:s.enabled,glyph:s.captionBox,foreground:s.observations[0].foreground},{name:'glyph',enabled:s.enabled,glyph:s.glyph,foreground:s.observations[1].foreground}]},png);
}
export async function auditTableButtonContentForeground({native,hostAssembly}){
 const bytes=await fs.readFile(path.join(native,'table-button-content-foreground.json')),n=JSON.parse(bytes),summary=verifyTableButtonContentForegroundReport(n);
 for(const [file,expected]of [['Orchard.dll',n.librarySha256],[hostAssembly,n.hostSha256]])assert.equal(hash(await fs.readFile(path.join(native,'app-identity',file))),expected);
 const exit=JSON.parse(await fs.readFile(path.join(native,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.timedOut,false);assert.equal(exit.exitCode,n.passed?0:1);
 const paintFailures=[];for(const p of n.pngs){const data=await fs.readFile(path.join(native,p.file));assert.equal(hash(data),p.sha256);const png=readRgbaPng(data);assert.equal(png.width,1000);assert.equal(png.height,240);assert.equal(p.width,1000);assert.equal(p.height,240);paintFailures.push(...verifyTableButtonContentForegroundPaint(n.states.find(s=>s.file===p.file),png));}
 return{...summary,passed:n.passed&&paintFailures.length===0,paintFailures:paintFailures.length,paintFailureDetails:paintFailures,librarySha256:n.librarySha256,hostSha256:n.hostSha256,reportSha256:hash(bytes),nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};const result=await auditTableButtonContentForeground({native:option('--native'),hostAssembly:option('--host-assembly')});await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({...result,paintFailureDetails:undefined}));process.exitCode=result.passed?0:1;}
