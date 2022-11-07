//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef LIQUIDSTRUCTS_CS_HLSL
#define LIQUIDSTRUCTS_CS_HLSL
// Generated from Charly.Liquid.ScreenParticle
// PackingRules = Exact
struct ScreenParticle
{
    float4 CameraPosition; // x: x y: y z: z w: w 
    float Radius;
};

// Generated from Charly.Liquid.ScreenCell
// PackingRules = Exact
struct ScreenCell
{
    float Alpha;
    float2 NearestParticle; // x: x y: y 
    float2 FurthestParticle; // x: x y: y 
    float NearestDepth;
    float3 NearestNormal; // x: x y: y z: z 
};


#endif
