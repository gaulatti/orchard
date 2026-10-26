import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {verifySeparator} from './verify-separator.mjs';
const [folder,host,output]=process.argv.slice(2),original=JSON.parse(fs.readFileSync(path.join(folder,'separator.json'),'utf8'));
assert(verifySeparator(folder,host).passed);const rejected=[];
function reject(id,mutate){const model=structuredClone(original);mutate(model);assert.throws(()=>verifySeparator(folder,host,model),id);rejected.push(id);}
reject('missing state',r=>r.states.pop());
reject('unwanted focus stop',r=>r.states[0].tabStop=true);
reject('semantic separator hidden',r=>r.states.find(s=>!s.decorative).controlElement=false);
reject('wrong native geometry',r=>r.states[0].box.height=2);
reject('wrong theme paint',r=>r.states[0].color=[255,255,255,255]);
reject('disabled dimming',r=>r.states.find(s=>!s.enabled).Opacity=.025);
reject('wrong captured bytes',r=>r.states[0].image.sha256='0'.repeat(64));
reject('lost caller binding',r=>r.contracts[0].bindingsRetained=false);
reject('unexpected foreground activation',r=>r.activations=1);
reject('false ARM64 execution claim',r=>r.nativeARM64RuntimeVerified=true);
const result={passed:true,negativeCasesRejected:rejected.length,rejected};fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(result));
