import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL('../tests/Reference/CapturePersistence.json',import.meta.url)),reference=JSON.parse(bytes);
assert.equal(reference.format,'orchard-capture-persistence-reference-v1');assert.equal(reference.cases.length,4);
const encoder=await fs.readFile(new URL('../samples/Orchard.Catalog/NativeCapturePng.cs',import.meta.url),'utf8');
assert.equal(reference.baseline.encoderSourceNormalization,'lf');assert.equal(hash(encoder.replace(/\r\n/g,'\n')),reference.baseline.encoderSourceSha256);
const runtime=await read(path.join(native,'result.json')),actual=await read(path.join(native,'capture-persistence.json'));
assert.equal(runtime.passed,true);assert.equal(runtime.component,'brush-alpha');
assert.equal(actual.format,'orchard-capture-persistence-v1');assert.equal(actual.generator,reference.generator);
assert.equal(actual.referenceSha256,hash(bytes));assert.equal(actual.matchedFrozenReference,true);
assert.equal(actual.immediateFilesRead,true);assert.equal(actual.writeFailurePropagated,true);assert.equal(actual.reports.length,4);
const reports=[];
for(const expected of reference.cases){
 const matches=actual.reports.filter(item=>item.id===expected.id);assert.equal(matches.length,1);const sample=matches[0];
 for(const field of ['width','height','inputSha256','pngSha256'])assert.equal(sample[field],expected[field]);
 assert.equal(sample.file,`capture-persistence-${expected.id}.png`);
 const png=await fs.readFile(path.join(native,sample.file)),image=readRgbaPng(png);
 assert.equal(hash(png),expected.pngSha256);assert.equal(image.width,expected.width);assert.equal(image.height,expected.height);
 assert.equal(hash(image.rgba),expected.rgbaSha256);
 reports.push({id:expected.id,width:image.width,height:image.height,pngBytesMatched:true,rgbaPixelsMatched:true});
}
const report={passed:true,diagnosticOnly:true,referenceSha256:hash(bytes),cases:reports,immediateFilesComplete:true,writeFailurePropagated:true,pngEncoderUnchanged:true,masks:0,tolerance:0};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
