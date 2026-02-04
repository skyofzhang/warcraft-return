Shader "MT/RiverWater"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.10, 0.35, 0.45, 0.65)
        _DeepColor("Deep Color", Color) = (0.03, 0.16, 0.22, 1)
        _ShallowColor("Shallow Color", Color) = (0.18, 0.48, 0.55, 1)
        _DepthMax("Depth Max (m)", Range(0.1, 10)) = 3.5
        _DepthColorStrength("Depth Color Strength", Range(0, 1)) = 0.85
        _DepthAlphaStrength("Depth Alpha Strength", Range(0, 1)) = 0.65
        _BaseMap("Base Map", 2D) = "white" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _NoiseTex("Noise", 2D) = "white" {}

        _FlowSpeed1("Flow Speed 1 (UV)", Vector) = (0.03, 0.00, 0, 0)
        _FlowSpeed2("Flow Speed 2 (Normal UV)", Vector) = (0.015, 0.010, 0, 0)

        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3
        _FresnelIntensity("Fresnel Intensity", Range(0, 1)) = 0.35
        _SpecularBoost("Specular Boost", Range(0, 2)) = 0.6

        _FoamColor("Foam Color", Color) = (0.85, 0.92, 0.95, 1)
        _FoamWidth("Foam Width (UV)", Range(0, 0.25)) = 0.08
        _FoamNoiseScale("Foam Noise Scale", Range(0.5, 8)) = 2.5
        _FoamIntensity("Foam Intensity", Range(0, 1)) = 0.55
        _ShoreFoamDepth("Shore Foam Depth (m)", Range(0.05, 2)) = 0.55
        _ShoreFoamIntensity("Shore Foam Intensity", Range(0, 1)) = 0.75
        _ChannelDepthPower("Channel Depth Power", Range(0.5, 4)) = 1.6
        _AlphaDeep("Deep Alpha", Range(0,1)) = 0.78
        _RefractionStrength("Refraction Strength", Range(0, 0.08)) = 0.03
        _RefractionFresnelBoost("Refraction Fresnel Boost", Range(0, 1)) = 0.35

        _Alpha("Alpha", Range(0,1)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DeepColor;
                float4 _ShallowColor;
                float _DepthMax;
                float _DepthColorStrength;
                float _DepthAlphaStrength;
                float4 _BaseMap_ST;
                float4 _FlowSpeed1;
                float4 _FlowSpeed2;
                float _FresnelPower;
                float _FresnelIntensity;
                float _SpecularBoost;
                float4 _FoamColor;
                float _FoamWidth;
                float _FoamNoiseScale;
                float _FoamIntensity;
                float _ShoreFoamDepth;
                float _ShoreFoamIntensity;
                float _ChannelDepthPower;
                float _AlphaDeep;
                float _RefractionStrength;
                float _RefractionFresnelBoost;
                float _Alpha;
            CBUFFER_END

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);      SAMPLER(sampler_BumpMap);
            TEXTURE2D(_NoiseTex);     SAMPLER(sampler_NoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float2 uvRaw      : TEXCOORD3; // 0..1 across strip width
                float2 uvTex      : TEXCOORD4; // transformed by _BaseMap_ST
                float3 tangentWS  : TEXCOORD5;
                float3 bitanWS    : TEXCOORD6;
                float4 screenPos  : TEXCOORD7;
                half  fogFactor   : TEXCOORD8;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs nor = GetVertexNormalInputs(v.normalOS, v.tangentOS);

                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = nor.normalWS;
                o.tangentWS = nor.tangentWS;
                o.bitanWS = nor.bitangentWS;
                o.viewDirWS = GetWorldSpaceViewDir(pos.positionWS);
                o.uvRaw = v.uv;
                o.uvTex = TRANSFORM_TEX(v.uv, _BaseMap);
                o.screenPos = ComputeScreenPos(pos.positionCS);
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewDirWS);

                float2 uv1 = i.uvTex + _FlowSpeed1.xy * _Time.y;
                float2 uv2 = i.uvTex + _FlowSpeed2.xy * _Time.y;

                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv1) * _BaseColor;

                // Normal flow (tangent space)
                half3 nTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv2));
                // Add subtle procedural wobble from noise (keep it subtle to avoid blotchy specular)
                float n0 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv2 * 0.8 + _Time.y * 0.03).r;
                float n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv2 * 0.8 + float2(0.07, 0.03) + _Time.y * 0.03).r;
                float2 grad = float2(n1 - n0, n0 - n1);
                nTS.xy = normalize(nTS.xy + grad * 0.10);
                float3 T = normalize(i.tangentWS);
                float3 B = normalize(i.bitanWS);
                float3 nWS = normalize(T * nTS.x + B * nTS.y + N * nTS.z);

                // Fresnel
                float fres = pow(saturate(1.0 - dot(normalize(nWS), V)), _FresnelPower) * _FresnelIntensity;

                // Simple specular highlight (single directional light if available)
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(normalize(nWS), H)), 40.0) * _SpecularBoost * mainLight.shadowAttenuation;

                // Foam along mesh edges: v=0/1 are banks for our strip UVs
                // IMPORTANT: only near banks should be 1.0; center should be ~0.
                float w0 = max(_FoamWidth, 0.001);
                float w1 = w0 * 2.0;
                float edgeL = 1.0 - smoothstep(w0, w1, i.uvRaw.y);
                float edgeR = 1.0 - smoothstep(w0, w1, 1.0 - i.uvRaw.y);
                float edge = saturate(edgeL + edgeR);
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uvRaw * _FoamNoiseScale + _Time.y * 0.05).r;
                float foam = saturate(edge * (0.65 + noise * 0.7)) * _FoamIntensity;

                float3 col = baseCol.rgb;
                // Depth-based color/alpha (requires depth texture)
                float2 screenUV = i.screenPos.xy / max(i.screenPos.w, 1e-5);
                float sceneRaw = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(sceneRaw, _ZBufferParams);
                float waterEye = -TransformWorldToView(i.positionWS).z;
                float depthDiff = max(0.0, sceneEye - waterEye);
                float depth01Scene = saturate(depthDiff / max(_DepthMax, 0.001));

                // In our procedural map the river plane sits just above the ground, so depthDiff is near-zero everywhere.
                // Use channel shape (uvRaw.y distance to banks) as stable "virtual depth".
                float center01 = 1.0 - abs(i.uvRaw.y * 2.0 - 1.0); // 1 at center, 0 at banks
                float depth01Uv = pow(saturate(center01), _ChannelDepthPower);

                // Combine: prefer the deeper of the two (so center stays deep even if scene depth is shallow)
                float depth01 = max(depth01Uv, depth01Scene);

                float3 depthCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth01);
                col = lerp(col, depthCol, _DepthColorStrength);

                // extra shoreline foam:
                // - UV-based: strictly near banks
                float shoreUv = edge * _ShoreFoamIntensity;
                // - Depth-based: only boost near banks (prevents "white river" when the ground is close everywhere)
                float sceneValid = step(sceneRaw, 0.9999); // depth=1 means sky
                float shoreScene = sceneValid * saturate(1.0 - depthDiff / max(_ShoreFoamDepth, 0.001));
                float shoreBoost = shoreUv + shoreScene * edge * 0.45;

                float foamAll = saturate(foam * (0.65 + shoreBoost) + shoreBoost * (0.20 + noise * 0.55));
                col = lerp(col, _FoamColor.rgb, foamAll);
                col += fres * 0.45;
                col += spec;

                half alphaDepth = lerp(_Alpha, _AlphaDeep, saturate(depth01 * _DepthAlphaStrength));
                half alpha = saturate(alphaDepth + fres * 0.18 + foamAll * 0.25);

                // Refraction (requires opaque texture). Keep subtle to avoid "milky" look.
                float2 refrUV = screenUV + (nTS.xy * (_RefractionStrength * (0.35 + fres * _RefractionFresnelBoost)));
                float3 sceneCol = SampleSceneColor(refrUV).rgb;
                col = lerp(sceneCol, col, saturate(alphaDepth + 0.12));

                half4 outCol = half4(col, alpha);
                outCol.rgb = MixFog(outCol.rgb, i.fogFactor);
                return outCol;
            }
            ENDHLSL
        }
    }
    FallBack Off
}

