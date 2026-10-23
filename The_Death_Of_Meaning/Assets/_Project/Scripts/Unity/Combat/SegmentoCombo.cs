using System;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Un golpe del combo = un tramo de un clip del Animator. Varios golpes pueden compartir
    /// clip (por ejemplo, las dos partes del ataque 2 o los tres golpes del combo final). Las
    /// posiciones son normalizadas sobre el clip (0 = inicio, 1 = fin).
    /// </summary>
    [Serializable]
    public sealed class SegmentoCombo
    {
        [Tooltip("Solo para identificarlo en el Inspector.")]
        public string Nombre = "Golpe";

        [Tooltip("Estado del Animator que reproduce el golpe (Attack, 1i o 2IC).")]
        public string Estado = "Attack";

        [Range(0f, 1f), Tooltip("Punto del clip donde empieza el golpe.")]
        public float Inicio = 0f;

        [Range(0f, 1f), Tooltip("Punto del clip donde termina el golpe y se vuelve a locomocion.")]
        public float Fin = 0.8f;

        [Range(0f, 1f)]
        [Tooltip(
            "Punto del clip (no del tramo) desde el que puede entrar el siguiente golpe. "
                + "Debe estar entre Inicio y Fin."
        )]
        public float PuntoCombo = 0.4f;

        [Min(0f), Tooltip("Segundos de mezcla al entrar al golpe desde la pose anterior.")]
        public float MezclaEntrada = 0.08f;

        [Min(0f), Tooltip("Segundos de mezcla al terminar el golpe hacia locomocion.")]
        public float MezclaSalida = 0.15f;

        [Min(0.05f)]
        [Tooltip(
            "Duracion en segundos del clip completo en el Animator (clip / velocidad del estado). "
                + "Solo convierte las mezclas de segundos a tiempo normalizado."
        )]
        public float DuracionClip = 0.7f;

        public SegmentoCombo() { }

        public SegmentoCombo(
            string nombre,
            string estado,
            float inicio,
            float fin,
            float puntoCombo,
            float mezclaEntrada,
            float mezclaSalida,
            float duracionClip
        )
        {
            Nombre = nombre;
            Estado = estado;
            Inicio = inicio;
            Fin = fin;
            PuntoCombo = puntoCombo;
            MezclaEntrada = mezclaEntrada;
            MezclaSalida = mezclaSalida;
            DuracionClip = duracionClip;
        }

        /// <summary>Inicio y fin ordenados para que cualquier valor del Inspector sea valido.</summary>
        public float InicioSeguro => Mathf.Clamp01(Inicio);

        public float FinSeguro => Mathf.Clamp(Fin, InicioSeguro + 0.02f, 1f);

        public float PuntoComboSeguro => Mathf.Clamp(PuntoCombo, InicioSeguro, FinSeguro);

        /// <summary>
        /// Combo de Ayla: basico, ataque 2 en dos partes y combo final en tres golpes. Los
        /// cortes salen del analisis de velocidad de la mano en cada clip.
        /// </summary>
        public static SegmentoCombo[] PorDefectoAyla()
        {
            return new[]
            {
                new SegmentoCombo("1 Basico", "Attack", 0f, 0.80f, 0.30f, 0.08f, 0.15f, 0.70f),
                new SegmentoCombo(
                    "2 Corte (1i, 1a parte)",
                    "1i",
                    0f,
                    0.55f,
                    0.30f,
                    0.08f,
                    0.12f,
                    0.86f
                ),
                new SegmentoCombo(
                    "3 Corte (1i, 2a parte)",
                    "1i",
                    0.50f,
                    0.85f,
                    0.60f,
                    0.08f,
                    0.15f,
                    0.86f
                ),
                new SegmentoCombo("4 Final 1/3", "2IC", 0f, 0.28f, 0.12f, 0.08f, 0.10f, 2.73f),
                new SegmentoCombo("5 Final 2/3", "2IC", 0.28f, 0.50f, 0.36f, 0.06f, 0.10f, 2.73f),
                new SegmentoCombo("6 Final 3/3", "2IC", 0.62f, 0.92f, 0.75f, 0.08f, 0.15f, 2.73f),
            };
        }
    }

    /// <summary>Que animacion sale de un ataque cargado segun cuantos golpes basicos lo preceden.</summary>
    public enum SalidaCargada
    {
        /// <summary>Clip de ataque cargado (estado Charged).</summary>
        Estandar,

        /// <summary>El golpe basico espejeado, que arranca con un pequeño retraso (ab => ac).</summary>
        EspejoBasico,

        /// <summary>Secuencia de giros (ab + ab + ac).</summary>
        Giros,
    }

    [Serializable]
    public struct ConfigSalidaCargada
    {
        [Min(0), Tooltip("Golpes basicos del combo antes de soltar el cargado.")]
        public int LigerosPrevios;

        public SalidaCargada Tipo;

        [Min(0f), Tooltip("Segundos que tarda en arrancar la animacion (y el golpe) tras soltar.")]
        public float Retardo;
    }
}
