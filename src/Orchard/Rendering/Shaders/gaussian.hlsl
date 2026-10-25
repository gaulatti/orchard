#define D2D_INPUT_COUNT 1
#define D2D_INPUT0_COMPLEX
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"
float2 Direction;
float2 Tap0;
float2 Tap1;
float2 Tap2;
float2 Tap3;
float2 Tap4;
float2 Tap5;
float2 Tap6;
float2 Tap7;
float2 Tap8;
float2 Tap9;
float2 Tap10;
float2 Tap11;
float2 Tap12;
D2D_PS_ENTRY(main){float4 value=0;
value += D2DSampleInputAtOffset(0, Direction * Tap0.x) * Tap0.y;
value += D2DSampleInputAtOffset(0, Direction * Tap1.x) * Tap1.y;
value += D2DSampleInputAtOffset(0, Direction * Tap2.x) * Tap2.y;
value += D2DSampleInputAtOffset(0, Direction * Tap3.x) * Tap3.y;
value += D2DSampleInputAtOffset(0, Direction * Tap4.x) * Tap4.y;
value += D2DSampleInputAtOffset(0, Direction * Tap5.x) * Tap5.y;
value += D2DSampleInputAtOffset(0, Direction * Tap6.x) * Tap6.y;
value += D2DSampleInputAtOffset(0, Direction * Tap7.x) * Tap7.y;
value += D2DSampleInputAtOffset(0, Direction * Tap8.x) * Tap8.y;
value += D2DSampleInputAtOffset(0, Direction * Tap9.x) * Tap9.y;
value += D2DSampleInputAtOffset(0, Direction * Tap10.x) * Tap10.y;
value += D2DSampleInputAtOffset(0, Direction * Tap11.x) * Tap11.y;
value += D2DSampleInputAtOffset(0, Direction * Tap12.x) * Tap12.y;
return value;}
