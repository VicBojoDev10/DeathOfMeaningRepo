// Custom Function del Shader Graph AvisoAtaque (TW-115). Devuelve el factor de brillo (0..1)
// del área de aviso: pulso que se acelera con el progreso, fijo al máximo en el tramo final,
// y borde más brillante que el centro. AvisoParpadeo.cs llena las propiedades por
// MaterialPropertyBlock.
//
// Forma: 0 = disco (radial desde el centro de las UV), 1 = franja con bordes en U,
//        2 = franja con bordes en V.

#ifndef TDOM_AVISO_ATAQUE_INCLUDED
#define TDOM_AVISO_ATAQUE_INCLUDED

float TDOM_AvisoFactor(
    float2 UV,
    float Progreso,
    float Duracion,
    float FrecuenciaInicial,
    float FrecuenciaFinal,
    float InicioFijo,
    float BrilloMinimo,
    float AnchoBorde,
    float BrilloCentro,
    float Forma)
{
    float p = saturate(Progreso);

    // Ciclos acumulados con frecuencia = lerp(inicial, final, p): integral de f(t) dt con
    // p = t / Duracion. Así el pulso se acelera sin saltos (con sin(2π·f(p)·t) daría tirones).
    float ciclos = Duracion * (FrecuenciaInicial * p + 0.5 * (FrecuenciaFinal - FrecuenciaInicial) * p * p);
    float pulso = 0.5 + 0.5 * cos(6.28318530718 * ciclos);
    pulso = lerp(BrilloMinimo, 1.0, pulso);

    // Últimos ~0.2 s: encendido fijo al máximo, señal de "ya".
    pulso = p >= InicioFijo ? 1.0 : pulso;

    // Distancia al borde: 0 en el borde, 1 en el centro.
    float distancia;
    if (Forma < 0.5)
        distancia = 1.0 - 2.0 * length(UV - 0.5);
    else if (Forma < 1.5)
        distancia = 2.0 * min(UV.x, 1.0 - UV.x);
    else
        distancia = 2.0 * min(UV.y, 1.0 - UV.y);

    float borde = 1.0 - smoothstep(0.0, AnchoBorde * 2.0, saturate(distancia));
    float espacial = lerp(BrilloCentro, 1.0, borde);

    return pulso * espacial;
}

void AvisoAtaque_float(
    float2 UV,
    float Progreso,
    float Duracion,
    float FrecuenciaInicial,
    float FrecuenciaFinal,
    float InicioFijo,
    float BrilloMinimo,
    float AnchoBorde,
    float BrilloCentro,
    float Forma,
    out float Factor)
{
    Factor = TDOM_AvisoFactor(UV, Progreso, Duracion, FrecuenciaInicial, FrecuenciaFinal,
        InicioFijo, BrilloMinimo, AnchoBorde, BrilloCentro, Forma);
}

void AvisoAtaque_half(
    half2 UV,
    half Progreso,
    half Duracion,
    half FrecuenciaInicial,
    half FrecuenciaFinal,
    half InicioFijo,
    half BrilloMinimo,
    half AnchoBorde,
    half BrilloCentro,
    half Forma,
    out half Factor)
{
    Factor = (half)TDOM_AvisoFactor(UV, Progreso, Duracion, FrecuenciaInicial, FrecuenciaFinal,
        InicioFijo, BrilloMinimo, AnchoBorde, BrilloCentro, Forma);
}

#endif
