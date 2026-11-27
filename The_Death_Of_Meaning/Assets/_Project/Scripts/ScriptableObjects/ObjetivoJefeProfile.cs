using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "ObjetivoJefeProfile", menuName = "TDOM/Objetivo Jefe Profile")]
    public sealed class ObjetivoJefeProfile : ScriptableObject
    {
        [TextArea]
        public string DestruirOjos = "Destruye los <color=#FF3B3B>ojos</color> de Jikotsu.";

        [TextArea]
        public string RomperEscudosAzules =
            "Rompe los escudos <color=#4DA6FF>azules</color> con <color=#4DA6FF>Zendre</color> y destruye los ojos.";

        [TextArea]
        public string RomperEscudosRojos =
            "Rompe los escudos <color=#FF4D4D>rojos</color> con <color=#FF4D4D>Ayla</color> y destruye los ojos.";

        [TextArea]
        public string RomperEscudosAmbos =
            "Rompe los escudos: <color=#4DA6FF>azules con Zendre</color> y <color=#FF4D4D>rojos con Ayla</color>.";

        [Min(0f)]
        public float DuracionFundido = 0.35f;
    }
}
