import React from 'react';
import {createRoot} from 'react-dom/client';
import {AlertDialog} from 'frozen/alert-dialog';
import {Button} from 'frozen/button';
import {Modal} from 'frozen/modal';
import {Drawer} from 'frozen/drawer';
import {Popover} from 'frozen/popover';

const w=window as any,root=createRoot(document.getElementById('root')!);let serial=0;
function View({initial}:any){
 const [spec,setSpec]=React.useState(initial);
 const [parent,setParent]=React.useState(!!initial.parentOpen),[middle,setMiddle]=React.useState(!!initial.middleOpen);
 w.updateAlertDialog=(patch:any)=>setSpec((previous:any)=>({...previous,...patch}));
 w.closeAlertParent=()=>setParent(false);
 w.unmountAlertFixture=()=>root.render(null);
 const cancel=()=>{w.alertEvents.push('cancel');if(spec.acceptCancel)setSpec((previous:any)=>({...previous,isOpen:false}));};
 const confirm=()=>{w.alertEvents.push('confirm');if(spec.acceptConfirm)setSpec((previous:any)=>({...previous,isOpen:false}));};
 const alert=<AlertDialog isOpen={!!spec.isOpen} title={spec.title??'Fictional confirmation'} description={Object.hasOwn(spec,'description')?spec.description:'Review the fictional local component example before continuing.'}
  cancelLabel={spec.cancelLabel} confirmLabel={spec.confirmLabel} confirmLoading={!!spec.confirmLoading} variant={spec.variant} className={spec.custom?'max-w-lg rounded-none':undefined} onCancel={cancel} onConfirm={confirm}/>;
 return <>
  <div id="fixture-background" style={{position:'fixed',inset:0,background:'var(--color-background)',backgroundImage:spec.pattern?'repeating-linear-gradient(90deg,transparent,transparent 30px,var(--color-sea) 30px,var(--color-sea) 32px)':undefined}}/>
  <div id="outside-controls" style={{position:'absolute',left:16,top:16,display:'flex',gap:12}}>
   <Button id="opener" onClick={()=>spec.nested?setParent(true):setSpec((previous:any)=>({...previous,isOpen:true}))}>Open fictional confirmation</Button>
   <Button id="outside-action" onClick={()=>w.alertEvents.push('outside')}>Outside action</Button>
  </div>
  {spec.nested?<Drawer isOpen={parent} onClose={()=>setParent(false)} title="Fictional parent drawer">
   <Button id="middle-opener" onClick={()=>setMiddle(true)}>Open fictional parent dialog</Button>
   <Modal isOpen={middle} onClose={()=>setMiddle(false)} title="Fictional parent dialog">
    <Button id="alert-opener" onClick={()=>setSpec((previous:any)=>({...previous,isOpen:true}))}>Open child confirmation</Button>
    {alert}
    <Popover content={<><Button id="branch-first">First original popup action</Button><Button id="branch-last">Last original popup action</Button></>}><Button id="branch-opener">Open original popup</Button></Popover>
    <Button id="middle-last">Last original dialog action</Button>
   </Modal>
   <Button id="parent-last">Last original drawer action</Button>
  </Drawer>:alert}
 </>;
}
w.showAlertDialog=(spec:any)=>{document.documentElement.dir=spec.rtl?'rtl':'ltr';w.alertEvents=[];root.render(<View key={++serial} initial={spec}/>);};
