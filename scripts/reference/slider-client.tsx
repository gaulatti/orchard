import React from 'react';
import {createRoot} from 'react-dom/client';
import {Slider} from 'frozen/slider';
const root=createRoot(document.getElementById('root')!);let current:any={};
window.sliderRequests=[];
function number(spec:any,key:string,defaultValue:any){return spec[key+'Kind']==='nan'?NaN:spec[key+'Kind']==='infinity'?Infinity:spec[key+'Kind']==='negative-infinity'?-Infinity:spec[key]??defaultValue;}
function Stage({spec}:any){return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:24,top:24,width:spec.width}}>
 <Slider key={spec.mountKey??spec.id} id="source-range" aria-label={spec.name??'Consumer range'} min={number(spec,'min',undefined)} max={number(spec,'max',undefined)} step={number(spec,'step',undefined)} value={number(spec,'value',undefined)}
  label={spec.label} showValue={spec.showValue} disabled={spec.disabled} onChange={value=>{window.sliderRequests.push(value);if(!current.freeze)window.showSlider({...current,value,valueKind:undefined});}}/>
</div>;}
window.showSlider=(spec:any)=>{current=spec;root.render(<Stage spec={spec}/>);};
