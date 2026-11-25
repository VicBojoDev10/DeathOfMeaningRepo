using TDOM.Contracts;

namespace TDOM.Gameplay.Combat
{
    public static class ResolutorEnergiaDisparo
    {
        public static AttackEvent SegunEnergia(
            AttackEvent evento,
            float energia,
            float costoCargado,
            AttackStep[] pasosLigeros
        )
        {
            bool cargadoPagable = evento.Kind != AttackKind.Charged || energia >= costoCargado;
            bool hayPasoLigero =
                pasosLigeros != null
                && evento.ComboIndex >= 0
                && evento.ComboIndex < pasosLigeros.Length;
            if (cargadoPagable || !hayPasoLigero)
                return evento;

            return new AttackEvent(
                AttackKind.Light,
                evento.ComboIndex,
                0f,
                pasosLigeros[evento.ComboIndex].Damage
            );
        }
    }
}
