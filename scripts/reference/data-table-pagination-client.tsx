import React from 'react';
import {createRoot} from 'react-dom/client';
import {getCoreRowModel,getFilteredRowModel,getPaginationRowModel,getSortedRowModel,useReactTable} from '@tanstack/react-table';
import {DataTablePagination} from '@frozen/data-table-pagination';

const root=createRoot(document.getElementById('root')!);let serial=0,changeModel:((change:any)=>void)|undefined;
const state=window as any;state.events=[];state.inputEvents=[];
function Consumer({initial}:{initial:any}){
 const[model,setModel]=React.useState(initial);changeModel=change=>setModel((old:any)=>({...old,...change}));
 const data=React.useMemo(()=>Array.from({length:initial.rows??47},(_,index)=>({id:'row-'+String(index+1).padStart(3,'0'),title:(initial.prefix??'Coverage ')+String(index+1).padStart(3,'0'),status:index%2?'Published':'Draft'})),[]);
 const secondData=React.useMemo(()=>Array.from({length:initial.secondRows??8},(_,index)=>({id:'desk-'+String(index+1).padStart(3,'0'),title:'Desk '+String(index+1).padStart(3,'0'),status:index%2?'Published':'Draft'})),[]);
 const columns=React.useMemo(()=>[{accessorKey:'title',filterFn:'includesString'},{accessorKey:'status',filterFn:'equalsString'}],[]);
 const[pagination,setPagination]=React.useState({pageIndex:initial.pageIndex??0,pageSize:initial.pageSize??10});
 const[rowSelection,setRowSelection]=React.useState(initial.selection??{}),[columnFilters,setColumnFilters]=React.useState(initial.filters??[]),[sorting,setSorting]=React.useState(initial.sorting??[]);
 const[secondPagination,setSecondPagination]=React.useState({pageIndex:0,pageSize:2}),[secondSelection,setSecondSelection]=React.useState({}),[secondFilters,setSecondFilters]=React.useState([]),[secondSorting,setSecondSorting]=React.useState([]);
 const originals=React.useRef<any[]>([]);
 const tableA=useReactTable({data,columns,getRowId:row=>row.id,state:{pagination,rowSelection,columnFilters,sorting},
  onPaginationChange:updater=>setPagination(old=>{const next=typeof updater==='function'?updater(old):updater;state.events.push({kind:'pagination',table:'A',previous:old,next});return next;}),
  onRowSelectionChange:setRowSelection,onColumnFiltersChange:setColumnFilters,onSortingChange:setSorting,
  getCoreRowModel:getCoreRowModel(),getFilteredRowModel:getFilteredRowModel(),getSortedRowModel:getSortedRowModel(),getPaginationRowModel:getPaginationRowModel(),
  autoResetPageIndex:initial.autoResetPageIndex??true,...(Object.hasOwn(model,'manualPageCount')?{manualPagination:true,pageCount:model.manualPageCount}:{}),
 });
 const tableB=useReactTable({data:secondData,columns,getRowId:row=>row.id,state:{pagination:secondPagination,rowSelection:secondSelection,columnFilters:secondFilters,sorting:secondSorting},onPaginationChange:updater=>setSecondPagination(old=>{const next=typeof updater==='function'?updater(old):updater;state.events.push({kind:'pagination',table:'B',previous:old,next});return next;}),onRowSelectionChange:setSecondSelection,onColumnFiltersChange:setSecondFilters,onSortingChange:setSecondSorting,getCoreRowModel:getCoreRowModel(),getFilteredRowModel:getFilteredRowModel(),getSortedRowModel:getSortedRowModel(),getPaginationRowModel:getPaginationRowModel()});
 if(!originals.current.length)originals.current=[tableA,tableB];
 const table=model.secondTable?tableB:tableA,originalData=model.secondTable?secondData:data,name=model.secondTable?'B':'A';
 state.table=table;state.setPagination=(value:any)=>(model.secondTable?setSecondPagination:setPagination)(value);state.setSelection=model.secondTable?setSecondSelection:setRowSelection;state.setFilters=model.secondTable?setSecondFilters:setColumnFilters;state.setSorting=model.secondTable?setSecondSorting:setSorting;
 state.setFirstPageIndex=(value:number)=>tableA.setPageIndex(value);
 state.snapshotPagination=()=>({name,sameTable:table===originals.current[model.secondTable?1:0],sameRows:table.options.data===originalData,pagination:table.getState().pagination,selection:table.getState().rowSelection,filters:table.getState().columnFilters,sorting:table.getState().sorting,pageCount:table.getPageCount(),pageOptions:table.getPageOptions(),canPrevious:table.getCanPreviousPage(),canNext:table.getCanNextPage(),filteredCount:table.getFilteredRowModel().rows.length,selectedCount:table.getFilteredSelectedRowModel().rows.length,columns:table.getAllColumns().map(column=>({id:column.id,same:column===originals.current[model.secondTable?1:0].getAllColumns().find((other:any)=>other.id===column.id)})),rows:table.getRowModel().rows.map(row=>({id:row.id,title:row.original.title,same:row.original===originalData.find(item=>item.id===row.id)}))});
 React.useLayoutEffect(()=>{document.getElementById('data-probe')!.textContent=table.getRowModel().rows.map(row=>row.original.title).join('|');});
 return <DataTablePagination table={table} {...(Object.hasOwn(model,'pageSizeOptions')?{pageSizeOptions:model.pageSizeOptions}:{})} className={model.className}/>;
}
state.showPagination=(model:any)=>{state.events=[];state.inputEvents=[];root.render(<Consumer key={++serial} initial={model}/>);};
state.updatePagination=(change:any)=>changeModel!(change);
for(const type of ['keydown','keyup','pointermove','pointerdown','pointerup','click','focusin'])document.addEventListener(type,event=>{const target=event.target as HTMLElement;state.inputEvents.push({type,trusted:event.isTrusted,tag:target.tagName,role:target.getAttribute('role'),text:target===document.body||target===document.documentElement?null:target.textContent,key:(event as KeyboardEvent).key??null,x:(event as PointerEvent).clientX??null,y:(event as PointerEvent).clientY??null,time:event.timeStamp});},true);
