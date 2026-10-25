param(
    [Parameter(Mandatory)][ValidateSet('X64','Arm64')][string]$Architecture,
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference = 'Stop'
# Offsets and hook instructions are bound to glyph-runtime-identities.json and
# actual run40159449547 PDB layouts/code, independently on both architectures.
$dump = @'
.printf "GLYPH_TRACE_RUN phase=%x seq=%x ctx=%p data=%p\n", @$t1, @$t2, @$t0, @$t6;
.echo GLYPH_TRACE_DATA_WORDS;
dd @$t6 L20;
r @$t7=poi(@$t6);
.if (@$t7 != 0) {
 r @$t9=dwo(@$t7+0xc);
 .if ((@$t9 > 0) and (@$t9 <= 0x80)) {
  .echo GLYPH_TRACE_RUN_WORDS;
  dd @$t7 Lc;
  .echo GLYPH_TRACE_NATIVE_FONT_FACE;
  .if (poi(@$t7) != 0) { dp (poi(@$t7)+0x10) L1 };
  .echo GLYPH_TRACE_INDICES;
  .if (poi(@$t7+0x10) != 0) { .for (r @$t8=0; @$t8<@$t9; r @$t8=@$t8+1) { .printf "%04x ", wo(poi(@$t7+0x10)+2*@$t8) }; .printf "\n" };
  .echo GLYPH_TRACE_ADVANCE_WORDS;
  .if (poi(@$t7+0x18) != 0) { .for (r @$t8=0; @$t8<@$t9; r @$t8=@$t8+1) { .printf "%08x ", dwo(poi(@$t7+0x18)+4*@$t8) }; .printf "\n" };
  .echo GLYPH_TRACE_OFFSET_WORDS;
  .if (poi(@$t7+0x20) != 0) { .for (r @$t8=0; @$t8<2*@$t9; r @$t8=@$t8+1) { .printf "%08x ", dwo(poi(@$t7+0x20)+4*@$t8) }; .printf "\n" };
 } .else { .echo GLYPH_TRACE_GLYPH_COUNT_LIMIT; q };
} .else { .echo GLYPH_TRACE_NULL_RUN; q };
.echo GLYPH_TRACE_END_RUN;
r @$t2=@$t2+1;
.if (@$t2 >= 0x80) { .echo GLYPH_TRACE_CAPTURE_LIMIT; q };
'@
$dump = ($dump -split '\r?\n' | ForEach-Object { $_.Trim() }) -join ' '
$snapshot = @'
.if (@$t0 != 0) {
 r @$t13=poi(@$t0+0x20);
 r @$t14=dwo(@$t0+0x1c);
 .if (@$t14 <= 4) {
  .for (r @$t12=0; @$t12<@$t14; r @$t12=@$t12+1) {
   r @$t6=@$t13+0x80*@$t12; __DUMP__
  };
 } .else { .echo GLYPH_TRACE_RUN_COUNT_LIMIT; q };
} .else { .echo GLYPH_TRACE_TARGET_NOT_BOUND };
'@
$snapshot = (($snapshot -split '\r?\n' | ForEach-Object { $_.Trim() }) -join ' ').Replace('__DUMP__',$dump)
$thisRegister = if ($Architecture -eq 'X64') { '@rcx' } else { '@x0' }
$description = if ($Architecture -eq 'X64') { 'poi(@rsp+0x38)' } else { '@x4' }
$messageRegister = $thisRegister
$textBytes = [Text.Encoding]::Unicode.GetBytes("We couldn't load this data. Please try again.")
$comparisons = @()
for ($offset=0; $offset+8 -le $textBytes.Length; $offset+=8) {
    $value = [BitConverter]::ToUInt64($textBytes,$offset)
    $comparisons += ('(poi(@$t5+0x{0:x}) == 0x{1:x16})' -f $offset,$value)
}
$comparisons += ('(wo(@$t5+0x58) == 0x002e)')
$match = $comparisons -join ' and '
$bind = @'
r @$t4=__DESCRIPTION__;
.if (@$t4 != 0) {
 .if (dwo(@$t4+0x10) == 0x2d) {
  r @$t5=poi(@$t4+8);
  .if (@$t5 != 0) {
   .if (__MATCH__) {
    r @$t3=__THIS__;
    r @$t14=poi(@$t3+0x18);
    .if (@$t14 != @$t0) { .printf "GLYPH_TRACE_BIND old=%p ctx=%p renderer=%p\n", @$t0, @$t14, @$t3; lmvm Microsoft_UI_Xaml };
    r @$t0=@$t14;
   };
  };
 };
};
'@
$bind = (($bind -split '\r?\n' | ForEach-Object { $_.Trim() }) -join ' ').Replace('__DESCRIPTION__',$description).Replace('__MATCH__',$match).Replace('__THIS__',$thisRegister)
$phase = @'
r @$t11=__MESSAGE__;
.if (@$t11 != 0) {
 .if (poi(@$t11) == 0x003a00540047004f) {
  r @$t1=wo(@$t11+8)-0x41;
  .if (@$t1 <= 0xc) {
   .printf "GLYPH_TRACE_PHASE %x\n", @$t1;
   .if (@$t1 == 0) { be 1 2 3 } .else { __SNAPSHOT__ };
  } .else { .echo GLYPH_TRACE_PHASE_LIMIT; q };
 };
};
'@
$phase = (($phase -split '\r?\n' | ForEach-Object { $_.Trim() }) -join ' ').Replace('__MESSAGE__',$messageRegister).Replace('__SNAPSHOT__',$snapshot)
$ctx = if ($Architecture -eq 'X64') { '@r15' } else { '@x23' }
$data = if ($Architecture -eq 'X64') { '@r14' } else { '@x22' }
$incoming = if ($Architecture -eq 'X64') { '@rdx' } else { '@x1' }
$tileMatrix = if ($Architecture -eq 'X64') { '@rcx' } else { '@x0' }
$finalMatrix = if ($Architecture -eq 'X64') { '@rdx' } else { '@x1' }
$prependOffset = if ($Architecture -eq 'X64') { '0xa5' } else { '0x84' }
$finalOffset = if ($Architecture -eq 'X64') { '0xd7' } else { '0xc0' }
$before = '.if ((@$t0 != 0) and ('+$ctx+' == @$t0)) { .echo GLYPH_TRACE_PREPEND; r @$t6='+$data+'; '+$dump+' .echo GLYPH_TRACE_INCOMING_MATRIX; dd '+$incoming+' L6; .echo GLYPH_TRACE_TILE_MATRIX; dd '+$tileMatrix+' L6; .echo GLYPH_TRACE_END_PREPEND };'
$final = '.if ((@$t0 != 0) and ('+$ctx+' == @$t0)) { .printf "GLYPH_TRACE_FINAL phase=%x ctx=%p\n", @$t1, @$t0; dd '+$finalMatrix+' L6; .echo GLYPH_TRACE_END_FINAL };'
$hit = 'r @$t10=@$t10+1; .if (@$t10 >= 0x2000) { .echo GLYPH_TRACE_HIT_LIMIT; q }; '
function Breakpoint([int]$Id,[string]$Symbol,[string]$Body) {
    # CDB CommandString has one escaping level before its command parser.
    'bu'+$Id+' '+$Symbol+' "'+($hit+$Body+' g').Replace('\','\\').Replace('"','\"')+'"'
}
$commands = @(
    '.echo GLYPH_TRACE_TRANSPORT_BEGIN'
    'r @$t0=0; r @$t1=0; r @$t2=0; r @$t10=0'
    (Breakpoint 0 'KERNELBASE!OutputDebugStringW' $phase)
    (Breakpoint 1 'Microsoft_UI_Xaml!DWriteTextRenderer::DrawGlyphRun' $bind)
    (Breakpoint 2 ('Microsoft_UI_Xaml!D2DTextDrawingContext::RasterizeGlyphRunToD2DOrRawBuffer+'+$prependOffset) $before)
    (Breakpoint 3 ('Microsoft_UI_Xaml!D2DTextDrawingContext::RasterizeGlyphRunToD2DOrRawBuffer+'+$finalOffset) $final)
    'bd 1 2 3'
    'g'
)
[IO.File]::WriteAllLines([IO.Path]::GetFullPath($Output),$commands,[Text.Encoding]::ASCII)
