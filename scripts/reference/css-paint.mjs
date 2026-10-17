import assert from 'node:assert/strict';

export function cssRgba(css){
 const lab=css.match(/^oklab\(([^)]+)\)$/)?.[1].split(/[\s/]+/).map(Number);
 if(lab){
  const[L,a,b,alpha=1]=lab,l=(L+.3963377774*a+.2158037573*b)**3,m=(L-.1055613458*a-.0638541728*b)**3,s=(L-.0894841775*a-1.2914855480*b)**3;
  const encode=v=>Math.max(0,Math.min(255,Math.round((v<=.0031308?12.92*v:1.055*v**(1/2.4)-.055)*255)));
  return[encode(4.0767416621*l-3.3077115913*m+.2309699292*s),encode(-1.2684380046*l+2.6097574011*m-.3413193965*s),encode(-.0041960863*l-.7034186147*m+1.7076147010*s),alpha];
 }
 const rgb=css.match(/^rgba?\(([^)]+)\)$/)?.[1].split(',').map(Number);assert(rgb,`Unsupported computed CSS color: ${css}`);return[rgb[0],rgb[1],rgb[2],rgb[3]??1];
}

export function assertNativePaint(paint,css,context){
 assert(paint,`${context}: missing native paint evidence`);const channels=cssRgba(css);assert.match(paint.color,/^#[0-9a-fA-F]{8}$/);assert(Number.isFinite(paint.opacity));
 assert.equal(paint.color.slice(3).toLowerCase(),channels.slice(0,3).map(v=>v.toString(16).padStart(2,'0')).join(''),`${context}: RGB differs`);
 assert(Math.abs(parseInt(paint.color.slice(1,3),16)/255*paint.opacity-channels[3])<.000001,`${context}: fractional alpha differs`);
}
