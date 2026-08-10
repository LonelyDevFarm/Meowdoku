Shader "Meowdoku/UI/Home Flow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Flow Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ScrollSpeed ("Scroll Speed", Vector) = (0.015,-0.015,0,0)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ScrollSpeed;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 flowUv = frac(input.uv + _ScrollSpeed.xy * _Time.y);
                fixed4 flow = tex2D(_MainTex, flowUv);
                fixed4 mask = tex2D(_MaskTex, saturate(input.uv));
                fixed maskValue = mask.a * max(mask.r, max(mask.g, mask.b));
                fixed alpha = saturate(flow.a - min(maskValue, flow.a)) *
                              input.color.a;
                clip(alpha - 0.00025);
                fixed3 rgb = flow.rgb * input.color.rgb * alpha;
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
