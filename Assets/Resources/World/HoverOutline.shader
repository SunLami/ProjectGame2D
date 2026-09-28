Shader "ProjectGame2D/HoverOutline"
{
    // Draws a solid-color N-pixel outline around a sprite's alpha silhouette (Stardew-Valley-style
    // hover highlight) while leaving pixels inside the silhouette untouched. Same swap-in-a-material
    // pattern as ResourceNodeWhiteFlash.shader -- see HoverOutline.cs, which toggles a
    // SpriteRenderer.sharedMaterial between its original and this one on hover.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineThickness ("Outline Thickness (texels)", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlineThickness;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 source = tex2D(_MainTex, input.uv);
                if (source.a > 0.01)
                    return source; // inside the sprite -- render untouched, no tint.

                float2 texel = _MainTex_TexelSize.xy * _OutlineThickness;
                float neighborAlpha =
                    tex2D(_MainTex, input.uv + float2(texel.x, 0)).a +
                    tex2D(_MainTex, input.uv - float2(texel.x, 0)).a +
                    tex2D(_MainTex, input.uv + float2(0, texel.y)).a +
                    tex2D(_MainTex, input.uv - float2(0, texel.y)).a +
                    tex2D(_MainTex, input.uv + float2(texel.x, texel.y)).a +
                    tex2D(_MainTex, input.uv - float2(texel.x, texel.y)).a +
                    tex2D(_MainTex, input.uv + float2(texel.x, -texel.y)).a +
                    tex2D(_MainTex, input.uv + float2(-texel.x, texel.y)).a;

                if (neighborAlpha > 0.01)
                    return _OutlineColor;

                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
