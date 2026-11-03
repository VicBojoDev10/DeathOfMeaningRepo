using System;
using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "CargaEspadaProfile", menuName = "TDOM/Vfx/Carga Espada Profile")]
    public sealed class CargaEspadaProfile : ScriptableObject
    {
        [Serializable]
        public struct Etapa
        {
            [Range(0f, 1f), Tooltip("Progreso de la carga (0 = empieza, 1 = carga completa).")]
            public float desde;

            [ColorUsage(true, true)]
            public Color color;

            [Min(0f)]
            public float intensidad;
        }

        [Header("Silueta en la espada")]
        public Etapa[] etapas =
        {
            new Etapa
            {
                desde = 0f,
                color = new Color(1f, 0.9f, 0.55f),
                intensidad = 2f,
            },
            new Etapa
            {
                desde = 0.5f,
                color = new Color(1f, 0.6f, 0.5f),
                intensidad = 2f,
            },
            new Etapa
            {
                desde = 1f,
                color = new Color(0.8f, 0.6f, 1f),
                intensidad = 2f,
            },
        };

        [Header("Partículas del cuerpo")]
        [Tooltip("Multiplicador de la emisión según el progreso de la carga (0 a 1).")]
        public AnimationCurve emisionCuerpo = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);
    }
}
