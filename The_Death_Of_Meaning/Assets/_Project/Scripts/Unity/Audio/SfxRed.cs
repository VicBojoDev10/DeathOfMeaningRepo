using TDOM.Data;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Audio
{
    public sealed class SfxRed : NetworkBehaviour
    {
        [SerializeField]
        private SfxBanco _sfxPrueba;

        private static bool _avisoSinAudioManager;
        private AudioManager _audio;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _audio = AudioManager.Instance;
            if (_audio == null && !_avisoSinAudioManager)
            {
                _avisoSinAudioManager = true;
                Debug.LogWarning("[SfxRed] No hay AudioManager en escena: los SFX no sonarán");
            }
        }

        public void Reproducir(SfxBanco banco, Vector3 posicion) => Enviar(banco, posicion, false);

        public void ReproducirEnEmisor(SfxBanco banco) => Enviar(banco, transform.position, true);

        public void Detener(SfxBanco banco)
        {
            int indice = IndiceDe(banco);
            if (indice == AudioManager.SinSfx)
                return;

            if (IsServer)
                DetenerSfxRpc(indice);
            else if (IsOwner)
                PedirDetenerSfxRpc(indice);
        }

        [ContextMenu("Probar SFX")]
        private void Probar() => ReproducirEnEmisor(_sfxPrueba);

        private void Enviar(SfxBanco banco, Vector3 posicion, bool seguirEmisor)
        {
            int indice = IndiceDe(banco);
            if (indice == AudioManager.SinSfx)
                return;

            if (IsServer)
                ReproducirSfxRpc(indice, posicion, seguirEmisor);
            else if (IsOwner)
                PedirReproducirSfxRpc(indice, posicion, seguirEmisor);
        }

        private int IndiceDe(SfxBanco banco) =>
            _audio != null ? _audio.IndiceSfx(banco) : AudioManager.SinSfx;

        [Rpc(SendTo.Server)]
        private void PedirReproducirSfxRpc(
            int indice,
            Vector3 posicion,
            bool seguirEmisor,
            RpcParams rpcParams = default
        )
        {
            if (rpcParams.Receive.SenderClientId == OwnerClientId)
                ReproducirSfxRpc(indice, posicion, seguirEmisor);
        }

        [Rpc(SendTo.Server)]
        private void PedirDetenerSfxRpc(int indice, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId == OwnerClientId)
                DetenerSfxRpc(indice);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReproducirSfxRpc(int indice, Vector3 posicion, bool seguirEmisor)
        {
            if (_audio == null)
                return;

            _audio.ReproducirSfx(
                _audio.SfxPorIndice(indice),
                posicion,
                seguirEmisor ? transform : null,
                NetworkObjectId
            );
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void DetenerSfxRpc(int indice)
        {
            if (_audio != null)
                _audio.DetenerSfx(_audio.SfxPorIndice(indice), NetworkObjectId);
        }
    }
}
