using System.Collections.Generic;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Unity.Combat;
using TDOM.Unity.Level;
using Unity.Netcode;
using UnityEngine;

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
        [SerializeField, Min(0f)] private float _distanciaMinimaEntreSnipers = 0.65f;

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
        private Transform _plataforma;
        private Collider[] _collidersPlataforma;

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

            SniperMinionController minionPrefab = _prefabMinion.GetComponent<SniperMinionController>();
            NetworkObject networkObjectPrefab = _prefabMinion.GetComponent<NetworkObject>();
            if (minionPrefab == null || networkObjectPrefab == null)
            {
                Debug.LogError($"[{nameof(SniperMinionSpawner)}] El prefab requiere SniperMinionController y NetworkObject.");
                return;
            }

            PlataformaCircular plataforma = GetComponentInParent<PlataformaCircular>();
            _plataforma = plataforma != null ? plataforma.transform : transform.parent;
            _collidersPlataforma = _plataforma != null
                ? _plataforma.GetComponentsInChildren<Collider>()
                : System.Array.Empty<Collider>();
            if (_plataforma == null || !TieneColliderDePlataforma())
            {
                Debug.LogError($"[{nameof(SniperMinionSpawner)}] {name} debe estar dentro de una plataforma con Collider.");
                return;
            }

            if (!TryElegirPosicionSpawn(out Vector3 puntoSuperficie))
            {
                Debug.LogWarning(
                    $"[{nameof(SniperMinionSpawner)}] No se encontró la superficie de { _plataforma.name } cerca del spawner. " +
                    "Comprueba que la plataforma tenga un Collider que cubra la zona caminable."
                );
                return;
            }

            // El modelo exportado tiene la cara superior en su eje local +Z.
            Vector3 normal = _plataforma.forward;
            Quaternion rotacionSpawn = Quaternion.LookRotation(_plataforma.up, normal);
            float altura = ObtenerAlturaPrefab(normal, rotacionSpawn);
            GameObject instancia = Instantiate(_prefabMinion, puntoSuperficie + normal * altura, rotacionSpawn);
            SniperMinionController minion = instancia.GetComponent<SniperMinionController>();
            NetworkObject networkObject = instancia.GetComponent<NetworkObject>();
            minion.ConfigurarPlataforma(_plataforma, _collidersPlataforma, puntoSuperficie, altura);
            minion.Murio += AlMorirMinion;
            _vivos.Add(minion);
            networkObject.Spawn(true);
        }

        private bool TieneColliderDePlataforma()
        {
            foreach (Collider collider in _collidersPlataforma)
                if (collider != null && collider.enabled && !collider.isTrigger)
                    return true;
            return false;
        }

        private bool TryElegirPosicionSpawn(out Vector3 puntoSuperficie)
        {
            SniperMinionProfile perfil = _prefabMinion != null
                ? _prefabMinion.GetComponent<SniperMinionController>()?.Perfil
                : null;
            float radio = perfil != null ? Mathf.Clamp(perfil.radioSpawn, 0.25f, 0.8f) : 0.5f;

            for (int intento = 0; intento < 32; intento++)
            {
                Vector3 candidato = transform.position;
                if (intento > 0)
                {
                    Vector2 aleatorio = Random.insideUnitCircle * radio;
                    candidato += _plataforma.right * aleatorio.x + _plataforma.up * aleatorio.y;
                }

                if (TryProyectarEnPlataforma(candidato, out puntoSuperficie)
                    && TieneEspacioParaSpawn(puntoSuperficie))
                    return true;
            }

            puntoSuperficie = default;
            return false;
        }

        private bool TryProyectarEnPlataforma(Vector3 candidato, out Vector3 puntoSuperficie)
        {
            Vector3 normal = _plataforma.forward;
            bool encontro = false;
            float distanciaMasCercana = float.MaxValue;
            puntoSuperficie = default;

            foreach (Collider collider in _collidersPlataforma)
            {
                if (collider == null || !collider.enabled || collider.isTrigger)
                    continue;

                float distanciaRayo = Mathf.Max(5f, collider.bounds.extents.magnitude * 2f + 2f);
                Vector3 arriba = candidato + normal * distanciaRayo;
                Vector3 abajo = candidato - normal * distanciaRayo;
                if (collider.Raycast(new Ray(arriba, -normal), out RaycastHit hitArriba, distanciaRayo * 2f))
                {
                    float distancia = (hitArriba.point - candidato).sqrMagnitude;
                    if (distancia < distanciaMasCercana)
                    {
                        distanciaMasCercana = distancia;
                        puntoSuperficie = hitArriba.point;
                        encontro = true;
                    }
                }
                if (collider.Raycast(new Ray(abajo, normal), out RaycastHit hitAbajo, distanciaRayo * 2f))
                {
                    float distancia = (hitAbajo.point - candidato).sqrMagnitude;
                    if (distancia < distanciaMasCercana)
                    {
                        distanciaMasCercana = distancia;
                        puntoSuperficie = hitAbajo.point;
                        encontro = true;
                    }
                }
            }

            return encontro;
        }

        private bool TieneEspacioParaSpawn(Vector3 puntoSuperficie)
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
                return true;

            foreach (NetworkObject objeto in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (objeto == null || !objeto.TryGetComponent(out SniperMinionController sniper))
                    continue;
                if (sniper.Plataforma != _plataforma)
                    continue;

                Vector3 separacionEnPlano = Vector3.ProjectOnPlane(
                    sniper.transform.position - puntoSuperficie,
                    _plataforma.forward
                );
                if (separacionEnPlano.sqrMagnitude < _distanciaMinimaEntreSnipers * _distanciaMinimaEntreSnipers)
                    return false;
            }

            return true;
        }

        private float ObtenerAlturaPrefab(Vector3 normal, Quaternion rotacionSpawn)
        {
            Collider collider = _prefabMinion.GetComponent<Collider>();
            if (collider == null)
                return 0.5f;

            if (collider is BoxCollider caja)
            {
                Vector3 escala = caja.transform.lossyScale;
                escala = new Vector3(Mathf.Abs(escala.x), Mathf.Abs(escala.y), Mathf.Abs(escala.z));
                Vector3 semiextensiones = Vector3.Scale(caja.size * 0.5f, escala);
                Vector3 normalLocal = Quaternion.Inverse(rotacionSpawn) * normal;
                return Mathf.Abs(normalLocal.x) * semiextensiones.x
                    + Mathf.Abs(normalLocal.y) * semiextensiones.y
                    + Mathf.Abs(normalLocal.z) * semiextensiones.z
                    + 0.02f;
            }

            return 0.5f;
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
