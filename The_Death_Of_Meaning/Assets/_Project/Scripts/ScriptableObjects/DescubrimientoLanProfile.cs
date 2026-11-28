using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(
        fileName = "DescubrimientoLanProfile",
        menuName = "TDOM/Descubrimiento LAN Profile"
    )]
    public sealed class DescubrimientoLanProfile : ScriptableObject
    {
        [Range(1024, 65535)]
        public int PuertoDescubrimiento = 47777;

        [Min(0.1f)]
        public float IntervaloAnuncio = 1f;

        [Min(0.5f)]
        public float Caducidad = 3.5f;

        public string TextoBuscando = "Buscando partidas en tu red...";

        public string FormatoPartida = "Partida de {0}  ({1})";
    }
}
