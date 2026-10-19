using DG.Tweening;
using TDOM.Data;
using TDOM.Gameplay.Core;
using TDOM.Unity.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public sealed class BossHealthBar : MonoBehaviour
    {
        private static readonly Vector3 EscalaCerrada = new Vector3(0f, 1f, 1f);

        [SerializeField]
        private Slider _slider;

        [SerializeField]
        private CanvasGroup _grupoNombre;

        [SerializeField]
        private TMP_Text _textoNombre;

        [SerializeField]
        private BossHealthBarRevealProfile _perfil;

        private readonly BossHealthBarReveal _revelacion = new BossHealthBarReveal(true);
        private BossController _jefe;
        private Sequence _animacion;

        private void Awake()
        {
            _revelacion.Mostrada += AlMostrarseBarra;
            _revelacion.Ocultada += AlOcultarseBarra;
            AlOcultarseBarra();
        }

        private void OnEnable()
        {
            BossController.Spawned += AlSpawnearJefe;
        }

        private void OnDisable()
        {
            BossController.Spawned -= AlSpawnearJefe;
            DesuscribirJefe();
        }

        private void OnDestroy()
        {
            _revelacion.Mostrada -= AlMostrarseBarra;
            _revelacion.Ocultada -= AlOcultarseBarra;
            _animacion?.Kill();
        }

        public void CambiarVisibilidadHud(bool visible) =>
            _revelacion.CambiarVisibilidadHud(visible);

        private void AlSpawnearJefe(BossController jefe)
        {
            DesuscribirJefe();
            _jefe = jefe;
            _jefe.Vida.OnValueChanged += AlCambiarVida;
            ActualizarVida(_jefe.Vida.Value);
            MostrarNombre(_jefe.Nombre);
            _revelacion.RegistrarJefe();
        }

        private void DesuscribirJefe()
        {
            if (_jefe != null)
                _jefe.Vida.OnValueChanged -= AlCambiarVida;

            _jefe = null;
            _revelacion.RetirarJefe();
        }

        private void AlCambiarVida(float anterior, float actual) => ActualizarVida(actual);

        private void ActualizarVida(float vida)
        {
            if (_slider != null)
                _slider.value = vida;
        }

        private void MostrarNombre(string nombre)
        {
            if (_textoNombre != null)
                _textoNombre.text = nombre;
        }

        private void AlMostrarseBarra()
        {
            if (_slider == null || _perfil == null)
                return;

            _animacion?.Kill();
            var barra = _slider.transform;
            barra.localScale = EscalaCerrada;
            FijarAlphaNombre(0f);
            _slider.gameObject.SetActive(true);

            _animacion = DOTween
                .Sequence()
                .Append(barra.DOScaleX(1f, _perfil.DuracionBarrido).SetEase(_perfil.CurvaBarrido))
                .AppendInterval(_perfil.RetrasoNombre);

            if (_grupoNombre != null)
                _animacion.Append(DOTween.To(FijarAlphaNombre, 0f, 1f, _perfil.DuracionNombre));
        }

        private void AlOcultarseBarra()
        {
            _animacion?.Kill();
            FijarAlphaNombre(0f);
            if (_slider != null)
                _slider.gameObject.SetActive(false);
        }

        private void FijarAlphaNombre(float alpha)
        {
            if (_grupoNombre != null)
                _grupoNombre.alpha = alpha;
        }
    }
}
