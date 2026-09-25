using System;
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

        [Header("Dummy AI (Placeholder F1)")]
        [SerializeField]
        private bool _dummyAiActivo = true;

        [SerializeField]
        private float _intervaloAtaqueDummy = 4f;

        private float _timerAtaqueDummy;
        private int _dummyAttackIndex = 0;

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
        }

        public override void OnNetworkDespawn()
        {
            Fase.OnValueChanged -= HandleFaseChanged;
        }

        private void HandleFaseChanged(int anterior, int actual)
        {
            Debug.Log($"[BossController] Fase cambió de {anterior} a {actual}");
            OnFaseChanged?.Invoke(anterior, actual);
        }

        private void Update()
        {
            // Toda lógica de IA y comportamiento del jefe corre únicamente en el servidor
            if (!IsServer)
                return;

            if (_dummyAiActivo)
            {
                TickDummyAI(Time.deltaTime);
            }
        }

        private void TickDummyAI(float dt)
        {
            if (!IsServer)
                return;

            _timerAtaqueDummy += dt;
            if (_timerAtaqueDummy >= _intervaloAtaqueDummy)
            {
                _timerAtaqueDummy = 0f;
                _dummyAttackIndex = (_dummyAttackIndex % 3) + 1;
                TelegraphAtaqueRpc(_dummyAttackIndex, $"AtaqueDummy_{_dummyAttackIndex}");
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void TelegraphAtaqueRpc(int ataqueId, string ataqueNombre = "")
        {
            Debug.Log(
                $"[BossController][RPC Telegraph] Aviso de ataque recibido: ID={ataqueId} ({ataqueNombre})"
            );
            // Placeholder: en PR de integración F1 se disparará aquí la animación/VFX del telegraph para clientes
        }

        public void CambiarFase(int nuevaFase)
        {
            if (!IsServer)
                return;

            Debug.Log($"[BossController][SERVER] Cambiando fase a: {nuevaFase}");
            Fase.Value = nuevaFase;
        }

        [ContextMenu("Siguiente Fase")]
        public void SiguienteFase()
        {
            if (!IsServer)
                return;

            CambiarFase(Fase.Value + 1);
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

        private void OnGUI()
        {
            if (!_mostrarDebugUI || !IsSpawned)
                return;

            GUILayout.BeginArea(new Rect(10, 200, 320, 420), "Boss AREK Debug", GUI.skin.window);

            string rol = IsServer
                ? (IsHost ? "Host (Server + Client)" : "Dedicated Server")
                : "Client";
            GUILayout.Label($"Rol: {rol}");
            GUILayout.Label($"Fase Actual (NetworkVariable): {Fase.Value}");

            if (IsServer)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Fase +1"))
                {
                    CambiarFase(Fase.Value + 1);
                }
                if (GUILayout.Button("Fase 1"))
                {
                    CambiarFase(1);
                }
                if (GUILayout.Button("Telegraph"))
                {
                    _dummyAttackIndex = (_dummyAttackIndex % 3) + 1;
                    TelegraphAtaqueRpc(_dummyAttackIndex, $"ManualTelegraph_{_dummyAttackIndex}");
                }
                GUILayout.EndHorizontal();
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
