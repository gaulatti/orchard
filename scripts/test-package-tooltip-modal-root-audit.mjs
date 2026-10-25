import fs from 'node:fs';
import assert from 'node:assert/strict';
import {auditPackageTooltip,auditPackageTooltipModel} from './verify-package-tooltip-modal-root.mjs';
const arg=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const folder=arg('--native'),packagePath=arg('--package'),output=arg('--output'),positive=JSON.parse(fs.readFileSync(folder+'/package-tooltip-modal-root.json'));auditPackageTooltip(folder,packagePath);const rejected=[];
function reject(name,change){const copy=structuredClone(positive);change(copy);assert.throws(()=>auditPackageTooltipModel(copy),undefined,name);rejected.push(name);}
reject('missing public case',r=>r.cases.pop());reject('duplicate public case',r=>r.cases[1]=structuredClone(r.cases[0]));reject('selector replaced',r=>r.cases[0].originalSelector=false);reject('live value lost',r=>r.cases[2].liveValueRendered=false);
reject('caller binding lost',r=>r.cases[1].bindingRetained=false);reject('inactive input enabled',r=>r.cases[1].pointerBlocked=false);reject('inactive peer exposed',r=>r.cases[1].peerExcluded=false);reject('caller model replaced',r=>r.cases[0].originalModel=false);
reject('explicit theme overridden',r=>r.cases[4].explicitTheme=false);reject('duplicate owner request',r=>r.cases[6].confirmationRequests=2);reject('caller-controlled parent closed',r=>r.cases[6].parentRetained=false);reject('tooltip stayed open after invocation',r=>r.cases[6].tooltipClosed=false);
reject('service owner retained',r=>r.cases[3].ownerReleased=false);reject('popover unload retained tooltip',r=>r.cases[7].closedWithOwner=false);reject('hardware input sent',r=>r.hardwareInputSent=true);reject('window activated',r=>r.activations=1);reject('focus acquired',r=>r.focus=1);reject('private library API used',r=>r.publicApisOnly=false);
const old=process.argv.indexOf('--old-native');if(old>=0){assert.throws(()=>auditPackageTooltipModel(JSON.parse(fs.readFileSync(process.argv[old+1]+'/package-tooltip-modal-root.json'))));rejected.push('actual old-package runtime failure');}
const stale=process.argv.indexOf('--old-package');if(stale>=0){assert.throws(()=>auditPackageTooltip(folder,process.argv[stale+1]));rejected.push('actual stale NuGet payload');}
fs.writeFileSync(output,JSON.stringify({passed:true,negativeCasesRejected:rejected.length,rejected,nativeComponentAccepted:false},null,2)+'\n');console.log(JSON.stringify({passed:true,negativeCasesRejected:rejected.length}));
