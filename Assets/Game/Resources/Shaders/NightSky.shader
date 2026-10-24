// ฟ้ากลางคืน: ดาว/ทางช้างเผือก/พระจันทร์/แสงจันทร์ระยิบบนน้ำ — แผ่นภาพบวกแสง (additive) ไม่โดนหมอก
// สีจุดยอด: rgb = สี · a = จังหวะกระพริบ (0 = ไม่กระพริบ เช่นพระจันทร์) · อยู่ใน Resources ให้ build รวม shader นี้ไว้เสมอ
Shader "CozyFishing/NightSky"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Fade ("Fade", Range(0, 1)) = 1
        _Twinkle ("Twinkle Speed", Float) = 2.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent-60" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float _Fade, _Twinkle;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            V vert(A v)
            {
                V o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float ph = v.color.a;
                float tw = ph > 0.001 ? 0.45 + 0.55 * saturate(0.5 + 0.5 * sin(_Time.y * _Twinkle * (0.6 + ph) + ph * 97.0)) : 1.0;
                o.color = half4(v.color.rgb * tw * _Fade, 1);
                o.uv = v.uv;
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return half4(t.rgb * t.a * i.color.rgb, 0);
            }
            ENDHLSL
        }
    }
}
