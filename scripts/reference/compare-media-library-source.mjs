import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {verifyMediaLibrarySource} from './verify-media-library-source.mjs';
const [first,second,output]=process.argv.slice(2);assert(first&&second&&output);
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const inputs=await Promise.all([first,second].map(async folder=>{const file=path.join(folder,'MediaLibrary.json'),bytes=await fs.readFile(file);return{folder,reference:JSON.parse(bytes),sha256:hash(bytes),audit:await verifyMediaLibrarySource(file,folder)};}));
const [a,b]=inputs;
for(const key of Object.keys(a.reference).filter(k=>!['cases','histories'].includes(k)))assert.deepEqual(a.reference[key],b.reference[key],'independent reference '+key);
const changes=[];
function diff(x,y,p){if(JSON.stringify(x)===JSON.stringify(y))return;if(x&&y&&typeof x==='object'&&typeof y==='object'){for(const k of new Set([...Object.keys(x),...Object.keys(y)]))diff(x[k],y[k],p+'/'+k);}else changes.push({path:p,first:x,second:y});}
for(let i=0;i<a.reference.cases.length;i++){
 const ca=a.reference.cases[i],cb=b.reference.cases[i];for(const k of ['id','scheme','spec','interaction','previewIndex','accessibility','measurement'])diff(ca[k],cb[k],`cases/${ca.scheme}/${ca.id}/${k}`);
 const pa=await fs.readFile(path.join(first,ca.file)),pb=await fs.readFile(path.join(second,cb.file));assert.deepEqual(pa,pb,ca.scheme+'/'+ca.id+' independent original PNG');
}
diff(a.reference.histories,b.reference.histories,'histories');
const report={format:'orchard-media-library-independent-recapture-v1',passed:changes.length===0,first:{folder:first,sha256:a.sha256,audit:a.audit},second:{folder:second,sha256:b.sha256,audit:b.audit},exactCanonicalPngPairs:100,canonicalPixelDifferencesAllowed:0,semanticChanges:changes,nativeImplemented:false,nativeComponentAccepted:false};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n',{flag:'wx'});assert.equal(changes.length,0,'independent source metadata differs; inspect retained report');console.log(JSON.stringify({passed:true,exactCanonicalPngPairs:100,states:100,histories:50}));
