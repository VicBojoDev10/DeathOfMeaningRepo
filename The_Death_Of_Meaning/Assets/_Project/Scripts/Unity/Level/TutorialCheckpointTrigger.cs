using TDOM.Unity;
using TDOM.Unity.Player;
using UnityEngine;

namespace TDOM.Unity.Level
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TutorialCheckpointTrigger : MonoBehaviour
    {
        [SerializeField]
        private CharacterIds _personaje;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerRoot jugador = other.GetComponentInParent<PlayerRoot>();
            if (jugador == null || !jugador.IsOwner || jugador.CharacterId != _personaje)
                return;

            jugador.RegistrarCheckpointTutorial();
        }
    }
}
