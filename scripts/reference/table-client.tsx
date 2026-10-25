import React from 'react';
import {createRoot} from 'react-dom/client';
import * as T from 'frozen/table';
import {Checkbox} from 'frozen/checkbox';

const root=createRoot(document.getElementById('root')!);let serial=0;
const initialRows=[
 {id:'cedar',name:'Cedar bulletin',count:12,status:'Queued'},
 {id:'birch',name:'Birch update',count:3,status:'Ready'},
 {id:'ash',name:'Ash brief',count:25,status:'Draft'}
];
function View({initial}:any){
 const [spec,setSpec]=React.useState(initial),[sort,setSort]=React.useState(initial.sort??{field:'',order:'asc'}),[keys,setKeys]=React.useState(initial.keys??[]);
 const w=window as any;w.patchTable=(patch:any)=>{if('sort'in patch)setSort(patch.sort);if('keys'in patch)setKeys(patch.keys);setSpec((s:any)=>({...s,...patch}));};
 const rows=spec.rows??initialRows;
 const columns=[
  {key:'name',header:'Fictional item',sortFn:spec.customCompare?((a:any,b:any)=>b.name.length-a.name.length):undefined},
  {key:'count',header:'Count',align:'right'},
  {key:'status',header:'Status',sortable:false,cell:(row:any,index:number)=><span data-row-id={row.id} data-row-index={index}>{row.status}:{index}</span>}
 ];
 const events=w.tableEvents;
 const footer=(data:any[])=> <T.TableRow data-footer-order={data.map(r=>r.id).join(',')}><T.TableCell colSpan={2}>Fictional total</T.TableCell><T.TableCell>{data.reduce((n,r)=>n+Number(r.count??0),0)}</T.TableCell></T.TableRow>;
 const data=<T.DataTable columns={columns} data={rows} getRowKey={(row:any,index:number)=>spec.indexKeys?String(index):row.id}
  caption={spec.caption===false?undefined:'Fictional local component data'} emptyMessage={spec.emptyMessage??'No results.'} selectable={spec.selectable!==false}
  sort={spec.controlledSort?sort:undefined} selectedKeys={spec.controlledSelection?new Set(keys):undefined}
  onSortChange={(value:any)=>{events.push({kind:'sort',value});if(spec.acceptSort)setSort(value);}}
  onSelectionChange={(value:any)=>{const next=[...value];events.push({kind:'selection',value:next});if(spec.acceptSelection)setKeys(next);}}
  footer={spec.footer===false?undefined:footer}/>;
 const primitives=<T.Table>
  <T.TableCaption>Original fictional caption</T.TableCaption>
  <T.TableHeader><T.TableRow><T.TableHead>Original heading</T.TableHead><T.TableHead>Count</T.TableHead><T.TableHead>Original action</T.TableHead></T.TableRow></T.TableHeader>
  <T.TableBody>
   <T.TableRow data-selected={spec.selected===true}><T.TableCell rowSpan={spec.rowSpan?2:1}><span style={spec.long?{display:'block',width:520}:undefined}>Original retained content</span></T.TableCell><T.TableCell>12</T.TableCell><T.TableCell><button id="table-original-action" disabled={spec.disabled} onClick={()=>events.push({kind:'action'})}>Inspect fictional item</button></T.TableCell></T.TableRow>
   <T.TableRow><T.TableCell colSpan={spec.rowSpan?1:2}>Two-column original cell</T.TableCell><T.TableCell><Checkbox aria-label="Original cell checkbox" defaultChecked/></T.TableCell></T.TableRow>
  </T.TableBody>
  <T.TableFooter><T.TableRow><T.TableCell colSpan={3}>Original fictional footer</T.TableCell></T.TableRow></T.TableFooter>
 </T.Table>;
 return <div id="table-fixture" dir={spec.rtl?'rtl':'ltr'} style={{width:spec.width??660,margin:20}}>{spec.primitive?primitives:data}</div>;
}
(window as any).tableEvents=[];
(window as any).showTable=(spec:any)=>{(window as any).tableEvents=[];root.render(<View key={++serial} initial={spec}/>);};
