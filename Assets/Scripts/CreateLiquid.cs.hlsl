//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef CREATELIQUID_CS_HLSL
#define CREATELIQUID_CS_HLSL
// Generated from ScreenParticle
// PackingRules = Exact
struct ScreenParticle
{
    float4 CameraPosition; // x: x y: y z: z w: w 
    float Radius;
};

// Generated from ScreenCell
// PackingRules = Exact
struct ScreenCell
{
    float Alpha;
    float3 NearestParticle; // x: x y: y z: z 
    float3 FurthestParticle; // x: x y: y z: z 
    float3 NearestNormal; // x: x y: y z: z 
};
#endif
