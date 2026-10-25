import React from 'react';
import {createRoot} from 'react-dom/client';
import {Modal} from 'frozen/modal';
import * as C from 'frozen/context-menu';
const root=createRoot(document.getElementById('root')!);let serial=0;
function View({spec}:any){
 const [owner,setOwner]=React.useState(true),[upper,setUpper]=React.useState(false),[mode,setMode]=React.useState(spec.mode);
 const w=window as any;w.updateContextModal=(patch:any)=>{if('owner'in patch)setOwner(patch.owner);if('upper'in patch)setUpper(patch.upper);if('mode'in patch)setMode(patch.mode);};
 return <><button id="background">Fictional background action</button><Modal isOpen={owner} title="Fictional component owner" onClose={()=>w.contextModalEvents.push('owner-close-request')}>
  <C.ContextMenu onOpenChange={open=>w.contextModalEvents.push(open?'open':'closed')}>
   <C.ContextMenuTrigger asChild><button id="context-target" style={{width:220,height:70}}>Fictional context target</button></C.ContextMenuTrigger>
   <C.ContextMenuContent id="owned-context-menu" onCloseAutoFocus={()=>w.contextModalEvents.push('close-auto-focus')}>
    <C.ContextMenuItem id="context-action" onSelect={event=>{w.contextModalEvents.push('select');if(mode!=='selection')event.preventDefault();if(mode==='ownership')setUpper(true);}}>Original fictional action</C.ContextMenuItem>
    <C.ContextMenuItem id="context-disabled" disabled>Unavailable fictional action</C.ContextMenuItem>
   </C.ContextMenuContent>
  </C.ContextMenu>
  <Modal isOpen={upper} title="Fictional upper component" onClose={()=>w.contextModalEvents.push('upper-close-request')}><button id="upper-action">Original upper action</button></Modal>
 </Modal></>;
}
(window as any).contextModalEvents=[];(window as any).showContextModal=(spec:any)=>{(window as any).contextModalEvents=[];root.render(<View key={++serial} spec={spec}/>);};
