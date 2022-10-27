//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef CREATELIQUID_CS_HLSL
#define CREATELIQUID_CS_HLSL
// Generated from ScreenParticle
// PackingRules = Exact
struct ScreenParticle
{
    float2 ClipPosition; // x: x y: y 
    float CameraDepth;
    float Radius;
    float3 Normal; // x: x y: y z: z 
};

// Generated from ScreenCell
// PackingRules = Exact
struct ScreenCell
{
    float Alpha;
    float NearestParticle;
    float FarthestParticle;
    float3 NearestNormal; // x: x y: y z: z 
};


#endif
