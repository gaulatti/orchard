import React from 'react';
import {createRoot} from 'react-dom/client';
import {FeedGrid} from 'frozen/feed-grid';
const root=createRoot(document.getElementById('root')!);
const colors=['#b9c7d1','#407fa3','#b84a50','#dac99f'];
window.showFeedGrid=(spec:any)=>root.render(<div id="host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:20,top:20,width:spec.width}}>
 <FeedGrid minColumnWidth={spec.minimum} style={spec.override?{padding:7,gap:5}:undefined}>
  {Array.from({length:spec.count},(_,i)=><div key={i} data-item={i} style={{height:40+(i%3)*20,background:colors[i%colors.length],display:spec.hidden?.includes(i)?'none':undefined}}/>)}
 </FeedGrid>
</div>);
