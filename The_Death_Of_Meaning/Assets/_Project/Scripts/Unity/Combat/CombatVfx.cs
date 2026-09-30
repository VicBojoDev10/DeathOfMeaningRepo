using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class CombatVfx : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem _golpe; // burst corto frente a HitboxOrigin

        [SerializeField]
        private ParticleSystem _carga; // loop mientras se sostiene el cargado

        [SerializeField]
        private ParticleSystem _cargadoSale; // burst grande al soltar

        [SerializeField]
        private ParticleSystem _disparo; // fogonazo en el origen del disparo (solo Zendre)

        [SerializeField]
        private Color[] _colorPorIndice = new Color[]
        {
            Color.white,
            Color.yellow,
            new Color(1f, 0.5f, 0f), // naranja
        };

        public void Golpe(int comboIndex, bool cargado, float carga)
        {
            if (cargado)
            {
                if (_cargadoSale != null)
                {
                    float escala = Mathf.Lerp(1f, 2.5f, carga);
                    _cargadoSale.transform.localScale = Vector3.one * escala;
                    _cargadoSale.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    _cargadoSale.Play();
                }
            }
            else
            {
                if (_golpe != null)
                {
                    Color color = Color.white;
                    if (_colorPorIndice != null && comboIndex >= 0 && comboIndex < _colorPorIndice.Length)
                    {
                        color = _colorPorIndice[comboIndex];
                    }

                    var main = _golpe.main;
                    main.startColor = color;
                    _golpe.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    _golpe.Play();
                }
            }
        }

        public void Carga(bool activa)
        {
            if (_carga == null)
                return;

            if (activa)
            {
                if (!_carga.isPlaying)
                    _carga.Play();
            }
            else
            {
                _carga.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public void Disparo(bool cargado)
        {
            if (_disparo == null)
                return;

            float escala = cargado ? 1.5f : 1.0f;
            _disparo.transform.localScale = Vector3.one * escala;
            _disparo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _disparo.Play();
        }
    }
}
