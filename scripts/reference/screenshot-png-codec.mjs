import fs from'node:fs/promises';
import crypto from'node:crypto';
import assert from'node:assert/strict';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export async function readScreenshotPngCodec(){
 const bytes=await fs.readFile(new URL('../../tests/Reference/OverlayScreenshotPngCodec.json',import.meta.url));
 assert.equal(hash(bytes),'a327a5b36420d08f3bc38abb418691999763a2cee0db2424172caad70331ffbe');
 const spec=JSON.parse(bytes),table=await fs.readFile(new URL('../../tests/PackageConsumer/ScreenshotPngCodec.u8',import.meta.url));
 assert.equal(table.length,65536);assert.equal(hash(table),spec.sha256);
 assert(spec.diagnosticOnly&&!spec.componentRendererInput&&!spec.sourceImagePixelsUsedToGenerateTable&&spec.mappingInjectiveForEveryAlpha&&spec.captureMethod==='Playwright page.screenshot omitBackground true'&&spec.browser==='155.0.4283.62');
 let legalPairs=0;for(let a=0;a<256;a++)for(let c=0;c<256;c++){
  if(c>a){assert.equal(table[a*256+c],0);continue;}
  assert.equal(Math.round(table[a*256+c]*a/255),c);legalPairs++;
 }
 assert.equal(legalPairs,32896);return{table,sha256:spec.sha256,legalPairs,diagnosticOnly:true};
}
