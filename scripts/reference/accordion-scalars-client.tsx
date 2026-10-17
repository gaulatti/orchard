import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {Accordion} from 'frozen/accordion';
const root=createRoot(document.getElementById('root')!);let generation=0;
window.showScalar=(spec:any)=>{
 const value=spec.scalar.kind==='number'?Number(spec.scalar.value):spec.scalar.kind==='boolean'?spec.scalar.value==='true':spec.scalar.kind==='null'?null:spec.scalar.value;
 const item:any={id:'first',title:spec.caption??'Scalar sample',subtitle:'Consumer text',content:<div style={{padding:16,fontSize:14,lineHeight:'20px'}}>Consumer body</div>};item[spec.slot]=value;
 flushSync(()=>root.render(<Accordion key={++generation} items={[item]} defaultExpandedId="first"/>));
};
