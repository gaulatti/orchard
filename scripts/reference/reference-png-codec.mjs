import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export async function readReferencePngCodec(){
 const manifest=await fs.readFile(new URL('../../tests/Reference/OverlayNearPngCodec.json',import.meta.url));
 assert.equal(hash(manifest),'efc4c003e3e69eb92076c6ff5894caae013557a20ac2192853b7fc4dbda35935');
 const spec=JSON.parse(manifest),table=await fs.readFile(new URL('../../tests/PackageConsumer/ReferencePngCodec.u8',import.meta.url));
 assert.equal(table.length,65536);assert.equal(hash(table),spec.sha256);
 assert(spec.diagnosticOnly&&!spec.componentRendererInput&&spec.mappingInjectiveForEveryAlpha&&spec.browser==='155.0.4283.62');
 let legalPairs=0;
 for(let a=0;a<256;a++)for(let c=0;c<256;c++){
  if(c>a){assert.equal(table[a*256+c],0);continue;}
  const value=table[a*256+c]*a/255,n=Math.floor(value),recovered=value-n===.5?n+n%2:Math.floor(value+.5);
  assert.equal(recovered,c);legalPairs++;
 }
 assert.equal(legalPairs,32896);
 return{table,sha256:spec.sha256,legalPairs,diagnosticOnly:true};
}
