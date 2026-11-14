// Soft glow outline for enemy ships.
// Draws a slightly inflated copy of the mesh (inside faces only) and lights it with a fresnel
// term, so only the silhouette edge glows while the middle stays clear: a subtle halo.
// Used by EnemyGlow.cs. Lives in Resources so it is always included in builds.
Shader "SpaceShooter/EnemyGlow"
{
    Properties
    {
        _Color ("Glow Color", Color) = (1, 0.36, 0.3, 1)
        _Width ("Width (world units)", Float) = 0.06
        _Intensity ("Intensity", Float) = 1.3
        _Power ("Edge Sharpness", Float) = 2.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Front
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Width, _Intensity, _Power;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float3 worldPos : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                float3 n = UnityObjectToWorldNormal(v.normal);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz + n * _Width;
                o.pos = UnityWorldToClipPos(wp);
                o.normal = n;
                o.worldPos = wp;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 v = unity_OrthoParams.w > 0.5
                    ? normalize(UNITY_MATRIX_V[2].xyz)
                    : normalize(_WorldSpaceCameraPos - i.worldPos);
                float edge = pow(1.0 - abs(dot(n, v)), _Power);
                return float4(_Color.rgb * edge * _Intensity, 0);
            }
            ENDCG
        }
    }
    Fallback Off
}
