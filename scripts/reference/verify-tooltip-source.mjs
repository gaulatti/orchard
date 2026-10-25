import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './read-rgba-png.mjs';
const arg=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1],n);return process.argv[i+1];};
const reference=arg('--reference'),frames=arg('--frames'),output=arg('--output'),hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const bytes=await fs.readFile(reference),source=JSON.parse(bytes);assert.equal(source.format,'orchard-tooltip-reference-v1');assert.equal(source.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.equal(source.dependencies.find(d=>d.path==='src/components/tooltip.tsx').sha256,'31006ce0674960ac4feb5ca94581a6dbfb88b507478007f952623398460f789b');assert.equal(source.cases.length,40);assert.equal(source.histories.length,4);assert.equal(new Set(source.cases.map(c=>c.file)).size,40);
const index=process.argv.indexOf('--recapture'),recapture=index<0?null:process.argv[index+1];if(recapture)assert.deepEqual(JSON.parse(await fs.readFile(path.join(recapture,'Tooltip.json'))),source);
for(const c of source.cases){assert.equal(path.basename(c.file),c.file);const image=await fs.readFile(path.join(frames,c.file));assert.equal(hash(image),c.pngSha256);const png=readRgbaPng(image);assert.equal(png.width,900);assert.equal(png.height,760);if(recapture)assert.deepEqual(await fs.readFile(path.join(recapture,c.file)),image);assert.equal(c.measurement.tooltip.zIndex,c.spec.scope==='modal'?51:180);assert.equal(c.measurement.focused,'tip-trigger');assert.equal(c.measurement.tooltip.inert,false);}
for(const h of source.histories){assert.equal(h.after.tooltip,null);assert.equal(h.after.dialogs,h.scope==='modal'?1:0);assert.equal(h.after.focused,'tip-trigger');assert.deepEqual(h.after.events,[]);}
await fs.mkdir(path.dirname(path.resolve(output)),{recursive:true});const result={passed:true,sourceStates:40,escapeHistories:4,pngs:40,pixelsValidated:40*900*760,independentRecaptureByteIdentical:!!recapture,sourceSha256:hash(bytes),nativeComponentAccepted:false};await fs.writeFile(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
