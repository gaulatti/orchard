import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import path from 'node:path';
import {verifyDataTableCatalog} from './verify-data-table-catalog.mjs';

const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};
const native=JSON.parse(await fs.readFile(path.join(option('--native'),'data-table-catalog.json')));
const reference=JSON.parse(await fs.readFile('tests/Reference/DataTable.json'));
verifyDataTableCatalog(native,reference);
const state=(n,stage)=>n.states.find(s=>s.scheme==='Light'&&s.stage===stage);
const mutations=[
 ['failed',n=>n.passed=false],['missing-state',n=>n.states.pop()],['missing-image',n=>n.pngs.pop()],
 ['duplicate-image',n=>n.pngs[1].file=n.pngs[0].file],['foreign-image',n=>n.pngs[0].file='other.png'],
 ['focus',n=>n.focus=1],['activation',n=>n.activations=1],['hardware-input',n=>n.hardwareInputSent=true],
 ['false-acceptance',n=>n.nativeComponentAccepted=true],['false-raster',n=>n.sourceRasterParityVerified=true],
 ['private-api',n=>n.publicApisOnly=false],['missing-guards',n=>n.guards=0],
 ['wrong-fixture',n=>state(n,'initial').fixtureType='ReplacementFixture'],
 ['original-content',n=>state(n,'original-action').originalActionsRetained=false],
 ['original-event',n=>state(n,'original-action').status='Original action: replacement'],
 ['rows',n=>state(n,'initial').rows.reverse()],['name',n=>state(n,'initial').rows[0].name='replacement'],
 ['number',n=>state(n,'initial').rows[0].count='0'],['page',n=>state(n,'page-two').pageIndex=0],
 ['page-count',n=>state(n,'empty').pageCount=1],['filter',n=>state(n,'filtered').filtered=23],
 ['filtered-selection',n=>state(n,'filtered').filteredSelected=2],
 ['cross-page-selection',n=>state(n,'unselect-page').selected=[]],
 ['search',n=>state(n,'filtered').search=''],['empty',n=>state(n,'empty').emptyText=''],
 ['viewport',n=>state(n,'initial').box.width=661],['invalid-geometry',n=>state(n,'initial').box.height=null],
 ['stale-status',n=>state(n,'selected-first').status=state(n,'initial').status],
 ['capture-size',n=>n.pngs[0].width=902]
];
for(const[name,mutate]of mutations){const bad=structuredClone(native);mutate(bad);assert.throws(()=>verifyDataTableCatalog(bad,reference),undefined,name);}
const audit={passed:true,corruptReportsRejected:mutations.length,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
