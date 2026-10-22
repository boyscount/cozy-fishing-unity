// ผิวน้ำมองจากใต้น้ำ: แผ่นฟ้าสว่างระยิบ (หน้าต่างของสเนลล์) ตรงเหนือหัว · ไกลออกไปสะท้อนกลับเป็นสีน้ำทึบ · ลายคลื่นแสงพลิ้วไหว
// ใช้ mesh เดียวกับผิวน้ำ แต่วาดด้านล่าง (Cull Front) เปิดเฉพาะตอนกล้องดำน้ำ (UnderwaterView)
Shader "CozyFishing/WaterUnder"
{
    Properties
    {
        _Tint ("Surface Tint", Color) = (0.75,0.9,0.8,1)
        _Deep ("Far Color", Color) = (0.12,0.2,0.16,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-20" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint, _Deep;
            CBUFFER_END
            float4 _CozyWind;
            float _CozyDim;
            struct Attr { float4 positionOS : POSITION; };
            struct Vary { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float fog : TEXCOORD1; };
            Vary vert(Attr v)
            {
                Vary o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionWS.y += sin(dot(o.positionWS.xz, float2(0.21, 0.13)) + _CozyWind.x * 0.9) * 0.015;
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            float caust(float2 p, float t)
            {
                float a = sin(p.x * 3.1 + t * 1.3 + sin(p.y * 2.3 + t)) + sin(p.y * 3.7 - t * 1.1 + sin(p.x * 1.9 - t * 0.7));
                return pow(saturate(1 - abs(a) * 0.9), 6);
            }
            half4 frag(Vary i) : SV_Target
            {
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));   // จากผิวน้ำมาหากล้อง
                float up = saturate(-viewDir.y);                                   // มองขึ้นตรง ๆ = 1
                float window = smoothstep(0.55, 0.85, up);                   // วงฟ้าเหนือหัว นอกวงสะท้อนกลับใต้น้ำ
                float t = _CozyWind.x;
                float c = caust(i.positionWS.xz * 1.4, t) + caust(i.positionWS.xz * 2.3 + 5, t * 1.3) * 0.6;
                Light l = GetMainLight();
                half3 sky = _Tint.rgb * (0.6 + 0.6 * l.color) + c * 0.35 * l.color;
                half3 col = lerp(_Deep.rgb * 1.2 + c * 0.05, sky, window);
                col *= (_CozyDim > 0 ? _CozyDim : 1);
                col = MixFog(col, i.fog * 0.5);
                return half4(col, lerp(0.85, 0.7, window));
            }
            ENDHLSL
        }
    }
}
