import assert from 'node:assert/strict';
export function verifyNativeHotkeys(r,source){
 assert.equal(r.hotkeyContracts.length,168);assert.equal(r.windowsKeyContracts.length,38);assert.equal(r.caseSearchContracts.length,8);assert(r.readOnlyKeyboardContracts.length>=26);
 for(const theme of ['Light','Dark']){
  const actual=r.hotkeyContracts.filter(c=>c.theme===theme),expected=source.cases.filter(c=>c.theme===theme);assert.deepEqual(actual.map(c=>c.id),expected.map(c=>c.id));
  for(let i=0;i<84;i++){const c=actual[i],s=expected[i];assert.equal(c.passed,true);assert.deepEqual(c.hint,s.before.hint);assert.deepEqual(c.renderedHint,s.before.hint);assert.equal(c.handled,s.defaultPrevented);assert.equal(c.open,s.open);assert.deepEqual(c.requests,s.requests);assert.equal(c.physicalKeyRoutingVerified,false);if(c.file)assert.equal(c.file,`command-spotlight-hotkeys-${theme}-${c.id}.png`);}
  const windows=r.windowsKeyContracts.filter(c=>c.theme===theme);assert.deepEqual(windows.map(c=>c.expectedName),['ArrowUp','ArrowDown','ArrowLeft','ArrowRight','F1','F24','Backspace','CapsLock','NumLock','ScrollLock','Escape','Alt','Meta','ContextMenu','PageUp','PageDown','Delete','Tab','Enter']);
  for(const c of windows){assert(c.passed&&c.handled&&c.open);assert.equal(c.legacyName,null);assert.equal(c.physicalKeyRoutingVerified,false);}
  const search=r.caseSearchContracts.filter(c=>c.theme===theme);assert.deepEqual(search.map(c=>c.id),['cyrillic','latin','garay','sigma']);for(const [i,c]of search.entries()){assert.equal(c.passed,true);assert.equal(c.query,['ᲊ','ɤ','𐵰','a:ς'][i]);assert.equal(c.actualQuery,c.query);assert.equal(c.selectedId,c.id);assert.equal(c.originalRetained,true);assert.equal(c.rows,1);}
  const layout=r.readOnlyKeyboardContracts.filter(c=>c.theme===theme);assert(layout.length>=13);for(const c of layout){assert.equal(c.available,true);assert.equal(c.passed,true);assert.equal(c.name,c.repeatedName);assert.equal(c.keyboardStateWritten,false);assert.equal(c.keyboardLayoutLoaded,false);assert.equal(c.hardwareInputSent,false);if(c.exactUsLayoutVerified){assert.equal(c.language,0x409);assert.equal(c.name,c.expectedUsName);assert.equal(c.actualModifiers,c.modifiers);}else assert.equal(c.expectedUsName,null);}
 }
 assert.equal(r.hotkeyContracts.filter(c=>c.file).length,8);
 assert(r.catalogs.every(c=>c.hotkeyCycleVerified===true));
 return{hotkeyStates:168,windowsKeyChecks:38,caseSearchChecks:8,readOnlyKeyboardChecks:r.readOnlyKeyboardContracts.length,hotkeyPngs:8};
}
