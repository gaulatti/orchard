import React from 'react';
import {createRoot} from 'react-dom/client';
import {Popover} from 'frozen/popover';

const w = window as any, root = createRoot(document.getElementById('root')!); let serial = 0;
function View({initial}: any) {
  const [spec, setSpec] = React.useState(initial);
  const [open, setOpen] = React.useState(initial.open);
  w.updatePopover = (patch: any) => setSpec((previous: any) => ({...previous, ...patch}));
  w.setPopoverOpen = setOpen;
  const changed = (value: boolean) => { w.popoverRequests.push(value); if (spec.accept) setOpen(value); };
  return <>
    <button id="outside" style={{position:'absolute',left:16,top:16,width:110,height:32}}>Outside action</button>
    <div id="anchor-host" style={{position:'absolute',left:spec.x??320,top:spec.y??240}}>
      <Popover side={spec.side} align={spec.align} sideOffset={spec.sideOffset} defaultOpen={spec.defaultOpen} open={spec.controlled?open:undefined} onOpenChange={changed}
        content={spec.empty?null:<div id="original-content" style={{width:spec.contentWidth??160,height:spec.contentHeight??60}}>
          {spec.editor?<><input id="editor" aria-label="Fictional popover editor" defaultValue="Original" style={{width:140,height:24}}/><button id="last-action">Last action</button></>:<span>Fictional content</span>}
        </div>}>
        <button id="trigger" disabled={spec.disabled} onClick={event=>{w.popoverTriggerClicks++;if(spec.prevent)event.preventDefault();}} style={{width:spec.triggerWidth??80,height:spec.triggerHeight??32}}>Open</button>
      </Popover>
    </div>
  </>;
}
w.showPopover = (spec: any) => {
  document.documentElement.dir = spec.rtl?'rtl':'ltr'; w.popoverRequests=[]; w.popoverTriggerClicks=0;
  root.render(<View key={++serial} initial={spec}/>);
};
