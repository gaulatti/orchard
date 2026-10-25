import React from 'react';
import {createRoot} from 'react-dom/client';
import {HoverCard} from 'frozen/hover-card';

const w = window as any, root = createRoot(document.getElementById('root')!); let serial = 0;
function View({initial}: any) {
  const [spec, setSpec] = React.useState(initial);
  w.updateHoverCard = (patch: any) => setSpec((previous: any) => ({...previous, ...patch}));
  return <>
    <button id="outside" style={{position:'absolute',left:16,top:16,width:110,height:32}}>Outside action</button>
    <div id="anchor-host" style={{position:'absolute',left:spec.x??320,top:spec.y??240,width:spec.clip?140:undefined,height:spec.clip?110:undefined,overflow:spec.clip?'hidden':undefined}}>
      <HoverCard side={spec.side} className={spec.custom?'w-96 p-6 rounded-none':undefined}
        content={spec.empty?null:<div id="original-content" style={{width:spec.contentWidth??160,height:spec.contentHeight??60}}>
          {spec.editor?<><input id="editor" aria-label="Fictional hover card editor" defaultValue="Original" style={{width:140,height:24}}/><button id="last-action">Last action</button></>:<span>Fictional content</span>}
        </div>}>
        <button id="trigger" disabled={spec.disabled} onClick={()=>w.hoverCardClicks++} style={{width:spec.triggerWidth??80,height:spec.triggerHeight??32}}>Hover</button>
      </HoverCard>
      <span id="following" style={{display:'inline-block',width:20,height:20,background:'var(--color-sea)'}}/>
    </div>
  </>;
}
w.showHoverCard = (spec: any) => {
  document.documentElement.dir = spec.rtl?'rtl':'ltr'; w.hoverCardClicks=0;
  root.render(<View key={++serial} initial={spec}/>);
};
