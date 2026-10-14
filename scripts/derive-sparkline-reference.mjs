import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2])throw new Error('Pass the frozen Sparkline browser capture JSON path.');
const bytes=await readFile(process.argv[2]),capture=JSON.parse(bytes);
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='7e143cfb557045bfad591be820c28b3bcb29e77469261f51723317f4ffef0ed5'||capture.react!=='19.2.4'||capture.recharts!=='3.8.1'||capture.cases.length!==36||new Set(capture.cases.map(x=>`${x.scheme}/${x.id}`)).size!==36)throw new Error('Frozen Sparkline capture differs.');
await writeFile(new URL('../tests/Reference/SparklineLayout.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,captureSha256:createHash('sha256').update(bytes).digest('hex')}));
