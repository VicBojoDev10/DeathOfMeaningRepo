using System.Collections.Generic;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Unity.Combat;
using TDOM.Unity.Level;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace TDOM.Unity
{
    /// <summary>Genera francotiradores desde una plataforma, desde la fase 2.</summary>
    public sealed class SniperMinionSpawner : MonoBehaviour
    {
        private static readonly HashSet<SniperMinionSpawner> Instancias = new();

        [Header("Spawn")]
        [SerializeField] private GameObject _prefabMinion;
        [SerializeField] private float _intervaloSpawn = 12f;
        [SerializeField] private int _maxVivos = 2;

        [Header("Drops")]
        [SerializeField] private TablaDropProfile _tablaDrop;
        [SerializeField] private PowerUpCatalogo _catalogo;
        [SerializeField] private GameObject _prefabPickup;

        private readonly List<SniperMinionController> _vivos = new();
        private readonly List<PowerUpPickup> _pickupsVivos = new();
        private BossController _jefe;
        private ReglaDropPowerUp _reglaDrop;
        private float _siguienteSpawn;
        private bool _activo;
        private NavMeshSurface _superficiePlataforma;
        private Transform _plataforma;
        private bool _navMeshPreparado;
        private NavMeshQueryFilter _filtroNavMesh;

        private void OnEnable()
        {
            Instancias.Add(this);
            BossController.ObservarJefe(NetworkManager.Singleton, AlAparecerJefe);
            BossController.Despawned += AlDesaparecerJefe;
            PrepararDrops();
        }

        private void OnDisable()
        {
            BossController.DejarDeObservarJefe(AlAparecerJefe);
            BossController.Despawned -= AlDesaparecerJefe;
            DesuscribirJefe();
            Instancias.Remove(this);
        }

        private void Update()
        {
            if (!_activo || _jefe == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                return;

            if (_jefe.Pausado || PauseManager.PausadoGlobal || PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso)
                return;

            _vivos.RemoveAll(minion => minion == null);
            _pickupsVivos.RemoveAll(pickup => pickup == null);
            if (_vivos.Count >= Mathf.Max(1, _maxVivos) || Time.time < _siguienteSpawn)
                return;

            Spawnear();
            _siguienteSpawn = Time.time + Mathf.Max(0.1f, _intervaloSpawn);
        }

        private void AlAparecerJefe(BossController jefe)
        {
            DesuscribirJefe();
            _jefe = jefe;
            if (_jefe == null)
                return;

            _jefe.Fase.OnValueChanged += AlCambiarFase;
            ActualizarFase(_jefe.Fase.Value);
        }

        private void AlDesaparecerJefe(BossController jefe)
        {
            if (_jefe != jefe)
                return;

            DesuscribirJefe();
            _activo = false;
        }

        private void DesuscribirJefe()
        {
            if (_jefe != null)
                _jefe.Fase.OnValueChanged -= AlCambiarFase;
            _jefe = null;
        }

        private void AlCambiarFase(int anterior, int actual) => ActualizarFase(actual);

        private void ActualizarFase(int fase)
        {
            if (_jefe == null || !_jefe.IsServer)
                return;

            _activo = fase >= 2;
            if (_activo && _siguienteSpawn <= Time.time)
                _siguienteSpawn = Time.time + Mathf.Max(0.1f, _intervaloSpawn);
        }

        private void Spawnear()
        {
            if (_prefabMinion == null)
            {
                Debug.LogError($"[{nameof(SniperMinionSpawner)}] Falta el prefab de francotirador en {name}.");
                _activo = false;
                return;
            }

            GameObject instancia = Instantiate(_prefabMinion, transform.position, transform.rotation);
            SniperMinionController minion = instancia.GetComponent<SniperMinionController>();
            NetworkObject networkObject = instancia.GetComponent<NetworkObject>();
            if (minion == null || networkObject == null)
            {
                Debug.LogError($"[{nameof(SniperMinionSpawner)}] El prefab requiere SniperMinionController y NetworkObject.");
                Destroy(instancia);
                return;
            }

            PlataformaCircular plataforma = GetComponentInParent<PlataformaCircular>();
            _plataforma = plataforma != null ? plataforma.transform : transform.parent;
            NavMeshAgent agentePrefab = _prefabMinion.GetComponent<NavMeshAgent>();
            if (agentePrefab == null)
            {
                Debug.LogError($"[{nameof(SniperMinionSpawner)}] El prefab sniper necesita NavMeshAgent.");
                Destroy(instancia);
                return;
            }
            _filtroNavMesh = new NavMeshQueryFilter
            {
                agentTypeID = agentePrefab.agentTypeID,
                areaMask = agentePrefab.areaMask,
            };

            if (!PrepararNavMeshPlataforma())
            {
                Destroy(instancia);
                return;
            }

            if (!TryElegirPosicionSpawn(out Vector3 posicionSpawn))
            {
                Debug.LogWarning(
                    $"[{nameof(SniperMinionSpawner)}] No se encontró un punto de NavMesh sobre {(_plataforma != null ? _plataforma.name : name)} " +
                    $"(Agent Type ID {_filtroNavMesh.agentTypeID}, spawner bajo plataforma: {_plataforma != null && transform.IsChildOf(_plataforma)}). Revisa el bake y que SniperMinionArek use el mismo Agent Type."
                );
                Destroy(instancia);
                return;
            }

            instancia.transform.SetPositionAndRotation(posicionSpawn, transform.rotation);
            minion.ConfigurarPlataforma(_plataforma, _superficiePlataforma);
            minion.Murio += AlMorirMinion;
            _vivos.Add(minion);
            networkObject.Spawn(true);
        }

        private bool PrepararNavMeshPlataforma()
        {
            if (_navMeshPreparado)
                return _superficiePlataforma != null && _superficiePlataforma.navMeshData != null;

            _navMeshPreparado = true;
            if (_plataforma == null)
            {
                Debug.LogError($"[{nameof(SniperMinionSpawner)}] El spawner {name} no está dentro de una plataforma.");
                return false;
            }

            _superficiePlataforma = _plataforma.GetComponent<NavMeshSurface>();
            if (_superficiePlataforma == null)
                _superficiePlataforma = _plataforma.gameObject.AddComponent<NavMeshSurface>();

            _superficiePlataforma.agentTypeID = _filtroNavMesh.agentTypeID;
            // Children representa la jerarquía actual de esta plataforma (incluida la raíz).
            _superficiePlataforma.collectObjects = CollectObjects.Children;
            _superficiePlataforma.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            int layerSniperPlataforma = LayerMask.NameToLayer("SniperPlatform");
            if (layerSniperPlataforma < 0)
            {
                Debug.LogError(
                    $"[{nameof(SniperMinionSpawner)}] No existe la layer SniperPlatform. " +
                    $"No se horneará la jerarquía completa para evitar incluir geometría ajena a { _plataforma.name }."
                );
                return false;
            }

            _superficiePlataforma.layerMask = 1 << layerSniperPlataforma;
            if (!TieneGeometriaEnLayer(layerSniperPlataforma))
            {
                Debug.LogError(
                    $"[{nameof(SniperMinionSpawner)}] No hay Renderer ni Collider en la layer SniperPlatform " +
                    $"dentro de la jerarquía de {_plataforma.name}. Asigna esa layer a la geometría de la plataforma."
                );
                return false;
            }

            _superficiePlataforma.minRegionArea = 0f;
            _superficiePlataforma.ignoreNavMeshAgent = true;
            _superficiePlataforma.ignoreNavMeshObstacle = true;
            // Reconstruye una vez con esta jerarquía, aunque hubiera datos horneados con All.
            _superficiePlataforma.BuildNavMesh();

            if (_superficiePlataforma.navMeshData == null)
            {
                Debug.LogError($"[{nameof(SniperMinionSpawner)}] No se pudo hornear el NavMesh de {name}.");
                return false;
            }

            return true;
        }

        private bool TryElegirPosicionSpawn(out Vector3 posicion)
        {
            SniperMinionProfile perfil = _prefabMinion != null
                ? _prefabMinion.GetComponent<SniperMinionController>()?.Perfil
                : null;
            float radio = perfil != null ? Mathf.Max(0.25f, perfil.radioSpawn) : 1.5f;
            Vector3 centro = transform.position;
            for (int intento = 0; intento < 16; intento++)
            {
                Vector2 aleatorio = Random.insideUnitCircle.normalized * Random.Range(radio * 0.55f, radio);
                Vector3 candidato = centro + new Vector3(aleatorio.x, 0f, aleatorio.y);
                if (!NavMesh.SamplePosition(candidato, out NavMeshHit hit, 0.8f, _filtroNavMesh))
                    continue;
                if (!EsPuntoDeEstaPlataforma(hit.position))
                    continue;
                if (!TieneMargenDelBorde(hit.position, 0.12f))
                    continue;

                posicion = hit.position;
                return true;
            }

            if (NavMesh.SamplePosition(centro, out NavMeshHit centroHit, 2f, _filtroNavMesh)
                && EsPuntoDeEstaPlataforma(centroHit.position))
            {
                posicion = centroHit.position;
                return true;
            }

            posicion = default;
            return false;
        }

        private bool TieneMargenDelBorde(Vector3 punto, float margen)
        {
            return NavMesh.FindClosestEdge(punto, out NavMeshHit borde, _filtroNavMesh)
                && borde.distance >= margen;
        }

        private bool TieneGeometriaEnLayer(int layer)
        {
            foreach (Renderer renderer in _plataforma.GetComponentsInChildren<Renderer>())
            {
                if (renderer.gameObject.layer == layer)
                    return true;
            }

            foreach (Collider collider in _plataforma.GetComponentsInChildren<Collider>())
            {
                if (collider.gameObject.layer == layer)
                    return true;
            }

            return false;
        }

        private bool EsPuntoDeEstaPlataforma(Vector3 punto)
        {
            if (_plataforma == null || _superficiePlataforma == null)
                return false;

            Collider[] colliders = _plataforma.GetComponentsInChildren<Collider>();
            Vector3 origen = punto + Vector3.up * 1.25f;
            foreach (RaycastHit hit in Physics.RaycastAll(origen, Vector3.down, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(_plataforma) && hit.collider.transform != _plataforma)
                    continue;
                if ((hit.point - punto).sqrMagnitude > 1f)
                    continue;

                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] == hit.collider)
                        return true;
                }
            }

            return false;
        }

        private void PrepararDrops()
        {
            if (_tablaDrop == null || _catalogo == null || _prefabPickup == null)
                return;

            float[] pesos = new float[_tablaDrop.Entradas.Length];
            for (int i = 0; i < pesos.Length; i++)
                pesos[i] = _catalogo.IndiceDe(_tablaDrop.Entradas[i].Perfil) >= 0
                    ? _tablaDrop.Entradas[i].Peso
                    : 0f;

            _reglaDrop = new ReglaDropPowerUp(
                _tablaDrop.ProbabilidadDrop,
                _tablaDrop.MuertesParaGarantizado,
                _tablaDrop.MaxVivos,
                pesos
            );
        }

        private void AlMorirMinion(SniperMinionController minion)
        {
            minion.Murio -= AlMorirMinion;
            _vivos.Remove(minion);
            if (_jefe == null || _jefe.Pausado)
                return;
            IntentarSoltarPowerUp(minion.transform.position);
        }

        private void IntentarSoltarPowerUp(Vector3 posicion)
        {
            if (_reglaDrop == null)
                return;

            int? indice = _reglaDrop.Elegir(Random.value, Random.value, _pickupsVivos.Count);
            if (!indice.HasValue)
                return;

            PowerUpProfile perfil = _tablaDrop.Entradas[indice.Value].Perfil;
            int indiceCatalogo = _catalogo.IndiceDe(perfil);
            if (indiceCatalogo < 0)
                return;

            GameObject instancia = Instantiate(_prefabPickup, posicion, Quaternion.identity);
            PowerUpPickup pickup = instancia.GetComponent<PowerUpPickup>();
            NetworkObject networkObject = instancia.GetComponent<NetworkObject>();
            if (pickup == null || networkObject == null)
            {
                Debug.LogError($"[{nameof(SniperMinionSpawner)}] El prefab de pickup requiere PowerUpPickup y NetworkObject.");
                Destroy(instancia);
                return;
            }

            pickup.Preparar(indiceCatalogo);
            pickup.Despawneado += AlDespawnearPickup;
            _pickupsVivos.Add(pickup);
            networkObject.Spawn();
        }

        private void AlDespawnearPickup(PowerUpPickup pickup)
        {
            pickup.Despawneado -= AlDespawnearPickup;
            _pickupsVivos.Remove(pickup);
        }

        /// <summary>Limpia minions y drops de todos los spawners de sniper de la escena.</summary>
        public static void LimpiarTodos()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                return;

            foreach (SniperMinionSpawner spawner in new List<SniperMinionSpawner>(Instancias))
                spawner.Limpiar();
        }

        private void Limpiar()
        {
            bool puedeDespawnear = NetworkManager.Singleton != null && !NetworkManager.Singleton.ShutdownInProgress;
            foreach (SniperMinionController minion in _vivos.ToArray())
            {
                if (minion == null)
                    continue;
                minion.Murio -= AlMorirMinion;
                if (puedeDespawnear && minion.IsSpawned)
                    minion.NetworkObject.Despawn(true);
            }
            _vivos.Clear();

            foreach (PowerUpPickup pickup in _pickupsVivos.ToArray())
            {
                if (pickup == null)
                    continue;
                pickup.Despawneado -= AlDespawnearPickup;
                if (puedeDespawnear && pickup.IsSpawned)
                    pickup.NetworkObject.Despawn();
            }
            _pickupsVivos.Clear();
            _reglaDrop = null;
            _activo = false;
            PrepararDrops();
            _siguienteSpawn = Time.time + Mathf.Max(0.1f, _intervaloSpawn);
        }
    }
}
