using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class EmpujeColliderInteriorBoss : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float _distanciaEmpuje = 3f;

        [SerializeField, Min(0f)]
        private float _duracionEmpuje = 0.25f;

        private void Reset()
        {
            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider otro)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                return;

            PlayerRoot jugador = otro.GetComponentInParent<PlayerRoot>();
            if (jugador == null || jugador.EstaAbatidoOMuerto)
                return;

            Vector3 direccion = jugador.transform.position - transform.position;
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 0.0001f)
            {
                direccion = jugador.transform.position - transform.root.position;
                direccion.y = 0f;
            }

            jugador.EmpujarDesdeServidor(direccion, _distanciaEmpuje, 0f, _duracionEmpuje);
        }
    }
}
