using TDOM.Data;
using TDOM.Gameplay.Audio;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Unity.Audio
{
    public sealed class SfxLocomocion : MonoBehaviour
    {
        [SerializeField]
        private SfxRed _red;

        [SerializeField]
        private SfxBanco _dash;

        [SerializeField]
        private SfxBanco _dashAire;

        [SerializeField]
        private SfxBanco _gancho;

        [SerializeField]
        private SfxBanco _salto;

        [SerializeField]
        private SfxBanco _aterrizaje;

        [SerializeField]
        private SfxBanco _dobleSalto;

        [SerializeField]
        private SfxBanco _embestidaArranque;

        [SerializeField]
        private SfxBanco _embestidaAvance;

        [SerializeField]
        private SfxBanco _embestidaFinal;

        [Header("Pasos")]
        [SerializeField]
        private SfxBanco _pasosCaminar;

        [SerializeField]
        private SfxBanco _pasosCorrer;

        [SerializeField]
        private SfxBanco _capaCaminar;

        [SerializeField]
        private SfxBanco _capaCorrer;

        [SerializeField]
        private SfxBanco _pararCaminar;

        [SerializeField]
        private SfxBanco _pararCorrer;

        [SerializeField]
        private float _rapidezMinimaPasos = 0.5f;

        [SerializeField]
        private SfxBanco _abatido;

        private readonly DetectorFinEmbestida _finEmbestida = new DetectorFinEmbestida();
        private readonly DetectorMarchaPasos _marcha = new DetectorMarchaPasos();

        private void Awake()
        {
            if (_red == null)
                _red = GetComponentInParent<SfxRed>();
        }

        public void Dash(bool enSuelo) =>
            ReproducirSiExiste(enSuelo || _dashAire == null ? _dash : _dashAire);

        public void Gancho() => ReproducirSiExiste(_gancho);

        public void Salto() => ReproducirSiExiste(_salto);

        public void Aterrizaje() => ReproducirSiExiste(_aterrizaje);

        public void DobleSalto() => ReproducirSiExiste(_dobleSalto);

        public void EmbestidaInicio()
        {
            ReproducirSiExiste(_embestidaArranque);
            ReproducirSiExiste(_embestidaAvance);
        }

        public void ActualizarEmbestida(bool activa)
        {
            _finEmbestida.Actualizar(activa);
            if (!_finEmbestida.Termino)
                return;

            if (_red != null && _embestidaAvance != null)
                _red.Detener(_embestidaAvance);
            ReproducirSiExiste(_embestidaFinal);
        }

        public void ActualizarPasos(bool enSuelo, float rapidez, bool corriendo)
        {
            _marcha.Actualizar(enSuelo, rapidez, corriendo, _rapidezMinimaPasos);
            AplicarCambioDeMarcha();
        }

        public void Abatido()
        {
            _marcha.Reiniciar();
            AplicarCambioDeMarcha();
            ReproducirSiExiste(_abatido);
        }

        private void AplicarCambioDeMarcha()
        {
            if (!_marcha.Cambio)
                return;

            DetenerMarcha(_marcha.Anterior);
            IniciarMarcha(_marcha.Actual);
            if (_marcha.Freno)
                ReproducirSiExiste(
                    _marcha.Anterior == MarchaPasos.Corriendo ? _pararCorrer : _pararCaminar
                );
        }

        private void IniciarMarcha(MarchaPasos marcha)
        {
            switch (marcha)
            {
                case MarchaPasos.Caminando:
                    ReproducirSiExiste(_pasosCaminar);
                    ReproducirSiExiste(_capaCaminar);
                    break;
                case MarchaPasos.Corriendo:
                    ReproducirSiExiste(_pasosCorrer);
                    ReproducirSiExiste(_capaCorrer);
                    break;
            }
        }

        private void DetenerMarcha(MarchaPasos marcha)
        {
            switch (marcha)
            {
                case MarchaPasos.Caminando:
                    DetenerSiExiste(_pasosCaminar);
                    DetenerSiExiste(_capaCaminar);
                    break;
                case MarchaPasos.Corriendo:
                    DetenerSiExiste(_pasosCorrer);
                    DetenerSiExiste(_capaCorrer);
                    break;
            }
        }

        private void DetenerSiExiste(SfxBanco banco)
        {
            if (_red != null && banco != null)
                _red.Detener(banco);
        }

        private void ReproducirSiExiste(SfxBanco banco)
        {
            if (_red != null && banco != null)
                _red.ReproducirEnEmisor(banco);
        }
    }
}
