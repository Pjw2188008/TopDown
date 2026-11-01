Shader "Story/StealthNoise"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _StealthOpacity ("Visible Opacity", Range(0,1)) = .22
        _NoiseStrength ("Noise Strength", Range(0,1)) = .35
        _NoisePixelSize ("Noise Pixel Size", Float) = 3
        _NoiseRate ("Noise Rate", Float) = 12
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment StealthFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _StealthOpacity, _NoiseStrength, _NoisePixelSize, _NoiseRate;
            fixed4 StealthFrag(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                float2 cell = floor(input.vertex.xy / max(1.0, _NoisePixelSize));
                float tick = floor(_Time.y * _NoiseRate);
                float noise = frac(sin(dot(cell, float2(12.9898,78.233)) + tick*37.719)*43758.5453);
                color.rgb = lerp(color.rgb, float3(noise,noise,noise), _NoiseStrength*.6);
                color.a *= _StealthOpacity * lerp(1.0, .5+noise*.5, _NoiseStrength);
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
