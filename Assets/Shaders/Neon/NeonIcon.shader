// Holographic button visuals drawn procedurally on a quad (no textures).
// _Mode 0 = rounded neon pad with pulsing ring, _Mode 1 = arrow icon pointing +V with
// scrolling chevron trail. _Active (0..1) brightens/animates when the player stands on it.
Shader "Polish/NeonIcon"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.2,2.5,3,1)
        _Mode ("Mode (0 pad, 1 arrow)", Float) = 0
        _Active ("Active", Range(0,1)) = 0
        _Speed ("Anim Speed", Float) = 1.4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color; half _Mode, _Active, _Speed;
            CBUFFER_END

            struct A { float4 p : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; };

            V vert (A a) { V o; o.cs = TransformObjectToHClip(a.p.xyz); o.uv = a.uv; return o; }

            float sdRoundBox(float2 p, float2 b, float r) { float2 q = abs(p) - b + r; return length(max(q, 0)) + min(max(q.x, q.y), 0) - r; }

            float sdArrow(float2 p)
            {
                // head: triangle tip at y=0.8, base at y=0.05, half width 0.62
                float2 hp = float2(abs(p.x), p.y - 0.05);
                float head = max(hp.y - 0.75 + hp.x * 1.2, -hp.y) ;
                head *= 0.64;
                float shaft = sdRoundBox(p - float2(0, -0.32), float2(0.17, 0.42), 0.06);
                return min(head, shaft);
            }

            half4 frag (V i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float t = _Time.y * _Speed;
                float aa = fwidth(p.x) * 1.5;
                half a = 0;

                if (_Mode < 0.5)
                {
                    float d = sdRoundBox(p, float2(0.86, 0.86), 0.32);
                    half ring = smoothstep(aa + 0.06, 0.0, abs(d));
                    half fill = (d < 0) ? 0.025 + 0.06 * _Active : 0;
                    half glow = exp(-max(d, 0) * 14) * (d > 0 ? 0.35 : 0);
                    // expanding ripple when active
                    float r = frac(t * 0.6);
                    half ripple = smoothstep(0.05, 0, abs(d + 0.7 - r * 0.75)) * (1 - r) * (0.25 + _Active);
                    // sweeping scan line
                    half scan = smoothstep(0.06, 0, abs(frac(p.y * 0.5 + 0.5 - t * 0.35) - 0.5)) * (d < 0) * 0.08;
                    half pulse = 0.75 + 0.25 * sin(t * 3.0);
                    a = ring * pulse * (1 + _Active) + fill + glow * 0.5 + ripple + scan;
                }
                else
                {
                    float bob = sin(t * 3.0) * 0.06 * (0.5 + _Active);
                    float2 q = p - float2(0, bob);
                    float d = sdArrow(q);
                    half body = smoothstep(aa, -aa, d);
                    half outline = smoothstep(aa + 0.05, 0, abs(d)) * 0.9;
                    half glow = exp(-max(d, 0) * 10) * 0.45;
                    // chevron trail behind the arrow
                    half trail = 0;
                    for (int k = 0; k < 3; k++)
                    {
                        float off = frac(t * 0.8 + k / 3.0);
                        float c = lerp(-1.0, -0.15, off);
                        float f = (q.y - c) + abs(q.x) * 0.9;
                        trail += smoothstep(0.07, 0.0, abs(f)) * step(abs(q.x), 0.55) * sin(off * 3.14159) * step(q.y, 0.0) * 0.45;
                    }
                    a = body * (0.55 + 0.45 * _Active) + outline + glow + trail * (0.4 + _Active);
                }

                half3 col = _Color.rgb * a;
                return half4(col, saturate(a));
            }
            ENDHLSL
        }
    }
}
