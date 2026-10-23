// ผิวน้ำแบบสมจริง: หักเหแสงมองเห็นพื้นบ่อ (opaque texture) · สีจางตามความลึก (depth texture) · สะท้อนฟ้า HDRI ตามมุมมอง (fresnel)
// · คลื่นเล็กหลายชั้น · ประกายแดด · ฟองบาง ๆ ริมฝั่ง · วงคลื่นจากทุ่น/ปลา
// ต้องเปิด Depth Texture + Opaque Texture ใน URP asset (SceneBuilder.ConfigureUrp ตั้งให้)
Shader "CozyFishing/Water"
{
    Properties
    {
        _Shallow ("Shallow Tint", Color) = (0.61,0.81,0.54,1)
        _Deep ("Deep Color", Color) = (0.24,0.48,0.36,1)
        _DepthScale ("Depth Scale", Float) = 1.9
        _Clarity ("Clarity (0 = murky)", Range(0,1)) = 0.55
        _Waves ("Wave Height", Float) = 0.015
        _Refraction ("Refraction", Range(0,0.1)) = 0.035
        _Glint ("Sun Glint", Range(0,4)) = 1.5
        _Reflect ("Sky Reflection", Range(0,1)) = 0.9
        _FoamWidth ("Shore Foam Width", Float) = 0.08
        _Flow ("Flow (xy)", Vector) = (0.05,0.04,0,0)
        _RapidZone ("Rapids Foam (xMin, xMax, strength)", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Shallow, _Deep, _Flow, _RapidZone;
                float _DepthScale, _Clarity, _Waves, _Refraction, _Glint, _Reflect, _FoamWidth;
            CBUFFER_END
            float4 _CozyWind;
            float4 _CozyRipples[6];   // xy = ตำแหน่ง, z = เวลาเริ่ม, w = แรง (ตั้งจาก WaterFx)
            float _CozyDim;           // กลางคืน: หรี่สีน้ำ (1 = กลางวัน)

            float wv(float2 p, float t)
            {
                return sin(dot(p, float2(0.21, 0.13)) + t * 0.9) * 0.5 + sin(dot(p, float2(-0.12, 0.27)) + t * 1.3) * 0.35
                     + sin(dot(p, float2(0.47, 0.39)) + t * 2.1) * 0.18 + sin(dot(p, float2(-0.71, 0.52)) + t * 2.9) * 0.09;
            }
            float h2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float n2(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(h2(i), h2(i + float2(1,0)), f.x), lerp(h2(i + float2(0,1)), h2(i + float2(1,1)), f.x), f.y); }
            // ระลอกเล็ก: ความชันของ noise 2 ชั้นที่ไหลสวนกัน
            float2 ripplesGrad(float2 p, float t)
            {
                float e = 0.08;
                float2 a = p * 2.3 + _Flow.xy * t * 4, b = p * 4.1 - _Flow.yx * t * 6;
                float c = n2(a) + n2(b) * 0.5;
                return float2(n2(a + float2(e, 0)) + n2(b + float2(e, 0)) * 0.5 - c, n2(a + float2(0, e)) + n2(b + float2(0, e)) * 0.5 - c) / e;
            }

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float4 screenPos : TEXCOORD1; float fog : TEXCOORD2; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                ws.y += wv(ws.xz, _CozyWind.x) * _Waves * 2;
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _CozyWind.x;
                float2 p = i.positionWS.xz;
                float2 suv = i.screenPos.xy / i.screenPos.w;
                float waterZ = LinearEyeDepth(i.positionCS.z, _ZBufferParams);
                float depth0 = max(0, LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams) - waterZ);

                // normal: คลื่นใหญ่ + ระลอกเล็ก + วงคลื่น
                float e = 0.35, A = _Waves * 2;
                float dx = (wv(p + float2(e, 0), t) - wv(p - float2(e, 0), t)) / (2 * e) * A;
                float dz = (wv(p + float2(0, e), t) - wv(p - float2(0, e), t)) / (2 * e) * A;
                float2 g = ripplesGrad(p, t) * 0.035;
                float rip = 0; float2 ripG = 0;
                [unroll] for (int j = 0; j < 6; j++)
                {
                    float age = t - _CozyRipples[j].z;
                    if (age > 0 && age < 2.4)
                    {
                        float2 dv = p - _CozyRipples[j].xy;
                        float d = length(dv) + 1e-4, rr = age * 1.1 * _CozyRipples[j].w, fade = (1 - age / 2.4) * _CozyRipples[j].w;
                        float ring = sin((d - rr) * 18) * smoothstep(0.35, 0, abs(d - rr)) * fade;
                        ripG += dv / d * ring * 0.25;
                        rip += smoothstep(0.04, 0, abs(d - rr)) * fade;
                    }
                }
                float3 N = normalize(float3(-dx - g.x - ripG.x, 1, -dz - g.y - ripG.y));
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));

                // หักเห: เลื่อน uv ตาม normal (เฉพาะที่น้ำลึกพอ) แล้วเช็กว่าไม่ไปดึงของที่อยู่หน้าน้ำ
                float2 off = N.xz * _Refraction * saturate(depth0 * 2);
                float2 ruv = suv + off;
                float depth = LinearEyeDepth(SampleSceneDepth(ruv), _ZBufferParams) - waterZ;
                if (depth < 0) { ruv = suv; depth = depth0; }
                half3 below = SampleSceneColor(ruv);

                // ดูดกลืนแสงตามความลึก: ตื้นเห็นพื้น (ย้อมสีน้ำ) → ลึกเป็นสีน้ำทึบ
                float k = 1 - exp(-depth / max(_DepthScale * _Clarity, 0.05));
                half3 lit = light.color * (saturate(light.direction.y) * 0.6 * light.shadowAttenuation + 0.25) + SampleSH(float3(0, 1, 0)) * 0.6;
                half3 col = lerp(below * _Shallow.rgb, _Deep.rgb * lit, k);

                // สะท้อนฟ้า (HDRI ผ่าน reflection probe ของฉาก) ตาม fresnel
                float3 R = reflect(-V, N);
                half3 sky = GlossyEnvironmentReflection(R, 0.06, 1);
                float fr = 0.02 + 0.98 * pow(1 - saturate(dot(V, N)), 5);
                col = lerp(col, sky, saturate(fr * _Reflect));

                // ประกายแดด
                float sp = pow(saturate(dot(R, light.direction)), 900) * 40 + pow(saturate(dot(R, light.direction)), 90) * 0.6;
                col += light.color * sp * _Glint * light.shadowAttenuation;

                // ฟองบาง ๆ ริมฝั่ง + วงคลื่น
                float fz = max(0.004, _FoamWidth + 0.04 * sin(p.x * 3 + p.y * 2 + t * 1.5) + n2(p * 3 + t * 0.3) * 0.05);   // ฟองแคบ ๆ ติดลบได้ → เกิดปื้นฟองขาวกลางน้ำลึก
                float foam = smoothstep(fz, fz * 0.3, depth0) * (0.5 + 0.5 * n2(p * 9 + t * 0.5)) * step(0.001, _FoamWidth);   // _FoamWidth = 0 ปิดฟอง (น้ำในนาตื้นมาก)
                foam = max(foam * 0.35, saturate(rip) * 0.55);
                // แก่งน้ำเชี่ยว: ริ้วฟองขาวยืดตามทิศน้ำไหล ขยับตามกระแส · ฟองหนาขึ้นตรงน้ำตื้น
                if (_RapidZone.z > 0)
                {
                    float zone = smoothstep(_RapidZone.x, _RapidZone.x + 1.5, p.x) * smoothstep(_RapidZone.y, _RapidZone.y - 1.5, p.x);
                    float2 fp = float2(p.x * 0.7 - t * _Flow.x * 2.2, p.y * 3.2);
                    float streak = n2(fp) * 0.65 + n2(fp * 2.3 + 7.1) * 0.35;
                    float shallow = 1 - smoothstep(0.15, 0.9, depth0);
                    float rf = smoothstep(0.56 - shallow * 0.2, 0.86, streak) * zone * _RapidZone.z * 0.8;
                    foam = max(foam, rf);
                }
                float dim = _CozyDim > 0 ? _CozyDim : 1;
                col = lerp(col, (half3)0.9 * lit, foam) * dim;

                // ขอบน้ำนุ่ม ไม่เป็นเส้นตัด
                float a = smoothstep(0, 0.06, depth0);
                col = MixFog(col, i.fog);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
