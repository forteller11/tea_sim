using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Charly.Liquid
{
    [GenerateHLSL(PackingRules.Exact, false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct ScreenParticle
    {
        public float4 CameraPosition;
        public float Radius;
    }

    [GenerateHLSL(PackingRules.Exact, false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct ScreenCell
    {
        public float Alpha;
        public float2 NearestParticle;
        public float2 FurthestParticle;
        public float NearestDepth;
        public float3 NearestNormal;
    }
}