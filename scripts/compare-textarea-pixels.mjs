import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const reference=JSON.parse(await fs.readFile(new URL('../tests/Reference/Textarea.json',import.meta.url),'utf8'));
const source=option('--source'),native320=option('--native-320'),native640=option('--native-640'),output=option('--output');
const cases=[];let differingPixels=0,absoluteRgbaError=0;
for(const test of reference.cases){
 const bytes=await fs.readFile(path.join(source,test.file));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),test.pngSha256);
 const expected=readRgbaPng(bytes),actual=readRgbaPng(await fs.readFile(path.join(test.width===320?native320:native640,test.file)));
 assert.equal(actual.width,expected.width);assert.equal(actual.height,expected.height);
 let pixels=0,error=0;
 for(let i=0;i<actual.rgba.length;i+=4){let different=false;for(let c=0;c<4;c++){const delta=Math.abs(actual.rgba[i+c]-expected.rgba[i+c]);error+=delta;different ||= delta!==0;}if(different)pixels++;}
 cases.push({id:test.id,width:test.width,scheme:test.scheme,state:test.state,file:test.file,differingPixels:pixels,absoluteRgbaError:error});differingPixels+=pixels;absoluteRgbaError+=error;
}
const report={comparisonCompleted:true,cases,differingPixels,absoluteRgbaError,wholeClientFrames:true,masks:0,resampling:false,sourcePixelParityVerified:differingPixels===0,nativeComponentAccepted:false};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({cases:cases.length,differingPixels,absoluteRgbaError,sourcePixelParityVerified:report.sourcePixelParityVerified}));
