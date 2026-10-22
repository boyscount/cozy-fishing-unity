// ปลาจากภาพถ่ายจริง: โมเดลพองจากรูปทรงในภาพ (FishPhotoTools) · ลายจากภาพ ตัดขอบด้วย alpha · สองหน้า
// ว่ายน้ำ: ลำตัวโค้งเป็นคลื่นแรงขึ้นทางหาง (หัวอยู่ +X หางอยู่ -X) · _Wiggle = 0 นิ่ง
Shader "CozyFishing/FishPhoto"
{
    Properties
    {
        _BaseMap ("Photo (alpha = shape)", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.45
        _Smoothness ("Smoothness", Range(0,1)) = 0.45
        _EmissionColor ("Emission", Color) = (0,0,0,0)
        _Wiggle ("Swim Wiggle", Float) = 0.06
        _SwimSpeed ("Swim Speed", Float) = 9
        _Phase ("Phase", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor, _EmissionColor;
            float _Cutoff, _Smoothness, _Wiggle, _SwimSpeed, _Phase;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

        float3 Swim(float3 p)
        {
            float tail = saturate(0.35 - p.x);                                   // หัว (+X) แทบไม่ขยับ หางสะบัดมาก
            p.z += sin(_Time.y * _SwimSpeed + _Phase - p.x * 7.0) * _Wiggle * tail * tail * 4.0;
            return p;
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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "CozyPBR.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float fog : TEXCOORD3; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(Swim(v.positionOS.xyz));
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(tex.a - _Cutoff);
                float3 n = normalize(i.normalWS) * (front ? 1 : -1);
                // ภาพถ่ายมีแสงเงาในตัวอยู่แล้ว: ผสมแสงฉากครึ่งเดียว ให้ปลาไม่มืด/สว่างเกินภาพจริง
                half4 lit = CozyShade(tex.rgb * _BaseColor.rgb, n, i.positionWS, i.positionCS, _Smoothness, 1, _EmissionColor.rgb, i.fog);
                half3 flat = MixFog(tex.rgb * _BaseColor.rgb * 0.95 + _EmissionColor.rgb, i.fog);
                return half4(lerp(flat, lit.rgb, 0.5), 1);
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(Swim(v.positionOS.xyz));
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, TransformObjectToWorldNormal(v.normalOS), _LightDirection));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, cs.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, cs.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half4 frag(Varyings i) : SV_Target { clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff); return 0; }
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
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(Swim(v.positionOS.xyz));
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half frag(Varyings i) : SV_Target { clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff); return 0; }
            ENDHLSL
        }
    }
}
