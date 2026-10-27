Shader "TDOM/LavaBurbuja"
{
    // Burbuja de lava hirviendo del origen de la Mano de AREK (TW-180). Quad que mira a la
    // cámara, sin texturas: superficie vino oscuro con grietas calientes y tres capas de
    // burbujas de distintos tamaños que nacen pequeñas, suben mientras crecen y revientan en un
    // anillo brillante; al reventar la celda se renueva, así que el borboteo es continuo.
    // _Progreso (0..1) lo manda BurbujaLava con un MaterialPropertyBlock y controla la opacidad.
    Properties
    {
        [HDR] _ColorProfundo("Color profundo (vino)", Color) = (0.24, 0.01, 0.05, 1)
        [HDR] _ColorBrillo("Color brillo (rojo calido)", Color) = (1, 0.17, 0.04, 1)
        [HDR] _ColorNucleo("Color nucleo (naranja rojizo)", Color) = (1, 0.4, 0.08, 1)
        _Intensidad("Intensidad", Float) = 2.2
        _Progreso("Progreso", Range(0, 1)) = 1
        _Densidad("Densidad de burbujas (celdas por lado)", Float) = 5
        _Tamano("Tamano maximo de burbuja", Range(0.1, 0.5)) = 0.38
        _Velocidad("Velocidad de ascenso", Float) = 0.55
        _Hervor("Hervor de la superficie", Range(0, 1)) = 0.6
        _FrecuenciaPulso("Frecuencia del pulso", Float) = 5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorProfundo;
                float4 _ColorBrillo;
                float4 _ColorNucleo;
                float _Intensidad;
                float _Progreso;
                float _Densidad;
                float _Tamano;
                float _Velocidad;
                float _Hervor;
                float _FrecuenciaPulso;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Quad billboard: el centro es el origen del objeto y los ejes salen de la cámara.
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 centroWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 derecha = UNITY_MATRIX_V[0].xyz;
                float3 arriba = UNITY_MATRIX_V[1].xyz;
                float escalaX = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float escalaY = length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));
                float3 posicionWS = centroWS
                    + derecha * (input.positionOS.x * escalaX)
                    + arriba * (input.positionOS.y * escalaY);
                output.positionCS = TransformWorldToHClip(posicionWS);
                output.uv = input.uv;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float Ruido(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Una capa de burbujas. Devuelve (cuerpo, borde, destello del reventón). Cada celda
            // tiene su burbuja: nace pequeña abajo, sube mientras crece y al final revienta en un
            // anillo que se abre y se apaga; después vuelve a nacer con otro tamaño y ritmo.
            float3 CapaBurbujas(float2 uv, float densidad, float velocidad, float semilla)
            {
                float2 p = uv * densidad;
                float2 celda = floor(p);
                float2 enCelda = frac(p);
                float3 acumulado = float3(0.0, 0.0, 0.0);

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 vecina = float2(x, y);
                        float2 azar = Hash22(celda + vecina + semilla);
                        float2 azar2 = Hash22(celda + vecina + semilla + 17.3);

                        float vida = frac(_Time.y * velocidad * (0.6 + 0.8 * azar.x) + azar.y);
                        float crece = smoothstep(0.0, 0.85, vida);
                        float revienta = smoothstep(0.85, 1.0, vida);

                        float radioMaximo = _Tamano * (0.35 + 0.65 * azar2.x);
                        float radio = radioMaximo * (0.15 + 0.85 * crece) * (1.0 + 0.5 * revienta);

                        float2 centro = vecina + float2(0.2 + 0.6 * azar.x, lerp(0.15, 0.85, vida));
                        centro.x += 0.06 * sin(_Time.y * 3.0 + azar2.y * 6.2831);
                        float distancia = length(enCelda - centro);

                        float cuerpo = (1.0 - smoothstep(radio * 0.8, radio, distancia)) * (1.0 - revienta);
                        float borde = (1.0 - smoothstep(0.0, 0.05 + 0.04 * revienta, abs(distancia - radio))) * (1.0 - 0.6 * revienta);
                        float destello = (1.0 - smoothstep(0.0, 0.08, abs(distancia - radio))) * revienta * (1.0 - revienta) * 4.0;

                        acumulado += float3(cuerpo, borde, destello);
                    }
                }

                return acumulado;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float tiempo = _Time.y;
                float radio = length(uv - 0.5) * 2.0;

                // Superficie que hierve: ruido deformado que sube despacio, con grietas calientes.
                float2 q = uv * 3.0 + float2(0.0, -tiempo * 0.35);
                float deformacion = Ruido(q * 2.0 + tiempo * 0.4);
                float ondas = Ruido(q + deformacion * _Hervor * 2.0);
                float grietas = pow(saturate(1.0 - abs(2.0 * ondas - 1.0)), 4.0);

                float3 burbujas = CapaBurbujas(uv, _Densidad, _Velocidad, 0.0);
                burbujas += CapaBurbujas(uv, _Densidad * 1.8, _Velocidad * 1.3, 31.7) * 0.8;
                burbujas += CapaBurbujas(uv, _Densidad * 3.2, _Velocidad * 1.7, 57.1) * 0.6;

                // Silueta irregular y núcleo más caliente en el centro.
                float mascara = 1.0 - smoothstep(0.62 + 0.18 * ondas, 1.0, radio);
                float nucleo = 1.0 - smoothstep(0.0, 0.75, radio);
                float pulso = 0.85 + 0.15 * sin(tiempo * _FrecuenciaPulso);

                float calor = saturate(
                    0.25 + nucleo * 0.45 + ondas * 0.2 + grietas * 0.35 * _Hervor
                    + burbujas.y * 0.8 + burbujas.z + burbujas.x * 0.15);

                float3 color = lerp(_ColorProfundo.rgb, _ColorBrillo.rgb, saturate(calor * 1.1));
                color = lerp(color, _ColorNucleo.rgb, saturate((calor - 0.75) * 3.0));
                color *= _Intensidad * pulso;

                float alfa = saturate(mascara * (0.55 + 0.45 * calor)) * _Progreso;
                return float4(color, alfa);
            }
            ENDHLSL
        }
    }
}
