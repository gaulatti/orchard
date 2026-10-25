#define D2D_INPUT_COUNT 1
#define D2D_INPUT0_COMPLEX
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"
float4 Box;
float4 CasterBox;
float Radius;
float Dimension;
float Mode;
D2D_PS_ENTRY(main)
{
    float2 scene = D2DGetScenePosition().xy;
    float4 input = D2DGetInputCoordinate(0);
    float2 pos = scene - (CasterBox.xy - 21);
    float2 extent = CasterBox.zw + 42;
    float edge = 42 + Radius + 0.5;
    float2 center = extent * 0.5;
    float2 p = sign(pos-center) * max(abs(pos-center)-(center-edge),0) + edge;
    float2 normalized = p / (2*edge);
    float2 position = p * (Dimension/(2*edge));
    float2 legacy = input.xy + input.zw * (position-scene);
    float2 reordered = (input.xy-input.zw*scene) + normalized*(input.zw*Dimension);
    float2 q = abs(scene-(Box.xy+Box.zw*0.5))-(Box.zw*0.5-Radius);
    float d = length(max(q,0)) + min(max(q.x,q.y),0) - Radius;
    float exterior = 1-saturate(0.5-d);
    if (Mode == 0) return input;
    if (Mode == 1) return float4(D2DSampleInputAtPosition(0,position).a,D2DSampleInput(0,reordered).a,D2DSampleInput(0,normalized).a,exterior);
    if (Mode == 2) return float4(legacy,reordered);
    if (Mode == 3) return float4(p,position);
    return float4(scene,normalized);
}
