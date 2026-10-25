#define D2D_INPUT_COUNT 0
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"
float2 Coefficients[512];
int Count;
float CircleRadius;
float Column(float x)
{
    if (abs(x) > CircleRadius) return 0;
    float y = sqrt(CircleRadius * CircleRadius - x * x) - 0.5;
    int index = (int)floor(y);
    if (y < 0) return (y + 0.5) * Coefficients[0].y;
    if (index >= Count - 1) return 0.5;
    return (1 - frac(y)) * Coefficients[index].y + frac(y) * Coefficients[index + 1].y;
}
D2D_PS_ENTRY(main)
{
    float pixel = D2DGetScenePosition().x;
    if (pixel > 511) return 0;
    float sum = 0;
    [loop] for (int i = 0; i < 2 * Count; i++)
    {
        float x = pixel - Count + i;
        int index = i < Count ? Count - i - 1 : i - Count;
        sum += Column(x) * Coefficients[index].x;
    }
    float value = floor(saturate(sum * 2) * 255 + 0.5) / 255;
    return value.xxxx;
}
