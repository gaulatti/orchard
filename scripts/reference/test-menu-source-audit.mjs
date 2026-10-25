import fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import assert from 'node:assert/strict';
import {verifyMenuModel,verifyMenuSource} from './verify-menu-source.mjs';

const option = name => {const i=process.argv.indexOf(name); assert(i>=0 && process.argv[i+1],name); return process.argv[i+1];};
const referenceFile=option('--reference'),frames=option('--frames'),output=option('--output');
const original=JSON.parse(await fs.readFile(referenceFile));
verifyMenuModel(original);
const rejected=[];
function reject(name,change) {const copy=structuredClone(original);change(copy);assert.throws(()=>verifyMenuModel(copy),undefined,name);rejected.push(name);}
reject('wrong source revision',r=>r.commit='0'.repeat(40));
reject('missing source case',r=>r.cases.pop());
reject('duplicate source case',r=>r.cases[1]=structuredClone(r.cases[0]));
reject('off-viewport menu',r=>r.cases[0].measurement.menus[0].box.x=-1);
reject('dropdown row changed',r=>r.cases[0].measurement.menus[0].items[0].box.height=36);
reject('context typography replaced by dropdown',r=>r.cases[11].measurement.menus[0].items[0].paint.fontSize='14px');
reject('disabled action enabled',r=>r.cases[0].measurement.menus[0].items[1].disabled=false);
reject('indeterminate state dropped',r=>r.cases[6].measurement.menus[0].items.find(i=>i.id==='check-item').checked='false');
reject('caller refusal ignored',r=>r.histories[0].reopened.menus[0].items.find(i=>i.id==='check-item').checked='true');
reject('caller acceptance ignored',r=>r.histories[3].reopened.menus[0].items.find(i=>i.id==='check-item').checked='false');
reject('native-style dismissal failure accepted',r=>r.histories[0].after.menus=structuredClone(r.histories[0].before.menus));
reject('duplicate action callback',r=>r.histories[2].after.events.push('sub-action'));
reject('synthetic reopened instance',r=>r.histories[0].retainedReactInstance=false);
reject('path traversal frame',r=>r.cases[0].file='../outside.png');
const temporary=await fs.mkdtemp(path.join(os.tmpdir(),'orchard-menu-audit-'));
try {
  await assert.rejects(verifyMenuSource(referenceFile,temporary),{code:'ENOENT'});rejected.push('missing PNG');
  const first=original.cases[0],bytes=Buffer.from(await fs.readFile(path.join(frames,first.file)));bytes[bytes.length-1]^=1;
  await fs.writeFile(path.join(temporary,first.file),bytes);
  await assert.rejects(verifyMenuSource(referenceFile,temporary),{code:'ERR_ASSERTION'});rejected.push('corrupted PNG');
} finally {await fs.rm(temporary,{recursive:true});}
const audit={format:'orchard-menu-source-rejections-v1',passed:true,negativeCasesRejected:rejected.length,rejected,nativeComponentAccepted:false};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
