using UnityEngine;

namespace TDOM.Data
{
    /// <summary>
    /// Daño por tocar el cuerpo del jefe (TW-242). Valores iniciales, sujetos a balance.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ContactoJefeProfile",
        menuName = "TDOM/Boss/Contacto Jefe Profile"
    )]
    public sealed class ContactoJefeProfile : ScriptableObject
    {
        [Header("Daño")]
        [Min(0f)]
        [Tooltip("Vida que pierde el jugador por cada golpe de contacto.")]
        public float dano = 10f;

        [Min(0f)]
        [Tooltip("Vida que pierde Ayla por cada golpe de contacto (el resto usa Daño).")]
        public float danoAyla = 5f;

        [Min(0f)]
        [Tooltip(
            "Segundos entre golpes al mismo jugador. Cuenta aunque salga y vuelva a tocar al jefe."
        )]
        public float intervaloEntreGolpes = 1f;
    }
}
