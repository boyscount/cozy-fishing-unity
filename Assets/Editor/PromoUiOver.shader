// ถ่ายคลิป: วางภาพ UI (ความโปร่งใสแบบคูณแล้ว) ทับภาพกล้อง — PromoCapture ใช้ตอนอัดพร้อม HUD
Shader "Hidden/PromoUiOver"
{
    Properties { _MainTex ("UI", 2D) = "black" {} }
    SubShader
    {
        ZTest Always ZWrite Off Cull Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 frag (v2f_img i) : SV_Target { return tex2D(_MainTex, i.uv); }
            ENDCG
        }
    }
}
