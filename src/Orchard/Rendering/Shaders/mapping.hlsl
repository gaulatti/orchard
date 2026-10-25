#define D2D_INPUT_COUNT 1
#define D2D_INPUT0_COMPLEX
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"
float2 Scale; float2 Offset;
D2D_PS_ENTRY(main) { return D2DSampleInputAtPosition(0,D2DGetScenePosition().xy*Scale+Offset); }
