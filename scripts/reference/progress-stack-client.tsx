import * as React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {ProgressStack} from 'frozen/progress-stack';
const root=createRoot(document.getElementById('root')!);
(window as any).showProgressStack=(spec:any)=>{
 document.getElementById('root')!.style.width=(spec.width??280)+'px';
 document.getElementById('root')!.dir=spec.rtl?'rtl':'ltr';
 const items=spec.items.map((item:any)=>({...item,value:Number(item.value)}));
 flushSync(()=>root.render(<ProgressStack items={items} max={spec.max===undefined?undefined:Number(spec.max)} showLegend={spec.showLegend} className={spec.custom?'custom-stack':undefined} barClassName={spec.custom?'custom-track':undefined}/>));
};
