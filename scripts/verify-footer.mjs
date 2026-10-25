import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {assertNativePaint} from './reference/css-paint.mjs';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),app=option('--app'),output=option('--output'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const bytes=await fs.readFile(new URL('../tests/Reference/Footer.json',import.meta.url)),reference=JSON.parse(bytes),report=JSON.parse(await fs.readFile(path.join(native,'footer.json'))),result=JSON.parse(await fs.readFile(path.join(native,'result.json'))),exit=JSON.parse(await fs.readFile(path.join(native,'process-exit.json')));
assert.equal(reference.sourceSha256,'77756e4546713842cf511b7521fe1a5b61f39d378797d37a4b77511486f7d082');assert.equal(report.referenceSha256,hash(bytes));assert.equal(report.scope,'foreground-footer-source-capture');
for(const flag of ['passed','diagnosticOnly','rawClientFrames'])assert.equal(report[flag],true,flag);for(const flag of ['sourcePixelsPainted','inputSequencesVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[flag],false,flag);
assert.equal(report.librarySha256,hash(await fs.readFile(path.join(app,'Orchard.dll'))));assert.equal(report.catalogSha256,hash(await fs.readFile(path.join(app,'Orchard.Catalog.dll'))));assert.equal(result.passed,true);assert.equal(result.component,'footer');assert.equal(result.environment.processArchitecture,result.environment.osArchitecture);assert(['X64','Arm64'].includes(result.environment.processArchitecture));assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);
const key=row=>row.scheme+'/'+row.id;assert.equal(reference.cases.length,64);assert.deepEqual(report.frames.map(key).sort(),reference.cases.map(key).sort());assert.deepEqual(Object.keys(report.captures).sort(),reference.cases.map(row=>row.file).sort());
let geometryChecks=0,maximumGeometryError=0,paintChecks=0,metadataChecks=0;const geometryErrors=[],paintErrors=[],metadataErrors=[],rows=[];
const box=(a,e,label)=>{if(a===null||e===null){assert.equal(a,e,label);return;}assert(a&&e,label);for(const field of ['x','y','width','height']){assert(Number.isFinite(a[field]),label);const error=Math.abs(a[field]-e[field]);geometryChecks++;maximumGeometryError=Math.max(maximumGeometryError,error);if(error>.001)geometryErrors.push({label:label+'/'+field,actual:a[field],expected:e[field],error});}};
const paint=(a,e,label)=>{paintChecks++;try{assertNativePaint(a,e,label);}catch(error){paintErrors.push({label,error:error.message});}};
for(const wanted of reference.cases){
 const actual=report.frames.find(row=>key(row)===key(wanted)),e=wanted.measurement,label=key(wanted);
 for(const name of ['host','footer','container','grid','brand','description','bottom','topRule','accent'])box(actual[name],e[name],label+'/'+name);
 for(const name of ['bottomLeft','bottomRight'])box(actual[name].box,e[name].box,label+'/'+name);
 assert.equal(actual.sections.length,e.sections.length,label);assert.equal(actual.links.length,e.links.length,label);
 for(let index=0;index<e.sections.length;index++){const a=actual.sections[index],w=e.sections[index];assert.equal(a.title,w.title,label);box(a.box,w.box,label+'/section/'+index);box(a.heading,w.heading,label+'/heading/'+index);}
 for(let index=0;index<e.links.length;index++){
  const a=actual.links[index],w=e.links[index],location=label+'/link/'+index;box(a.box,w.box,location);metadataChecks+=4;
  const original=wanted.spec.sections.flatMap(section=>section.items).find(item=>item.href===a.href&&item.label===a.label);
  for(const[field,expected]of [['href',w.href],['role','Hyperlink'],['external',original?.external??false]])if(a[field]!==expected)metadataErrors.push({location,field,actual:a[field],expected});
  if(index!==0&&!a.originalItem)metadataErrors.push({location,field:'originalItem',actual:false,expected:true});
  if(index!==0)paint(a.paint,w.paint.color,location);
 }
 paint(actual.paint.footer,e.paint.footer.background,label+'/background');paint(actual.paint.topRule,e.paint.topRule.background,label+'/top-rule');paint(actual.paint.bottomRule,e.paint.bottom.border[1],label+'/bottom-rule');if(e.accent)paint(actual.paint.accent,e.paint.accent.background,label+'/accent');
 const nativeBytes=await fs.readFile(path.join(native,wanted.file)),sourceBytes=await fs.readFile(new URL('../tests/Reference/Footer/'+wanted.file,import.meta.url));assert.equal(hash(sourceBytes),wanted.pngSha256);assert.equal(hash(nativeBytes),report.captures[wanted.file]);const a=readRgbaPng(nativeBytes),b=readRgbaPng(sourceBytes);assert.deepEqual([a.width,a.height],[wanted.spec.viewportWidth,wanted.spec.viewportHeight]);assert.deepEqual([a.width,a.height],[b.width,b.height]);let differingPixels=0;for(let offset=0;offset<a.rgba.length;offset+=4)if(a.rgba.readUInt32LE(offset)!==b.rgba.readUInt32LE(offset))differingPixels++;rows.push({id:wanted.id,scheme:wanted.scheme,file:wanted.file,differingPixels});
}
const passed=geometryErrors.length===0&&paintErrors.length===0&&metadataErrors.length===0;
const audit={passed,scope:'foreground-footer-initial-geometry-paint-audit',diagnosticOnly:true,sourceStates:reference.cases.length,geometryChecks,maximumGeometryError,geometryErrors,paintChecks,paintErrors,metadataChecks,metadataErrors,exactPngs:rows.filter(row=>row.differingPixels===0).length,differingPixels:rows.reduce((sum,row)=>sum+row.differingPixels,0),rows,inputSequencesVerified:false,fullLifecycleVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false,librarySha256:report.librarySha256,catalogSha256:report.catalogSha256};await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,rows:undefined,geometryErrors:geometryErrors.length,paintErrors:paintErrors.length,metadataErrors:metadataErrors.length}));assert(passed,'Footer source audit differs; see full diagnostic.');
