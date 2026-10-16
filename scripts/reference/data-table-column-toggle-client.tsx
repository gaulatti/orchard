import React from 'react';
import {createRoot} from 'react-dom/client';
import {getCoreRowModel,useReactTable} from '@tanstack/react-table';
import {DataTableColumnToggle} from '@frozen/data-table-column-toggle';

const root=createRoot(document.getElementById('root')!);const state=window as any;let serial=0,changeModel:((value:any)=>void)|undefined;
state.events=[];state.inputEvents=[];state.outsideClicks=0;
const basicColumns=()=>[
 {id:'select',header:'Select',cell:()=> 'Select'},
 {accessorKey:'title',header:'Story'},
 {accessorKey:'status',header:'Status'},
 {id:'rendered',accessorFn:(row:any)=>row.title,header:()=> <span>Rendered heading</span>},
 {id:'blank',accessorFn:(row:any)=>row.status,header:''},
 {accessorKey:'locked',header:'Locked',enableHiding:false},
];
function Consumer({initial}:{initial:any}){
 const[model,setModel]=React.useState(initial);changeModel=value=>setModel((old:any)=>({...old,...value}));
 const dataA=React.useMemo(()=>initial.rows===0?[]:[{id:'r1',title:'Harbor',status:'Draft',locked:'A'},{id:'r2',title:'Reed',status:'Published',locked:'B'}],[]);
 const dataB=React.useMemo(()=>[{id:'s1',title:'Desk North',status:'Queued',locked:'C'},{id:'s2',title:'Desk West',status:'Ready',locked:'D'}],[]);
 const columns=React.useMemo(()=>initial.kind==='empty'?basicColumns().filter(item=>item.id==='select'||item.accessorKey==='locked'):initial.kind==='long'?Array.from({length:20},(_,index)=>({id:'field-'+index,accessorFn:(row:any)=>row.title,header:'Field '+index})):initial.kind==='group'?[{id:'group',header:'Group',columns:[{accessorKey:'title',header:'Story'},{accessorKey:'status',header:'Status'}]},{id:'extra',accessorFn:(row:any)=>row.title,header:'Extra'}]:basicColumns(),[]);
 const secondColumns=React.useMemo(()=>basicColumns().map(item=>item.accessorKey==='title'?{...item,header:'Desk story'}:item),[]);
 const[visibilityA,setVisibilityA]=React.useState(initial.visibility??{}),[visibilityB,setVisibilityB]=React.useState({});
 const tableA=useReactTable({data:dataA,columns,state:{columnVisibility:visibilityA},enableHiding:model.enableHiding??true,getRowId:row=>row.id,getCoreRowModel:getCoreRowModel(),onColumnVisibilityChange:updater=>setVisibilityA(old=>{const next=typeof updater==='function'?updater(old):updater;state.events.push({table:'A',previous:old,next});return next;})});
 const tableB=useReactTable({data:dataB,columns:secondColumns,state:{columnVisibility:visibilityB},getRowId:row=>row.id,getCoreRowModel:getCoreRowModel(),onColumnVisibilityChange:updater=>setVisibilityB(old=>{const next=typeof updater==='function'?updater(old):updater;state.events.push({table:'B',previous:old,next});return next;})});
 const originals=React.useRef<any[]>([]);if(!originals.current.length)originals.current=[tableA,tableB];
 const second=!!model.secondTable,table=second?tableB:tableA,data=second?dataB:dataA;
 state.table=table;state.setVisibility=(value:any)=>(second?setVisibilityB:setVisibilityA)(value);state.setOldVisibility=setVisibilityA;
 state.snapshotColumns=()=>({name:second?'B':'A',sameTable:table===originals.current[second?1:0],sameRows:table.options.data===data,visibility:table.getState().columnVisibility,
  columns:table.getAllColumns().map(column=>({id:column.id,hasAccessor:typeof column.accessorFn!=='undefined',canHide:column.getCanHide(),visible:column.getIsVisible(),header:typeof column.columnDef.header==='string'?column.columnDef.header:column.id,same:column===originals.current[second?1:0].getAllColumns().find((other:any)=>other.id===column.id)})),
  rows:table.getRowModel().rows.map(row=>({id:row.id,same:row.original===data.find(item=>item.id===row.id),cells:row.getVisibleCells().map(cell=>({column:cell.column.id,value:String(cell.getValue()??''),same:cell.row.original===row.original}))}))});
 React.useLayoutEffect(()=>{document.getElementById('data-probe')!.textContent=table.getRowModel().rows.map(row=>row.getVisibleCells().map(cell=>cell.column.id+'='+String(cell.getValue()??'')).join('|')).join(';');});
 return <DataTableColumnToggle table={table} {...(Object.hasOwn(model,'label')?{label:model.label}:{})}/>;
}
state.showColumns=(model:any)=>{state.events=[];state.inputEvents=[];state.outsideClicks=0;root.render(<Consumer key={++serial} initial={model}/>);};
state.updateColumns=(value:any)=>changeModel!(value);
document.getElementById('outside')!.addEventListener('click',()=>state.outsideClicks++);
for(const type of ['keydown','keyup','pointerdown','pointerup','click','focusin'])document.addEventListener(type,event=>{const target=event.target as HTMLElement;state.inputEvents.push({type,trusted:event.isTrusted,tag:target.tagName,role:target.getAttribute('role'),text:target===document.body?null:target.textContent,key:(event as KeyboardEvent).key??null,time:event.timeStamp});},true);
