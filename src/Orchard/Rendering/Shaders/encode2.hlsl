#define D2D_INPUT_COUNT 1
#define D2D_INPUT0_COMPLEX
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"
float4 Tint; float4 Box; float4 CasterBox; float Radius; float Dimension; float General; float ProfileRadius;
D2D_PS_ENTRY(main) { float2 xy=D2DGetScenePosition().xy; float2 pos=xy-(CasterBox.xy-21); float2 extent=CasterBox.zw+42; float edge=42+Radius+0.5; float2 center=extent*0.5; float2 p=sign(pos-center)*max(abs(pos-center)-(center-edge),0)+edge; if(General==1.0) p=pos; float2 samplePosition=General==2.0?float2((length(pos-extent*0.5)+0.5)*512.0/ProfileRadius,0.5):General==1.0?p:p*(Dimension/(2*edge)); float a=0; if(all(pos>=0)&&all(pos<extent)) {
 if(General==3.0) {
  float2 nearEdge=42.0-pos; float2 farEdge=pos-CasterBox.zw;
  if(all(CasterBox.zw>=42.0)) {
   float2 edgeDistance=max(nearEdge,farEdge); float2 px=float2(edgeDistance.x*(128.0/42.0),0.5); float2 py=float2(edgeDistance.y*(128.0/42.0),0.5);
   a=D2DSampleInputAtPosition(0,px).a*D2DSampleInputAtPosition(0,py).a;
  } else {
   float2 lx=float2(nearEdge.x*(128.0/42.0),0.5); float2 rx=float2(farEdge.x*(128.0/42.0),0.5); float2 ty=float2(nearEdge.y*(128.0/42.0),0.5); float2 by=float2(farEdge.y*(128.0/42.0),0.5);
   float cx=1.0-D2DSampleInputAtPosition(0,lx).a-D2DSampleInputAtPosition(0,rx).a; float cy=1.0-D2DSampleInputAtPosition(0,ty).a-D2DSampleInputAtPosition(0,by).a; a=cx*cy;
  }
 } else a=D2DSampleInputAtPosition(0,samplePosition).a;
} float2 q=abs(xy-(Box.xy+Box.zw*0.5))-(Box.zw*0.5-Radius); float d=length(max(q,0))+min(max(q.x,q.y),0)-Radius; a*=1-saturate(0.5-d); precise float x=a*255;float hi=floor(x);precise float rest=(x-hi)*255;float mid=floor(rest);return float4(hi/255,mid/255,rest-mid,1); }
