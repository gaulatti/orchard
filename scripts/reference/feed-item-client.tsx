import React from 'react';
import {createRoot} from 'react-dom/client';
import {FeedItem} from 'frozen/feed-item';
const root=createRoot(document.getElementById('root')!);
const portrait='data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" width="8" height="8"><rect width="8" height="8" fill="%23407fa3"/></svg>';
function View({initial}) {
 const [spec,setSpec]=React.useState(initial);window.updateFeedItem=patch=>setSpec(previous=>({...previous,...patch}));
 const relevance=spec.relevanceKind==='nan'?NaN:spec.relevanceKind==='infinity'?Infinity:spec.relevance;
 return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:20,top:20,width:spec.width}}>
  <FeedItem author={spec.author??'Ada Example'} avatarSrc={spec.image==='valid'?portrait:spec.image==='broken'?'data:image/png;base64,broken':undefined} init={spec.init}
   content={spec.content??'Fictional local coverage. A reusable feed component preserves original source data.'} language={spec.language??'en'} postedAt={spec.postedAt??'2026-10-18T15:00:00Z'} relevance={relevance??87.5} sourceUrl={spec.sourceUrl??'https://news.example/story'} categories={spec.categories??['Local','Review']}/>
 </div>;
}
window.showFeedItem=spec=>root.render(<View key={spec.key??spec.id} initial={spec}/>);
