import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const contractBytes=fs.readFileSync(new URL('../tests/Reference/DataTableNavigationPaint.json',import.meta.url));
export const dataTableNavigationInkContract=JSON.parse(contractBytes);
export const dataTableNavigationInkContractSha256=hash(contractBytes);
const c=dataTableNavigationInkContract,referenceBytes=fs.readFileSync(new URL('../'+c.reference,import.meta.url)),sourceBytes=fs.readFileSync(new URL('../'+c.buttonSource,import.meta.url)),reference=JSON.parse(referenceBytes);
assert.equal(c.format,'orchard-data-table-navigation-paint-v1');assert.equal(hash(referenceBytes),c.referenceSha256);assert.equal(hash(sourceBytes),c.buttonSourceSha256);assert.equal(reference.commit,c.commit);assert.equal(reference.dependencies.find(d=>d.path==='src/components/button.tsx').sha256,c.buttonSourceSha256);assert.equal(Number(sourceBytes.toString().match(/disabled:opacity-(\d+)/)[1])/100,c.disabledOpacity);
for(const scheme of ['Light','Dark']){const color=reference.cases.find(s=>s.scheme===scheme&&s.name==='default').snapshot.footer.buttons[0].styles.color,m=color.match(/^rgb\((\d+),\s*(\d+),\s*(\d+)\)$/);assert(m);assert.deepEqual(c.foregroundRgba[scheme],[...m.slice(1).map(Number),255]);}

// Retain the existing 13x13 sample locations and minimum eight ink pixels.
// Bounds now follow the pinned foreground and disabled opacity. A fixed
// dark-channel cutoff incorrectly assumed the old, fully opaque SVG stroke.
export function verifyDataTableNavigationInk({scheme,stage,buttons,image}){
 assert(['Light','Dark'].includes(scheme));let probes=0;
 for(const button of buttons){assert.equal(typeof button.enabled,'boolean');const cx=Math.round(button.cx),cy=Math.round(button.cy);assert(cx-6>=0&&cy-6>=0&&cx+6<image.width&&cy+6<image.height);const background=[...image.rgba.subarray(((cy-6)*image.width+cx-6)*4,((cy-6)*image.width+cx-6)*4+4)];assert.equal(background[3],255);
  const opacity=button.enabled?1:c.disabledOpacity,fg=c.foregroundRgba[scheme],target=background.slice(0,3).map((v,i)=>v*(1-opacity)+fg[i]*opacity),lo=target.map((v,i)=>Math.floor(Math.min(v,background[i]))-1),hi=target.map((v,i)=>Math.ceil(Math.max(v,background[i]))+1);let ink=0;
  for(let y=cy-6;y<=cy+6;y++)for(let x=cx-6;x<=cx+6;x++){const offset=(y*image.width+x)*4;let painted=false;for(let channel=0;channel<3;channel++){const value=image.rgba[offset+channel];assert(value>=lo[channel]&&value<=hi[channel],`${scheme}/${stage}/${button.name}: arrow ink exceeds source foreground/opacity bounds`);painted||=Math.abs(value-background[channel])>1;}ink+=painted?1:0;}
  assert(ink>=8,`${scheme}/${stage}/${button.name}: pagination arrow has no visible paint`);probes++;
 }
 return probes;
}
