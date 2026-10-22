// พื้นผิวทึบแสงแบบสมจริง (PBR) สำหรับโมเดลจาก Blender: สีวัสดุ / สีจุดยอด / ลายผิวแบบ triplanar (ไม้ ฟาง เปลือกไม้)
// + มอสบนด้านที่หันขึ้น + ใบไม้ไหวตามลม (เทียบ mossy()/swaying() ของเวอร์ชันเว็บ)
Shader "CozyFishing/Surface"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Toggle(_VERTEX_COLOR)] _UseVertexColor ("Use Vertex Color", Float) = 0
        [Toggle(_TRIPLANAR)] _UseTriplanar ("Use Triplanar Texture", Float) = 0
        _TriMap ("Triplanar Albedo", 2D) = "white" {}
        [Normal] _TriNormal ("Triplanar Normal", 2D) = "bump" {}
        _TriScale ("Triplanar Tiles / m", Float) = 0.5
        _Smoothness ("Smoothness", Range(0,1)) = 0.15
        _MossColor ("Moss Color", Color) = (0.30,0.42,0.16,1)
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
            float4 _BaseColor, _TriMap_ST, _MossColor, _EmissionColor;
            float _TriScale, _Smoothness, _MossAmount, _Sway, _SwayStart, _UseVertexColor, _UseTriplanar;
        CBUFFER_END
        float4 _CozyWind;   // x = เวลา, y = แรงลม (ตั้งจาก WaterFx)

        float3 ApplySway(float3 posOS)
        {
            float3 wp = TransformObjectToWorld(posOS);
            float k = max(posOS.y - _SwayStart, 0) * _Sway * (_CozyWind.y + 0.0001);
            posOS.x += sin(_CozyWind.x * 1.3 + wp.x * 0.3 + wp.z * 0.2) * k;
            posOS.z += cos(_CozyWind.x * 1.1 + wp.z * 0.3) * k * 0.6;
            return posOS;
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
            #pragma shader_feature_local _VERTEX_COLOR
            #pragma shader_feature_local _TRIPLANAR
            #include "CozyPBR.hlsl"
            TEXTURE2D(_TriMap); SAMPLER(sampler_TriMap);
            TEXTURE2D(_TriNormal); SAMPLER(sampler_TriNormal);

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float4 color : TEXCOORD2; float fog : TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 pos = _Sway > 0 ? ApplySway(v.positionOS.xyz) : v.positionOS.xyz;
                o.positionWS = TransformObjectToWorld(pos);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                half3 albedo = _BaseColor.rgb;
                #if _VERTEX_COLOR
                    albedo *= i.color.rgb;
                #endif
                #if _TRIPLANAR
                    // ลายผิวฉายจาก 3 แกน ไม่ต้องมี UV (โมเดล low-poly จาก Blender ไม่มี UV ที่ใช้ได้)
                    float3 w = pow(abs(n), 4); w /= (w.x + w.y + w.z);
                    float3 p = i.positionWS * _TriScale;
                    half3 tx = SAMPLE_TEXTURE2D(_TriMap, sampler_TriMap, p.zy).rgb, ty = SAMPLE_TEXTURE2D(_TriMap, sampler_TriMap, p.xz).rgb, tz = SAMPLE_TEXTURE2D(_TriMap, sampler_TriMap, p.xy).rgb;
                    albedo *= tx * w.x + ty * w.y + tz * w.z;
                    half3 nx = UnpackNormal(SAMPLE_TEXTURE2D(_TriNormal, sampler_TriNormal, p.zy)), ny = UnpackNormal(SAMPLE_TEXTURE2D(_TriNormal, sampler_TriNormal, p.xz)), nz = UnpackNormal(SAMPLE_TEXTURE2D(_TriNormal, sampler_TriNormal, p.xy));
                    n = normalize(n + float3(0, nx.y, nx.x) * w.x + float3(ny.x, 0, ny.y) * w.y + float3(nz.x, nz.y, 0) * w.z);
                #endif
                if (_MossAmount > 0)
                {
                    float mz = CozyNoise(i.positionWS.xz * 1.3 + i.positionWS.y) * 0.6 + CozyNoise(i.positionWS.xz * 4.0 - i.positionWS.y) * 0.4;
                    albedo *= 0.85 + mz * 0.3;
                    float up = smoothstep(0.35, 0.75, n.y + (mz - 0.5) * 0.5);
                    albedo = lerp(albedo, _MossColor.rgb * (0.8 + mz * 0.4), smoothstep(0.38, 0.55, up * _MossAmount * 1.6));
                }
                return CozyShade(albedo, n, i.positionWS, i.positionCS, _Smoothness, 1, _EmissionColor.rgb, i.fog);
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
