// ต้นข้าว/หญ้าแบบใบเรียวโค้ง วาดด้วย GPU instancing: สีจุดยอด (rgb = สีใบ, a = ความสูงตามใบ 0..1)
// · ลมพัดโค้งปลายใบเป็นคลื่นข้ามทุ่ง · สีต่างกันเล็กน้อยทุกกอ · แสงทะลุใบ · สองหน้า
Shader "CozyFishing/Grass"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _Variation ("Per-clump Variation", Range(0,1)) = 0.25
        _Bend ("Wind Bend (m)", Float) = 0.12
        _Smoothness ("Smoothness", Range(0,1)) = 0.3
        _Translucency ("Translucency", Range(0,2)) = 0.7
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _Variation, _Bend, _Smoothness, _Translucency;
        CBUFFER_END
        float4 _CozyWind;

        float3 BendWS(float3 wp, float h)
        {
            float t = _CozyWind.x, w = _CozyWind.y + 0.0001;
            // คลื่นลมใหญ่เคลื่อนข้ามทุ่ง + สั่นเล็ก
            float gust = sin(wp.x * 0.35 + wp.z * 0.22 - t * 1.6) * 0.6 + 0.4 + sin(wp.x * 1.7 + t * 3.1) * 0.15;
            float k = h * h * _Bend * w;
            return wp + float3(gust * k, -abs(gust) * k * 0.35, gust * k * 0.45);
        }
        float ClumpHash() { return frac(sin(dot(float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m23), float2(12.9898, 78.233))) * 43758.5453); }
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

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float4 color : TEXCOORD2; float fog : TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionWS = BendWS(TransformObjectToWorld(v.positionOS.xyz), v.color.a);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                // normal เอียงขึ้นฟ้าบางส่วน ให้แสงนุ่มแบบทุ่งหญ้าจริง (ใบบาง ๆ ไม่มีด้านมืดจัด)
                o.normalWS = normalize(lerp(TransformObjectToWorldNormal(v.normalOS), float3(0, 1, 0), 0.45));
                float hsh = ClumpHash();
                float3 tint = lerp(float3(1, 1, 1), lerp(float3(0.85, 0.95, 0.75), float3(1.12, 1.08, 0.88), hsh), _Variation * 2);
                o.color = float4(v.color.rgb * tint * _BaseColor.rgb, v.color.a);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                if (!front) n = normalize(float3(-n.x, n.y, -n.z));
                half occ = lerp(0.45, 1, i.color.a);   // โคนกอมืดกว่า (แสงลอดลงไม่ถึง)
                half4 c = CozyShade(i.color.rgb, n, i.positionWS, i.positionCS, _Smoothness * i.color.a, occ, 0, i.fog);
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                c.rgb += i.color.rgb * light.color * pow(saturate(dot(-V, light.direction)), 3) * _Translucency * i.color.a * light.shadowAttenuation;
                return c;
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
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                return TransformWorldToHClip(BendWS(TransformObjectToWorld(v.positionOS.xyz), v.color.a));
            }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
