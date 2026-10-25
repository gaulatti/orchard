import React from 'react';
import {createRoot} from 'react-dom/client';
import * as T from 'frozen/table';
const root=createRoot(document.getElementById('root')!);let serial=0;
function View({initial}:any){
 const [spec,setSpec]=React.useState(initial),[keys,setKeys]=React.useState(initial.keys??[]);
 (window as any).patchTableText=(patch:any)=>{if('keys'in patch)setKeys(patch.keys);setSpec((s:any)=>({...s,...patch}));};
 const events=(window as any).tableTextEvents;
 const data=spec.data;
 return <div id="table-text-fixture" lang={spec.language} dir={spec.rtl?'rtl':'ltr'} style={{width:660,margin:20}}>
  {data?<T.DataTable columns={[{key:'name',header:'Fictional item'},{key:'count',header:'Count',align:'right'}]} data={data}
   getRowKey={(row:any)=>row.id} selectable selectedKeys={spec.controlled?new Set(keys):undefined}
   onSelectionChange={value=>{const next=[...value];events.push({kind:'selection',keys:next});if(spec.accept)setKeys(next);}}/>
   :<T.Table><T.TableHeader><T.TableRow><T.TableHead>{spec.label}</T.TableHead>
    <T.SortableTableHeader field="label" label={spec.label} currentSort={{field:'',order:'asc'}} onSort={(field,order)=>events.push({kind:'sort',field,order})}/>
    <T.SortableTableHeader field="plain" label={spec.label} sortable={false} currentSort={{field:'',order:'asc'}} onSort={()=>{throw Error('Nonsortable heading invoked');}}/>
   </T.TableRow></T.TableHeader></T.Table>}
 </div>;
}
(window as any).tableTextEvents=[];
(window as any).showTableText=(spec:any)=>{(window as any).tableTextEvents=[];root.render(<View key={++serial} initial={spec}/>);};
