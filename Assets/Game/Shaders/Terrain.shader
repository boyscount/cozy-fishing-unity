// พื้นดินสมจริง: ผสมลายผิว PBR 3 ชุด (หญ้า / โคลน / คันนา) ตามน้ำหนักในสีจุดยอด + ความชื้นใต้น้ำ
//   สีจุดยอด: r = โคลน, g = ทางดิน/คันนา, b = ความเปียก/ลึก (มืดลง ลื่นขึ้น), a = เขียวเข้ม (ภูเขาไกล)
//   UV = ตำแหน่ง xz ในโลก · สุ่มขนาดลายหญ้า 2 ระดับด้วย noise เพื่อไม่ให้เห็นลายซ้ำ
Shader "CozyFishing/Terrain"
{
    Properties
    {
        _GrassMap ("Grass Albedo", 2D) = "white" {}
        [Normal] _GrassNormal ("Grass Normal", 2D) = "bump" {}
        _GrassRough ("Grass Roughness", 2D) = "white" {}
        _MudMap ("Mud Albedo", 2D) = "white" {}
        [Normal] _MudNormal ("Mud Normal", 2D) = "bump" {}
        _MudRough ("Mud Roughness", 2D) = "white" {}
        _PathMap ("Path Albedo", 2D) = "white" {}
        [Normal] _PathNormal ("Path Normal", 2D) = "bump" {}
        _PathRough ("Path Roughness", 2D) = "white" {}
        _Tiles ("Tiles / m (grass, mud, path, macro)", Vector) = (0.28, 0.35, 0.4, 0.03)
        _GrassTint ("Grass Tint", Color) = (1,1,1,1)
        _MountTint ("Far Hills Tint", Color) = (0.55,0.7,0.5,1)
        _NormalStrength ("Normal Strength", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "CozyPBR.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Tiles, _GrassTint, _MountTint;
                float _NormalStrength;
            CBUFFER_END
            TEXTURE2D(_GrassMap); TEXTURE2D(_GrassNormal); TEXTURE2D(_GrassRough);
            TEXTURE2D(_MudMap); TEXTURE2D(_MudNormal); TEXTURE2D(_MudRough);
            TEXTURE2D(_PathMap); TEXTURE2D(_PathNormal); TEXTURE2D(_PathRough);
            SAMPLER(sampler_linear_repeat);   // sampler เดียวใช้ทุกลาย (WebGL จำกัดจำนวน sampler)
            #define S(t, uv) SAMPLE_TEXTURE2D(t, sampler_linear_repeat, uv)

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float4 color : TEXCOORD2; float fog : TEXCOORD3; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.positionWS.xz;
                float3 n0 = normalize(i.normalWS);
                float macro = CozyNoise(p * _Tiles.w * 4) * 0.6 + CozyNoise(p * _Tiles.w * 13) * 0.4;

                // หญ้า: 2 ขนาด ผสมตาม noise + สีอ่อนเข้มเป็นหย่อม ๆ
                float2 ug = p * _Tiles.x, ug2 = p * _Tiles.x * 0.37 + 0.31;
                float gm = smoothstep(0.3, 0.7, macro);
                half3 gA = lerp(S(_GrassMap, ug).rgb, S(_GrassMap, ug2).rgb, gm);
                half3 gN = lerp(UnpackNormal(S(_GrassNormal, ug)), UnpackNormal(S(_GrassNormal, ug2)), gm);
                half gR = lerp(S(_GrassRough, ug).r, S(_GrassRough, ug2).r, gm);
                gA *= _GrassTint.rgb * lerp(0.82, 1.12, macro);

                float2 um = p * _Tiles.y, up = p * _Tiles.z;
                half3 mA = S(_MudMap, um).rgb, mN = UnpackNormal(S(_MudNormal, um)); half mR = S(_MudRough, um).r;
                half3 pA = S(_PathMap, up).rgb, pN = UnpackNormal(S(_PathNormal, up)); half pR = S(_PathRough, up).r;

                // ขอบเขตโคลน/คันนาแบบไม่เรียบ (ใช้ noise ดันน้ำหนัก) ให้ดูเป็นธรรมชาติ
                float edge = CozyNoise(p * 1.7) - 0.5;
                float wMud = saturate(i.color.r + edge * 0.35 * saturate(i.color.r * 4) );
                float wPath = saturate(i.color.g + edge * 0.3 * saturate(i.color.g * 4));
                wMud = smoothstep(0.15, 0.85, wMud); wPath = smoothstep(0.2, 0.8, wPath);

                half3 albedo = gA; half3 nT = gN; half rough = gR;
                albedo = lerp(albedo, mA, wMud); nT = lerp(nT, mN, wMud); rough = lerp(rough, mR, wMud);
                albedo = lerp(albedo, pA, wPath); nT = lerp(nT, pN, wPath); rough = lerp(rough, pR, wPath);
                albedo = lerp(albedo, albedo * _MountTint.rgb, i.color.a);

                // เปียก/ใต้น้ำ: มืดลง ลื่นขึ้น ลายนูนน้อยลง
                float wet = i.color.b;
                albedo *= lerp(1, 0.45, wet);
                rough = lerp(rough, 0.25, wet * 0.8);
                nT.xy *= _NormalStrength * (1 - wet * 0.6);

                float3 n = CozyWorldNormal(n0, normalize(nT));
                return CozyShade(albedo, n, i.positionWS, i.positionCS, 1 - rough, 1, 0, i.fog);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 vert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
