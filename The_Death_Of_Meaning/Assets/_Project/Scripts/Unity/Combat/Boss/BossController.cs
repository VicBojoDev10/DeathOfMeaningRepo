using System;
using System.Collections.Generic;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class BossController : NetworkBehaviour
    {
        [Header("Fase de Combate")]
        public NetworkVariable<int> Fase = new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public event Action<int, int> OnFaseChanged;

        [Header("Puntos Débiles (Ojos)")]
        [SerializeField]
        private HitZone[] _hitZones;

        public HitZone[] HitZones => _hitZones;

        [Header("Phase System")]
        [SerializeField]
        private BossPhaseProfile[] _fases;

        [SerializeField]
        private float _vidaMaxima = 1000f;

        private BossPhaseStateMachine _maquina;

        private readonly Dictionary<
            HitZone,
            NetworkVariable<float>.OnValueChangedDelegate
        > _hitZoneListeners = new();

        [Header("Patrón de ataques")]
        [SerializeField]
        private float _cooldownEntreAtaques = 3f;

        [SerializeField]
        private float _duracionTelegraph = 1.25f;

        // TW-96: lanzador de los ataques reales; si queda vacío se toma del mismo objeto.
        [SerializeField]
        private AtaquesArek _ataques;

        // Solo existe en el servidor. Decide cuándo avisar (Telegraph) y cuándo golpear (Impacto).
        private BossAttackScheduler _scheduler;

        // Posición dentro de la secuencia de la fase actual; solo para mostrar el siguiente ataque.
        private int _indiceEnFase;

        [Header("Debug UI")]
        [SerializeField]
        private bool _mostrarDebugUI = true;

        private void Awake()
        {
            if (_hitZones == null || _hitZones.Length == 0)
            {
                _hitZones = GetComponentsInChildren<HitZone>();
            }
        }

        public override void OnNetworkSpawn()
        {
            Fase.OnValueChanged += HandleFaseChanged;

            Debug.Log(
                $"[BossController] Spawneado en red. Fase inicial: {Fase.Value}, IsServer: {IsServer}"
            );

            if (!IsServer)
                return;

            if (_fases == null || _fases.Length == 0)
            {
                Debug.LogError("[BossController] No hay BossPhaseProfile configurados.");
                return;
            }

            _maquina = new BossPhaseStateMachine(_fases);
            _scheduler = new BossAttackScheduler(
                _maquina,
                _cooldownEntreAtaques,
                _duracionTelegraph
            );
            _indiceEnFase = 0;

            // TW-96
            if (_ataques == null)
                _ataques = GetComponent<AtaquesArek>();

            if (_hitZones == null)
                return;

            foreach (var hz in _hitZones)
            {
                if (hz == null)
                    continue;

                NetworkVariable<float>.OnValueChangedDelegate listener = (anterior, actual) =>
                {
                    OnDanioZona(actual - anterior);
                };

                hz.DanioAcumulado.OnValueChanged += listener;
                _hitZoneListeners[hz] = listener;
            }
        }

        public override void OnNetworkDespawn()
        {
            Fase.OnValueChanged -= HandleFaseChanged;

            foreach (var pair in _hitZoneListeners)
            {
                if (pair.Key != null)
                {
                    pair.Key.DanioAcumulado.OnValueChanged -= pair.Value;
                }
            }

            _hitZoneListeners.Clear();
        }

        private void HandleFaseChanged(int anterior, int actual)
        {
            Debug.Log($"[BossController] Fase cambió de {anterior} a {actual}");
            OnFaseChanged?.Invoke(anterior, actual);
        }

        private void OnDanioZona(float delta)
        {
            if (!IsServer)
                return;

            if (delta <= 0f)
                return;

            if (_maquina == null)
                return;

            float porcentajeDanio = delta / _vidaMaxima;

            Debug.Log($"[BossController] Daño recibido: {delta:F1} ({porcentajeDanio:P2})");

            _maquina.AplicarDaño(porcentajeDanio);

            if (_maquina.FaseActual != Fase.Value)
            {
                Debug.Log($"[BossController] Cambio de fase detectado: {_maquina.FaseActual}");

                CambiarFase(_maquina.FaseActual);

                // La máquina ya reinició el índice; el scheduler reinicia el tiempo.
                _scheduler?.ReiniciarPorCambioDeFase();
                _indiceEnFase = 0;
            }
        }

        private void Update()
        {
            // Toda la lógica del jefe corre únicamente en el servidor.
            if (!IsServer || _scheduler == null)
                return;

            var evento = _scheduler.Tick(Time.deltaTime, out var ataque);

            switch (evento)
            {
                case EventoAtaque.Telegraph:
                    TelegraphAtaqueRpc(ataque);
                    // TW-96: el ataque se lanza en el Telegraph; cada prefab trae su propio aviso.
                    if (_ataques != null)
                        _ataques.Lanzar(ataque);
                    break;
                case EventoAtaque.Impacto:
                    ImpactoAtaqueRpc(ataque);
                    _indiceEnFase++;
                    break;
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void TelegraphAtaqueRpc(BossAttackKind ataque)
        {
            Debug.Log($"[BossController][RPC Telegraph] Aviso de ataque: {ataque}");
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void ImpactoAtaqueRpc(BossAttackKind ataque)
        {
            Debug.Log($"[BossController][RPC Impacto] Golpe: {ataque}");
        }

        public void CambiarFase(int nuevaFase)
        {
            if (!IsServer)
                return;

            Debug.Log($"[BossController][SERVER] Cambiando fase a: {nuevaFase}");
            Fase.Value = nuevaFase;
        }

        public void RegistrarDanioEnZona(int index, float cantidad)
        {
            if (!IsServer)
                return;

            if (_hitZones != null && index >= 0 && index < _hitZones.Length)
            {
                _hitZones[index].RegistrarDanio(cantidad);
            }
        }

        // Ataque que viene después del actual en la secuencia de la fase (solo para el debug UI).
        private string SiguienteAtaqueParaDebug()
        {
            if (_maquina == null)
                return "-";

            var orden = _fases[_maquina.FaseActual - 1].OrdenDeAtaque;
            if (orden == null || orden.Length == 0)
                return "-";

            int indiceSiguiente = (_indiceEnFase + 1) % orden.Length;
            return orden[indiceSiguiente].Tipo.ToString();
        }

        private void OnGUI()
        {
            if (!_mostrarDebugUI || !IsSpawned)
                return;

            GUILayout.BeginArea(new Rect(10, 200, 320, 480), "Boss AREK Debug", GUI.skin.window);

            string rol = IsServer
                ? (IsHost ? "Host (Server + Client)" : "Dedicated Server")
                : "Client";
            GUILayout.Label($"Rol: {rol}");
            GUILayout.Label($"Fase Actual (NetworkVariable): {Fase.Value}");

            if (IsServer && _scheduler != null)
            {
                string etapa = _scheduler.EnTelegraph ? "Telegraph" : "Cooldown";
                GUILayout.Label($"Ataque actual: {_scheduler.AtaqueActual} ({etapa})");
                GUILayout.Label($"Siguiente: {SiguienteAtaqueParaDebug()}");
                GUILayout.Label($"Tiempo restante: {_scheduler.TiempoRestante:F2} s");
            }
            else
            {
                GUILayout.Label("Patrón de ataques: solo visible en el servidor");
            }

            GUILayout.Space(6);
            GUILayout.Label($"Puntos Débiles (HitZones: {_hitZones?.Length ?? 0}):");

            if (_hitZones != null)
            {
                for (int i = 0; i < _hitZones.Length; i++)
                {
                    var hz = _hitZones[i];
                    if (hz == null)
                        continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(
                        $"[{hz.ZoneIndex:D2}] {hz.ZoneName}: {hz.DanioAcumulado.Value:F0} dmg",
                        GUILayout.Width(180)
                    );

                    if (IsServer)
                    {
                        if (GUILayout.Button("+10", GUILayout.Width(50)))
                        {
                            hz.RegistrarDanio(10f);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndArea();
        }
    }
}
