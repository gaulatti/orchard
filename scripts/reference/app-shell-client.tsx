import React from 'react';
import {createRoot} from 'react-dom/client';
import {AppShell} from 'frozen/app-shell';
const root=createRoot(document.getElementById('root')!);
function View({initial}) {
 const [spec,setSpec]=React.useState(initial);
 window.updateAppShell=patch=>setSpec(previous=>({...previous,...patch}));
 const slot=(name,height,color)=>Object.hasOwn(spec,name)?spec[name]:height===undefined?null:<div data-slot={name} style={{height,background:color}}/>;
 const header=slot('header',spec.headerHeight,'#8db69c');
 const footer=slot('footer',spec.footerHeight,'#b9c7d1');
 const body=spec.live?<div data-slot="body"><input data-input="body" defaultValue="Original shell input"/><button data-action="body" onClick={()=>window.appShellEvents.push('body')}>Local shell action</button></div>:slot('body',spec.bodyHeight,'#e6c7a7');
 return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:20,top:20,width:spec.width,'--bleecker-header-height':spec.headerToken===undefined?undefined:spec.headerToken+'px'}}>
  <AppShell className="reference-app-shell" header={header} footer={footer} offsetHeader={spec.offset} contentClassName={spec.contentPadding===undefined?undefined:'reference-content-padding'}>
   {body}
  </AppShell>
  <style>{`.reference-app-shell{${spec.height===undefined?'':`height:${spec.height}px;`}${spec.minimum===undefined?'':`min-height:${spec.minimum}px;`}${spec.padding===undefined?'':`padding:${spec.padding}px;`}}.reference-content-padding{padding:${spec.contentPadding}px}`}</style>
 </div>;
}
window.appShellEvents=[];
window.showAppShell=spec=>root.render(<View key={spec.key??spec.id} initial={spec}/>);
