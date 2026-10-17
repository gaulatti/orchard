import React from 'react';
import { createRoot } from 'react-dom/client';
import { flushSync } from 'react-dom';
import { IconBadge } from 'frozen/icon-badge';

const root = createRoot(document.getElementById('root')!);
let key = 0;
const events: string[] = [];
function content(spec: any) {
  if (spec.child === 'empty') return null;
  if (spec.child === 'number') return Number(spec.value);
  if (spec.child === 'bigint') return BigInt(spec.value);
  if (spec.child === 'boolean') return spec.value === 'true';
  if (spec.child === 'text') return spec.value;
  if (spec.child === 'large') return <span data-child="large" style={{width:100,height:80,flexShrink:0,backgroundColor:'currentColor'}} />;
  if (spec.child === 'rich') return <><button id="child-button" aria-label="Child action" onClick={() => events.push('child')}>A</button><input id="child-input" aria-label="Child value" defaultValue="Harbor" style={{width:40}} /></>;
  return <svg data-child="vector" width="24" height="24" viewBox="0 0 24 24" aria-hidden="true" style={spec.childColor ? {color:spec.childColor} : undefined}><path d="M4 5H20V19H4Z" fill="none" stroke="currentColor" strokeWidth="2" /></svg>;
}
(window as any).badgeEvents = events;
(window as any).showBadge = (spec: any, remount = true) => {
  if (remount) { key++; events.length = 0; }
  const host = document.getElementById('root')!;
  host.style.width = spec.parentWidth ? `${spec.parentWidth}px` : '280px';
  host.style.display = spec.parentFlex ? 'flex' : 'block';
  const props = {id:'badge',size:spec.size,variant:spec.variant,style:spec.style,
    ...(spec.named ? {role:'group','aria-label':'Consumer icon marker'} : {}),
    ...(spec.child === 'rich' ? {onClick:() => events.push('badge')} : {})};
  flushSync(() => root.render(<IconBadge key={key} {...props}>{content(spec)}</IconBadge>));
};
