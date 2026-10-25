#define D2D_INPUT_COUNT 0
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"
float ErfApprox(float value)
{
    float x = abs(value);
    float t = 1.0 / (1.0 + 0.3275911 * x);
    float polynomial = (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t;
    return sign(value) * (1.0 - polynomial * exp(-x * x));
}
D2D_PS_ENTRY(main)
{
    float pixel = D2DGetScenePosition().x;
    float alpha = pixel < 1.0 ? 1.0 : pixel > 127.0 ? 0.0 : 0.5 * (ErfApprox((3.0 - 6.0 * pixel / 128.0) * 0.7071067811865476) + 1.0);
    alpha = floor(saturate(alpha) * 255.0 + 0.5) / 255.0;
    return alpha.xxxx;
}
