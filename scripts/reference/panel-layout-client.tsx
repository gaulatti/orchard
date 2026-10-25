import React from 'react';
import {createRoot} from 'react-dom/client';
import {PanelLayout, PanelColumn} from 'frozen/panel-layout';

const root = createRoot(document.getElementById('root')!);
const colors = ['#b9c7d1', '#407fa3', '#b84a50', '#dac99f'];
function Block({spec, index}: any) {
  return <div data-item={spec.id} style={{width: spec.width, minWidth: spec.minWidth, height: spec.height ?? '100%',
    minHeight: 0, flexShrink: 0, background: colors[index % colors.length], display: spec.hidden ? 'none' : undefined}}/>;
}
function children(items: any) {
  return items.map((item: any, index: number) => item.items
    ? <PanelColumn key={item.id} grow={item.grow} className={item.hidden ? 'hidden' : undefined}>
        {children(item.items)}
      </PanelColumn>
    : <Block key={item.id} grow={item.grow} spec={item} index={index}/>);
}
window.showPanelLayout = (spec: any) => root.render(<div id="host" dir={spec.rtl ? 'rtl' : 'ltr'}
  style={{position: 'absolute', left: 20, top: 20, width: spec.width, height: spec.height, display: 'flex'}}>
  {spec.column ? <PanelColumn>{children(spec.items)}</PanelColumn>
    : <PanelLayout padding={spec.padding ? 'p-[8px]' : undefined}>{children(spec.items)}</PanelLayout>}
</div>);
