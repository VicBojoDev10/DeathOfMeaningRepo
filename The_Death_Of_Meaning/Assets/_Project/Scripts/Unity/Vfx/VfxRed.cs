using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public sealed class VfxRed : NetworkBehaviour
    {
        [SerializeField]
        private VfxPool _pool;

        public VfxPool Pool => _pool;

        private void Awake()
        {
            if (_pool == null)
                _pool = GetComponent<VfxPool>();
        }

        public void Lanzar(VfxBase efecto, Vector3 posicion, Quaternion rotacion) =>
            Lanzar(efecto, posicion, rotacion, Vector3.zero);

        public void Lanzar(VfxBase efecto, Vector3 posicion, Quaternion rotacion, Vector3 velocidad)
        {
            int indice = IndiceDe(efecto);
            if (indice < 0)
                return;

            if (IsServer)
                ReproducirRpc(indice, posicion, rotacion, velocidad);
            else if (IsOwner)
                PedirReproducirRpc(indice, posicion, rotacion, velocidad);
        }

        public void LanzarEntre(VfxBase efecto, Vector3 a, Vector3 b)
        {
            int indice = IndiceDe(efecto);
            if (indice < 0)
                return;

            if (IsServer)
                ReproducirEntreRpc(indice, a, b);
            else if (IsOwner)
                PedirReproducirEntreRpc(indice, a, b);
        }

        public void Detener(VfxBase efecto)
        {
            int indice = IndiceDe(efecto);
            if (indice < 0)
                return;

            if (IsServer)
                DetenerRpc(indice);
            else if (IsOwner)
                PedirDetenerRpc(indice);
        }

        private int IndiceDe(VfxBase efecto) => _pool != null ? _pool.Indice(efecto) : -1;

        [Rpc(SendTo.Server)]
        private void PedirReproducirRpc(
            int indice,
            Vector3 posicion,
            Quaternion rotacion,
            Vector3 velocidad,
            RpcParams rpcParams = default
        )
        {
            if (rpcParams.Receive.SenderClientId == OwnerClientId)
                ReproducirRpc(indice, posicion, rotacion, velocidad);
        }

        [Rpc(SendTo.Server)]
        private void PedirReproducirEntreRpc(
            int indice,
            Vector3 a,
            Vector3 b,
            RpcParams rpcParams = default
        )
        {
            if (rpcParams.Receive.SenderClientId == OwnerClientId)
                ReproducirEntreRpc(indice, a, b);
        }

        [Rpc(SendTo.Server)]
        private void PedirDetenerRpc(int indice, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId == OwnerClientId)
                DetenerRpc(indice);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReproducirRpc(
            int indice,
            Vector3 posicion,
            Quaternion rotacion,
            Vector3 velocidad
        )
        {
            var instancia = _pool != null ? _pool.Pedir(indice) : null;
            if (instancia != null)
                instancia.Reproducir(posicion, rotacion, velocidad);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReproducirEntreRpc(int indice, Vector3 a, Vector3 b)
        {
            var instancia = _pool != null ? _pool.Pedir(indice) : null;
            if (instancia != null)
                instancia.ReproducirEntre(a, b);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void DetenerRpc(int indice)
        {
            if (_pool != null)
                _pool.Detener(indice);
        }
    }
}
