import React from 'react';
import {createRoot} from 'react-dom/client';
import {DetailLayout} from 'frozen/detail-layout';

const root = createRoot(document.getElementById('root')!);
function View({initial}) {
  const [spec, setSpec] = React.useState(initial);
  const [mainValue, setMainValue] = React.useState('Main value');
  const [railValue, setRailValue] = React.useState('Rail value');
  window.updateDetail = patch => setSpec(previous => ({...previous, ...patch}));
  const box = (kind, height, color) => <div data-body={kind} style={{height, background: color}}/>;
  const rail = spec.rail === 'box' ? (spec.live ? <input data-input="rail" value={railValue} onChange={event => setRailValue(event.target.value)}/> : box('rail', spec.railHeight ?? 100, '#a65d57')) : spec.rail;
  const content = <DetailLayout ratio={spec.ratio} side={spec.side} sticky={spec.sticky} sideRail={rail} sideRailLabel={spec.label}
    dir={spec.rtl ? 'rtl' : 'ltr'} style={spec.layoutHeight ? {height: spec.layoutHeight} : undefined}>
    {spec.empty ? null : spec.live ? <input data-input="main" value={mainValue} onChange={event => setMainValue(event.target.value)}/> : box('main', spec.mainHeight ?? 400, '#b9c7d1')}
  </DetailLayout>;
  return <div id="host" style={{marginLeft: 20, marginTop: 20, width: spec.width}}>
    {spec.scroll ? <div id="viewport" style={{height: 240, overflow: 'auto', scrollbarWidth: 'none'}}>
      <div style={{paddingTop: 120, paddingBottom: 600}}>{content}</div>
    </div> : content}
  </div>;
}
window.showDetail = spec => root.render(<View key={spec.key ?? spec.id} initial={spec}/>);
