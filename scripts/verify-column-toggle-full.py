import hashlib,json,sys
from pathlib import Path

arguments=sys.argv[1:]; filtersMode='--filters' in arguments
if filtersMode:arguments.remove('--filters')
prior=Path('artifacts/smoke-shared-select-validated-full' if filtersMode else 'artifacts/smoke-pagination-owned-validated-full')
current,output=map(Path,arguments)
read=lambda path:json.loads(path.read_text(encoding='utf-8-sig'))
before={p.name for p in prior.glob('*.png')};after={p.name for p in current.glob('*.png')}
expected={f'data-table-{"filters" if filtersMode else "column-toggle"}-{scheme}.png' for scheme in ['Light','Dark']}
glyphs=[]
for component in ['sunburst-chart','radial-bar-chart']:
 for scheme in ['Light','Dark']:
  name=f'{component}-{scheme}.json';old={r['id']:r['glyphSha256'] for r in read(prior/name)['reports']};new={r['id']:r['glyphSha256'] for r in read(current/name)['reports']}
  glyphs.append(dict(component=component,scheme=scheme,cases=len(old),changed=[k for k,v in old.items() if new.get(k)!=v]))
finals=[read(p) for p in current.glob('*.png.layout.json')]
tips=[r for p in current.glob('tooltip-presentation-*.json') for r in read(p)]
sources=[r for scheme in ['Light','Dark'] for r in read(current/f'data-table-toolbar-source-{scheme}.json')]
tabs=[r for p in current.glob('data-table-toolbar-tab-*.json') for r in read(p)]
states=[dict(scheme=scheme,states=read(current/f'data-table-toolbar-reset-states-{scheme}.json'),release=read(current/f'data-table-toolbar-reset-release-{scheme}.json')) for scheme in ['Light','Dark']]
stationary=[read(current/f'data-table-toolbar-reset-stationary-{scheme}.json') for scheme in ['Light','Dark']]
pagination=[dict(scheme=scheme,basic=read(current/f'data-table-pagination-{scheme}.json'),lifecycle=read(current/f'data-table-pagination-lifecycle-{scheme}.json')) for scheme in ['Light','Dark']]
columns=[dict(scheme=scheme,lifecycle=read(current/f'data-table-column-toggle-lifecycle-{scheme}.json')) for scheme in ['Light','Dark']]
report=dict(previousPngs=len(before),pngs=len(after),missingPreviousPngs=sorted(before-after),newPngs=sorted(after-before),glyphs=glyphs,nativeGlyphCases=sum(r['cases'] for r in glyphs),finalPresentations=len(finals),failedPresentations=sum(r['stable']<2 for r in finals),tooltipGates=len(tips),failedTooltipGates=sum(not r['passed'] or r['stable']<2 for r in tips),toolbarSourceCases=len(sources),failedSourceCases=sum(bool(r['errors']) or r['writes']!=0 for r in sources),toolbarTabTransfers=tabs,resetStates=states,stationary=stationary,pagination=pagination)
report['passed']=read(current/'result.json')['passed'] and len(before)==(1032 if filtersMode else 1030) and len(after)==(1034 if filtersMode else 1032) and not report['missingPreviousPngs'] and after-before==expected and report['nativeGlyphCases']==174 and not any(r['changed'] for r in glyphs) and len(finals)==(18 if filtersMode else 16) and not report['failedPresentations'] and len(tips)==28 and not report['failedTooltipGates'] and len(sources)==118 and not report['failedSourceCases'] and len(tabs)==8 and all(r['downs']==1 and r['ups']==1 and r['downOwned'] and r['upOwned'] for r in tabs) and all([r['phase'] for r in item['states']]==['hover','held','released'] and item['states'][1]['IsPressed'] and not item['states'][2]['IsPressed'] and item['states'][2]['callbacks']==1 and item['release']['receipts']==1 and item['release']['exact'] for item in states) and all(item['passed'] and item['recreated'] and item['stationary']==item['after'] and item['callbacks']==1 for item in stationary) and all(item['basic']['passed'] and item['lifecycle']['passed'] and item['lifecycle']['finalObservers']==0 for item in pagination)
report['columns']=columns
report['passed']=report['passed'] and all(item['lifecycle']['passed'] and item['lifecycle']['finalObservers']==0 and item['lifecycle']['visibilityRequests']==1 and item['lifecycle']['replacementRequests']==0 and all(item['lifecycle'][key] for key in ['originalColumns','originalRows','disabledRejected','staleTemplateRejected','templateReplaced','remounted']) for item in columns)
if filtersMode:
 filters=[dict(scheme=scheme,lifecycle=read(current/f'data-table-filters-lifecycle-{scheme}.json'),unload=read(current/f'data-table-filters-unload-{scheme}.json')) for scheme in ['Light','Dark']]
 report['filters']=filters
 report['passed']=report['passed'] and all(item['lifecycle']['passed'] and item['lifecycle']['originalRequests']==1 and item['lifecycle']['replacementRequests']==0 and all(item['lifecycle'][key] for key in ['originalRows','originalColumn','staleOptionRejected','disabledOptionRejected','disabledFilterRejected','detachedTemplateRejected','templateReplaced','tableReplaced','remounted']) and item['unload']['passed'] and item['unload']['finalObservers']==0 for item in filters)
output.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ['resetStates','toolbarTabTransfers','pagination','stationary']}))
if not report['passed']:sys.exit(1)
