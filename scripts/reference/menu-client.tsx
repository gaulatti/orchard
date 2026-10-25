import React from 'react';
import {createRoot} from 'react-dom/client';
import * as D from 'frozen/dropdown-menu';
import * as C from 'frozen/context-menu';
const root=createRoot(document.getElementById('root')!);
let serial=0;
function View({initial}:any){
 const [spec,setSpec]=React.useState(initial);const w=window as any;
 w.updateMenu=(patch:any)=>setSpec((p:any)=>({...p,...patch}));
 const record=(name:string)=>()=>w.menuEvents.push(name);
 const changed=(key:string,value:any)=>{w.menuEvents.push({key,value});if(spec.accept)setSpec((p:any)=>({...p,[key]:value}));};
 const rows=spec.empty?[]:spec.rows??[{id:'alpha',label:'Fictional Profile'},{id:'disabled',label:'Unavailable action',disabled:true},{id:'settings',label:'Fictional Settings'}];
 const triggerStyle={width:180,height:36,fontFamily:'Encode Sans',fontSize:14,border:'1px solid #E6D5B8',borderRadius:7,background:spec.scheme==='Dark'?'#182533':'white',color:spec.scheme==='Dark'?'#F0F4F8':'#2D2D2D'};
 if(spec.kind==='context')return <div dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:spec.left??20,top:spec.top??20}}><C.ContextMenu><C.ContextMenuTrigger asChild><div id="menu-trigger" tabIndex={0} style={{...triggerStyle,width:220,height:70}}>Fictional context target</div></C.ContextMenuTrigger><C.ContextMenuContent id="root-menu">{rows.map((r:any)=><C.ContextMenuItem key={r.id} id={'item-'+r.id} inset={r.inset??spec.inset} disabled={r.disabled} onSelect={record(r.id)}>{r.label}</C.ContextMenuItem>)}</C.ContextMenuContent></C.ContextMenu></div>;
 return <div dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:spec.left??20,top:spec.top??20}}><D.DropdownMenu><D.DropdownMenuTrigger asChild><button id="menu-trigger" style={triggerStyle}>Fictional menu</button></D.DropdownMenuTrigger><D.DropdownMenuContent id="root-menu" align={spec.align??'start'}>
  {!spec.empty&&spec.label&&<D.DropdownMenuLabel inset={spec.inset}>Visible fictional fields</D.DropdownMenuLabel>}
  {rows.map((r:any)=><D.DropdownMenuItem key={r.id} id={'item-'+r.id} inset={r.inset??spec.inset} disabled={r.disabled} onSelect={record(r.id)}>{r.rich?<span style={{display:'flex',alignItems:'center',gap:8}}><svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L9 17l-4 1 1-4Z" fill="none" stroke="currentColor" strokeWidth="2"/></svg><span>{r.label}</span></span>:r.label}{r.shortcut&&<D.DropdownMenuShortcut>{r.shortcut}</D.DropdownMenuShortcut>}</D.DropdownMenuItem>)}
  {!spec.empty&&spec.selection&&<><D.DropdownMenuSeparator/><D.DropdownMenuCheckboxItem id="check-item" checked={spec.checked??false} onCheckedChange={v=>changed('checked',v)}>Fictional notifications</D.DropdownMenuCheckboxItem><D.DropdownMenuRadioGroup value={spec.radio??'a'} onValueChange={v=>changed('radio',v)}><D.DropdownMenuRadioItem id="radio-a" value="a">Fictional Alpha mode</D.DropdownMenuRadioItem><D.DropdownMenuRadioItem id="radio-b" value="b">Fictional Beta mode</D.DropdownMenuRadioItem></D.DropdownMenuRadioGroup></>}
  {!spec.empty&&spec.submenu&&<D.DropdownMenuSub><D.DropdownMenuSubTrigger id="sub-trigger" inset={spec.inset}>More fictional choices</D.DropdownMenuSubTrigger><D.DropdownMenuSubContent id="sub-menu"><D.DropdownMenuLabel>Nested fictional fields</D.DropdownMenuLabel><D.DropdownMenuItem id="sub-action" onSelect={record('sub-action')}>Nested original action</D.DropdownMenuItem><D.DropdownMenuItem disabled>Nested unavailable action</D.DropdownMenuItem></D.DropdownMenuSubContent></D.DropdownMenuSub>}
 </D.DropdownMenuContent></D.DropdownMenu></div>;
}
(window as any).menuEvents=[];(window as any).showMenu=(spec:any)=>{(window as any).menuEvents=[];root.render(<View key={++serial} initial={spec}/>);};
