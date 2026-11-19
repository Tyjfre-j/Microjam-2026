// Holographic neon for 3D title text: cyan->magenta gradient across world X, bright HDR
// fresnel rim, scrolling scanlines, a periodic light sweep and a soft flicker.
Shader "Polish/NeonHolo"
{
    Properties
    {
        [HDR] _ColorA ("Color A", Color) = (0.1,1.6,2.4,1)
        [HDR] _ColorB ("Color B", Color) = (2.2,0.35,1.9,1)
        _GradientScale ("Gradient Scale", Float) = 0.08
        _Face ("Face Brightness", Range(0,1)) = 0.35
        _RimPower ("Rim Power", Range(0.5,8)) = 2
        _ScanDensity ("Scanline Density", Float) = 3
        _ScanStrength ("Scanline Strength", Range(0,1)) = 0.15
        _SweepSpeed ("Sweep Speed", Float) = 0.25
        _Flicker ("Flicker", Range(0,0.3)) = 0.04
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA, _ColorB; half _GradientScale, _Face, _RimPower, _ScanDensity, _ScanStrength, _SweepSpeed, _Flicker;
            CBUFFER_END
            struct A { float4 p : POSITION; float3 n : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 ws : TEXCOORD0; float3 n : TEXCOORD1; };
            V vert (A a) { V o; UNITY_SETUP_INSTANCE_ID(a); o.ws = TransformObjectToWorld(a.p.xyz); o.cs = TransformWorldToHClip(o.ws); o.n = TransformObjectToWorldNormal(a.n); return o; }
            half4 frag (V i) : SV_Target
            {
                float t = _Time.y;
                half g = saturate(frac(i.ws.x * _GradientScale + t * 0.05) * 2 - 0) ;
                g = abs(frac(i.ws.x * _GradientScale + t * 0.05) * 2 - 1);
                half3 neon = lerp(_ColorA.rgb, _ColorB.rgb, smoothstep(0, 1, g));
                float3 n = normalize(i.n);
                float3 vd = normalize(GetWorldSpaceViewDir(i.ws));
                half rim = pow(1 - saturate(dot(n, vd)), _RimPower);
                half facing = saturate(dot(n, vd));
                half scan = 1 - _ScanStrength * step(0.5, frac(i.ws.y * _ScanDensity - t * 0.8));
                half sweep = smoothstep(0.08, 0, abs(frac(i.ws.x * 0.02 - t * _SweepSpeed) - 0.5)) * 1.5;
                half flick = 1 - _Flicker * step(0.97, frac(sin(floor(t * 20) * 12.9898) * 43758.5453));
                half3 col = neon * (_Face * facing + rim * 1.4 + sweep) * scan * flick;
                // darker extrusion sides so the letters read in 3D
                col *= lerp(0.45, 1, facing);
                return half4(col, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex v
            #pragma fragment f
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA, _ColorB; half _GradientScale, _Face, _RimPower, _ScanDensity, _ScanStrength, _SweepSpeed, _Flicker;
            CBUFFER_END
            struct A { float4 p : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 v (A a) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(a); return TransformWorldToHClip(TransformObjectToWorld(a.p.xyz)); }
            half f () : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
