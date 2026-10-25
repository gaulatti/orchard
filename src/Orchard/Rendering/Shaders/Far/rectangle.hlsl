#define D2D_INPUT_COUNT 1
#define D2D_INPUT0_COMPLEX
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"
float4 Box;
float4 Caster;
float4 Tint;
float Radius;
float Sigma;
float Dimension;
float Fast;
float Plane;
float integral(float d)
{
    return D2DSampleInputAtPosition(0, float2(d * (Dimension / (6.0 * Sigma)), 0.5)).r;
}
float axis(float p, float lo, float hi)
{
    return Fast == 1.0 ? integral(max(lo - p, p - hi)) : 1.0 - integral(lo - p) - integral(p - hi);
}
D2D_PS_ENTRY(main)
{
    float2 p = D2DGetScenePosition().xy;
    float extent = 3.0 * Sigma;
    float4 inset = float4(Caster.xy + extent, Caster.xy + Caster.zw - extent);
    float4 proxy = float4(Caster.xy - extent, Caster.xy + Caster.zw + extent);
    float a = 0.0;
    if (all(p >= proxy.xy) && all(p < proxy.zw))
    {
        a = axis(p.x, inset.x, inset.z) * axis(p.y, inset.y, inset.w);
        float2 q = abs(p - (Box.xy + Box.zw * 0.5)) - (Box.zw * 0.5 - Radius);
        float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - Radius;
        a *= 1.0 - saturate(0.5 - d);
    }
    float x = a * 2048.0;
    float hi = floor(x);
    float rest = (x - hi) * 2048.0;
    float mid = floor(rest);
    float value = Plane == 0.0 ? hi / 2048.0 : Plane == 1.0 ? mid / 2048.0 : rest - mid;
    return float4(0.0, 0.0, 0.0, value);
}
