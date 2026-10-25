import React from 'react';
import {createRoot} from 'react-dom/client';
import {Pagination} from 'frozen/pagination';

const root=createRoot(document.getElementById('root')!);
let serial=0;
function View({initial}:any){
 const [spec,setSpec]=React.useState(initial);
 window.updatePagination=patch=>setSpec(previous=>({...previous,...patch}));
 window.paginationSpec=spec;
 return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:20,top:20,width:spec.width??600}}>
  <Pagination currentPage={spec.currentPage} totalPages={spec.totalPages} hasPrevPage={spec.hasPrevPage} hasNextPage={spec.hasNextPage}
   onPageChange={page=>{window.paginationRequests.push(page);if(spec.accept)setSpec(previous=>({...previous,currentPage:page}));}}/>
 </div>;
}
window.paginationRequests=[];
window.showPagination=spec=>{window.paginationRequests=[];root.render(<View key={++serial} initial={spec}/>);};
