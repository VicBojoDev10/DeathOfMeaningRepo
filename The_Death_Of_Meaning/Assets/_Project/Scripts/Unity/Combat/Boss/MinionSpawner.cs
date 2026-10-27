using System.Collections.Generic;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Unity.Combat;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace TDOM.Unity
{
    /// <summary>
    /// Spawnea los minions de AREK cuando el jefe lanza su Especial (TW-125). Va en BossArek.prefab
    /// y solo trabaja en el servidor. La cantidad sale de BossPhaseProfile.MinionsEnEspecial de la
    /// fase actual; si spawnea o no lo decide MinionSpawnRule.
    /// </summary>
    public class MinionSpawner : NetworkBehaviour
    {
        [SerializeField]
        private GameObject _prefabMinion;

        [SerializeField]
        private MinionProfile _perfil;

        [SerializeField]
        private AtaquesArek _ataques;

        [SerializeField]
        private BossController _jefe;

        [Header("Drops de power-ups (TW-276)")]
        [SerializeField]
        private TablaDropProfile _tablaDrop;

        [Tooltip("Debe ser el mismo catálogo que usa PowerUpPickup.prefab.")]
        [SerializeField]
        private PowerUpCatalogo _catalogo;

        [SerializeField]
        private GameObject _prefabPickup;

        // Solo servidor.
        private MinionSpawnRule _regla;
        private readonly List<MinionController> _vivos = new List<MinionController>();
        private ReglaDropPowerUp _reglaDrop;
        private readonly List<PowerUpPickup> _pickupsVivos = new List<PowerUpPickup>();

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            if (_ataques == null)
                _ataques = GetComponent<AtaquesArek>();
            if (_jefe == null)
                _jefe = GetComponent<BossController>();

            if (_prefabMinion == null || _perfil == null || _ataques == null || _jefe == null)
            {
                Debug.LogError(
                    "[MinionSpawner] Falta asignar el prefab del minion, el MinionProfile, AtaquesArek o BossController."
                );
                return;
            }

            _regla = new MinionSpawnRule();
            PrepararDrops();
            _ataques.AtaqueEjecutado += AlEjecutarAtaque;
            _jefe.OnFaseChanged += AlCambiarFase;

            if (_jefe.HitZones == null)
                return;

            foreach (var zona in _jefe.HitZones)
            {
                if (zona != null)
                    zona.DanioRegistrado += AlDaniarJefe;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_regla == null)
                return;

            _ataques.AtaqueEjecutado -= AlEjecutarAtaque;
            _jefe.OnFaseChanged -= AlCambiarFase;

            if (_jefe.HitZones != null)
            {
                foreach (var zona in _jefe.HitZones)
                {
                    if (zona != null)
                        zona.DanioRegistrado -= AlDaniarJefe;
                }
            }

            LimpiarTodos();
        }

        /// <summary>
        /// Solo servidor (TW-278): despawnea los minions vivos y los pickups sueltos, vacía las
        /// listas y deja la regla de spawn como nueva (Vivos = 0). Lo llama GameSessionManager al
        /// ganar, perder o reiniciar, y OnNetworkDespawn (el jefe despawneado se lleva a sus
        /// minions). Durante el apagado de la red solo suelta las referencias: Netcode ya
        /// despawnea todo por su cuenta.
        /// </summary>
        public void LimpiarTodos()
        {
            if (!IsServer)
                return;

            bool puedeDespawnear = NetworkManager != null && !NetworkManager.ShutdownInProgress;

            // Copias: despawnear dispara Murio/Despawneado, que modifican las listas originales.
            foreach (var minion in _vivos.ToArray())
            {
                if (minion == null)
                    continue;

                minion.Murio -= AlMorirMinion;
                if (puedeDespawnear && minion.IsSpawned)
                    minion.NetworkObject.Despawn(true);
            }
            _vivos.Clear();

            foreach (var pickup in _pickupsVivos.ToArray())
            {
                if (pickup == null)
                    continue;

                pickup.Despawneado -= AlDespawnearPickup;
                if (puedeDespawnear && pickup.IsSpawned)
                    pickup.NetworkObject.Despawn();
            }
            _pickupsVivos.Clear();

            // Si OnNetworkSpawn no llegó a crearla (faltan referencias), no hay nada que reiniciar.
            if (_regla != null)
                _regla = new MinionSpawnRule();
        }

        // La regla de drops solo existe si están asignadas la tabla, el catálogo y el prefab.
        private void PrepararDrops()
        {
            if (_tablaDrop == null || _catalogo == null || _prefabPickup == null)
            {
                Debug.LogWarning(
                    "[MinionSpawner] Sin tabla de drops, catálogo o prefab de pickup: los minions no sueltan power-ups."
                );
                return;
            }

            var pesos = new float[_tablaDrop.Entradas.Length];
            for (int i = 0; i < pesos.Length; i++)
            {
                bool enCatalogo = _catalogo.IndiceDe(_tablaDrop.Entradas[i].Perfil) >= 0;
                pesos[i] = enCatalogo ? _tablaDrop.Entradas[i].Peso : 0f;
            }

            _reglaDrop = new ReglaDropPowerUp(
                _tablaDrop.ProbabilidadDrop,
                _tablaDrop.MuertesParaGarantizado,
                _tablaDrop.MaxVivos,
                pesos
            );
        }

        // Los vivos de la fase anterior se quedan; el siguiente Especial spawnea los de la nueva fase.
        private void AlCambiarFase(int anterior, int actual)
        {
            _regla.LiberarPorCambioDeFase();
            Debug.Log(
                $"[MinionSpawner] Fase {anterior} -> {actual}: el siguiente Especial spawnea aunque queden {_regla.Vivos} vivos."
            );
        }

        // Mismo criterio que BossController: el golpe no cuenta si el jefe es invulnerable (TW-116).
        private void AlDaniarJefe(float cantidad)
        {
            if (cantidad > 0f && !_jefe.Pausado)
                _regla.RegistrarDanioAlJefe();
        }

        private void AlEjecutarAtaque(BossAttackKind tipo)
        {
            if (tipo != BossAttackKind.Especial)
                return;

            var perfilFase = _jefe.PerfilFaseActual;
            int enFase = perfilFase != null ? perfilFase.MinionsEnEspecial : 0;
            int cantidad = _regla.CantidadASpawnear(enFase);

            if (cantidad == 0)
            {
                Debug.Log(
                    $"[MinionSpawner] Especial sin minions (fase {_jefe.Fase.Value}: {enFase}, vivos: {_regla.Vivos}, puede: {_regla.PuedeSpawnear})."
                );
                return;
            }

            int spawneados = Spawnear(cantidad);
            _regla.RegistrarSpawn(spawneados);
            Debug.Log(
                $"[MinionSpawner] Fase {_jefe.Fase.Value}: spawneó {spawneados} de {cantidad} minions."
            );
        }

        private float RadioMaximo =>
            _perfil.radioSpawn + _perfil.pasoRadioSpawn * Mathf.Max(0, _perfil.intentosSpawn - 1);

        // Puntos repartidos en un círculo alrededor del jefe, ajustados al NavMesh más cercano.
        private int Spawnear(int cantidad)
        {
            int spawneados = 0;
            float desfase = Random.Range(0f, 360f);

            for (int i = 0; i < cantidad; i++)
            {
                float angulo = desfase + 360f * i / cantidad;
                Vector3 direccion = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward;

                if (!TryPuntoDeSpawn(direccion, out Vector3 posicion))
                {
                    Debug.LogWarning(
                        $"[MinionSpawner] No hay NavMesh en la dirección {angulo:F0}° (radio {_perfil.radioSpawn:F1} a {RadioMaximo:F1} m); se salta ese minion."
                    );
                    continue;
                }

                // Excepción aprobada a la regla de Instantiate: es la misma ruta de spawn en red
                // que usan AtaquesArek y PlayerCombat (no hay pool de NetworkObjects).
                GameObject go = Instantiate(
                    _prefabMinion,
                    posicion,
                    Quaternion.LookRotation(direccion, Vector3.up)
                );

                var minion = go.GetComponent<MinionController>();
                var no = go.GetComponent<NetworkObject>();
                if (minion == null || no == null)
                {
                    Debug.LogError(
                        "[MinionSpawner] El prefab del minion necesita MinionController y NetworkObject."
                    );
                    Destroy(go);
                    return spawneados;
                }

                minion.Murio += AlMorirMinion;
                _vivos.Add(minion);
                no.Spawn(true);
                spawneados++;
            }

            return spawneados;
        }

        // Prueba radios cada vez más lejos en esa dirección (el cuerpo del jefe o huecos del piso
        // pueden tapar el radio base). En cada uno baja al piso y busca el NavMesh más cercano.
        private bool TryPuntoDeSpawn(Vector3 direccion, out Vector3 posicion)
        {
            for (int intento = 0; intento < Mathf.Max(1, _perfil.intentosSpawn); intento++)
            {
                float radio = _perfil.radioSpawn + _perfil.pasoRadioSpawn * intento;
                Vector3 punto = BajarAlPiso(transform.position + direccion * radio);

                if (
                    NavMesh.SamplePosition(
                        punto,
                        out NavMeshHit hit,
                        _perfil.distanciaBusquedaNavMesh,
                        NavMesh.AllAreas
                    )
                )
                {
                    posicion = hit.position;
                    return true;
                }
            }

            posicion = default;
            return false;
        }

        // El pivote del jefe está por encima del piso: baja el punto al primer collider de abajo
        // que no sea del propio jefe.
        private Vector3 BajarAlPiso(Vector3 punto)
        {
            var hits = Physics.RaycastAll(
                punto,
                Vector3.down,
                _perfil.alturaBusquedaPiso,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

            float masCerca = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= masCerca)
                    continue;

                masCerca = hit.distance;
                punto = hit.point;
            }

            return punto;
        }

        private void AlMorirMinion(MinionController minion)
        {
            minion.Murio -= AlMorirMinion;
            _vivos.Remove(minion);
            _regla.RegistrarMuerte();
            Debug.Log($"[MinionSpawner] Murió un minion. Vivos: {_regla.Vivos}");

            if (!_jefe.Pausado)
                IntentarSoltarPowerUp(minion.transform.position);
        }

        private void IntentarSoltarPowerUp(Vector3 posicionMinion)
        {
            if (_reglaDrop == null)
                return;

            int? entrada = _reglaDrop.Elegir(Random.value, Random.value, _pickupsVivos.Count);
            if (!entrada.HasValue)
                return;

            var perfil = _tablaDrop.Entradas[entrada.Value].Perfil;
            int indiceCatalogo = _catalogo.IndiceDe(perfil);
            if (indiceCatalogo < 0)
                return;

            // Excepción aprobada a la regla de Instantiate: misma ruta de spawn en red que los minions.
            GameObject go = Instantiate(
                _prefabPickup,
                BajarAlPiso(posicionMinion),
                Quaternion.identity
            );

            var pickup = go.GetComponent<PowerUpPickup>();
            var no = go.GetComponent<NetworkObject>();
            if (pickup == null || no == null)
            {
                Debug.LogError(
                    "[MinionSpawner] El prefab del pickup necesita PowerUpPickup y NetworkObject."
                );
                Destroy(go);
                return;
            }

            pickup.Preparar(indiceCatalogo);
            pickup.Despawneado += AlDespawnearPickup;
            _pickupsVivos.Add(pickup);
            no.Spawn();
            Debug.Log(
                $"[MinionSpawner] Drop: {perfil.Nombre} (pickups vivos: {_pickupsVivos.Count})."
            );
        }

        private void AlDespawnearPickup(PowerUpPickup pickup)
        {
            pickup.Despawneado -= AlDespawnearPickup;
            _pickupsVivos.Remove(pickup);
        }
    }
}
