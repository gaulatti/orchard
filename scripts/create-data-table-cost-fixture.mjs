import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';

const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export function createDataTableCostFixture(original){
 const eol=original.includes('\r\n')?'\r\n':'\n',changes=[];let source=original;
 const edit=(before,after,count=1)=>{assert.equal(source.split(before).length,count+1,'Source anchor changed: '+before.slice(0,100));source=source.replaceAll(before,after);changes.push({before,after,count});};
 edit('using System.Diagnostics;','using System.Diagnostics;'+eol+'using System.Diagnostics.Tracing;');
 const helper=[
  '    [EventSource(Name="Orchard-DataTable-NativeCost")]',
  '    private sealed class CostEvents:EventSource',
  '    {',
  '        internal static readonly CostEvents Log=new();',
  '        [Event(1,Level=EventLevel.Informational)]',
  '        public void Phase(string stage,string phase,int boundary,int sourceRows,int pageSize,long allocatedBytes,long pauseTicks)',
  '        {if(IsEnabled())WriteEvent(1,stage,phase,boundary,sourceRows,pageSize,allocatedBytes,pauseTicks);}',
  '    }',
 ].join(eol)+eol;
 edit('    private delegate bool Visitor',helper+'    private delegate bool Visitor');
 edit('var stage="initial";','var stage="initial";var activeRows=0;var activePageSize=0;var markerCount=0;'+eol+'        void Mark(string phase,int boundary){Require(CostEvents.Log.IsEnabled(),"Native-cost trace detached before fixture completion.");CostEvents.Log.Phase(stage,phase,boundary,activeRows,activePageSize,GC.GetTotalAllocatedBytes(false),GC.GetTotalPauseDuration().Ticks);markerCount++;}');
 const layout=source.split(eol).find(l=>l.includes('async Task Layout()'));
 const markedLayout=layout.replace('for(var i=0;i<3;i++){','for(var i=0;i<3;i++){Mark("layout-wait-"+i,0);').replace('await idle.Task;root.Measure','await idle.Task;Mark("layout-wait-"+i,1);Mark("layout-pass-"+i,0);root.Measure').replace('root.UpdateLayout();Guard();}}','root.UpdateLayout();Guard();Mark("layout-pass-"+i,1);}}');
 assert.notEqual(markedLayout,layout);edit(layout,markedLayout);
 const capture=source.split(eol).find(l=>l.includes('async Task Capture(string file)'));
 const markedCapture=capture.replace('Capture(string file){Guard();','Capture(string file){Mark("capture",0);Guard();').replace('});Guard();}','});Guard();Mark("capture",1);}');edit(capture,markedCapture);
 edit('            foreach(var timing in',[
  '            var traceDeadline=Stopwatch.GetTimestamp()+Stopwatch.Frequency*20L;',
  '            while(!CostEvents.Log.IsEnabled()){Require(Stopwatch.GetTimestamp()<traceDeadline,"Native-cost trace was not attached within20seconds.");await Task.Delay(50);Guard();}',
  '            Mark("fixture",0);Mark("loading-probes",0);',
  '            foreach(var timing in',
 ].join(eol));
 edit('            foreach(var profile in','            Mark("loading-probes",1);'+eol+'            foreach(var profile in');
 edit('var count=profile.Count;var size=profile.PageSize;var warmup=', 'var count=profile.Count;var size=profile.PageSize;stage=$"{count}-{size}-setup";activeRows=count;activePageSize=size;Mark("setup",0);var warmup=');
 edit('root.Children.Add(data);await Layout();Guard();','root.Children.Add(data);await Layout();Guard();Mark("setup",1);');
 edit('stage=$"{count}-{size}-{operation}";Guard();','stage=$"{count}-{size}-{operation}";Mark("operation",0);Mark("before-observation",0);Guard();');
 edit('var timer=Stopwatch.StartNew();mutation();var mutationMs=', 'Mark("before-observation",1);Mark("timed",0);var timer=Stopwatch.StartNew();Mark("mutation",0);mutation();Mark("mutation",1);var mutationMs=');
 edit('await Layout();timer.Stop();process.Refresh();','await Layout();timer.Stop();Mark("timed",1);process.Refresh();');
 edit('Guard();var snapshot=Verify(operation,before,beforeCells,unloaded);','Guard();Mark("verification",0);var snapshot=Verify(operation,before,beforeCells,unloaded);Mark("verification",1);');
 const progress='await File.WriteAllTextAsync(output+".progress.json",JsonSerializer.Serialize(new{stage,completedMeasurements=measurements.Count,measurement}));';
 edit(progress,'Mark("progress-write",0);'+progress+'Mark("progress-write",1);Mark("operation",1);');
 const cleanup=source.split(eol).find(l=>l.includes('contracts.Add('));edit(cleanup,'                Mark("cleanup",0);'+eol+cleanup+eol+'                Mark("cleanup",1);');
 edit('            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format=', '            Mark("fixture",1);'+eol+'            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format=');
 edit('orchard-data-table-performance-native-v3','orchard-data-table-performance-diagnostic-v1',2);
 edit('passed=true,measurements,snapshots','passed=true,diagnosticEvents=true,normalTimingRun=false,phaseMarkers=markerCount,measurements,snapshots');
 let roundtrip=source;for(const {before,after,count}of changes.toReversed()){assert.equal(roundtrip.split(after).length,count+1);roundtrip=roundtrip.replaceAll(after,before);}assert.equal(roundtrip,original,'Instrumentation must be fully reversible without changing the workload');
 return{source,changes:changes.map(({count,before})=>({count,anchorSha256:hash(before)})),sourceSha256:hash(original),generatedSha256:hash(source),sourceRoundtripExact:true};
}

if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const arg=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};
 const input=arg('--source'),output=path.resolve(arg('--output'));await fs.mkdir(output,{recursive:false});
 let normalBackup=null;
 if(process.argv.includes('--normal-host')){
  const app=path.resolve(arg('--normal-host')),bytes=await fs.readFile(path.join(app,'Orchard.Catalog.dll'));
  assert(bytes.includes(Buffer.from('orchard-data-table-performance-native-v3','utf16le')),'Preserve a normal v3 host before diagnostic compilation');assert(!bytes.includes(Buffer.from('orchard-data-table-performance-diagnostic-v1','utf16le')));
  normalBackup=path.join(output,'normal-backup');await fs.mkdir(normalBackup);const files=[];
  for(const name of ['Orchard.dll','Orchard.Catalog.dll','Orchard.Catalog.pdb','Orchard.pdb','resources.pri','Orchard.Catalog.exe','Orchard.Catalog.deps.json','Orchard.Catalog.runtimeconfig.json']){const b=await fs.readFile(path.join(app,name));await fs.writeFile(path.join(normalBackup,name),b,{flag:'wx'});files.push({name,sha256:hash(b),bytes:b.length});}
  await fs.writeFile(path.join(normalBackup,'identity.json'),JSON.stringify({recordedAt:new Date().toISOString(),app,files,restoreRequiredAfterDiagnosticBuild:true},null,2)+'\n',{flag:'wx'});
 }
 const generated=createDataTableCostFixture(await fs.readFile(input,'utf8')),fixture=path.join(output,'DataTablePerformanceFixture.cs');await fs.writeFile(fixture,generated.source,{flag:'wx'});
 const escaped=fixture.replaceAll('&','&amp;').replaceAll('"','&quot;').replaceAll('<','&lt;');
 const remove=path.relative(path.resolve('samples/Orchard.Catalog'),path.resolve(input)).replaceAll('\\','/').replaceAll('&','&amp;').replaceAll('"','&quot;');
 const targets=`<Project><ItemGroup Condition="'$(MSBuildProjectName)' == 'Orchard.Catalog'"><Compile Remove="${remove}"/><Compile Include="${escaped}" Link="DataTablePerformanceFixture.cs"/></ItemGroup></Project>\n`;
 await fs.writeFile(path.join(output,'NativeCost.targets'),targets,{flag:'wx'});
 await fs.writeFile(path.join(output,'generation.json'),JSON.stringify({recordedAt:new Date().toISOString(),input,fixture,targets:path.join(output,'NativeCost.targets'),normalBackup,...generated,source:undefined,diagnosticOnly:true,normalTimingRun:false,productionSourceChanged:false},null,2)+'\n',{flag:'wx'});console.log(JSON.stringify({sourceRoundtripExact:true,sourceSha256:generated.sourceSha256,generatedSha256:generated.generatedSha256,normalBackup,diagnosticOnly:true}));
}
