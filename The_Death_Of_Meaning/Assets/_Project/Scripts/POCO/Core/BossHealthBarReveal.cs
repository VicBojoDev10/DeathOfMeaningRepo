using System;

namespace TDOM.Gameplay.Core
{
    public sealed class BossHealthBarReveal
    {
        private bool _jefePresente;
        private bool _hudVisible;

        public BossHealthBarReveal(bool hudVisibleInicial) => _hudVisible = hudVisibleInicial;

        public bool Revelada { get; private set; }

        public event Action Revelar;
        public event Action Ocultar;

        public void RegistrarJefe()
        {
            _jefePresente = true;
            IntentarRevelar();
        }

        public void RetirarJefe()
        {
            _jefePresente = false;
            if (!Revelada)
                return;

            Revelada = false;
            Ocultar?.Invoke();
        }

        public void CambiarVisibilidadHud(bool visible)
        {
            _hudVisible = visible;
            IntentarRevelar();
        }

        private void IntentarRevelar()
        {
            if (Revelada || !_jefePresente || !_hudVisible)
                return;

            Revelada = true;
            Revelar?.Invoke();
        }
    }
}
