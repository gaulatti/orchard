import React from 'react';
import {createRoot} from 'react-dom/client';
import {CollectionFilters} from 'frozen/collection-filters';

const w=window as any,root=createRoot(document.getElementById('root')!);let serial=0;
function View({initial}:any){
 const[spec,setSpec]=React.useState(initial);w.collectionSpec=spec;
 w.updateCollection=(patch:any)=>setSpec((previous:any)=>({...previous,...patch}));
 return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:20,top:20,width:spec.width}}>
  <CollectionFilters currentFilters={spec.currentFilters} currentSort={spec.currentSort}
   filterOptions={spec.filterOptions} sortOptions={spec.sortOptions} defaultExpanded={spec.expanded}
   onFilterChange={value=>{w.collectionRequests.push({kind:'filters',value});if(spec.accept)setSpec((previous:any)=>({...previous,currentFilters:value}));}}
   onSortChange={value=>{w.collectionRequests.push({kind:'sort',value});if(spec.accept)setSpec((previous:any)=>({...previous,currentSort:value}));}}/>
 </div>;
}
w.showCollection=spec=>{w.collectionRequests=[];root.render(<View key={++serial} initial={spec}/>);};
