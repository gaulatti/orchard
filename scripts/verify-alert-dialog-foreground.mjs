import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {verifyTableNavigationForegroundPaint} from './verify-table-navigation-foreground.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function brush(v){assert(v&&v.rgba.length===4);assert(v.rgba.every(x=>Number.isInteger(x)&&x>=0&&x<=255));assert(Number.isFinite(v.opacity)&&v.opacity>=0&&v.opacity<=1);}
export function verifyAlertDialogForegroundReport(n,reference){
 assert.equal(n.format,'orchard-alert-dialog-native-v1');assert.equal(n.passed,true,'Existing modal geometry/ownership checks must pass');assert.equal(n.failures.length,0);assert.equal(n.cases.length,8);assert(n.guards>=100);assert.equal(n.focus,0);assert.equal(n.activations,0);
 for(const key of ['activateCalled','showCalled','focusCalled','physicalInputSent','nativeInputVerified','sourceRasterParityVerified'])assert.equal(n[key],false,key);
 assert.deepEqual(n.cases.map(c=>[c.id,c.scheme]),reference.cases.map(c=>[c.id,c.scheme]));let brushFailures=0,observations=0;
 for(const c of n.cases){assert.equal(c.passed,true);assert.equal(c.confirmEnabled,false);assert.equal(c.cancelEnabled,false);assert.deepEqual(c.buttonForegrounds.map(v=>v.role),['cancel-label','confirm-label','confirm-loader']);
  for(const v of c.buttonForegrounds){observations++;assert.equal(v.enabled,false);brush(v.foreground);if(v.actual!==null)brush(v.actual);assert(Math.abs(v.foreground.opacity-.45)<1e-7,'Rendered foreground must retain source disabled opacity');brushFailures+=JSON.stringify(v.foreground)===JSON.stringify(v.actual)?0:1;for(const k of ['x','y','width','height'])assert(Number.isFinite(v.box[k]));assert(v.box.width>0&&v.box.height>0);if(v.role==='confirm-loader')assert.equal(v.visible,true);}
 }
 return{captureValid:true,cases:8,foregroundObservations:observations,brushFailures};
}
export async function auditAlertDialogForeground({native,reference,hostAssembly='PackageConsumer.dll'}){
 const source=await fs.readFile(reference),r=JSON.parse(source),original=JSON.parse(await fs.readFile('tests/Reference/AlertDialog.json'));
 const ids=['default-loading-ltr','default-loading-rtl','destructive-loading-ltr','destructive-loading-rtl'];assert.deepEqual(r.cases,original.cases.filter(c=>ids.includes(c.id)),'Subset must retain actual pinned source cases');
 const button=await fs.readFile('tests/Reference/AuthShell/Source/src/components/button.tsx');assert.equal(hash(button),'119925584b3ec53c790fb0b762631894cf70eed12d488d5e9ae6743b7a67f929');assert(button.toString().includes('disabled:opacity-45'));
 const bytes=await fs.readFile(path.join(native,'alert-dialog-native.json')),n=JSON.parse(bytes),summary=verifyAlertDialogForegroundReport(n,r);assert.equal(hash(source),n.referenceSha256);
 for(const [file,sha]of [['Orchard.dll',n.librarySha256],[hostAssembly,n.hostSha256]])assert.equal(hash(await fs.readFile(path.join(native,'app-identity',file))),sha);
 const exit=JSON.parse(await fs.readFile(path.join(native,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);
 const paintFailures=[];for(const c of n.cases){const data=await fs.readFile(path.join(native,c.capture.file));assert.equal(hash(data),c.capture.sha256);const png=readRgbaPng(data);const buttons=c.buttonForegrounds.map(v=>({name:v.role,enabled:v.enabled,glyph:v.box,foreground:v.foreground}));paintFailures.push(...verifyTableNavigationForegroundPaint({scheme:c.scheme,kind:c.id,name:'loading',buttons},png));}
 return{...summary,passed:summary.brushFailures===0&&paintFailures.length===0,paintFailures:paintFailures.length,paintFailureDetails:paintFailures,librarySha256:n.librarySha256,hostSha256:n.hostSha256,reportSha256:hash(bytes),full56CaseSuiteVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};const result=await auditAlertDialogForeground({native:option('--native'),reference:option('--reference'),hostAssembly:option('--host-assembly')});await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({...result,paintFailureDetails:undefined}));process.exitCode=result.passed?0:1;}
