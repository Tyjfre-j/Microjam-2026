// Player shader: toon-banded lighting from main light, bright fresnel outline glow,
// breathing emission. Makes the tiny player pop against the busy cube.
Shader "Polish/PlayerGlow"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1,0.95,0.85,1)
        _ShadowColor ("Shadow Color", Color) = (0.45,0.4,0.6,1)
        _Bands ("Toon Bands", Range(1,6)) = 3
        [HDR] _RimColor ("Rim Glow", Color) = (1.6,1.2,0.5,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2.2
        _PulseSpeed ("Pulse Speed", Float) = 3
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _ShadowColor, _RimColor;
                half _Bands, _RimPower, _PulseSpeed;
            CBUFFER_END

            struct A { float4 p : POSITION; float3 n : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 n : TEXCOORD0; float3 ws : TEXCOORD1; };

            V vert (A a)
            {
                V o; UNITY_SETUP_INSTANCE_ID(a);
                o.ws = TransformObjectToWorld(a.p.xyz);
                o.cs = TransformWorldToHClip(o.ws);
                o.n = TransformObjectToWorldNormal(a.n);
                return o;
            }

            half4 frag (V i) : SV_Target
            {
                float3 n = normalize(i.n);
                Light l = GetMainLight();
                half ndl = saturate(dot(n, l.direction) * 0.5 + 0.5);
                ndl = floor(ndl * _Bands) / _Bands;
                half3 col = lerp(_ShadowColor.rgb, _BaseColor.rgb, ndl);
                float3 vd = normalize(GetWorldSpaceViewDir(i.ws));
                half rim = pow(1.0 - saturate(dot(n, vd)), _RimPower);
                half pulse = 0.75 + 0.25 * sin(_Time.y * _PulseSpeed);
                col += _RimColor.rgb * rim * pulse;
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
                half4 _BaseColor, _ShadowColor, _RimColor;
                half _Bands, _RimPower, _PulseSpeed;
            CBUFFER_END
            struct A { float4 p : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 v (A a) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(a); return TransformWorldToHClip(TransformObjectToWorld(a.p.xyz)); }
            half f () : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
