import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {auditTimeline} from './verify-timeline.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const source=option('--source'),output=option('--output'),directories=new Map([1024,639,640].map(width=>[width,option(`--native-${width}`)]));
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const bytes=await fs.readFile(new URL('../tests/Reference/Timeline.json',import.meta.url)),reference=JSON.parse(bytes);
assert.equal(hash(await fs.readFile(path.join(source,'reference.json'))),hash(bytes));
assert.equal(hash((await fs.readFile(new URL('./reference/capture-timeline.cjs',import.meta.url),'utf8')).replace(/\r\n/g,'\n')),reference.scriptSha256);
const gates=[];for(const [width,directory]of directories)gates.push(await auditTimeline(directory,width));
const pairs=[];
for(const test of reference.cases){
 const sourceBytes=await fs.readFile(path.join(source,test.file));assert.equal(hash(sourceBytes),test.pngSha256);
 const nativeBytes=await fs.readFile(path.join(directories.get(test.viewportWidth),test.file)),browser=readRgbaPng(sourceBytes),native=readRgbaPng(nativeBytes);
 assert.equal(native.width,browser.width,test.file);assert.equal(native.height,browser.height,test.file);
 let differentPixels=0,absoluteRgbaError=0,maxChannel=0;
 for(let offset=0;offset<browser.rgba.length;offset+=4){
  let changed=false;for(let channel=0;channel<4;channel++){const delta=Math.abs(browser.rgba[offset+channel]-native.rgba[offset+channel]);changed||=delta!==0;absoluteRgbaError+=delta;maxChannel=Math.max(maxChannel,delta);}
  differentPixels+=Number(changed);
 }
 pairs.push({id:test.id,viewportWidth:test.viewportWidth,scheme:test.scheme,sourceSha256:test.pngSha256,nativeSha256:hash(nativeBytes),differentPixels,absoluteRgbaError,maxChannel});
}
assert.equal(pairs.length,72);
const report={format:'orchard-timeline-pixel-audit-v1',checkedNativeGatesPassed:true,referenceSha256:hash(bytes),verifierSha256:hash(await fs.readFile(new URL(import.meta.url))),
 sourcePixelParityVerified:pairs.every(pair=>pair.differentPixels===0),nativeComponentAccepted:false,masks:0,tolerance:0,resampling:false,verifiedViewportCrops:gates.reduce((sum,gate)=>sum+gate.verifiedViewportCrops,0),baselines:gates.reduce((sum,gate)=>sum+gate.baselines,0),cases:pairs.length,
 differingCases:pairs.filter(pair=>pair.differentPixels>0).length,differentPixels:pairs.reduce((sum,pair)=>sum+pair.differentPixels,0),absoluteRgbaError:pairs.reduce((sum,pair)=>sum+pair.absoluteRgbaError,0),pairs};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,pairs:undefined}));
