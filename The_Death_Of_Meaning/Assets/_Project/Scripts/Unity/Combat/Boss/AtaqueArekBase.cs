using TDOM.Data;
using TDOM.Unity.Audio;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using TDOM.Unity.Vfx;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public abstract class AtaqueArekBase : NetworkBehaviour
    {
        private const float DuracionFlashDebugGolpe = 0.2f;

        private readonly NetworkVariable<ulong> _lanzadorId = new NetworkVariable<ulong>();

        private const float VelocidadNormal = 1f;

        private readonly NetworkVariable<float> _velocidad = new NetworkVariable<float>(
            VelocidadNormal
        );

        private ulong _lanzadorPendiente;

        private float _velocidadPendiente = VelocidadNormal;

        private VfxRed _vfxJefe;

        private SfxRed _sfxJefe;

        protected AtaquesArek Lanzador { get; private set; }

        /// <summary>VFX en red del jefe (VfxRed de la raíz de AREK); null si no lo tiene.</summary>
        protected VfxRed VfxJefe
        {
            get
            {
                if (_vfxJefe == null && Lanzador != null)
                    _vfxJefe = Lanzador.GetComponentInParent<VfxRed>();
                return _vfxJefe;
            }
        }

        protected SfxRed SfxJefe
        {
            get
            {
                if (_sfxJefe == null && Lanzador != null)
                    _sfxJefe = Lanzador.GetComponentInParent<SfxRed>();
                return _sfxJefe;
            }
        }

        protected void ReproducirSfxEnJefe(SfxBanco banco)
        {
            if (IsServer && banco != null && SfxJefe != null)
                SfxJefe.ReproducirEnEmisor(banco);
        }

        protected void ReproducirSfxEn(SfxBanco banco, Vector3 punto)
        {
            if (IsServer && banco != null && SfxJefe != null)
                SfxJefe.Reproducir(banco, punto);
        }

        protected void DetenerSfxJefe(SfxBanco banco)
        {
            if (IsServer && banco != null && SfxJefe != null)
                SfxJefe.Detener(banco);
        }

        // Se llama antes de Spawn(): la NetworkVariable aun no conoce su NetworkBehaviour, asi que
        // el id se guarda aparte y se escribe en OnNetworkSpawn, antes de que el spawn llegue a los
        // clientes.
        public void AsignarLanzador(AtaquesArek lanzador, float velocidad)
        {
            _lanzadorPendiente = lanzador.NetworkObjectId;
            _velocidadPendiente = velocidad;
        }

        protected float DeltaAtaque => Time.deltaTime * _velocidad.Value;

        protected float MultiplicadorVelocidad => _velocidad.Value;

        protected float AvisoEscalado(float aviso) => aviso / _velocidad.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _lanzadorId.Value = _lanzadorPendiente;
                _velocidad.Value = _velocidadPendiente;
            }

            if (
                NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                    _lanzadorId.Value,
                    out var obj
                )
            )
                Lanzador = obj.GetComponent<AtaquesArek>();

            if (Lanzador == null)
                Debug.LogError(
                    $"[{GetType().Name}] No se encontró AtaquesArek que lanzó el ataque."
                );

            AlSpawnear();
        }

        protected abstract void AlSpawnear();

        internal static void NotificarGolpeAlJugadorLocal()
        {
            var jugador = BuscarJugadorLocal();
            if (jugador == null)
                return;

            jugador.RecibirGolpe();

            var feedbackDebug = jugador.GetComponentInChildren<CombatDebugFeedback>();
            if (feedbackDebug != null)
                feedbackDebug.FlashActive(DuracionFlashDebugGolpe);
        }

        protected internal static PlayerRoot BuscarJugadorLocal()
        {
            var cliente = NetworkManager.Singleton?.LocalClient;
            if (cliente == null)
                return null;

            foreach (var objeto in cliente.OwnedObjects)
            {
                if (objeto.TryGetComponent(out PlayerRoot jugador))
                    return jugador;
            }

            return null;
        }
    }
}
