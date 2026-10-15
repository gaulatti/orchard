const buttons=(labels,extra={})=>({kind:'buttons',labels,...extra});
const basic={searchColumn:'title',filters:buttons(['Status']),actions:buttons(['New record'])};
const samples=[
 {id:'empty'}, {id:'default',...basic}, {id:'search-only',searchColumn:'title'}, {id:'filters-only',filters:buttons(['Status'])}, {id:'actions-only',actions:buttons(['New record'])},
 {id:'column-filter',...basic,columnFilters:[{id:'title',value:'Harbor'}]},
 {id:'other-column-filter',...basic,columnFilters:[{id:'status',value:'Draft'}]},
 {id:'global-filter',...basic,globalFilter:'Harbor'},
 {id:'both-filters',...basic,columnFilters:[{id:'title',value:'Harbor'}],globalFilter:'Harbor'},
 {id:'initial-filter',...basic,columnFilters:[{id:'title',value:'Reed'}],initialColumnFilters:[{id:'title',value:'Harbor'}]},
 {id:'initial-and-global',...basic,columnFilters:[{id:'status',value:'Draft'}],initialColumnFilters:[{id:'title',value:'Harbor'}],globalFilter:'Harbor'},
 {id:'empty-column-value',...basic,columnFilters:[{id:'title',value:''}]},
 {id:'missing-column',...basic,searchColumn:'absent'}, {id:'empty-search-column',...basic,searchColumn:''},
 {id:'placeholder-empty',searchColumn:'title',searchPlaceholder:''},
 {id:'placeholder-long',searchColumn:'title',searchPlaceholder:'Search a very long fictional record title or status across the local workspace'},
 {id:'multilingual',searchColumn:'title',searchPlaceholder:'港口 日本語 العربية Δοκιμή',filters:buttons(['公開状態']),actions:buttons(['إضافة'])},
 {id:'rtl',...basic,direction:'rtl'},
 {id:'multiple-slots',searchColumn:'title',filters:buttons(['Status','Category'],{width:130}),actions:buttons(['Export','New record'],{width:110})},
 {id:'disabled-slots',...basic,filters:buttons(['Status'],{disabled:true}),actions:buttons(['New record'],{disabled:true}),globalFilter:'Harbor'},
 {id:'tall-slots',searchColumn:'title',filters:buttons(['A multiline consumer filter'],{height:64,width:176,wrap:true}),actions:buttons(['New record'],{height:48})},
 {id:'text-slots',searchColumn:'title',filters:{kind:'text',text:'Consumer filters'},actions:{kind:'text',text:'Consumer actions'}},
 {id:'zero-slots',filters:0,actions:0}, {id:'false-slots',filters:false,actions:false}, {id:'true-slots',filters:true,actions:true}, {id:'empty-string-slots',filters:'',actions:''},
 {id:'small-viewport',...basic,viewportWidth:375,hostWidth:340},
 {id:'below-breakpoint',...basic,viewportWidth:639,hostWidth:600},
 {id:'at-breakpoint',...basic,viewportWidth:640,hostWidth:600},
 {id:'above-breakpoint',...basic,viewportWidth:641,hostWidth:600},
 {id:'constrained-host',...basic,hostWidth:360}, {id:'tiny-host',...basic,hostWidth:180},
 {id:'zero-host',...basic,hostWidth:0}, {id:'fractional-host',...basic,hostWidth:779.5},
 {id:'wide-host',...basic,hostWidth:1200},
 {id:'custom-layout',...basic,className:'consumer-toolbar',customCss:'.consumer-toolbar{gap:20px;padding:8px;border:2px solid var(--color-sea)}'},
 {id:'live-source',...basic},
 ...[['undefined',{$undefined:true}],['null',null],['false',false],['zero',0],['negative-zero',{$number:'-0'}],['nan',{$number:'NaN'}],['infinity',{$number:'Infinity'}],['empty',''],['space',' '],['string-zero','0'],['number',1],['true',true],['array',[]],['object',{}]].map(([id,globalFilter])=>({id:'global-'+id,globalFilter})),
 ...[['zero',0],['false',false],['number',42],['array',['Harbor','Reed']],['object',{query:'Harbor'}],['undefined',{$undefined:true}],['null',null],['nan',{$number:'NaN'}]].map(([id,value])=>({id:'column-'+id,searchColumn:'title',columnFilters:[{id:'title',value}]}))
];
module.exports=samples;
