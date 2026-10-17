import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const source=option('--source'),native=option('--native'),output=option('--output');
const reference=JSON.parse(await fs.readFile(new URL('../tests/Reference/Metric.json',import.meta.url),'utf8'));
const cases=[];let differingPixels=0,absoluteRgbaError=0;
for(const test of reference.cases.filter(c=>c.file)){
 const bytes=await fs.readFile(path.join(source,test.file));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),test.pngSha256);
 const expected=readRgbaPng(bytes),actual=readRgbaPng(await fs.readFile(path.join(native,test.file)));
 assert.equal(actual.width,expected.width);assert.equal(actual.height,expected.height);
 let different=0,error=0;for(let i=0;i<actual.rgba.length;i+=4){let changed=false;for(let c=0;c<4;c++){const delta=Math.abs(actual.rgba[i+c]-expected.rgba[i+c]);error+=delta;changed||=delta!==0;}if(changed)different++;}
 differingPixels+=different;absoluteRgbaError+=error;cases.push({id:test.id,scheme:test.scheme,file:test.file,differingPixels:different,absoluteRgbaError:error});
}
assert.equal(cases.length,12);
const report={comparisonCompleted:true,cases,differingPixels,absoluteRgbaError,masks:0,resampling:false,sourcePixelParityVerified:differingPixels===0,componentAccepted:false};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,cases:cases.length}));
