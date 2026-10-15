using System;
using System.Collections.Generic;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class AtaquesArek : NetworkBehaviour
    {
        [Header("Prefabs de Ataques")]
        [SerializeField]
        private GameObject _prefabAtaqueMano;

        [SerializeField]
        private Transform _puntoOrigenMano;

        // Antes era el "Tentáculo" (TW-86); en realidad es el Mortero (TW-96).
        [SerializeField]
        private GameObject _prefabAtaqueMortero;

        [SerializeField]
        private GameObject _prefabAtaqueTentaculo;

        [SerializeField]
        private GameObject _prefabAtaqueRayo;

        [SerializeField]
        private Transform _bocaRayo; // opcional; si es null se busca el hijo "Boca" del jefe

        // Cada ataque lee su perfil de aquí al spawnear (servidor y clientes).
        [Header("Perfiles de tuning de los ataques")]
        [SerializeField]
        private AtaqueManoProfile _perfilMano;

        [SerializeField]
        private AtaqueTentaculoProfile _perfilTentaculo;

        [SerializeField]
        private AtaqueMorteroProfile _perfilMortero;

        [SerializeField]
        private AtaqueRayoProfile _perfilRayo;

        [Header("Elección de objetivo")]
        [SerializeField]
        private BossTargetingProfile _perfilObjetivo;

        public AtaqueManoProfile PerfilMano => _perfilMano;
        public AtaqueTentaculoProfile PerfilTentaculo => _perfilTentaculo;
        public AtaqueMorteroProfile PerfilMortero => _perfilMortero;
        public AtaqueRayoProfile PerfilRayo => _perfilRayo;
        public BossTargetingProfile PerfilObjetivo => _perfilObjetivo;

        /// <summary>Solo servidor: se lanzó un ataque (también desde los botones de debug). Lo usa MinionSpawner.</summary>
        public event Action<BossAttackKind> AtaqueEjecutado;

        /// <summary>Solo servidor: jugador elegido para el último ataque (null si nadie estaba en rango).</summary>
        public ulong? ObjetivoActual { get; private set; }

        // Solo servidor.
        private BossAggroTracker _agro;
        private BossTargetSelector _selector;
        private HitZone[] _zonas;
        private readonly List<BossTargetCandidate> _candidatos = new List<BossTargetCandidate>();
        private readonly List<PlayerRoot> _jugadores = new List<PlayerRoot>();

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            _agro = new BossAggroTracker(0f);
            _selector = new BossTargetSelector(0f, 0f);

            _zonas = GetComponentsInChildren<HitZone>(true);
            foreach (var zona in _zonas)
                zona.DanioRecibido += AlRecibirDanio;
        }

        public override void OnNetworkDespawn()
        {
            if (_zonas == null)
                return;

            foreach (var zona in _zonas)
                zona.DanioRecibido -= AlRecibirDanio;
        }

        private void AlRecibirDanio(ulong atacanteId, float cantidad)
        {
            _agro?.Registrar(atacanteId, cantidad, Time.time);
        }

        // Lo llama el patrón (BossController). Solo ataca si hay un jugador en el rango de ese ataque.
        // Mapeo de Confluence: Basico = Mano, Pesado = Tentáculo, Especial = Rayo, InstaKill = Mortero.
        public void Lanzar(BossAttackKind tipo)
        {
            if (!IsServer)
                return;

            if (!TryElegirObjetivo(tipo))
                return;

            Ejecutar(tipo);
        }

        private bool TryElegirObjetivo(BossAttackKind tipo)
        {
            AtaqueArekProfile perfil = PerfilDe(tipo);
            if (perfil == null || _perfilObjetivo == null)
            {
                Debug.LogError(
                    $"[AtaquesArek] Falta el perfil de {tipo} o el BossTargetingProfile en el Inspector."
                );
                return false;
            }

            ActualizarAjustes();
            ArmarCandidatos();
            var motivo = _selector.Elegir(_candidatos, perfil.Rango, out ulong objetivoId);

            if (motivo == MotivoObjetivo.Ninguno)
            {
                // Más adelante: aquí el jefe spawnea minions en vez de atacar.
                ObjetivoActual = null;
                Debug.Log(
                    $"[AtaquesArek] {tipo}: ningún jugador en rango ({perfil.Rango:F0} m); no ataca."
                );
                return false;
            }

            ObjetivoActual = objetivoId;
            Debug.Log($"[AtaquesArek] {tipo} contra cliente {objetivoId} ({motivo}).");
            return true;
        }

        /// <summary>
        /// Solo servidor: a quién mira el jefe ahora (dentro del radio de detección, por agro o
        /// el más cercano). Lo usa BossGiroHaciaObjetivo.
        /// </summary>
        public bool TryObjetivoVisible(out ulong objetivoId)
        {
            objetivoId = 0;
            if (!IsServer || _selector == null || _perfilObjetivo == null)
                return false;

            ActualizarAjustes();
            ArmarCandidatos();
            var motivo = _selector.Elegir(
                _candidatos,
                _perfilObjetivo.radioDeteccion,
                out objetivoId
            );
            return motivo != MotivoObjetivo.Ninguno;
        }

        /// <summary>Solo servidor: posición del jugador de ese cliente, si sigue spawneado.</summary>
        public bool TryPosicionJugador(ulong clientId, out Vector3 posicion)
        {
            posicion = default;
            ListarJugadores(_jugadores);
            foreach (var jugador in _jugadores)
            {
                if (jugador.OwnerClientId != clientId)
                    continue;

                posicion = jugador.transform.position;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Jugadores spawneados (PlayerRoot). GameSessionManager los spawnea con
        /// SpawnWithOwnership, así que NetworkClient.PlayerObject está vacío: se buscan aquí.
        /// </summary>
        public void ListarJugadores(List<PlayerRoot> resultado)
        {
            resultado.Clear();
            foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (objeto != null && objeto.TryGetComponent(out PlayerRoot jugador))
                    resultado.Add(jugador);
            }
        }

        // Se leen cada vez para poder ajustar el perfil en Play.
        private void ActualizarAjustes()
        {
            _agro.Ventana = _perfilObjetivo.ventanaAgro;
            _selector.RadioDeteccion = _perfilObjetivo.radioDeteccion;
            _selector.FactorAgroNotable = _perfilObjetivo.factorAgroNotable;
        }

        // Distancia en XZ desde el jefe y daño reciente de cada jugador spawneado.
        private void ArmarCandidatos()
        {
            _candidatos.Clear();
            float ahora = Time.time;

            ListarJugadores(_jugadores);
            foreach (var jugador in _jugadores)
            {
                Vector3 hacia = jugador.transform.position - transform.position;
                hacia.y = 0f;

                ulong id = jugador.OwnerClientId;
                _candidatos.Add(
                    new BossTargetCandidate(id, hacia.magnitude, _agro.DanioReciente(id, ahora))
                );
            }
        }

        private AtaqueArekProfile PerfilDe(BossAttackKind tipo)
        {
            switch (tipo)
            {
                case BossAttackKind.Basico:
                    return _perfilMano;
                case BossAttackKind.Pesado:
                    return _perfilTentaculo;
                case BossAttackKind.Especial:
                    return _perfilRayo;
                case BossAttackKind.InstaKill:
                    return _perfilMortero;
                default:
                    return null;
            }
        }

        private void Ejecutar(BossAttackKind tipo)
        {
            switch (tipo)
            {
                case BossAttackKind.Basico:
                    LanzarAtaqueMano();
                    break;
                case BossAttackKind.Pesado:
                    LanzarAtaqueTentaculo();
                    break;
                case BossAttackKind.Especial:
                    LanzarAtaqueRayo();
                    break;
                case BossAttackKind.InstaKill:
                    LanzarAtaqueMortero();
                    break;
            }

            AtaqueEjecutado?.Invoke(tipo);
        }

        private void LanzarAtaqueMano()
        {
            Vector3 pos = _puntoOrigenMano != null ? _puntoOrigenMano.position : transform.position;
            Quaternion rot =
                _puntoOrigenMano != null ? _puntoOrigenMano.rotation : transform.rotation;

            // El barrido de 180° se centra en el objetivo, aunque el jefe no haya terminado de girar.
            if (
                ObjetivoActual.HasValue
                && TryPosicionJugador(ObjetivoActual.Value, out Vector3 posicionObjetivo)
            )
            {
                Vector3 hacia = posicionObjetivo - pos;
                hacia.y = 0f;
                if (hacia.sqrMagnitude > 0.0001f)
                    rot = Quaternion.LookRotation(hacia.normalized, Vector3.up);
            }

            Spawnear(_prefabAtaqueMano, "_prefabAtaqueMano", pos, rot);
        }

        // Se spawnea en la posición y rotación del jefe; AtaqueMortero elige los 3 puntos al spawnear.
        private void LanzarAtaqueMortero()
        {
            Spawnear(
                _prefabAtaqueMortero,
                "_prefabAtaqueMortero",
                transform.position,
                transform.rotation
            );
        }

        // Se spawnea en la posición y rotación del jefe; AtaqueTentaculo elige la dirección al spawnear.
        private void LanzarAtaqueTentaculo()
        {
            Spawnear(
                _prefabAtaqueTentaculo,
                "_prefabAtaqueTentaculo",
                transform.position,
                transform.rotation
            );
        }

        private void LanzarAtaqueRayo()
        {
            if (_prefabAtaqueRayo == null)
            {
                Debug.LogError("[AtaquesArek] _prefabAtaqueRayo no está asignado en el Inspector.");
                return;
            }

            GameObject go = Instantiate(_prefabAtaqueRayo, transform.position, transform.rotation);

            // Si no se asignó en el Inspector, usa el hijo "Boca" del jefe.
            Transform boca = _bocaRayo != null ? _bocaRayo : transform.Find("Boca");

            var rayo = go.GetComponent<AtaqueRayo>();
            if (rayo != null)
            {
                rayo.AsignarLanzador(this);
                if (boca != null)
                    rayo.AsignarBoca(boca);
            }

            var no = go.GetComponent<NetworkObject>();
            if (no != null)
                no.Spawn();
        }

        private void Spawnear(GameObject prefab, string nombreCampo, Vector3 pos, Quaternion rot)
        {
            if (prefab == null)
            {
                Debug.LogError($"[AtaquesArek] {nombreCampo} no está asignado en el Inspector.");
                return;
            }

            GameObject go = Instantiate(prefab, pos, rot);

            var ataque = go.GetComponent<AtaqueArekBase>();
            if (ataque != null)
                ataque.AsignarLanzador(this);

            var no = go.GetComponent<NetworkObject>();
            if (no != null)
                no.Spawn();
        }

        // Los botones de debug no revisan el rango del ataque, pero sí apuntan a quien el jefe
        // está mirando (radio de detección); si no ve a nadie, el ataque cae al azar.
        private void EjecutarDebug(BossAttackKind tipo)
        {
            ObjetivoActual = TryObjetivoVisible(out ulong objetivoId) ? objetivoId : (ulong?)null;
            Ejecutar(tipo);
        }

        private void OnGUI()
        {
            // Solo visible para el host (servidor con cliente local) y cuando está spawneado
            if (!IsServer || !IsHost || !IsSpawned)
                return;

            // Los botones de debug lanzan el ataque sin revisar su rango (ver EjecutarDebug).
            // Ubicado en la esquina superior derecha para no solapar el debug UI de la izquierda
            GUILayout.BeginArea(
                new Rect(Screen.width - 180, 20, 160, 215),
                "Boss Attacks Host",
                GUI.skin.window
            );
            if (GUILayout.Button("Lanzar Mano", GUILayout.Height(35)))
            {
                EjecutarDebug(BossAttackKind.Basico);
            }
            if (GUILayout.Button("Lanzar Tentáculo", GUILayout.Height(35)))
            {
                EjecutarDebug(BossAttackKind.Pesado);
            }
            if (GUILayout.Button("Lanzar Mortero", GUILayout.Height(35)))
            {
                EjecutarDebug(BossAttackKind.InstaKill);
            }
            if (GUILayout.Button("Lanzar Rayo", GUILayout.Height(35)))
            {
                EjecutarDebug(BossAttackKind.Especial);
            }
            GUILayout.EndArea();
        }
    }
}
