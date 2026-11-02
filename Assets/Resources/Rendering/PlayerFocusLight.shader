Shader "Story/PlayerFocusLight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _DarknessColor ("Darkness Color", Color) = (.015,.02,.04,1)
        _Darkness ("Darkness", Range(0,1)) = .65
        _Center ("Player Viewport Position", Vector) = (.5,.5,0,0)
        _ViewSize ("World View Size", Vector) = (10,6,0,0)
        _Radii ("Inner and Outer Radius", Vector) = (1.1,4.5,0,0)
        _Viewport ("Camera Viewport", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _Center, _ViewSize, _Radii, _Viewport, _DarknessColor;
            float _Darkness;
            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Output input) : SV_Target
            {
                float2 uv = (input.uv - _Viewport.xy) / max(_Viewport.zw, float2(.00001,.00001));
                float inside = step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);
                float distanceFromPlayer = length((uv - _Center.xy) * _ViewSize.xy);
                float fade = smoothstep(_Radii.x, max(_Radii.y, _Radii.x + .01), distanceFromPlayer);
                return half4(_DarknessColor.rgb, fade * _Darkness * inside);
            }
            ENDCG
        }
    }
}
