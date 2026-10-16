import React from 'react';
import {createRoot} from 'react-dom/client';
import {getCoreRowModel,getFilteredRowModel,useReactTable} from '@tanstack/react-table';
import {DataTableSelectFilter} from '@frozen/data-table-filters';

const state=window as any,root=createRoot(document.getElementById('root')!);let serial=0,update:((next:any)=>void)|undefined;
const encode=(value:any):any=>value===undefined?{$undefined:true}:Array.isArray(value)?value.map(encode):value&&typeof value==='object'?Object.fromEntries(Object.entries(value).map(([key,item])=>[key,encode(item)])):value;
state.events=[];state.inputEvents=[];state.outsideClicks=0;
function Consumer({initial}:{initial:any}){
 const[model,setModel]=React.useState(initial);update=value=>setModel((old:any)=>({...old,...value}));
 const dataA=React.useMemo(()=>[{id:'harbor',title:'Harbor',status:'Draft'},{id:'reed',title:'Reed',status:'Published'},{id:'vale',title:'Vale',status:'Draft'}],[]),dataB=React.useMemo(()=>[{id:'desk',title:'Desk North',status:'Published'},{id:'wire',title:'Wire West',status:'Draft'}],[]);
 const columns=React.useMemo(()=>[{accessorKey:'title',filterFn:'includesString'},{accessorKey:'status',filterFn:'equalsString'}],[]);
 const[filtersA,setFiltersA]=React.useState(initial.filters??[]),[filtersB,setFiltersB]=React.useState(initial.secondFilters??[]);
 const options=(name:string,data:any,filters:any,setFilters:any)=>({data,columns,getRowId:(row:any)=>row.id,state:{columnFilters:filters},getCoreRowModel:getCoreRowModel(),getFilteredRowModel:getFilteredRowModel(),onColumnFiltersChange:(updater:any)=>setFilters((old:any)=>{const next=typeof updater==='function'?updater(old):updater;state.events.push({table:name,previous:encode(old),next:encode(next)});return next;})});
 const a=useReactTable(options('A',dataA,filtersA,setFiltersA) as any),b=useReactTable(options('B',dataB,filtersB,setFiltersB) as any),originals=React.useRef<any[]>([]);if(!originals.current.length)originals.current=[a,b];
 const second=!!model.second,table=second?b:a,data=second?dataB:dataA,columnId=model.columnId??'status',column=table.getAllLeafColumns().find(item=>item.id===columnId);
 state.setFilters=(value:any)=>(second?setFiltersB:setFiltersA)(value);state.setOldFilters=setFiltersA;
 state.filterSnapshot=()=>({name:second?'B':'A',sameTable:table===originals.current[second?1:0],sameRows:table.options.data===data,columnId,columnPresent:!!column,filterValue:encode(column?.getFilterValue()),filters:encode(table.getState().columnFilters),columns:table.getAllLeafColumns().map(item=>({id:item.id,same:item===originals.current[second?1:0].getAllLeafColumns().find((original:any)=>original.id===item.id)})),rows:table.getRowModel().rows.map(row=>({id:row.id,title:row.original.title,status:row.original.status,same:row.original===data.find(item=>item.id===row.id)}))});
 React.useLayoutEffect(()=>{document.getElementById('data-probe')!.textContent=table.getRowModel().rows.map(row=>row.id+':'+row.original.title+':'+row.original.status).join('|');});
 const choices=model.options??[{label:'Draft',value:'Draft'},{label:'Published',value:'Published'}];
 return <DataTableSelectFilter table={table} columnId={columnId} options={choices} {...(Object.hasOwn(model,'placeholder')?{placeholder:model.placeholder}:{})} className={model.className}/>;
}
state.showFilters=(model:any)=>{state.events=[];state.inputEvents=[];state.outsideClicks=0;root.render(<Consumer key={++serial} initial={model}/>);};state.updateFilters=(value:any)=>update!(value);
document.getElementById('outside')!.addEventListener('click',()=>state.outsideClicks++);
for(const type of ['keydown','keyup','pointerdown','pointerup','click','focusin'])document.addEventListener(type,event=>{const target=event.target as HTMLElement;state.inputEvents.push({type,trusted:event.isTrusted,tag:target.tagName,role:target.getAttribute('role'),text:target===document.body||target===document.documentElement?null:target.textContent,key:(event as KeyboardEvent).key??null,time:event.timeStamp});},true);
