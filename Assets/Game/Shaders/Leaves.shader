// ใบไม้สมจริง: ลายใบจากโมเดล Poly Haven (ตัดขอบด้วย alpha) · สองหน้า · แสงทะลุใบเมื่อย้อนแดด · ไหวตามลมทั้งกิ่งและใบสั่นระริก
// ไหวตามความสูงจากโคนของโมเดล (จุด pivot) จึงใช้กับต้นไม้/พุ่มไม้ขนาดใดก็ได้
Shader "CozyFishing/Leaves"
{
    Properties
    {
        _BaseMap ("Leaf Albedo (alpha = mask)", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _Smoothness ("Smoothness", Range(0,1)) = 0.2
        _Translucency ("Translucency", Range(0,2)) = 0.8
        _Sway ("Branch Sway (m)", Float) = 0.15
        _SwayHeight ("Sway Full Height (m)", Float) = 5
        _Flutter ("Leaf Flutter (m)", Float) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor;
            float _Cutoff, _Smoothness, _Translucency, _Sway, _SwayHeight, _Flutter;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        float4 _CozyWind;   // x = เวลา, y = แรงลม

        float3 WindWS(float3 wp)
        {
            float3 root = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
            float h = saturate((wp.y - root.y) / max(_SwayHeight, 0.01));
            float t = _CozyWind.x, w = _CozyWind.y + 0.0001;
            float phase = root.x * 0.37 + root.z * 0.23;
            float2 branch = float2(sin(t * 1.1 + phase) + 0.4 * sin(t * 2.3 + phase * 1.7), cos(t * 0.9 + phase) * 0.6) * _Sway * h * h * w;
            float flutter = sin(t * 7.0 + dot(wp, float3(3.1, 2.3, 4.7))) * _Flutter * h * w;
            return wp + float3(branch.x + flutter, flutter * 0.5, branch.y + flutter);
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
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "CozyPBR.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float fog : TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionWS = WindWS(TransformObjectToWorld(v.positionOS.xyz));
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                clip(tex.a - _Cutoff);
                float3 n = normalize(i.normalWS) * (front ? 1 : -1);
                half4 c = CozyShade(tex.rgb, n, i.positionWS, i.positionCS, _Smoothness, 1, 0, i.fog);
                // แสงทะลุใบ: มองย้อนแดดแล้วใบเรืองเขียวอมเหลือง
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float back = pow(saturate(dot(-V, light.direction)), 3) * 0.7 + saturate(dot(-n, light.direction)) * 0.3;
                c.rgb += tex.rgb * light.color * back * _Translucency * light.shadowAttenuation;
                return c;
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
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 ws = WindWS(TransformObjectToWorld(v.positionOS.xyz));
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
            half4 frag(Varyings i) : SV_Target { clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a - _Cutoff); return 0; }
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
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformWorldToHClip(WindWS(TransformObjectToWorld(v.positionOS.xyz)));
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half frag(Varyings i) : SV_Target { clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a - _Cutoff); return 0; }
            ENDHLSL
        }
    }
}
