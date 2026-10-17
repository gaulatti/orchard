export function expo(x){if(x<=0)return 0;if(x>=1)return 1;let t=x;for(let index=0;index<12;index++){const value=3*(1-t)*(1-t)*t*.16+3*(1-t)*t*t*.3+t*t*t,derivative=3*(1-t)*(1-t)*.16+6*(1-t)*t*(.3-.16)+3*t*t*(1-.3);t=Math.max(0,Math.min(1,t-(value-x)/derivative));}return 1-(1-t)**3;}
export const revealHeight=(height,fraction)=>Math.floor(height*fraction*64)/64;
import {cssRgba} from './css-paint.mjs';
export function cssEndpoint(css){
 const lab=css.match(/^oklab\(([^)]+)\)$/)?.[1].split(/[\s/]+/).map(Number);
 const [first,second,third,alpha=1]=lab??cssRgba(css);
 return{Legacy:!lab,First:first*alpha,Second:second*alpha,Third:third*alpha,Alpha:alpha};
}
