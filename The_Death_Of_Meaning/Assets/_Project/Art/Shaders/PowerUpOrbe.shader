Shader "TDOM/PowerUpOrbe"
{
    // Orbe de power-up (TW-276): color emisivo con borde más brillante. El color sale del
    // PowerUpProfile (PowerUpPickup lo manda con un MaterialPropertyBlock).
    Properties
    {
        [HDR] _Color("Color", Color) = (0.2, 1, 0.3, 1)
        _Intensidad("Intensidad", Float) = 2
        _PotenciaBorde("Potencia del borde", Float) = 2.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensidad;
                half _PotenciaBorde;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posicion = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posicion.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(posicion.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 vista = normalize(input.viewDirWS);
                half borde = pow(saturate(1.0 - dot(normal, vista)), _PotenciaBorde);
                half3 color = _Color.rgb * _Intensidad * (0.6 + borde);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
