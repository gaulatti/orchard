import React from 'react';
import {createRoot} from 'react-dom/client';
import {AdminShell} from 'frozen/admin-shell';
const root=createRoot(document.getElementById('root')!);
function View({initial}){
 const [spec,setSpec]=React.useState(initial);window.updateAdminShell=patch=>setSpec(previous=>({...previous,...patch}));
 const slot=(name,width,height)=>Object.hasOwn(spec,name)?spec[name]:height===undefined?null:<div data-slot={name} style={{width:spec[name+'Width']??width,height,flexShrink:0}}/>;
 const body=spec.live?<div data-slot="body"><input data-input="body" defaultValue="Original admin input"/><button data-action="body" onClick={()=>window.adminShellEvents.push('body')}>Local admin action</button></div>:slot('body',undefined,spec.bodyHeight);
 return <div id="host" style={{position:'absolute',left:20,top:20,width:spec.width}} dir={spec.rtl?'rtl':'ltr'}>
  <AdminShell className="reference-admin-shell" header={slot('header',undefined,spec.headerHeight)} sidebar={slot('sidebar',spec.sidebarWidth,spec.sidebarHeight)} footer={slot('footer',undefined,spec.footerHeight)} contentClassName="reference-admin-main">{body}</AdminShell>
  <style>{`.reference-admin-shell{${spec.height===undefined?'':`height:${spec.height}px;`}${spec.minimum===undefined?'':`min-height:${spec.minimum}px;`}${spec.padding===undefined?'':`padding:${spec.padding}px;`}}.reference-admin-main{${spec.contentPadding===undefined?'':`&>div:first-child{padding:${spec.contentPadding}px}`}}`}</style>
 </div>;
}
window.adminShellEvents=[];window.showAdminShell=spec=>root.render(<View key={spec.key??spec.id} initial={spec}/>);
