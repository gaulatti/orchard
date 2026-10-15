import React from 'react';
import {createRoot} from 'react-dom/client';
import {getCoreRowModel,getFilteredRowModel,useReactTable} from '@tanstack/react-table';
import {DataTableToolbar} from '@frozen/data-table-toolbar';

const root=createRoot(document.getElementById('root')!);let serial=0,changeModel:((change:any)=>void)|undefined;
const state=window as any;state.events=[];state.inputEvents=[];
function decode(value:any):any {
 if(value&&typeof value==='object'&&value.$undefined===true)return undefined;
 if(value&&typeof value==='object'&&Object.hasOwn(value,'$number')){
  switch(value.$number){case'NaN':return NaN;case'Infinity':return Infinity;case'-Infinity':return-Infinity;case'-0':return-0;default:throw Error('Unknown frozen number descriptor.');}
 }
 if(Array.isArray(value))return value.map(decode);
 if(value&&typeof value==='object')return Object.fromEntries(Object.entries(value).map(([key,item])=>[key,decode(item)]));
 return value;
}
function encode(value:any):any {
 if(value===undefined)return{$undefined:true};
 if(typeof value==='number'&&(!Number.isFinite(value)||Object.is(value,-0)))return{$number:Object.is(value,-0)?'-0':String(value)};
 if(Array.isArray(value))return value.map(encode);
 if(value&&typeof value==='object')return Object.fromEntries(Object.entries(value).map(([key,item])=>[key,encode(item)]));
 return value;
}
function Consumer({initial}:{initial:any}) {
 const[model,setModel]=React.useState(initial);changeModel=change=>setModel((old:any)=>({...old,...change}));
 const data=React.useMemo(()=>[
  {id:'harbor',title:'Harbor',category:'Harbor',status:'Draft'},
  {id:'reed',title:'Reed',category:'Reed',status:'Published'},
  {id:'vale',title:'Vale',category:'Vale',status:'Draft'}
 ],[]);
 const columnsA=React.useMemo(()=>[{accessorKey:'title',filterFn:'includesString'},{accessorKey:'status',filterFn:'includesString'}],[]);
 const columnsB=React.useMemo(()=>[{accessorKey:'category',filterFn:'includesString'},{accessorKey:'status',filterFn:'includesString'}],[]);
 const[filtersA,setFiltersA]=React.useState(()=>decode(initial.columnFilters??[]));
 const[filtersB,setFiltersB]=React.useState(()=>decode(initial.secondColumnFilters??[]));
 const[globalA,setGlobalA]=React.useState(()=>decode(initial.globalFilter));
 const[globalB,setGlobalB]=React.useState(()=>decode(initial.secondGlobalFilter));
 const originals=React.useRef<any[]>([]);
 function options(name:string,columns:any,filters:any,global:any,setFilters:any,setGlobal:any) {
  return{data,columns,getRowId:(row:any)=>row.id,initialState:{columnFilters:decode(name==='A'?initial.initialColumnFilters??[]:initial.secondInitialColumnFilters??[])},
   state:{columnFilters:filters,globalFilter:global},getCoreRowModel:getCoreRowModel(),getFilteredRowModel:getFilteredRowModel(),
   onColumnFiltersChange:(updater:any)=>setFilters((old:any)=>{const next=typeof updater==='function'?updater(old):updater;state.events.push({kind:'column-filters',table:name,previous:encode(old),next:encode(next)});return next;}),
   onGlobalFilterChange:(updater:any)=>setGlobal((old:any)=>{const next=typeof updater==='function'?updater(old):updater;state.events.push({kind:'global-filter',table:name,previous:encode(old),next:encode(next)});return next;})};
 }
 const tableA=useReactTable(options('A',columnsA,filtersA,globalA,setFiltersA,setGlobalA) as any);
 const tableB=useReactTable(options('B',columnsB,filtersB,globalB,setFiltersB,setGlobalB) as any);
 if(!originals.current.length)originals.current=[tableA,tableB];
 const table=model.secondTable?tableB:tableA,name=model.secondTable?'B':'A';
 function slot(value:any,role:string):any {
  if(value&&typeof value==='object'){
   if(value.kind==='buttons')return <>{value.labels.map((label:string,index:number)=><button key={index} data-slot={role} data-slot-index={index} style={{height:value.height??36,width:value.width??(role==='filters'?176:104),flexShrink:0,padding:'0 12px',border:'1px solid var(--color-sand)',borderRadius:7,fontSize:value.fontSize??14,lineHeight:'20px',whiteSpace:value.wrap?'normal':'nowrap'}} disabled={value.disabled} onClick={()=>{
    state.events.push({kind:'slot',slot:role,index,table:name,sameTable:table===originals.current[model.secondTable?1:0],sameRows:table.options.data===data});
    if(role==='filters')table.getColumn('status')!.setFilterValue(table.getColumn('status')!.getFilterValue()?'':'Draft');
   }}>{label}</button>)}</>;
   if(value.kind==='text')return <span data-slot={role} style={{fontSize:value.size??16,lineHeight:'24px',whiteSpace:value.wrap?'normal':'nowrap'}}>{value.text}</span>;
  }
  return value;
 }
 state.table=table;state.model=model;state.setColumnFilters=(value:any)=>table.setColumnFilters(decode(value));state.setGlobalFilter=(value:any)=>table.setGlobalFilter(decode(value));
 state.snapshotTable=()=>({name,sameTable:table===originals.current[model.secondTable?1:0],state:{columnFilters:encode(table.getState().columnFilters),globalFilter:encode(table.getState().globalFilter)},initialColumnFilters:encode(table.initialState.columnFilters),globalTruthy:Boolean(table.getState().globalFilter),
  columns:table.getAllColumns().map(column=>({id:column.id,value:encode(column.getFilterValue()),same:column===originals.current[model.secondTable?1:0].getAllColumns().find((candidate:any)=>candidate.id===column.id)})),
  rows:table.getRowModel().rows.map(row=>({id:row.id,title:(row.original as any).title,same:row.original===data.find(record=>record.id===row.id)}))});
 const visible=table.getRowModel().rows.map(row=>(row.original as any).title);
 React.useLayoutEffect(()=>{document.getElementById('data-probe')!.textContent=visible.join('|');});
 return <DataTableToolbar table={table} searchColumn={model.searchColumn} searchPlaceholder={model.searchPlaceholder} filterRenderers={slot(model.filters,'filters')} actions={slot(model.actions,'actions')} className={model.className}/>;
}
state.showToolbar=(model:any)=>{state.events=[];state.inputEvents=[];root.render(<Consumer key={++serial} initial={model}/>);};
state.updateToolbar=(change:any)=>changeModel!(change);
for(const type of ['keydown','keyup','input','pointermove','pointerdown','pointerup','click','focusin'])document.addEventListener(type,event=>{
 const target=event.target as HTMLElement;
 state.inputEvents.push({type,trusted:event.isTrusted,tag:target.tagName,id:target.id,slot:target.getAttribute('data-slot'),text:target===document.body||target===document.documentElement?null:target.textContent,key:(event as KeyboardEvent).key??null,value:target instanceof HTMLInputElement?target.value:null,x:(event as PointerEvent).clientX??null,y:(event as PointerEvent).clientY??null,buttons:(event as PointerEvent).buttons??null,time:event.timeStamp});
},true);
