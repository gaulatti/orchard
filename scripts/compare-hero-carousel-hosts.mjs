import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {verifyHeroCarouselNative} from './verify-hero-carousel.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return process.argv[i+1];};
const source=option('--source'),consumer=option('--consumer'),packageFile=option('--package'),output=option('--output');
assert(!fs.existsSync(output),'Use a fresh comparison output');
const a=verifyHeroCarouselNative(source,'Orchard.Catalog.dll',output+'.source-audit.json');
const b=verifyHeroCarouselNative(consumer,'PackageConsumer.dll',output+'.consumer-audit.json');
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
assert.equal(a.librarySha256,b.librarySha256,'Source/package executed different library bytes');
const sourceReport=JSON.parse(fs.readFileSync(path.join(source,'hero-carousel.json'),'utf8'));
const consumerReport=JSON.parse(fs.readFileSync(path.join(consumer,'hero-carousel.json'),'utf8'));
for(const key of ['states','contracts'])assert.deepEqual(sourceReport[key],consumerReport[key],key+' differ');
let byteIdenticalPngPairs=0,rgbaIdenticalPairs=0,differingPixels=0,maximumChannelDifference=0;const frames=[];
for(const state of sourceReport.states){
 const x=fs.readFileSync(path.join(source,state.file)),y=fs.readFileSync(path.join(consumer,state.file));let different=0,max=0;
 if(x.equals(y)){byteIdenticalPngPairs++;rgbaIdenticalPairs++;}
 else{const left=readRgbaPng(x),right=readRgbaPng(y);assert.equal(left.width,right.width);assert.equal(left.height,right.height);for(let i=0;i<left.rgba.length;i+=4){let changed=false;for(let c=0;c<4;c++){const delta=Math.abs(left.rgba[i+c]-right.rgba[i+c]);max=Math.max(max,delta);changed||=delta>0;}if(changed)different++;}if(!different)rgbaIdenticalPairs++;}
 differingPixels+=different;maximumChannelDifference=Math.max(maximumChannelDifference,max);frames.push({id:state.id,scheme:state.scheme,differingPixels:different,maximumChannelDifference:max});
}
const result={format:'orchard-hero-carousel-host-comparison-v2',passed:differingPixels===0,source,consumer,package:packageFile,packageSha256:hash(packageFile),librarySha256:a.librarySha256,states:a.states,contracts:a.contracts,byteIdenticalPngPairs,rgbaIdenticalPairs,differingPixels,maximumChannelDifference,allowedChannelDifference:0,frames,sourceRasterParityVerified:false,nativeComponentAccepted:false};
fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({...result,frames:undefined}));
assert.equal(differingPixels,0,'Source/package captures differ; inspect complete comparison report');
