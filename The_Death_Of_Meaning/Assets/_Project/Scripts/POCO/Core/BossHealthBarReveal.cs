using System;

namespace TDOM.Gameplay.Core
{
    public sealed class BossHealthBarReveal
    {
        private bool _jefePresente;
        private bool _hudVisible;

        public BossHealthBarReveal(bool hudVisibleInicial) => _hudVisible = hudVisibleInicial;

        public bool Visible { get; private set; }

        public event Action Mostrada;
        public event Action Ocultada;

        public void RegistrarJefe()
        {
            _jefePresente = true;
            IntentarRevelar();
        }

        public void RetirarJefe()
        {
            _jefePresente = false;
            if (!Visible)
                return;

            Visible = false;
            Ocultada?.Invoke();
        }

        public void CambiarVisibilidadHud(bool visible)
        {
            _hudVisible = visible;
            IntentarRevelar();
        }

        private void IntentarRevelar()
        {
            if (Visible || !_jefePresente || !_hudVisible)
                return;

            Visible = true;
            Mostrada?.Invoke();
        }
    }
}
