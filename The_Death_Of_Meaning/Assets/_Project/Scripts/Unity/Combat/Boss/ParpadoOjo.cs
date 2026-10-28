using TDOM.Gameplay.Combat;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Párpado animado de un ojo del jefe (Parpado3DAnimations_flicker_Close). Lo maneja
    /// OjosArekVisual con el estado del ojo, que ya llega sincronizado a todas las máquinas, así
    /// que no necesita red propia. El ojo está abierto mientras se puede apuntar a él (Brillando y
    /// Abierto); al pasar a Enfriando o Cerrado el párpado parpadea y se cierra. La primera
    /// llamada solo coloca el párpado en su pose, sin animar (spawn del jefe o cliente que entra
    /// tarde).
    /// </summary>
    public sealed class ParpadoOjo : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        private static readonly int CerrarId = Animator.StringToHash("Cerrar");
        private static readonly int AbrirId = Animator.StringToHash("Abrir");

        private bool _inicializado;
        private bool _abierto;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        public void Aplicar(EstadoOjo estado)
        {
            if (_animator == null)
                return;

            bool abierto = WeakPointStateMachine.AdmiteAncla(estado);
            if (_inicializado && abierto == _abierto)
                return;

            // Cerrar corre hacia delante hasta su último frame; Abrir es el tramo final del mismo
            // clip a velocidad -1, así que arranca desde su final. Al colocar la pose inicial se
            // salta al frame donde ya está terminada.
            if (abierto)
                _animator.Play(AbrirId, 0, _inicializado ? 1f : 0f);
            else
                _animator.Play(CerrarId, 0, _inicializado ? 0f : 1f);

            _inicializado = true;
            _abierto = abierto;
        }
    }
}
