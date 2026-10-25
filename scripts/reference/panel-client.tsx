import React from 'react';
import {createRoot} from 'react-dom/client';
import {Panel} from 'frozen/panel';

const root = createRoot(document.getElementById('root')!);
function View({initial}) {
  const [spec,setSpec] = React.useState(initial);
  const [value,setValue] = React.useState('Consumer text');
  window.updatePanel = patch => setSpec(previous=>({...previous,...patch}));
  const box = (slot,height,color,width?) => <div data-body={slot} style={{height,background:color,width}}/>;
  const filter = spec.slots==='filter'||spec.slots==='all' ? (spec.live ? <input data-input="filter" value={value} onChange={event=>setValue(event.target.value)}/> : box('filter',32,'#8db69c')) : spec.filter;
  const toolbar = spec.slots==='toolbar'||spec.slots==='all' ? (spec.live ? <button data-action="toolbar" onClick={()=>window.panelEvents.push('toolbar')}>Consumer action</button> : <>{box('tool-a',24,'#c79e72',80)}{box('tool-b',32,'#a65d57',64)}</>) : spec.toolbar;
  const dragHandle = spec.drag ? (spec.live ? <button data-action="drag" onClick={()=>window.panelEvents.push('drag')}>Handle</button> : box('drag',18,'#a1a5c0',18)) : spec.dragValue;
  const count = spec.countKind==='nan' ? NaN : spec.countKind==='infinity' ? Infinity : spec.countKind==='negative-infinity' ? -Infinity : spec.count;
  return <div id="host" style={{position:'absolute',left:20,top:20,width:spec.width,height:spec.height}} dir={spec.rtl?'rtl':'ltr'}>
    <Panel title={spec.title ?? 'Live coverage'} count={count} accent={spec.accent} filter={filter} toolbar={toolbar} dragHandle={dragHandle}
      width={spec.panelWidth} grow={spec.grow} isDragging={spec.isDragging} variant="monitor">
      {spec.empty ? null : spec.live ? <div style={{height:600}}><input data-input="body" defaultValue="Body value"/></div> : spec.scalar ?? box('content',spec.bodyHeight??600,'#b9c7d1')}
    </Panel>
  </div>;
}
window.panelEvents=[];
window.showPanel = spec=>root.render(<View key={spec.key??spec.id} initial={spec}/>);
