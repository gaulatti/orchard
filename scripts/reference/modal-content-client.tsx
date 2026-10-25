import React from 'react';
import {createRoot} from 'react-dom/client';
import {Modal} from 'frozen/modal';
const root=createRoot(document.getElementById('root')!);let serial=0;
function View({kind}:any){
 const [grown,setGrown]=React.useState(false);(window as any).growModalContent=setGrown;
 const text=grown?Array(kind==='plain'?200:60).fill(kind==='plain'?'Fictional original plain content':'Fictional original model paragraph').join('\n'):'Fictional short note';
 let content:React.ReactNode;
 if(kind==='text')content=<div id="original" style={{whiteSpace:'pre-line'}}>{text}</div>;
 else if(kind==='plain')content=text;
 else if(kind==='binding')content=<div id="original" style={{height:grown?1000:20}}/>;
 else content=<div id="original"><div id="child" style={{height:kind==='child'&&grown?1000:20}}/>{kind==='collection'&&grown?<div style={{height:1000}}/>:null}</div>;
 return <Modal isOpen title="Fictional content contract" onClose={()=>{}}>{content}</Modal>;
}
(window as any).showModalContent=(kind:any)=>root.render(<View key={++serial} kind={kind}/>);
