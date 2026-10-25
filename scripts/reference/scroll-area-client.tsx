import React from 'react';
import {createRoot} from 'react-dom/client';
import {ScrollArea} from 'frozen/scroll-area';

const w = window as any, root = createRoot(document.getElementById('root')!); let serial = 0;
function View({initial}: any) {
  const [spec, setSpec] = React.useState(initial);
  w.updateScrollArea = (patch: any) => setSpec((previous: any) => ({...previous, ...patch}));
  const style: React.CSSProperties = {};
  if (Object.hasOwn(spec, 'height')) style.height = spec.height;
  if (Object.hasOwn(spec, 'styleMaxHeight')) style.maxHeight = spec.styleMaxHeight;
  if (Object.hasOwn(spec, 'styleOverflow')) style.overflow = spec.styleOverflow;
  return <>
    <button id="outside" style={{position:'absolute',left:16,top:16,width:110,height:32}}>Outside action</button>
    <div id="host" style={{position:'absolute',left:spec.x??100,top:spec.y??100,width:spec.width??256,height:spec.parentHeight}}>
      <ScrollArea id="area" data-marker="original-scroll-area" aria-label="Fictional scroll area" role="region" tabIndex={spec.tabIndex}
        orientation={spec.orientation} maxHeight={spec.maxHeight===null?undefined:spec.maxHeight??'240px'} className={spec.custom?'border border-sand/20 p-2':undefined}
        style={style}
        onScroll={()=>{w.scrollAreaEventCount++;}}>
        {spec.empty?null:<div id="original-content" style={{width:spec.contentWidth===null?undefined:spec.contentWidth??520,height:spec.contentHeight??480,background:'var(--color-background)',backgroundImage:'linear-gradient(90deg,transparent 49%,var(--color-sea) 49%,var(--color-sea) 51%,transparent 51%)'}}>
          {spec.editor?<><input id="editor" aria-label="Fictional scrolling editor" defaultValue="Original" style={{width:140,height:24}}/><button id="last-action">Last action</button></>:<span>Fictional content</span>}
          {spec.wrap?<p style={{margin:0}}>Fictional wrapping text demonstrates a naturally sized caller paragraph with multiple words.</p>:null}
        </div>}
      </ScrollArea>
      <div id="following" style={{width:20,height:20,background:'var(--color-sea)'}}/>
    </div>
  </>;
}
w.showScrollArea = (spec: any) => { document.documentElement.dir=spec.rtl?'rtl':'ltr'; w.scrollAreaEventCount=0;root.render(<View key={++serial} initial={spec}/>); };
