// White energy-shield: bright fresnel rim, faint inner fill, slow pulse and a subtle
// moving shimmer band. Additive + HDR so the post-processing bloom gives it a soft glow.
// Driven by ShieldGlow.cs. Lives in Resources so it is always included in builds.
Shader "SpaceShooter/ShieldFresnel"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity ("Rim Intensity", Float) = 2.2
        _FillIntensity ("Inner Fill", Float) = 0.06
        _PulseSpeed ("Pulse Speed", Float) = 2.5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.2
        _Fade ("Fade (0-1)", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Back
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _RimPower, _RimIntensity, _FillIntensity, _PulseSpeed, _PulseAmount, _Fade;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 normal   : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 objPos   : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.objPos = v.vertex.xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                // Orthographic cameras look along one direction; perspective ones from a point.
                float3 v = unity_OrthoParams.w > 0.5
                    ? normalize(UNITY_MATRIX_V[2].xyz)
                    : normalize(_WorldSpaceCameraPos - i.worldPos);

                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower);

                float t = _Time.y;
                float pulse = 1.0 + _PulseAmount * sin(t * _PulseSpeed);

                // Thin shimmer band sweeping slowly across the bubble.
                float band = sin(i.objPos.y * 18.0 - t * 3.0) * 0.5 + 0.5;
                band = pow(band, 12.0) * 0.25 * (0.3 + rim);

                float glow = (rim * _RimIntensity + _FillIntensity + band) * pulse * _Fade;
                return float4(_Color.rgb * glow, 0);
            }
            ENDCG
        }
    }
    Fallback Off
}
