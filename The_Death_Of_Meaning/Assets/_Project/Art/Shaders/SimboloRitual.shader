Shader "TDOM/SimboloRitual"
{
    Properties
    {
        _MainTex ("Symbols", 2D) = "white" {}
        _Rect ("Symbol UV", Vector) = (0,0,1,1)
        _GeneralAlpha ("Opacity", Range(0,1)) = 1
        _Trazo ("Reveal", Range(0,1)) = 1
        _Intensity ("Intensity", Float) = 1
        [Toggle] _RedMask ("Use Red Channel Mask", Float) = 0
        [HDR] _Tint ("Rune Glow", Color) = (0.1,0.8,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Rect;
                float _GeneralAlpha;
                float _Trazo;
                float _Intensity;
                float _RedMask;
                float4 _Tint;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, _Rect.xy + input.uv * _Rect.zw).rgb;
                float alpha = smoothstep(0.5, 0.8, 1.0 - max(color.r, max(color.g, color.b)));
                alpha = lerp(alpha, color.r, _RedMask);
                alpha *= smoothstep(1.0 - _Trazo - 0.03, 1.0 - _Trazo, input.uv.y);
                return half4(_Tint.rgb * _Intensity, alpha * _GeneralAlpha);
            }
            ENDHLSL
        }
    }
}
