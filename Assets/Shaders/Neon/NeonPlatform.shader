// Thin neon platform: shrinks the visual thickness in the vertex stage (collider untouched),
// dark glassy core, HDR glowing edges (picked up by bloom), hue drifting across world space,
// bright top lip where the player walks, and a travelling energy pulse along the length.
Shader "Polish/NeonPlatform"
{
    Properties
    {
        _Thickness ("Visual Thickness", Range(0.1,1)) = 0.55
        _CoreColor ("Core Color", Color) = (0.03,0.04,0.09,1)
        [HDR] _ColorA ("Neon A", Color) = (0.0,2.2,3.0,1)
        [HDR] _ColorB ("Neon B", Color) = (2.6,0.3,2.4,1)
        _HueScale ("Hue World Scale", Float) = 0.9
        _HueSpeed ("Hue Speed", Float) = 0.15
        _EdgeWidth ("Edge Width (world)", Float) = 0.006
        _EdgeBoost ("Edge Boost", Range(0,4)) = 1.6
        _TopLip ("Top Lip Brightness", Range(0,3)) = 1.2
        _PulseSpeed ("Pulse Speed", Float) = 0.8
        _PulseStrength ("Pulse Strength", Range(0,2)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Neon"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Thickness;
                half4 _CoreColor, _ColorA, _ColorB;
                half _HueScale, _HueSpeed, _EdgeWidth, _EdgeBoost, _TopLip, _PulseSpeed, _PulseStrength;
            CBUFFER_END

            struct A { float4 p : POSITION; float3 n : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V
            {
                float4 cs : SV_POSITION;
                float3 os : TEXCOORD0;
                float3 ws : TEXCOORD1;
                float3 nws : TEXCOORD2;
                float3 scale : TEXCOORD3;
                float fog : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            V vert (A a)
            {
                V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_TRANSFER_INSTANCE_ID(a, o);
                float3 p = a.p.xyz;
                p.y *= _Thickness;
                o.os = p;
                o.ws = TransformObjectToWorld(p);
                o.cs = TransformWorldToHClip(o.ws);
                o.nws = TransformObjectToWorldNormal(a.n);
                float4x4 m = GetObjectToWorldMatrix();
                o.scale = float3(length(m._m00_m10_m20), length(m._m01_m11_m21) * _Thickness, length(m._m02_m12_m22));
                o.fog = ComputeFogFactor(o.cs.z);
                return o;
            }

            half4 frag (V i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 half3s = float3(0.5, 0.5 * _Thickness, 0.5);
                float3 d = (half3s - abs(i.os)) * (i.scale / float3(1, _Thickness, 1));
                d = max(d, 0);
                // distance to nearest edge on the current face = 2nd smallest axis distance
                float mn = min(d.x, min(d.y, d.z));
                float mx = max(d.x, max(d.y, d.z));
                float edgeDist = d.x + d.y + d.z - mn - mx;
                half edge = exp(-edgeDist / max(_EdgeWidth, 1e-4));

                float h = frac(dot(i.ws, float3(0.6, 0.35, 0.25)) * _HueScale + _Time.y * _HueSpeed);
                half3 neon = lerp(_ColorA.rgb, _ColorB.rgb, smoothstep(0, 1, abs(h * 2 - 1)));

                // energy pulse running along the platform length (object x)
                float run = frac(i.os.x + 0.5 - _Time.y * _PulseSpeed + dot(i.ws, 3.1.xxx));
                half pulse = smoothstep(0.12, 0, abs(run - 0.5)) * _PulseStrength;

                half top = saturate(i.os.y / (0.5 * _Thickness)) ;
                top = pow(top, 6) * _TopLip;

                half3 col = _CoreColor.rgb + neon * 0.08;
                col += neon * (edge * _EdgeBoost + top + pulse * (0.4 + edge));

                // soft fresnel sheen on the glassy core
                float3 vd = normalize(GetWorldSpaceViewDir(i.ws));
                half fr = pow(1 - saturate(abs(dot(normalize(i.nws), vd))), 3);
                col += neon * fr * 0.35;

                col = MixFog(col, i.fog);
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
                half _Thickness;
                half4 _CoreColor, _ColorA, _ColorB;
                half _HueScale, _HueSpeed, _EdgeWidth, _EdgeBoost, _TopLip, _PulseSpeed, _PulseStrength;
            CBUFFER_END
            struct A { float4 p : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 v (A a) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(a); float3 p = a.p.xyz; p.y *= _Thickness; return TransformWorldToHClip(TransformObjectToWorld(p)); }
            half f () : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
