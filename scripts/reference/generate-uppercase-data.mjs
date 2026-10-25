import fs from'node:fs/promises';import crypto from'node:crypto';import assert from'node:assert/strict';
const opt=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const bytes=await fs.readFile(opt('--unicode-data'));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),'2e1efc1dcb59c575eedf5ccae60f95229f706ee6d031835247d843c11d96470c');
const mappings=bytes.toString('utf8').split(/\r?\n/).filter(Boolean).map(line=>line.split(';')).filter(fields=>fields[12]).map(fields=>fields[0]+';'+fields[12]);
assert(mappings.includes('1C8A;1C89'));const text='# Unicode 17.0.0 simple uppercase mappings; generated, do not edit.\n# Source: https://www.unicode.org/Public/17.0.0/ucd/UnicodeData.txt\n# Source SHA256: 2e1efc1dcb59c575eedf5ccae60f95229f706ee6d031835247d843c11d96470c\n# License: licenses/LICENSE-Unicode.txt\n'+mappings.join('\n')+'\n';
const target='src/Orchard/Text/Uppercase-17.0.0.txt';if(process.argv.includes('--check'))assert.equal(await fs.readFile(target,'utf8'),text);else await fs.writeFile(target,text);console.log(JSON.stringify({passed:true,mappings:mappings.length,checked:process.argv.includes('--check')}));
