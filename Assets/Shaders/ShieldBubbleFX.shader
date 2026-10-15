Shader "ProjectGame2D/ShieldBubbleFX"
{
    // Procedural glass-bubble rim (fresnel), not a pre-baked sprite animation (D-118 Shield VFX).
    // A flat quad fakes a hemisphere's surface normal from UV distance-from-center, then a real
    // Fresnel term (normal vs. a fixed top-down view direction) makes the CENTER transparent and
    // the RIM glow -- the opposite of a flat radial gradient, and the actual trick behind "Shader
    // Graph bubble shield" tutorials. _HalfSide selects which half of the circle this instance
    // draws, so one material drives ShieldFXBack (top half, behind Player) and another drives
    // ShieldFXFront (bottom half, in front of Player) without needing two different shaders.
    Properties
    {
        _RimColor ("Rim Colour", Color) = (1, 0.98, 0.9, 1)
        _TintColor ("Inner Tint", Color) = (0.6, 0.6, 0.47, 1)
        _InnerAlpha ("Inner Haze Alpha", Range(0, 0.6)) = 0.12
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.2
        _RimIntensity ("Rim Intensity", Range(0, 3)) = 1.6
        _HighlightPos ("Highlight Pos (UV, xy)", Vector) = (0.32, 0.72, 0, 0)
        _HighlightSize ("Highlight Size", Range(0.05, 0.6)) = 0.22
        _HighlightIntensity ("Highlight Intensity", Range(0, 2)) = 0.8
        _ShimmerSpeed ("Shimmer Speed", Range(0, 5)) = 1.4
        _ShimmerAmount ("Shimmer Amount", Range(0, 0.5)) = 0.12
        _HalfSide ("Half Side (0=top/back, 1=bottom/front)", Range(0, 1)) = 0
        _EdgeFeather ("Edge Feather", Range(0.001, 0.2)) = 0.03
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            // Required: this project's URP uses the 2D Renderer (Renderer2DData), which only
            // renders passes tagged "Universal2D" (or "UniversalForward" for its unlit fallback) --
            // a Pass with no LightMode tag is silently skipped, which is why this rendered nothing
            // until this tag was added.
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _RimColor;
                float4 _TintColor;
                float _InnerAlpha;
                float _RimPower;
                float _RimIntensity;
                float4 _HighlightPos;
                float _HighlightSize;
                float _HighlightIntensity;
                float _ShimmerSpeed;
                float _ShimmerAmount;
                float _HalfSide;
                float _EdgeFeather;
            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 centered = (IN.uv - 0.5) * 2.0; // -1..1 across the inscribed circle
                float dist = length(centered);

                // Fake hemisphere normal: nz = 1 at the centre (facing the camera straight on),
                // falling to 0 at the silhouette edge (grazing angle) -- same shape a real dome's
                // surface normal would have from a top-down camera.
                float nz = sqrt(saturate(1.0 - dot(centered, centered)));
                float fresnel = pow(saturate(1.0 - nz), _RimPower) * _RimIntensity;

                // Subtle shimmer travelling around the rim so it doesn't read as a flat static ring.
                float angle = atan2(centered.y, centered.x);
                fresnel *= 1.0 + _ShimmerAmount * sin(angle * 6.0 + _Time.y * _ShimmerSpeed);

                // Soft highlight blob (the "ivory/olive reflection sweeping over upper-left").
                float highlightDist = length(IN.uv - _HighlightPos.xy) / _HighlightSize;
                float highlight = saturate(1.0 - highlightDist) * _HighlightIntensity;

                float alpha = saturate(fresnel + highlight + _InnerAlpha * (1.0 - saturate(dist)));

                // Hard-edge circle clip with a short antialiased feather, and the back/front half-split.
                float circleMask = 1.0 - smoothstep(1.0 - _EdgeFeather, 1.0, dist);
                float halfMask = _HalfSide < 0.5 ? step(0.0, -centered.y) : step(0.0, centered.y);
                alpha *= circleMask * halfMask;

                half3 color = lerp(_TintColor.rgb, _RimColor.rgb, saturate(fresnel + highlight));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
