import assert from 'node:assert/strict';

export function cssRgba(css){
 const lab=css.match(/^oklab\(([^)]+)\)$/)?.[1].split(/[\s/]+/).map(Number);
 if(lab){
  const[L,a,b,alpha=1]=lab,l=(.9999999984505198*L+.39633779217376786*a+.2158037580607588*b)**3,m=(1.0000000088817609*L-.10556134232365635*a-.06385417477170591*b)**3,s=(1.0000000546724108*L-.08948418209496575*a-1.2914855378640917*b)**3;
  const encode=v=>Math.max(0,Math.min(255,Math.round((v<=.0031308?12.92*v:1.055*v**(1/2.4)-.055)*255)));
  return[encode(4.0765482669796285*l-3.3078604530458593*m+.23101769051428148*s),encode(-1.2684805115952908*l+2.6099303721593876*m-.3413293183059835*s),encode(-.004155928994110783*l-.7035236918221306*m+1.7078390411687758*s),alpha];
 }
 const rgb=css.match(/^rgba?\(([^)]+)\)$/)?.[1].split(',').map(Number);assert(rgb,`Unsupported computed CSS color: ${css}`);return[rgb[0],rgb[1],rgb[2],rgb[3]??1];
}

export function assertNativePaint(paint,css,context){
 assert(paint,`${context}: missing native paint evidence`);const channels=cssRgba(css);assert.match(paint.color,/^#[0-9a-fA-F]{8}$/);assert(Number.isFinite(paint.opacity));
 assert.equal(paint.color.slice(3).toLowerCase(),channels.slice(0,3).map(v=>v.toString(16).padStart(2,'0')).join(''),`${context}: RGB differs`);
 assert(Math.abs(parseInt(paint.color.slice(1,3),16)/255*paint.opacity-channels[3])<.000001,`${context}: fractional alpha differs`);
}
