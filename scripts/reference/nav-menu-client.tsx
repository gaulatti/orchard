import React from 'react';
import {createRoot} from 'react-dom/client';
import {NavMenu} from 'frozen/nav-menu';

const w=window as any,root=createRoot(document.getElementById('root')!);let serial=0;
function View({initial}:any){
 const[spec,setSpec]=React.useState(initial);w.navSpec=spec;
 w.updateNav=(patch:any)=>setSpec((previous:any)=>({...previous,...patch}));
 const renderLink=spec.custom?props=><a href={props.item.href} className={props.className} data-source-marker={props.item.marker} onClick={event=>{event.preventDefault();w.navRequests.push({kind:'custom',marker:props.item.marker,original:spec.items.includes(props.item)});props.onClick?.();}}>{props.children}</a>:undefined;
 return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:20,top:20,width:spec.width}}>
  <NavMenu direction={spec.direction} items={spec.items} renderLink={renderLink} onItemClick={()=>w.navRequests.push({kind:'menu-click'})}/>
 </div>;
}
w.showNav=spec=>{w.navRequests=[];root.render(<View key={++serial} initial={spec}/>);};
