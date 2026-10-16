import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output');
const read=async name=>JSON.parse((await fs.readFile(path.join(native,name),'utf8')).replace(/^\uFEFF/,''));
const result=await read('result.json'),report=await read('loading-overlay-scroll.json');
assert.equal(result.passed,true);assert.equal(result.component,'loading-overlay-scroll');assert.deepEqual(report.failures,[]);
for(const phase of['initial','scroll-300','scroll-1000','scroll-0','scroll-1600','render-transform'])assert(report.reports.some(frame=>frame.phase===phase));
for(const frame of report.reports){
 assert.equal(frame.valid,true);
 for(const [actual,expected]of[[frame.at.X,0],[frame.at.Y,0],[frame.ActualWidth,frame.viewport.Width],[frame.ActualHeight,frame.viewport.Height]])assert(Number.isFinite(actual)&&Math.abs(actual-expected)<=.05);
}
const audit={passed:true,componentAccepted:false,frames:report.reports.length,viewportPinnedDuringScrollAndTransform:true,geometryTolerance:.05,sourcePixelParityVerified:false};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
