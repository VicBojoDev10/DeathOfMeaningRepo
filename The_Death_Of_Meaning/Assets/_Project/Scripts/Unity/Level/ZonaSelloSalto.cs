using TDOM.Unity.Player;
using UnityEngine;

namespace TDOM.Unity.Level
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ZonaSelloSalto : MonoBehaviour
    {
        private enum AccionSello
        {
            Sellar,
            Romper,
        }

        [SerializeField]
        private CharacterIds _personaje = CharacterIds.Ayla;

        [SerializeField]
        private AccionSello _accion;

        private void Awake() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            PlayerRoot jugador = other.GetComponentInParent<PlayerRoot>();
            if (jugador == null || !jugador.IsOwner || jugador.CharacterId != _personaje)
                return;
            if (!jugador.TryGetComponent(out SelloSaltoJugador sello))
                return;

            if (_accion == AccionSello.Sellar)
                sello.Sellar();
            else
                sello.Romper();
        }
    }
}
