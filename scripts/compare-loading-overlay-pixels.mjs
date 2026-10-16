import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {execFileSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const source=option('--source'),output=option('--output'),directories=new Map([1024,640,320].map(width=>[width,option(`--native-${width}`)]));
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const bytes=await fs.readFile(new URL('../tests/Reference/LoadingOverlay.json',import.meta.url)),reference=JSON.parse(bytes);
assert.equal(hash(await fs.readFile(path.join(source,'reference.json'))),hash(bytes));
const gates=[];
for(const [width,directory]of directories){
 const gate=path.join(directory,'pixel-input-audit.json');
 execFileSync(process.execPath,[fileURLToPath(new URL('./verify-loading-overlay.mjs',import.meta.url)),'--native',directory,'--output',gate],{stdio:'pipe'});
 gates.push(JSON.parse(await fs.readFile(gate,'utf8')));
}
const pairs=[];
for(const test of reference.cases){
 const sourceBytes=await fs.readFile(path.join(source,test.file));assert.equal(hash(sourceBytes),test.pngSha256);
 const nativeBytes=await fs.readFile(path.join(directories.get(test.width),test.file)),browser=readRgbaPng(sourceBytes),native=readRgbaPng(nativeBytes);
 assert.equal(native.width,browser.width);assert.equal(native.height,browser.height);
 let differentPixels=0,absoluteRgbaError=0,maxChannel=0;
 for(let offset=0;offset<browser.rgba.length;offset+=4){
  let changed=false;for(let channel=0;channel<4;channel++){const delta=Math.abs(browser.rgba[offset+channel]-native.rgba[offset+channel]);changed||=delta!==0;absoluteRgbaError+=delta;maxChannel=Math.max(maxChannel,delta);}
  differentPixels+=Number(changed);
 }
 pairs.push({id:test.id,width:test.width,height:test.height,scheme:test.scheme,sourceSha256:test.pngSha256,nativeSha256:hash(nativeBytes),differentPixels,absoluteRgbaError,maxChannel});
}
assert.equal(pairs.length,60);
const report={format:'orchard-loading-overlay-pixel-audit-v1',nativeGeometryAndInputVerified:true,componentAccepted:false,referenceSha256:hash(bytes),sourcePixelParityVerified:pairs.every(pair=>pair.differentPixels===0),masks:0,tolerance:0,resampling:false,cases:60,
 baselines:gates.reduce((sum,gate)=>sum+gate.baselines,0),maximumGeometryError:Math.max(...gates.map(gate=>gate.maximumGeometryError)),maximumBaselineError:Math.max(...gates.map(gate=>gate.maximumBaselineError)),
 differingCases:pairs.filter(pair=>pair.differentPixels>0).length,differentPixels:pairs.reduce((sum,pair)=>sum+pair.differentPixels,0),absoluteRgbaError:pairs.reduce((sum,pair)=>sum+pair.absoluteRgbaError,0),pairs};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,pairs:undefined}));
