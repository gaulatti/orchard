import React from 'react';
import {createRoot} from 'react-dom/client';
import {DataTable} from 'frozen/data-table';
import {Checkbox} from 'frozen/checkbox';
import {Button} from 'frozen/button';

const w=window as any,root=createRoot(document.getElementById('root')!);let serial=0;
const rows=Array.from({length:23},(_,index)=>({id:'fictional-'+String(index).padStart(2,'0'),name:['Cedar','Birch','Ash'][index%3]+' '+(index+1),count:(index*7)%29,status:index%2?'Ready':'Queued',flag:index%2===0,profile:{name:'Profile '+(index+1)}}));
w.defaultDataTableRows=rows;
function View({initial}:any){
 const[spec,setSpec]=React.useState(initial);w.patchDataTable=(patch:any)=>setSpec((old:any)=>({...old,...patch}));
 const columns=React.useMemo(()=>{
  if(spec.noColumns)return [];
  const header=(label:string)=>(context:any)=>{w.dataTable=context.table;return spec.plainHeaders?label:<Button variant="ghost" onClick={(event:any)=>context.column.toggleSorting(undefined,event.shiftKey)} data-sort-column={context.column.id}>{label}</Button>;};
  const name={accessorKey:spec.nested?'profile.name':'name',header:header('Fictional item'),sortingFn:spec.customCompare?((a:any,b:any,id:string)=>a.getValue(id).length-b.getValue(id).length):'auto'};
  const count={accessorKey:'count',header:header('Count')};
  const status={accessorKey:'status',header:(context:any)=>{w.dataTable=context.table;return spec.blankHeader?null:'Status';},cell:(context:any)=><Button variant="ghost" disabled={spec.disabledStatus} data-original-row={context.row.original.id} onClick={()=>w.dataTableEvents.push({kind:'original-cell-action',row:context.row.original.id,value:context.getValue()})}>{context.getValue()}</Button>};
  const selection={id:'select',header:(context:any)=>{w.dataTable=context.table;return <Checkbox aria-label="Select page" checked={context.table.getIsAllPageRowsSelected()} onChange={(event:any)=>context.table.toggleAllPageRowsSelected(event.target.checked)}/>;},cell:(context:any)=><Checkbox aria-label={'Select row '+context.row.id} checked={context.row.getIsSelected()} onChange={context.row.getToggleSelectedHandler()}/>};
  let definitions:any[]=spec.groups?[{id:'description',header:'Description',columns:spec.deepGroups?[{id:'identity',header:'Identity',columns:[name]},count]:[name,count]},status]:[name,count,status];
  if(spec.boolean)definitions.push({accessorKey:'flag',header:header('Flag')});
  if(spec.display)definitions.push({id:'details',header:'Consumer content',cell:(context:any)=><span data-consumer-row={context.row.id}>Original {context.row.original.id}</span>});
  if(!spec.noSelection)definitions.unshift(selection);
  return definitions;
 },[spec.groups,spec.deepGroups,spec.noColumns,spec.disabledStatus,spec.nested,spec.customCompare,spec.plainHeaders,spec.blankHeader,spec.boolean,spec.display,spec.noSelection]);
 const data=spec.rows??rows;
 return <section id="data-table-fixture" dir={spec.rtl?'rtl':'ltr'} style={{width:spec.width??660,margin:20}}><DataTable columns={columns} data={data} searchKey={spec.searchKey}/></section>;
}
w.showDataTable=(spec:any)=>{w.dataTableEvents=[];w.dataTable=null;root.render(<View key={++serial} initial={spec}/>);};
