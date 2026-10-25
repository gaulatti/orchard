import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const file='tests/Reference/ScrollArea.json',bytes=await fs.readFile(file),r=JSON.parse(bytes);assert.equal(r.format,'orchard-scroll-area-reference-v1');assert.equal(r.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.deepEqual(r.ignoredDefaultArgs,['--hide-scrollbars']);assert.equal(r.cases.length,94);assert.equal(r.paints.length,12);assert.equal(r.interactions.length,16);
assert.equal(hash(await fs.readFile('scripts/reference/capture-scroll-area.cjs')),r.scriptSha256);assert.equal(hash(await fs.readFile('scripts/reference/scroll-area-client.tsx')),r.clientSha256);
for(const row of [...r.cases,...r.paints]){const png=await fs.readFile(path.join('tests/Reference/ScrollArea',row.file));assert.equal(hash(png),row.pngSha256);const decoded=readRgbaPng(png);assert.equal(decoded.width,800);assert.equal(decoded.height,700);}
const samples=[];for(const row of r.paints){const png=readRgbaPng(await fs.readFile(path.join('tests/Reference/ScrollArea',row.file))),at=(row.point.y*800+row.point.x)*4,actual=[...png.rgba.subarray(at,at+4)],expected=row.scheme==='Light'?(row.hover?[242,233,219,255]:[247,242,233,255]):row.hover?[21,31,43,255]:[18,28,40,255];assert.deepEqual(actual,expected,row.scheme+'/'+row.orientation+'/'+row.hover);samples.push({scheme:row.scheme,orientation:row.orientation,hover:row.hover,rgba:actual});}
const rest=r.cases.find(row=>row.scheme==='Light'&&row.id==='default');assert.equal(rest.measurement.client.width,250);assert.equal(rest.measurement.client.height,240);
const both=r.cases.find(row=>row.scheme==='Light'&&row.id==='both-ltr-overflow');assert.equal(both.measurement.client.width,250);assert.equal(both.measurement.client.height,225);
const short=r.cases.find(row=>row.scheme==='Light'&&row.id==='vertical-ltr-small-maximum'),png=readRgbaPng(await fs.readFile(path.join('tests/Reference/ScrollArea',short.file))),rows=[];for(let y=100;y<164;y++){const at=(y*800+353)*4;if(png.rgba[at]!==255||png.rgba[at+1]!==255||png.rgba[at+2]!==255)rows.push(y);}assert.deepEqual(rows,Array.from({length:17},(_,i)=>100+i));
const result={passed:true,referenceSha256:hash(bytes),sourceStates:94,scrollbarPaintStates:12,inputHistories:16,fullClientPngs:106,sourcePixelsVerified:samples,minimumVerticalThumbPixelsObserved:17,headlessScrollbarHideSwitchRemoved:true,nativeScrollAreaImplemented:false,nativeComponentAccepted:false};
const at=process.argv.indexOf('--output');assert(at>=0&&process.argv[at+1]);await fs.writeFile(path.resolve(process.argv[at+1]),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({...result,sourcePixelsVerified:samples.length}));
