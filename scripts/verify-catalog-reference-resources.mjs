import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';

const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
export function verifyCatalogReferenceReport(report,expected) {
  assert.equal(report.passed,true);assert.equal(report.windowsCreated,false);
  assert.equal(report.hardwareInputSent,false);assert.equal(report.osSettingsChanged,false);
  assert.equal(report.nativeArchitecture,'X64');assert(report.guards>expected.resources.length);
  assert.equal(report.resources.length,expected.resources.length);
  assert.equal(new Set(report.resources.map(r=>r.name)).size,report.resources.length);
  let originalBytes=0,encodedBytes=0,jsonResources=0;
  for(const original of expected.resources) {
    const actual=report.resources.find(r=>r.name===original.name);assert(actual,original.name);
    assert.equal(actual.sha256,original.sha256,original.name);assert.equal(actual.length,original.bytes,original.name);
    assert.equal(actual.seekable,true);assert.equal(actual.readOnly,true);assert.equal(actual.rewindVerified,true);
    const json=original.name.startsWith('Orchard.Catalog.Reference.')&&original.name.endsWith('.json');
    assert.equal(actual.jsonParsed,json);if(json){assert(actual.encodedBytes<actual.length);jsonResources++;}
    else assert.equal(actual.encodedBytes,actual.length);
    originalBytes+=actual.length;encodedBytes+=actual.encodedBytes;
  }
  return {resources:report.resources.length,jsonResources,originalBytes,encodedBytes,savedBytes:originalBytes-encodedBytes};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href) {
  const arg=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return process.argv[i+1];};
  const folder=arg('--native'),app=arg('--app'),expectedBytes=await fs.readFile(arg('--expected')),expected=JSON.parse(expectedBytes),raw=await fs.readFile(path.join(folder,'resources.json')),report=JSON.parse(raw);
  for(const e of expected.resources)assert.equal(hash(await fs.readFile(e.path)),e.sha256,'Original reference changed: '+e.path);
  const counts=verifyCatalogReferenceReport(report,expected),exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert(exit.exited&&exit.exitCode===0&&!exit.timedOut);
  assert.equal(hash(await fs.readFile(path.join(app,'Orchard.Catalog.dll'))),report.hostSha256);
  const reject=change=>{const bad=structuredClone(report);change(bad);assert.throws(()=>verifyCatalogReferenceReport(bad,expected));};
  reject(r=>r.resources.pop());reject(r=>r.resources[0].sha256='0'.repeat(64));reject(r=>r.resources[0].readOnly=false);reject(r=>r.resources[0].rewindVerified=false);reject(r=>r.resources[1].name=r.resources[0].name);reject(r=>r.resources.find(e=>e.jsonParsed).jsonParsed=false);
  const result={passed:true,...counts,originalFilesUnchanged:true,readOnlySeekableStreamsVerified:true,negativeCasesRejected:6,hostSha256:report.hostSha256,reportSha256:hash(raw),expectedSha256:hash(expectedBytes),hiddenBoundaryVerified:true};
  await fs.writeFile(arg('--output'),JSON.stringify(result,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(result));
}
