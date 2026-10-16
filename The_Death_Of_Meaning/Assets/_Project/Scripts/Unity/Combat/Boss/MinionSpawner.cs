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

        // Solo servidor.
        private MinionSpawnRule _regla;
        private readonly List<MinionController> _vivos = new List<MinionController>();

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

            foreach (var minion in _vivos)
            {
                if (minion != null)
                    minion.Murio -= AlMorirMinion;
            }
            _vivos.Clear();
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
        }
    }
}
