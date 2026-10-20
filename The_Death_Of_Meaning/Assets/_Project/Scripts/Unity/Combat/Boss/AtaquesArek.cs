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

        // Parpadeo de las áreas de aviso, común a todos los ataques.
        [SerializeField]
        private AvisoAtaqueProfile _perfilAviso;

        [Header("Elección de objetivo")]
        [SerializeField]
        private BossTargetingProfile _perfilObjetivo;

        // TW-126: cada piedra del Mortero deja una columna; aquí viven porque el ataque se despawnea.
        [Header("Columnas del Mortero")]
        [SerializeField]
        private GameObject _prefabColumnaMortero;

        public AtaqueManoProfile PerfilMano => _perfilMano;
        public AtaqueTentaculoProfile PerfilTentaculo => _perfilTentaculo;
        public AtaqueMorteroProfile PerfilMortero => _perfilMortero;
        public AtaqueRayoProfile PerfilRayo => _perfilRayo;
        public AvisoAtaqueProfile PerfilAviso => _perfilAviso;
        public BossTargetingProfile PerfilObjetivo => _perfilObjetivo;

        /// <summary>Solo servidor: se lanzó un ataque (también desde los botones de debug). Lo usa MinionSpawner.</summary>
        public event Action<BossAttackKind> AtaqueEjecutado;

        /// <summary>Solo servidor: jugador elegido para el último ataque (null si nadie estaba en rango).</summary>
        public ulong? ObjetivoActual { get; private set; }

        /// <summary>Un impacto válido cambia el tracking al jugador que lo conectó.</summary>
        public void RegistrarImpacto(ulong atacanteId)
        {
            if (!IsServer)
                return;

            _logicaMovimiento.FijarObjetivo(atacanteId);
            _agro?.Registrar(atacanteId, 1f, Time.time);
            Debug.Log($"[AtaquesArek] Tracking cambiado al cliente {atacanteId} por impacto.");
        }

        // Solo servidor.
        private BossAggroTracker _agro;
        private HitZone[] _zonas;
        private readonly List<BossTargetCandidate> _candidatos = new List<BossTargetCandidate>();
        private readonly List<BossTargetCandidate> _candidatosMovimiento =
            new List<BossTargetCandidate>();
        private readonly List<PlayerRoot> _jugadores = new List<PlayerRoot>();
        private readonly TDOM.Gameplay.Combat.BossMovement _logicaMovimiento =
            new TDOM.Gameplay.Combat.BossMovement();

        // Solo servidor: columnas vivas, de la más vieja a la más nueva.
        private struct ColumnaViva
        {
            public ColumnaMortero Columna;
            public int AtaqueDeNacimiento;
        }

        private readonly List<ColumnaViva> _columnas = new List<ColumnaViva>();
        private int _ataquesLanzados;
        private BossController _jefe;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            _agro = new BossAggroTracker(0f);
            _zonas = GetComponentsInChildren<HitZone>(true);
            foreach (var zona in _zonas)
                zona.DanioRecibido += AlRecibirDanio;

            _jefe = GetComponent<BossController>();
            if (_jefe != null)
                _jefe.OnFaseChanged += AlCambiarFase;
        }

        public override void OnNetworkDespawn()
        {
            _logicaMovimiento.ResetearObjetivo();
            if (_jefe != null)
                _jefe.OnFaseChanged -= AlCambiarFase;

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

            if (!TryActualizarObjetivoFijado(out ulong objetivoId, out bool zendreAnclado))
            {
                ObjetivoActual = null;
                return false;
            }

            if (!zendreAnclado && !EstaEnRangoDeAtaque(objetivoId, perfil.Rango))
            {
                ObjetivoActual = null;
                Debug.Log(
                    $"[AtaquesArek] {tipo}: ningún jugador en rango ({perfil.Rango:F0} m); no ataca."
                );
                return false;
            }

            ObjetivoActual = objetivoId;
            Debug.Log($"[AtaquesArek] {tipo} contra cliente {objetivoId} (objetivo fijado).");
            return true;
        }

        /// <summary>
        /// Solo servidor: objetivo fijado por cercanía mientras siga en el radio de detección.
        /// Si Zendre tiene un ojo anclado, se prioriza a Zendre. Lo usa BossGiroHaciaObjetivo.
        /// </summary>
        public bool TryObjetivoVisible(out ulong objetivoId)
        {
            return TryActualizarObjetivoFijado(out objetivoId, out _);
        }

        public bool TryObjetivoMovimiento(out ulong objetivoId, out bool zendreAnclado)
        {
            return TryActualizarObjetivoFijado(out objetivoId, out zendreAnclado);
        }

        private bool TryActualizarObjetivoFijado(out ulong objetivoId, out bool zendreAnclado)
        {
            objetivoId = 0;
            zendreAnclado = false;
            if (!IsServer || _perfilObjetivo == null || _agro == null)
                return false;

            ArmarCandidatos();
            _candidatosMovimiento.Clear();
            foreach (var candidato in _candidatos)
            {
                if (candidato.Distancia <= _perfilObjetivo.radioDeteccion)
                    _candidatosMovimiento.Add(
                        new BossTargetCandidate(candidato.Id, candidato.Distancia, 0f)
                    );
            }

            ulong? anclado = null;
            if (TryObjetivoAnclado(out ulong idAnclado))
            {
                anclado = idAnclado;
                zendreAnclado = true;
            }

            return _logicaMovimiento.TryElegirObjetivo(
                _candidatosMovimiento,
                anclado,
                out objetivoId
            );
        }

        private bool EstaEnRangoDeAtaque(ulong objetivoId, float rangoAtaque)
        {
            float alcance = Mathf.Min(rangoAtaque, _perfilObjetivo.radioDeteccion);
            foreach (var candidato in _candidatosMovimiento)
            {
                if (candidato.Id == objetivoId && candidato.Distancia <= alcance)
                    return true;
            }
            return false;
        }

        private bool TryObjetivoAnclado(out ulong objetivoId)
        {
            objetivoId = 0;
            ListarJugadores(_jugadores);
            foreach (var jugador in _jugadores)
            {
                if (jugador.CharacterId != CharacterIds.Zendre)
                    continue;

                var launcher = jugador.GetComponentInChildren<AnchorLauncher>();
                var ancla = launcher != null ? launcher.AnclaActual : null;
                if (ancla == null || !ancla.IsSpawned || !ancla.Pegada.Value || !ancla.PegadaEnOjo)
                    continue;

                objetivoId = jugador.OwnerClientId;
                return true;
            }

            return false;
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

        // Distancia en XZ desde el jefe y daño reciente de cada jugador spawneado.
        private void ArmarCandidatos()
        {
            _candidatos.Clear();
            if (_perfilObjetivo != null)
                _agro.Ventana = _perfilObjetivo.ventanaAgro;
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
            _ataquesLanzados++;
            HundirColumnasVencidas();

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

        /// <summary>
        /// Solo servidor: spawnea una columna con la base en el punto de impacto. Si se pasa del
        /// máximo, la más vieja se hunde.
        /// </summary>
        public void SpawnearColumna(Vector3 punto)
        {
            if (!IsServer)
                return;

            if (_prefabColumnaMortero == null || _perfilMortero == null)
            {
                Debug.LogError(
                    "[AtaquesArek] _prefabColumnaMortero o _perfilMortero no está asignado en el Inspector."
                );
                return;
            }

            GameObject go = Instantiate(_prefabColumnaMortero, punto, Quaternion.identity);
            var columna = go.GetComponent<ColumnaMortero>();
            var no = go.GetComponent<NetworkObject>();
            if (columna == null || no == null)
            {
                Debug.LogError(
                    "[AtaquesArek] _prefabColumnaMortero necesita ColumnaMortero y NetworkObject."
                );
                Destroy(go);
                return;
            }

            columna.AsignarAltura(
                UnityEngine.Random.Range(
                    _perfilMortero.alturaColumnaMin,
                    _perfilMortero.alturaColumnaMax
                )
            );
            no.Spawn();

            _columnas.Add(
                new ColumnaViva { Columna = columna, AtaqueDeNacimiento = _ataquesLanzados }
            );

            int maximo = Mathf.Max(1, _perfilMortero.maxColumnas);
            while (_columnas.Count > maximo)
                HundirColumna(0);
        }

        // Se quitan al empezar el ataque número ataquesDeVidaColumna después del Mortero que las creó.
        private void HundirColumnasVencidas()
        {
            if (_perfilMortero == null)
                return;

            for (int i = _columnas.Count - 1; i >= 0; i--)
            {
                int edad = _ataquesLanzados - _columnas[i].AtaqueDeNacimiento;
                if (edad >= _perfilMortero.ataquesDeVidaColumna)
                    HundirColumna(i);
            }
        }

        private void HundirColumna(int indice)
        {
            ColumnaMortero columna = _columnas[indice].Columna;
            _columnas.RemoveAt(indice);
            if (columna != null)
                columna.Hundir();
        }

        // TW-116: opcional, detrás de limpiarColumnasAlCambiarFase.
        private void AlCambiarFase(int anterior, int actual)
        {
            if (!IsServer || _perfilMortero == null || !_perfilMortero.limpiarColumnasAlCambiarFase)
                return;

            for (int i = _columnas.Count - 1; i >= 0; i--)
                HundirColumna(i);
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
