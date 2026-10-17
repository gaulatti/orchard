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
assert.equal(actual.opaqueReports.length,3);
const opaque=[];
for(const [id,width,height] of [['odd-size',17,3],['channel-grid',256,256],['catalog-size',1024,745]]){
 const matches=actual.opaqueReports.filter(item=>item.id===id);assert.equal(matches.length,1);const sample=matches[0];
 assert.equal(sample.width,width);assert.equal(sample.height,height);assert.equal(sample.repetitions,12);assert.equal(sample.exactWindowsPng,true);
 assert.equal(sample.file,`capture-opaque-${id}.png`);assert.equal(sample.oracleFile,`capture-opaque-${id}-windows.png`);
 const png=await fs.readFile(path.join(native,sample.file)),oracle=await fs.readFile(path.join(native,sample.oracleFile));assert.equal(Buffer.compare(png,oracle),0,'Opaque PNG bytes differ from Windows conversion.');
 const image=readRgbaPng(png);assert.equal(image.width,width);assert.equal(image.height,height);
 const expected=Buffer.alloc(width*height*4);
 for(let y=0;y<height;y++)for(let x=0;x<width;x++){const at=(y*width+x)*4;expected[at]=(x*11+y*7)&255;expected[at+1]=(x*37+y*13)&255;expected[at+2]=x&255;expected[at+3]=255;}
 assert.equal(Buffer.compare(image.rgba,expected),0,'Opaque RGBA bytes differ from the original pixels.');
 for(const field of ['windowsMilliseconds','captureMilliseconds'])assert.ok(Number.isFinite(sample[field])&&sample[field]>0);
 opaque.push({id,width,height,repetitions:12,exactWindowsPng:true,rgbaSha256:hash(image.rgba),windowsMilliseconds:sample.windowsMilliseconds,captureMilliseconds:sample.captureMilliseconds});
}
assert.equal(actual.reuseReports.length,3);
const reuse=[];
for(const [id,width,height] of [['odd-size',17,3],['channel-grid',256,256],['catalog-size',1024,745]]){
 const matches=actual.reuseReports.filter(item=>item.id===id);assert.equal(matches.length,1);const sample=matches[0];
 for(const [field,value] of Object.entries({width,height,repetitions:12,opaqueInputs:6,transparentInputs:6,exactWindowsPng:true}))assert.equal(sample[field],value);
 assert.equal(sample.file,`capture-reuse-${id}.png`);assert.equal(sample.oracleFile,`capture-reuse-${id}-windows.png`);
 const input=Buffer.alloc(width*height*4),iteration=11,alpha=23+iteration*13;
 for(let y=0;y<height;y++)for(let x=0;x<width;x++){const at=(y*width+x)*4;input[at]=Math.min((x+iteration*17)&255,alpha);input[at+1]=Math.min((x*37+y*13+iteration*7)&255,alpha);input[at+2]=Math.min((x*11+y*7+iteration*19)&255,alpha);input[at+3]=alpha;}
 assert.equal(sample.inputSha256,hash(input));
 const png=await fs.readFile(path.join(native,sample.file)),oracle=await fs.readFile(path.join(native,sample.oracleFile));
 assert.equal(Buffer.compare(png,oracle),0,'Reused PNG bytes differ from Windows conversion.');
 const image=readRgbaPng(png);assert.equal(image.width,width);assert.equal(image.height,height);
 reuse.push({id,width,height,repetitions:12,opaqueInputs:6,transparentInputs:6,exactWindowsPng:true,inputSha256:hash(input),rgbaSha256:hash(image.rgba)});
}
const report={passed:true,diagnosticOnly:true,referenceSha256:hash(bytes),cases:reports,opaque,reuse,immediateFilesComplete:true,writeFailurePropagated:true,pngEncoderUnchanged:true,masks:0,tolerance:0};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
