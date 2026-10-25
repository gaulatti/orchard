#define D2D_INPUT_COUNT 0
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"
float Dimension;
D2D_PS_ENTRY(main)
{
    float i = floor(D2DGetScenePosition().x);
    float x = (-6.0 * ((i + 0.5) / Dimension) + 3.0) * 0.7071067811865476;
    float signX = x < 0.0 ? -1.0 : 1.0;
    x = abs(x);
    float t = 1.0 / (1.0 + 0.3275911 * x);
    float e = signX * (1.0 - (((((1.061405429 * t - 1.453152027) * t + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t) * exp(-x * x));
    float a = i == 0.0 ? 1.0 : i == Dimension - 1.0 ? 0.0 : floor(255.0 * (0.5 * (e + 1.0)) + 0.5) / 255.0;
    return float4(a, a, a, 1.0);
}
