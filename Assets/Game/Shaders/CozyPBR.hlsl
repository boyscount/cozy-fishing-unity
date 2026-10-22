// แสงแบบสมจริง (PBR ของ URP): แดด + เงา + แสงฟ้าจาก HDRI (SH) + เงาสะท้อนฟ้า + หมอก — ใช้ร่วมกันทุก shader ทึบแสง
#ifndef COZY_PBR_INCLUDED
#define COZY_PBR_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


half4 CozyShade(half3 albedo, float3 normalWS, float3 positionWS, float4 positionCS, half smoothness, half occlusion, half3 emission, float fog)
{
    InputData d = (InputData)0;
    d.positionWS = positionWS;
    d.normalWS = normalWS;
    d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
    d.shadowCoord = TransformWorldToShadowCoord(positionWS);
    d.fogCoord = fog;
    d.bakedGI = SampleSH(normalWS);
    d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS);
    d.shadowMask = half4(1, 1, 1, 1);

    SurfaceData s = (SurfaceData)0;
    s.albedo = albedo;
    s.smoothness = smoothness;
    s.occlusion = occlusion;
    s.emission = emission;
    s.alpha = 1;
    half4 c = UniversalFragmentPBR(d, s);
    c.rgb = MixFog(c.rgb, fog);
    return c;
}

// ระนาบพื้นผิวในแนวโลก (UV = xz) → แปลง normal map เป็น normal โลก
float3 CozyWorldNormal(float3 n, half3 nTS)
{
    float3 t = normalize(float3(1, 0, 0) - n * n.x);
    float3 b = cross(t, n);
    return normalize(nTS.x * t + nTS.y * b + nTS.z * n);
}

float CozyHash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
float CozyNoise(float2 p)
{
    float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    return lerp(lerp(CozyHash(i), CozyHash(i + float2(1, 0)), f.x), lerp(CozyHash(i + float2(0, 1)), CozyHash(i + float2(1, 1)), f.x), f.y);
}
#endif
