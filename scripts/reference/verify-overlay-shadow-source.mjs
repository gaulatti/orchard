import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './read-rgba-png.mjs';

export const shadowIds=['theme-opaque','transparent','null','blue-40','blue-80','blue-opaque','gradient-half','gradient-opaque'];
export const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export const point=(image,x,y)=>[...image.rgba.subarray((y*image.width+x)*4,(y*image.width+x)*4+4)];
export function outsideBox(x,y){return x<160||x>=360||y<140||y>=260;}
export function verifyShadowSourceModel(s){
 assert.equal(s.format,'orchard-overlay-shadow-source-v1');assert(s.passed&&s.diagnosticOnly&&!s.nativeComponentAccepted);
 assert.equal(s.browser,'155.0.4283.62');assert.equal(s.referenceSha256,'12b264b46566b389eff73d6b21e7f32feb465cdc52f2c86ab8379555a49a075e');
 assert.equal(s.width,520);assert.equal(s.height,440);assert.equal(s.cases.length,16);
 assert.deepEqual(s.cases.map(c=>[c.theme,c.id]),['Light','Dark'].flatMap(t=>shadowIds.map(id=>[t,id])));
 for(const c of s.cases){assert.deepEqual(c.box,{x:160,y:140,width:200,height:120});assert.deepEqual(c.radius,[14,14,14,14]);assert.equal(c.outsideDifferencesFromOpaque,0);assert.equal(c.stableFrames,2);assert(c.attempts>=2&&c.attempts<=6);}
}
export async function verifyShadowSource(reference,imagesFolder){
 const bytes=await fs.readFile(reference),s=JSON.parse(bytes);verifyShadowSourceModel(s);const images=new Map(),baselines=new Map();
 for(const c of s.cases){assert.equal(path.basename(c.file),c.file);const imageBytes=await fs.readFile(path.join(imagesFolder,c.file));assert.equal(hash(imageBytes),c.sha256);const image=readRgbaPng(imageBytes);assert.equal(image.width,520);assert.equal(image.height,440);assert.deepEqual(point(image,210,200),c.inside);assert.deepEqual(point(image,145,200),c.outside);if(c.id==='theme-opaque')baselines.set(c.theme,image);const baseline=baselines.get(c.theme);for(let y=0;y<440;y++)for(let x=0;x<520;x++)if(outsideBox(x,y)){const at=(y*520+x)*4;for(let k=0;k<4;k++)assert.equal(image.rgba[at+k],baseline.rgba[at+k]);}if(c.id==='transparent'||c.id==='null')assert.deepEqual(c.inside,[...c.canvas,255]);images.set(c.theme+'/'+c.id,image);}
 return{s,images,referenceSha256:hash(bytes)};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const[reference,imagesFolder,output]=process.argv.slice(2),r=await verifyShadowSource(reference,imagesFolder);const report={passed:true,cases:r.s.cases.length,pngs:r.images.size,referenceSha256:r.referenceSha256,outsideIndependentOfBackground:true,nativeComponentAccepted:false};await fs.writeFile(output,JSON.stringify(report,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(report));}
