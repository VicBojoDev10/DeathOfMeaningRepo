using TDOM.Gameplay.Combat;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Párpado animado de un ojo del jefe (Parpado3DAnimations_flicker_Close). Lo maneja
    /// OjosArekVisual con el estado del ojo, que ya llega sincronizado a todas las máquinas, así
    /// que no necesita red propia. Mientras el ojo está abierto (se puede golpear) el párpado
    /// reproduce el loop de parpadeo y lleva el material de contorno que lo marca como
    /// interactuable. Al pasar a Cerrado (se quedó sin vida) se le quita el contorno y se
    /// reproduce la animación de cierre desde el inicio hasta el frame 17, donde se queda
    /// quieto; al empezar una fase nueva vuelve al loop. La primera llamada solo coloca el
    /// párpado en su pose, sin animar (spawn del jefe o cliente que entra tarde).
    /// </summary>
    public sealed class ParpadoOjo : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        [Header("Borde de interacción")]
        [Tooltip(
            "Renderer del párpado. Vacío = el primero activo que se encuentre bajo este objeto."
        )]
        [SerializeField]
        private Renderer _render;

        [Tooltip(
            "Material de contorno (AllIn13DShaderOutline, solo perímetro). Se agrega al párpado mientras el ojo está abierto y se quita al cerrarse. Vacío = sin borde."
        )]
        [SerializeField]
        private Material _materialBorde;

        private static readonly int CerrarId = Animator.StringToHash("Cerrar");
        private static readonly int AbrirId = Animator.StringToHash("Abrir");

        private bool _inicializado;
        private bool _abierto;
        private Material[] _sinBorde;
        private Material[] _conBorde;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            if (_render == null)
                _render = BuscarRenderer();

            PrepararMaterialesBorde();
        }

        public void Aplicar(EstadoOjo estado)
        {
            bool abierto = WeakPointStateMachine.AdmiteAncla(estado);
            if (_inicializado && abierto == _abierto)
                return;

            if (_animator != null)
            {
                // Abierto: el loop desde el principio. Cerrado: el clip de cierre (frames 0 a
                // 17) que se queda en su último frame; al colocar la pose inicial se salta
                // directo a ese frame.
                if (abierto)
                    _animator.Play(AbrirId, 0, 0f);
                else
                    _animator.Play(CerrarId, 0, _inicializado ? 0f : 1f);
            }

            PonerBorde(abierto);

            _inicializado = true;
            _abierto = abierto;
        }

        // El primer renderer activo bajo el párpado; los "Contorno..." desactivados que pueda
        // traer el prefab se saltan para no ponerle el borde a un renderer que no se dibuja.
        private Renderer BuscarRenderer()
        {
            Renderer respaldo = null;
            foreach (var render in GetComponentsInChildren<Renderer>(true))
            {
                if (render.enabled && !render.gameObject.name.StartsWith("Contorno"))
                    return render;

                respaldo ??= render;
            }
            return respaldo;
        }

        private void PrepararMaterialesBorde()
        {
            if (_materialBorde == null || _render == null)
                return;

            var originales = _render.sharedMaterials;
            var conBorde = new Material[originales.Length + 1];
            originales.CopyTo(conBorde, 0);
            conBorde[originales.Length] = _materialBorde;

            _sinBorde = originales;
            _conBorde = conBorde;
        }

        private void PonerBorde(bool visible)
        {
            if (_conBorde == null)
                return;

            _render.sharedMaterials = visible ? _conBorde : _sinBorde;
        }
    }
}
