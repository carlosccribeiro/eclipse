// Deviation: URP HLSL quad shader instead of Decal Projector / Shader Graph (fallback foreseen in M07 plan; same TelegraphView API).
// Unlit, alpha blended, drawn at queue Transparent+100 (3100) so enemy telegraphs render above player VFX (06 section 3.10).
// Quad UV 0..1; p = uv*2-1. Shapes: 0 circle, 1 cone (forward = +v), 2 line, 3 ring.
// Fill grows from the center to the border (_Progress 0..1); a line fills from its start along its length.
Shader "Eclipse/TelegraphFill"
{
    Properties
    {
        _FillColor ("Fill Color", Color) = (0.9, 0.1, 0.1, 0.55)
        _BorderColor ("Border Color", Color) = (1, 0.2, 0.2, 1)
        _Progress ("Progress", Range(0, 1)) = 0
        _Shape ("Shape", Float) = 0
        _HalfArcCos ("Half Arc Cos", Range(-1, 1)) = 0
        _InnerRatio ("Inner Ratio", Range(0, 1)) = 0
        _Dashed ("Dashed Border", Float) = 0
        _Pulse ("Pulse Border", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+100" }

        Pass
        {
            Name "Telegraph"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FillColor;
                half4 _BorderColor;
                half _Progress;
                half _Shape;
                half _HalfArcCos;
                half _InnerRatio;
                half _Dashed;
                half _Pulse;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float edge = 0.04;
                float inside = 1.0;
                float fillMask = 0.0;
                float border = 0.0;

                if (_Shape > 1.5 && _Shape < 2.5)
                {
                    // line: u across the width, v along the length from the start.
                    float along = i.uv.y;
                    float across = abs(p.x);
                    fillMask = step(along, _Progress);
                    border = step(1.0 - edge * 2.0, across) + step(along, edge * 0.5) + step(1.0 - edge * 0.5, along);
                }
                else if (_Shape > 2.5)
                {
                    // ring: between inner and outer radius, filling from inner to outer.
                    float d = length(p);
                    inside = step(d, 1.0) * step(_InnerRatio, d);
                    float reach = _InnerRatio + (1.0 - _InnerRatio) * _Progress;
                    fillMask = inside * step(d, reach);
                    border = inside * (step(1.0 - edge, d) + step(d, _InnerRatio + edge));
                }
                else
                {
                    // circle (0) and cone (1): radial fill from the center.
                    float d = length(p);
                    inside = step(d, 1.0);
                    if (_Shape > 0.5)
                    {
                        float c = d > 1e-4 ? p.y / d : 1.0;
                        inside *= step(_HalfArcCos, c);
                    }
                    fillMask = inside * step(d, _Progress);
                    border = inside * step(1.0 - edge, d);
                }

                border = saturate(border) * inside;
                if (_Dashed > 0.5)
                {
                    float angle = atan2(p.y, p.x);
                    border *= step(0.0, sin(angle * 18.0 + _Time.y * 3.0));
                }
                float pulse = _Pulse > 0.5 ? (0.6 + 0.4 * sin(_Time.y * 8.0)) : 1.0;

                half4 baseCol = half4(_FillColor.rgb, _FillColor.a * 0.25);
                half4 col = lerp(baseCol, _FillColor, fillMask);
                col = lerp(col, half4(_BorderColor.rgb, _BorderColor.a * pulse), border);
                col.a *= inside;
                clip(col.a - 0.001);
                return col;
            }
            ENDHLSL
        }
    }
}
