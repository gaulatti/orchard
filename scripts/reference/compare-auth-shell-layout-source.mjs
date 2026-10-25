import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {verifyAuthShellLayoutSource} from './verify-auth-shell-layout-source.mjs';
const[first,second,output]=process.argv.slice(2);assert(first&&second&&output);const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const inputs=await Promise.all([first,second].map(async folder=>{const file=path.join(folder,'AuthShellLayout.json'),bytes=await fs.readFile(file);return{folder,reference:JSON.parse(bytes),sha256:hash(bytes),audit:await verifyAuthShellLayoutSource(file,folder)};}));
const changes=[];function diff(a,b,p){if(JSON.stringify(a)===JSON.stringify(b))return;if(a&&b&&typeof a==='object'&&typeof b==='object'){for(const k of new Set([...Object.keys(a),...Object.keys(b)]))diff(a[k],b[k],p+'/'+k);}else changes.push({path:p,first:a,second:b});}
diff(inputs[0].reference,inputs[1].reference,'reference');let pairs=0;for(const c of inputs[0].reference.cases){assert.deepEqual(await fs.readFile(path.join(first,c.file)),await fs.readFile(path.join(second,c.file)),c.scheme+'/'+c.id);pairs++;}
const result={format:'orchard-auth-shell-layout-independent-recapture-v1',passed:changes.length===0,first:{folder:first,sha256:inputs[0].sha256,audit:inputs[0].audit},second:{folder:second,sha256:inputs[1].sha256,audit:inputs[1].audit},exactCanonicalPngPairs:pairs,canonicalPixelDifferencesAllowed:0,metadataFieldsExcluded:[],semanticChanges:changes,authenticationImplemented:false,nativeImplemented:false,nativeComponentAccepted:false};await fs.writeFile(output,JSON.stringify(result,null,2)+'\n',{flag:'wx'});assert.equal(changes.length,0,'Retained independent captures differ');console.log(JSON.stringify({passed:true,states:pairs,histories:8,metadataFieldsExcluded:[]}));
