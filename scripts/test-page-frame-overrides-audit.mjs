import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import {verifyPageFrameOverrides} from './verify-page-frame-overrides.mjs';

const option = name => { const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1], name); return process.argv[at + 1]; };
const report = JSON.parse(await fs.readFile(option('--report'), 'utf8'));
const identity = {librarySha256: report.librarySha256, hostSha256: report.hostSha256};
verifyPageFrameOverrides(report, identity);
const mutations = {
  'stock font instead of inherited value': x => { x.checks.find(c => c.id.endsWith('/font-32')).observed.renderedFont = 14; },
  'clipped enlarged content': x => { x.checks.find(c => c.id.endsWith('/font-32')).observed.frameHeight = 1; },
  'replaced original content': x => { x.checks[0].observed.contentRetained = false; },
  'discarded child override': x => { x.checks.find(c => c.id.endsWith('/child-font-override')).observed.childFont = 40; },
  'shrunk caller width': x => { x.checks.find(c => c.id.endsWith('/oversized-content')).observed.childWidth = 200; },
  'ignored alignment': x => { x.checks.find(c => c.id.endsWith('/alignment-Right')).observed.x = 0; },
  'missing scenario': x => { x.checks.pop(); },
  'foreground input': x => { x.focus = 1; },
  'stale binary': x => { x.librarySha256 = '0'.repeat(64); },
};
for (const [name, mutate] of Object.entries(mutations)) { const candidate = structuredClone(report); mutate(candidate); assert.throws(() => verifyPageFrameOverrides(candidate, identity), undefined, name); }
const audit = {passed: true, negativeCasesRejected: Object.keys(mutations).length};
await fs.writeFile(option('--output'), JSON.stringify(audit, null, 2) + '\n'); console.log(JSON.stringify(audit));
