#define D2D_INPUT_COUNT 0
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"
float4 Box; float Radius;
D2D_PS_ENTRY(main) { float2 q=abs(D2DGetScenePosition().xy-(Box.xy+Box.zw*0.5))-(Box.zw*0.5-Radius); float d=length(max(q,0))+min(max(q.x,q.y),0)-Radius; float a=saturate(0.5-d); return a.xxxx; }
