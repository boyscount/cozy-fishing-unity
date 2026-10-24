// วิญญาณผี (แบบสมจริง): ผิวซีดเทาอมฟ้า มืด เกือบทึบ มีรอยด่างช้ำ · ขอบเรืองจาง ๆ แค่พอเห็นรูปร่างในความมืด
// · ภาพกระตุก/สัญญาณขาด: บางจังหวะร่างทั้งชิ้นเลื่อนเป็นแถบแนวนอน (เหมือนภาพหลุดเฟรม) · วูบหายเป็นจังหวะ
// _Dark = 1 ใช้กับผีเงาดำ/ผมดำ — ทึบดำ ขอบหม่น · อยู่ใน Resources เพื่อให้ build รวม shader นี้ไว้เสมอ
Shader "CozyFishing/Ghost"
{
    Properties
    {
        _Color ("Glow", Color) = (0.75, 0.9, 0.95, 1)
        _Alpha ("Alpha (fade)", Range(0, 1)) = 1
        _Dark ("Dark Body", Range(0, 1)) = 0
        _Wobble ("Wobble", Float) = 0.01
        _Flicker ("Flicker", Range(0, 1)) = 0.35
        _Glitch ("Glitch", Range(0, 1)) = 0.6
        _FeetFade ("Feet Fade (m)", Float) = 0.9
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _Color; float _Alpha, _Dark, _Wobble, _Flicker, _Glitch, _Cull, _FeetFade;
        CBUFFER_END
        float hash(float n) { return frac(sin(n) * 43758.5453); }
        float3 Displace(float3 p, float3 wp)
        {
            float t = _Time.y;
            p.x += sin(t * 2.1 + wp.y * 3.0) * _Wobble; p.z += cos(t * 1.7 + wp.y * 2.3) * _Wobble;
            return p;
        }
        float3 GlitchWS(float3 wp)
        {
            float t = _Time.y;
            float slice = floor(wp.y * 7.0);
            float tick = floor(t * 13.0);
            float g = step(1.0 - 0.09 * _Glitch, hash(slice * 1.7 + tick * 3.1));            // บางแถบเลื่อนเป็นจังหวะ
            float whole = step(1.0 - 0.04 * _Glitch, hash(tick * 0.37));                       // บางเฟรมทั้งร่างกระโดด
            float3 off = float3(hash(slice + tick) - 0.5, 0, hash(slice * 2.3 + tick) - 0.5) * 0.22 * g;
            off.x += (hash(tick * 1.3) - 0.5) * 0.35 * whole;
            return wp + off;
        }
        ENDHLSL
        // ขั้นแรก: เขียนความลึกอย่างเดียว (ร่างโปร่งแสงไม่เห็นชิ้นส่วนข้างในซ้อนกัน)
        Pass
        {
            Name "DepthPrime"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct A { float4 positionOS : POSITION; };
            float4 vert(A v) : SV_POSITION { float3 wp = TransformObjectToWorld(v.positionOS.xyz); return TransformWorldToHClip(GlitchWS(TransformObjectToWorld(Displace(v.positionOS.xyz, wp)))); }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "Ghost"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest LEqual Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; float fog : TEXCOORD2; };
            V vert(A v)
            {
                V o;
                float3 wp0 = TransformObjectToWorld(v.positionOS.xyz);
                o.positionWS = GlitchWS(TransformObjectToWorld(Displace(v.positionOS.xyz, wp0)));
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(V i, bool front : SV_IsFrontFace) : SV_Target
            {
                float3 n = normalize(i.normalWS) * (front ? 1 : -1);
                float3 vd = normalize(GetWorldSpaceViewDir(i.positionWS));
                float ndv = saturate(dot(n, vd));
                float rim = pow(1 - ndv, 3.0);
                float t = _Time.y;
                float fl = 1 - _Flicker * step(0.9, hash(floor(t * 11))) * 0.75;
                // ผิวศพ: ด่างช้ำจาง ๆ ตามตำแหน่ง · ด้านบนซีดกว่าด้านล่าง (เหมือนแสงจันทร์)
                float mott = 0.75 + 0.25 * hash(floor(i.positionWS.x * 9) + floor(i.positionWS.y * 9) * 7.1 + floor(i.positionWS.z * 9) * 3.3);
                float top = saturate(n.y * 0.5 + 0.5);
                half3 skin = lerp(half3(0.025, 0.03, 0.035), half3(0.2, 0.23, 0.25), top * 0.55 + ndv * 0.2) * mott;   // ผิวศพคล้ำ ไม่ใช่ขาวเรือง
                skin = lerp(skin, _Color.rgb * 0.2, 0.2);
                half3 col = skin + _Color.rgb * rim * 0.32;
                half3 dark = lerp(half3(0.012, 0.012, 0.014), _Color.rgb * 0.22, rim);
                col = lerp(col, dark, _Dark);
                float scan = 0.97 + 0.03 * sin(i.positionWS.y * 140 + t * 30);                    // เส้นสัญญาณภาพจาง ๆ
                col *= scan;
                float a = _Alpha * fl * lerp(0.72 + rim * 0.22, 0.93, _Dark);
                // ช่วงขาจางหายเป็นหมอก (ผีไทยไม่มีเท้า)
                float originY = UNITY_MATRIX_M._m13;
                if (_FeetFade > 0.01) a *= smoothstep(0.0, 1.0, saturate((i.positionWS.y - originY) / _FeetFade));
                col = MixFog(col, i.fog);
                return half4(col, saturate(a));
            }
            ENDHLSL
        }
    }
}
