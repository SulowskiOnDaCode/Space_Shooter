// Additive HDR particle shader used by SpaceExplosion.
// Lives in a Resources folder so it is always included in builds.
Shader "SpaceShooter/ExplosionAdditive"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("HDR Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend One One
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _Intensity;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                half4  color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 t = tex2D(_MainTex, i.uv);
                half a = t.a * i.color.a;
                // Pre-multiplied additive, boosted into HDR so bloom picks it up.
                return half4(i.color.rgb * t.rgb * a * _Intensity, 0);
            }
            ENDCG
        }
    }
    Fallback Off
}
