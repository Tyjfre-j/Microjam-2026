// Stylized unlit surface used for cube stickers and platforms.
// Features: texture tint, soft UV-edge bevel, fresnel rim, height gradient,
// animated specular sheen sweep, subtle emission pulse, distance fog.
// SRP Batcher compatible + GPU instancing.
Shader "Polish/Stylized"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Header(Bevel)]
        _BevelWidth ("Bevel Width (UV)", Range(0,0.5)) = 0.08
        _BevelDarken ("Bevel Darken", Range(0,1)) = 0.35
        _BevelHighlight ("Bevel Top Highlight", Range(0,1)) = 0.25

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3
        _RimStrength ("Rim Strength", Range(0,2)) = 0.35

        [Header(Gradient)]
        _GradientTop ("Gradient Top Tint", Color) = (1,1,1,1)
        _GradientBottom ("Gradient Bottom Tint", Color) = (0.82,0.85,0.95,1)

        [Header(Sheen)]
        _SheenColor ("Sheen Color", Color) = (1,1,1,1)
        _SheenStrength ("Sheen Strength", Range(0,1)) = 0.18
        _SheenSpeed ("Sheen Speed", Float) = 0.35
        _SheenWidth ("Sheen Width", Range(0.01,0.5)) = 0.08

        [Header(Emission)]
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _PulseSpeed ("Pulse Speed", Float) = 1.5
        _PulseAmount ("Pulse Amount", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BevelWidth, _BevelDarken, _BevelHighlight;
                half4 _RimColor; half _RimPower, _RimStrength;
                half4 _GradientTop, _GradientBottom;
                half4 _SheenColor; half _SheenStrength, _SheenSpeed, _SheenWidth;
                half4 _EmissionColor; half _PulseSpeed, _PulseAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                float  heightOS   : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                o.uv         = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor  = ComputeFogFactor(p.positionCS.z);
                o.heightOS   = saturate(v.positionOS.y + 0.5);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;

                // Height gradient (object space) for depth
                col.rgb *= lerp(_GradientBottom.rgb, _GradientTop.rgb, i.heightOS);

                // Soft bevel from UV edges
                float2 e = min(i.uv, 1.0 - i.uv);
                float edge = min(e.x, e.y);
                half bevel = 1.0 - smoothstep(0.0, max(_BevelWidth, 1e-4), edge);
                col.rgb *= 1.0 - bevel * _BevelDarken;
                half topLip = (1.0 - smoothstep(0.0, max(_BevelWidth, 1e-4), 1.0 - i.uv.y)) * step(e.x, e.y + 0.0001);
                col.rgb += topLip * _BevelHighlight;

                // Fresnel rim
                float3 n = normalize(i.normalWS);
                float3 vdir = normalize(GetWorldSpaceViewDir(i.positionWS));
                half rim = pow(1.0 - saturate(dot(n, vdir)), _RimPower);
                col.rgb += _RimColor.rgb * rim * _RimStrength;

                // Diagonal sheen sweep (world space so neighbours read as one surface)
                float s = frac((i.positionWS.x + i.positionWS.y * 0.6 + i.positionWS.z * 0.3) * 0.5 - _Time.y * _SheenSpeed);
                half sheen = smoothstep(_SheenWidth, 0.0, abs(s - 0.5));
                col.rgb += _SheenColor.rgb * sheen * _SheenStrength;

                // Emission pulse
                half pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                col.rgb += _EmissionColor.rgb * pulse;

                col.rgb = MixFog(col.rgb, i.fogFactor);
                col.a = 1;
                return col;
            }
            ENDHLSL
        }

        // Depth + shadow-caster-less depth pass so post FX (DoF/SSAO) work
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
            struct A { float4 p : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 v (A a) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(a); return TransformWorldToHClip(TransformObjectToWorld(a.p.xyz)); }
            half f () : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
