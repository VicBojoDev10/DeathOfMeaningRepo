using TDOM.Unity;
using UnityEngine;

namespace TDOM.Unity.Level
{
    public sealed class ArenaDoorAnimator : MonoBehaviour
    {
        private Animator[] _animators;

        private void Awake() => _animators = GetComponentsInChildren<Animator>(true);

        private void OnEnable()
        {
            ArenaEncounterNetwork.PuertasSolicitadas += CambiarEstadoPuertas;
            EstablecerPoseCerrada();
        }

        private void OnDisable() => ArenaEncounterNetwork.PuertasSolicitadas -= CambiarEstadoPuertas;

        private void CambiarEstadoPuertas(bool abrir)
        {
            if (_animators == null) _animators = GetComponentsInChildren<Animator>(true);
            foreach (var animator in _animators)
            {
                string controlador = animator.runtimeAnimatorController != null
                    ? animator.runtimeAnimatorController.name : string.Empty;
                string estado = controlador == "Circle_001"
                    ? (abrir ? "RightDoorAnimation" : "RightDoorClosingAnimation")
                    : controlador == "Circle_002"
                        ? (abrir ? "LeftDoorOpening" : "LeftDoorClosingAnimation")
                        : null;
                if (estado != null) animator.Play(estado, 0, 0f);
            }
        }

        private void EstablecerPoseCerrada()
        {
            if (_animators == null) _animators = GetComponentsInChildren<Animator>(true);
            foreach (var animator in _animators)
            {
                string controlador = animator.runtimeAnimatorController != null
                    ? animator.runtimeAnimatorController.name : string.Empty;
                string estado = controlador == "Circle_001" ? "RightDoorClosingAnimation"
                    : controlador == "Circle_002" ? "LeftDoorClosingAnimation" : null;
                if (estado == null || !animator.isActiveAndEnabled) continue;
                animator.Play(estado, 0, 1f);
                animator.Update(0f);
            }
        }
    }
}
