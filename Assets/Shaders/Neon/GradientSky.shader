// Cheap procedural skybox: 3-stop vertical gradient, twinkling hashed stars, soft nebula bands.
Shader "Polish/GradientSky"
{
    Properties
    {
        _Top ("Top", Color) = (0.03,0.02,0.10,1)
        _Mid ("Horizon", Color) = (0.16,0.06,0.28,1)
        _Bottom ("Bottom", Color) = (0.01,0.01,0.04,1)
        [HDR] _NebulaA ("Nebula A", Color) = (0.25,0.05,0.45,1)
        [HDR] _NebulaB ("Nebula B", Color) = (0.0,0.25,0.4,1)
        _StarDensity ("Star Density", Range(50,600)) = 260
        _StarBrightness ("Star Brightness", Range(0,4)) = 1.6
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Top, _Mid, _Bottom, _NebulaA, _NebulaB; half _StarDensity, _StarBrightness;
            CBUFFER_END
            struct A { float4 p : POSITION; };
            struct V { float4 cs : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert (A a) { V o; o.cs = TransformObjectToHClip(a.p.xyz); o.dir = a.p.xyz; return o; }
            float hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            half4 frag (V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                half3 col = d.y > 0 ? lerp(_Mid.rgb, _Top.rgb, pow(saturate(d.y), 0.6)) : lerp(_Mid.rgb, _Bottom.rgb, pow(saturate(-d.y), 0.5));
                float n1 = sin(d.x * 3.1 + d.y * 5.3 + _Time.y * 0.02) * sin(d.z * 4.7 - d.y * 2.3);
                float n2 = sin(d.z * 2.3 + d.x * 6.1 - _Time.y * 0.015) * sin(d.y * 3.9 + d.x * 1.7);
                col += _NebulaA.rgb * saturate(n1) * 0.35 + _NebulaB.rgb * saturate(n2) * 0.3;
                float3 cell = floor(d * _StarDensity);
                float h = hash(cell);
                float3 local = frac(d * _StarDensity) - 0.5;
                half star = step(0.985, h) * smoothstep(0.35, 0.0, length(local));
                star *= 0.6 + 0.4 * sin(_Time.y * (2 + h * 5) + h * 40);
                col += star * _StarBrightness;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
