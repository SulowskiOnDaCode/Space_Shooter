// Monochrome steel skybox matching SpaceBackground.shader. Not visible in the game view
// (the camera draws the procedural background quad); it exists so reflections on the ships
// show the same cool, monochrome space instead of the old daylight HDRI.
// Used by LightingRig.cs. Lives in Resources so it is always included in builds.
Shader "SpaceShooter/SpaceSkybox"
{
    Properties
    {
        _Base ("Base", Color) = (0.012, 0.015, 0.024, 1)
        _NebulaA ("Nebula A", Color) = (0.30, 0.37, 0.48, 1)
        _NebulaB ("Nebula B", Color) = (0.42, 0.52, 0.66, 1)
        _NebulaC ("Nebula C", Color) = (0.55, 0.62, 0.72, 1)
        _Intensity ("Intensity", Float) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _Base, _NebulaA, _NebulaB, _NebulaC;
            float _Intensity;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float vnoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                float n000 = hash31(i), n100 = hash31(i + float3(1,0,0));
                float n010 = hash31(i + float3(0,1,0)), n110 = hash31(i + float3(1,1,0));
                float n001 = hash31(i + float3(0,0,1)), n101 = hash31(i + float3(1,0,1));
                float n011 = hash31(i + float3(0,1,1)), n111 = hash31(i + float3(1,1,1));
                return lerp(lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y),
                            lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y), u.z);
            }

            float fbm(float3 p)
            {
                float v = 0.0, a = 0.5;
                for (int i = 0; i < 4; i++) { v += a * vnoise(p); p = p * 2.07 + 11.3; a *= 0.5; }
                return v;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 q = d * 2.2;
                float w = fbm(q + 4.1);
                float n = fbm(q + w * 1.8);

                float cloud = smoothstep(0.38, 0.8, n);
                float3 neb = lerp(_NebulaA.rgb, _NebulaB.rgb, saturate(w * 1.5 - 0.25)) * cloud;
                neb += _NebulaC.rgb * 0.5 * smoothstep(0.55, 0.85, fbm(q * 1.7 + 23.0));

                // Sparse stars so glossy hulls get a few tiny glints.
                float3 cell = floor(d * 140.0);
                float s = hash31(cell);
                float star = step(0.9965, s) * (0.6 + 0.4 * hash31(cell + 7.0));

                float3 col = _Base.rgb + neb * _Intensity + star;
                return float4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
