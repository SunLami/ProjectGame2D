Shader "ProjectGame2D/UI/SkillIconSocket"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Shape ("Shape (0 diamond, 1 hexagon)", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color;
            float _Shape;
            float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p = abs(i.uv * 2.0 - 1.0);
                float shape = _Shape < .5 ? p.x + p.y : max(p.y, p.x + .5 * p.y);
                float silhouette = 1.0 - smoothstep(.98, 1.0, shape);
                // Source V2 provides palette; foreground art is an independent transparent Image.
                fixed3 corners = (tex2D(_MainTex, float2(.04,.04)).rgb +
                    tex2D(_MainTex, float2(.96,.04)).rgb +
                    tex2D(_MainTex, float2(.04,.96)).rgb +
                    tex2D(_MainTex, float2(.96,.96)).rgb) * .25;
                float radius = length(i.uv - .5) * 2.0;
                float rings = pow(saturate(.5 + .5 * cos(radius * 25.0)), 12.0) * .0012;
                fixed3 hue = corners / max(max(corners.r, max(corners.g, corners.b)), .001);
                fixed3 backdrop = corners * 2.0 + hue * (.002 + .004 * saturate(1.0-radius) + rings);
                fixed4 c = fixed4(backdrop, silhouette) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - .001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
