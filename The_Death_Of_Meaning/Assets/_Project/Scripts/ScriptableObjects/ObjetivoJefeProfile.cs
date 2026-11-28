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
            "Rompe los escudos <color=#4DA6FF>azules</color> y destruye los ojos.";

        [TextArea]
        public string RomperEscudosRojos =
            "Rompe los escudos <color=#FF4D4D>rojos</color> y destruye los ojos.";

        [TextArea]
        public string ApoyarAyla =
            "Apoya a <color=#FF4D4D>Ayla</color> mientras rompe los escudos rojos.";

        [TextArea]
        public string ApoyarZendre =
            "Apoya a <color=#4DA6FF>Zendre</color> mientras rompe los escudos azules.";

        [TextArea]
        public string EsperarCompanero = "Espera a {0}.";

        [TextArea]
        public string AlcanzarCompanero = "Alcanza a {0}.";

        public string NombreAyla = "<color=#FF4D4D>Ayla</color>";

        public string NombreZendre = "<color=#4DA6FF>Zendre</color>";

        [Min(0f)]
        public float DuracionFundido = 0.35f;
    }
}
