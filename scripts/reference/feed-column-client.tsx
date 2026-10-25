import React from 'react';
import {createRoot} from 'react-dom/client';
import {FeedColumn} from 'frozen/feed-column';
const root=createRoot(document.getElementById('root')!);
function View({initial}) {
 const [spec,setSpec]=React.useState(initial),[value,setValue]=React.useState('Consumer filter');
 window.updateFeedColumn=patch=>setSpec(previous=>({...previous,...patch}));
 const filter=spec.filterHeight ? <div data-slot="filter" style={{height:spec.filterHeight,background:'#8db69c'}}/> : spec.live ? <input data-input="filter" value={value} onChange={event=>setValue(event.target.value)}/> : spec.filter;
 const count=spec.countKind==='nan'?NaN:spec.countKind==='infinity'?Infinity:spec.count;
 return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:20,top:20,width:spec.hostWidth??800,height:spec.height}}>
  <FeedColumn title={spec.title??'Live coverage'} count={count} width={spec.width} accent={spec.accent} filter={filter}
    style={spec.override?{width:240,minWidth:180,borderRadius:4}:undefined}>
   {spec.empty?null:spec.live?<div data-slot="content" style={{height:600}}><input data-input="body" defaultValue="Body value"/><button data-action="body" onClick={()=>window.feedColumnEvents.push('body')}>Local action</button></div>:spec.scalar??<div data-slot="content" style={{height:spec.bodyHeight??600,background:'#b9c7d1'}}/>}
  </FeedColumn>
 </div>;
}
window.feedColumnEvents=[];
window.showFeedColumn=spec=>root.render(<View key={spec.key??spec.id} initial={spec}/>);
