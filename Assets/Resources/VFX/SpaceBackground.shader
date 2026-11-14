// Clean, monochrome "digital realism" space backdrop:
//   near-black cool gradient, one soft steel-blue haze, crisp white stars (rare ones with a fine
//   cross glint), a faint world-anchored grid for a digital feel, and a soft vignette.
// Everything parallaxes. Driven by SpaceBackground.cs. Lives in Resources so it's in builds.
Shader "SpaceShooter/ProceduralSpace"
{
    Properties
    {
        _Offset ("Parallax Offset (world units)", Vector) = (0,0,0,0)
        _Aspect ("Aspect", Float) = 1.777
        _Scale ("View Height (world units)", Float) = 14

        _BaseTop ("Base Top", Color) = (0.016, 0.020, 0.032, 1)
        _BaseBottom ("Base Bottom", Color) = (0.006, 0.007, 0.012, 1)
        _Haze ("Haze Tint", Color) = (0.42, 0.52, 0.66, 1)
        _HazeIntensity ("Haze Intensity", Float) = 0.11

        _StarBrightness ("Star Brightness", Float) = 1.0

        _GridColor ("Grid Color", Color) = (0.62, 0.74, 0.9, 1)
        _GridMinor ("Grid Minor Spacing (world)", Float) = 2
        _GridMajor ("Grid Major Every N Minor", Float) = 5
        _GridAlpha ("Grid Strength", Float) = 0.035
        _GridParallax ("Grid Parallax", Float) = 0.45

        _Vignette ("Vignette", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "IgnoreProjector"="True" "PreviewType"="Plane" }
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _Offset;
            float _Aspect, _Scale;
            float4 _BaseTop, _BaseBottom, _Haze, _GridColor;
            float _HazeIntensity, _StarBrightness, _GridMinor, _GridMajor, _GridAlpha, _GridParallax, _Vignette;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), u.x),
                            lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                const float2x2 rot = float2x2(0.8, -0.6, 0.6, 0.8);
                for (int i = 0; i < 4; i++) { v += a * vnoise(p); p = mul(rot, p) * 2.03 + 17.1; a *= 0.5; }
                return v;
            }

            // Crisp point stars; the brightest few get a thin 4-point glint.
            float stars(float2 p, float density, float probability, float radius, float bright, float px, float glint)
            {
                float2 g = p * density;
                float2 id = floor(g);
                float2 f = frac(g) - 0.5;

                float h = hash21(id);
                if (h > probability) return 0.0;
                float h2 = hash21(id + 31.7);
                float h3 = hash21(id + 71.3);

                float2 pos = (float2(hash21(id + 3.1), hash21(id + 9.7)) - 0.5) * 0.6;
                float2 dv = (f - pos) / density;               // world units
                float d = length(dv);

                float r = radius * lerp(0.6, 1.2, h2);
                float rr = max(r, px * 0.8);
                float core = exp(-(d * d) / (rr * rr)) * (r * r) / (rr * rr);

                // Cross glint only on a few bright stars.
                float spikes = 0.0;
                if (glint > 0.0 && h2 > 0.82)
                {
                    float w = px * 0.6;
                    float len = radius * 14.0;
                    spikes = (exp(-abs(dv.y) / w) * exp(-abs(dv.x) / len) +
                              exp(-abs(dv.x) / w) * exp(-abs(dv.y) / len)) * 0.35 * glint;
                }

                float twinkle = 0.85 + 0.15 * sin(_Time.y * (0.8 + h3 * 2.0) + h * 40.0);
                return (core + spikes) * bright * twinkle * lerp(0.35, 1.0, h2);
            }

            // Thin anti-aliased grid lines (world space).
            float gridLines(float2 p, float spacing, float px, float widthPx)
            {
                float2 c = abs(frac(p / spacing + 0.5) - 0.5) * spacing;   // distance to nearest line
                float2 l = 1.0 - smoothstep(px * widthPx * 0.5, px * (widthPx * 0.5 + 1.0), c);
                return max(l.x, l.y);
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float2 p = float2(uv.x * _Aspect, uv.y) * _Scale;   // world units around view centre
                float px = _Scale / _ScreenParams.y;                // world units per pixel
                float2 o = _Offset.xy;

                // Base gradient
                float3 col = lerp(_BaseBottom.rgb, _BaseTop.rgb, i.uv.y);

                // Single soft haze layer (one hue only), with darker dust breaks.
                float2 hp = (p + o * 0.03) * 0.06;
                float haze = smoothstep(0.42, 0.85, fbm(hp + fbm(hp * 1.7 + 3.0) * 0.8));
                haze *= lerp(0.55, 1.0, smoothstep(0.35, 0.65, fbm(hp * 2.4 + 9.0)));
                col += _Haze.rgb * haze * _HazeIntensity;

                // Faint digital grid, minor + slightly stronger major lines.
                float2 gp = p + o * _GridParallax;
                float minor = gridLines(gp, _GridMinor, px, 1.0);
                float major = gridLines(gp, _GridMinor * _GridMajor, px, 1.4);
                col += _GridColor.rgb * _GridAlpha * (minor * 0.6 + major * 1.4);

                // Stars: far / mid / near (white, slightly cool).
                float3 starTint = float3(0.9, 0.95, 1.0);
                float s = 0.0;
                s += stars(p + o * 0.06, 2.2, 0.40, 0.016, 0.7 * _StarBrightness, px, 0.0);
                s += stars(p + o * 0.16, 0.9, 0.35, 0.026, 1.1 * _StarBrightness, px, 0.0);
                s += stars(p + o * 0.35, 0.28, 0.35, 0.045, 1.6 * _StarBrightness, px, 1.0);
                col += starTint * s;

                // Vignette keeps the eye on the play area.
                float vig = 1.0 - _Vignette * smoothstep(0.35, 0.95, length(uv * float2(1.0, 1.15)) * 1.25);
                col *= vig;

                return float4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
