using System.Collections.Generic;
using TDOM.Unity.Combat;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    /// <summary>Administra los orbes fijos de la arena. Todo spawn y despawn ocurre en el servidor.</summary>
    public sealed class PowerUpsSpawnController : MonoBehaviour
    {
        [Header("Orbes fijos de vida")]
        [SerializeField]
        private List<Transform> _puntosSpawn = new List<Transform>();

        [SerializeField]
        private GameObject _prefabPowerUp;

        [Tooltip("Índice del perfil de vida dentro del catálogo configurado en PowerUpPickup.")]
        [SerializeField]
        [Min(0)]
        private int _indiceCatalogo = 0;

        private readonly List<PowerUpPickup> _orbesActivos = new List<PowerUpPickup>();
        private BossController _jefeObservado;

        private void OnEnable()
        {
            BossController.Spawned += AlAparecerJefe;
            BuscarJefeActivo();
        }

        private void OnDisable()
        {
            BossController.Spawned -= AlAparecerJefe;
            DejarDeObservarJefe();
        }

        private void AlAparecerJefe(BossController jefe)
        {
            if (jefe == null || !jefe.IsServer)
                return;

            DejarDeObservarJefe();
            _jefeObservado = jefe;
            _jefeObservado.OnFaseChanged += AlCambiarFase;
            ColocarOrbes();
        }

        private void BuscarJefeActivo()
        {
            var red = NetworkManager.Singleton;
            if (red == null || !red.IsServer || red.SpawnManager == null)
                return;

            foreach (var objeto in red.SpawnManager.SpawnedObjectsList)
            {
                if (objeto != null && objeto.TryGetComponent(out BossController jefe))
                {
                    AlAparecerJefe(jefe);
                    return;
                }
            }
        }

        private void DejarDeObservarJefe()
        {
            if (_jefeObservado != null)
                _jefeObservado.OnFaseChanged -= AlCambiarFase;
            _jefeObservado = null;
        }

        private void AlCambiarFase(int anterior, int actual)
        {
            if (!EsServidorActivo())
                return;

            LimpiarOrbes();
            ColocarOrbes();
        }

        private void ColocarOrbes()
        {
            if (!EsServidorActivo())
                return;

            if (_prefabPowerUp == null)
            {
                Debug.LogError("[PowerUpsSpawnController] Falta asignar el prefab de power-up.", this);
                return;
            }

            foreach (var punto in _puntosSpawn)
            {
                if (punto == null)
                    continue;

                var instancia = Instantiate(_prefabPowerUp, punto.position, punto.rotation);
                var pickup = instancia.GetComponent<PowerUpPickup>();
                var networkObject = instancia.GetComponent<NetworkObject>();
                if (pickup == null || networkObject == null)
                {
                    Debug.LogError(
                        "[PowerUpsSpawnController] El prefab necesita PowerUpPickup y NetworkObject.",
                        instancia
                    );
                    Destroy(instancia);
                    continue;
                }

                pickup.Preparar(_indiceCatalogo, permanecerHastaRecoger: true);
                _orbesActivos.Add(pickup);
                networkObject.Spawn();
            }
        }

        /// <summary>Limpia los orbes fijos antes de reiniciar la partida. Solo servidor.</summary>
        public static void LimpiarEnServidor()
        {
            var controladores = FindObjectsOfType<PowerUpsSpawnController>();
            foreach (var controlador in controladores)
                controlador.LimpiarOrbes();
        }

        private void LimpiarOrbes()
        {
            if (!EsServidorActivo())
            {
                _orbesActivos.Clear();
                return;
            }

            bool puedeDespawnear = NetworkManager.Singleton != null
                && !NetworkManager.Singleton.ShutdownInProgress;
            foreach (var pickup in _orbesActivos.ToArray())
            {
                if (pickup != null && puedeDespawnear && pickup.IsSpawned)
                    pickup.NetworkObject.Despawn();
            }
            _orbesActivos.Clear();
        }

        private static bool EsServidorActivo() =>
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    }
}
