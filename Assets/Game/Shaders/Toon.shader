// แสงเงาแบบการ์ตูนนุ่ม ๆ (4 ระดับ) + รับเงา + แสงรอบข้างจากฟ้า + มอสบนด้านที่หันขึ้น + ใบไม้ไหวตามลม
// ใช้กับทุกโมเดลจาก Blender (เทียบ toon()/mossy()/swaying() ของเวอร์ชันเว็บ)
Shader "CozyFishing/Toon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
        [Toggle(_VERTEX_COLOR)] _UseVertexColor ("Use Vertex Color", Float) = 0
        _MossColor ("Moss Color", Color) = (0.37,0.56,0.23,1)
        _MossAmount ("Moss Amount", Range(0,1)) = 0
        _Sway ("Wind Sway", Range(0,0.5)) = 0
        _SwayStart ("Sway Start Height", Float) = 1.5
        _EmissionColor ("Emission", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor, _BaseMap_ST, _MossColor, _EmissionColor;
            float _MossAmount, _Sway, _SwayStart, _UseVertexColor;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        float4 _CozyWind;   // x = เวลา, y = แรงลม (ตั้งจาก WindController)

        float3 ApplySway(float3 posOS)
        {
            float3 wp = TransformObjectToWorld(posOS);
            float k = max(posOS.y - _SwayStart, 0) * _Sway * (_CozyWind.y + 0.0001);
            posOS.x += sin(_CozyWind.x * 1.3 + wp.x * 0.3 + wp.z * 0.2) * k;
            posOS.z += cos(_CozyWind.x * 1.1 + wp.z * 0.3) * k * 0.6;
            return posOS;
        }
        float hash3(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }
        float noise3(float3 p)
        {
            float3 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
            return lerp(lerp(lerp(hash3(i), hash3(i + float3(1,0,0)), f.x), lerp(hash3(i + float3(0,1,0)), hash3(i + float3(1,1,0)), f.x), f.y),
                        lerp(lerp(hash3(i + float3(0,0,1)), hash3(i + float3(1,0,1)), f.x), lerp(hash3(i + float3(0,1,1)), hash3(i + float3(1,1,1)), f.x), f.y), f.z);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma shader_feature_local _VERTEX_COLOR
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float4 color : TEXCOORD3; float fog : TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 pos = _Sway > 0 ? ApplySway(v.positionOS.xyz) : v.positionOS.xyz;
                o.positionWS = TransformObjectToWorld(pos);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                #if _VERTEX_COLOR
                    baseCol.rgb *= i.color.rgb;
                #endif
                if (_MossAmount > 0)
                {
                    float nz = noise3(i.positionWS * 1.3) * 0.6 + noise3(i.positionWS * 4.0) * 0.4;
                    baseCol.rgb *= 0.88 + nz * 0.24;
                    float up = smoothstep(0.35, 0.75, n.y + (nz - 0.5) * 0.5);
                    baseCol.rgb = lerp(baseCol.rgb, _MossColor.rgb * (0.85 + nz * 0.3), smoothstep(0.42, 0.5, up * _MossAmount * 1.6));
                }
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float ndl = saturate(dot(n, light.direction)) * light.shadowAttenuation;
                // 4 ระดับแสง (เงา → กลาง → สว่าง → ไฮไลต์) ต่างกันไม่มากให้ดูนุ่ม
                float ramp = ndl < 0.08 ? 0.47 : ndl < 0.35 ? 0.70 : ndl < 0.75 ? 0.89 : 1.0;
                float3 ambient = SampleSH(n);
                float3 col = baseCol.rgb * (ambient * 0.9 + light.color * ramp * 0.85) + _EmissionColor.rgb;
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 pos = _Sway > 0 ? ApplySway(v.positionOS.xyz) : v.positionOS.xyz;
                float3 ws = TransformObjectToWorld(pos);
                float3 nws = TransformObjectToWorldNormal(v.normalOS);
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, nws, _LightDirection));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, cs.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, cs.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                return o;
            }
            half4 frag(Varyings i) : SV_Target { return 0; }
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
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 pos = _Sway > 0 ? ApplySway(v.positionOS.xyz) : v.positionOS.xyz;
                o.positionCS = TransformObjectToHClip(pos);
                return o;
            }
            half frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
