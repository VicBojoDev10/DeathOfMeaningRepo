using System;
using System.Collections;
using System.Collections.Generic;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using TDOM.Unity.Player;
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

        public static event Action<BossController> Spawned;

        public event Action<int, int> OnFaseChanged;

        private bool _pausado;

        public void Pausar(bool pausado) => _pausado = pausado;

        public bool Pausado => _pausado || PauseManager.PausadoGlobal;

        private bool EnTransicion => _maquina != null && _maquina.EnTransicion;

        [Header("Puntos Débiles (Ojos)")]
        [SerializeField]
        private HitZone[] _hitZones;

        public HitZone[] HitZones => _hitZones;

        [SerializeField]
        private OjosArek _ojos;

        private bool _ojosListos;

        [Header("Phase System")]
        [SerializeField]
        private BossPhaseProfile[] _fases;

        /// <summary>Perfil de la fase actual (null si no está configurado). Lo usa MinionSpawner.</summary>
        public BossPhaseProfile PerfilFaseActual => PerfilDeFase(Fase.Value);

        [SerializeField]
        private float _vidaMaxima = 1000f;

        private BossPhaseStateMachine _maquina;

        [Header("Patrón de ataques")]
        [SerializeField]
        private float _cooldownEntreAtaques = 3f;

        [SerializeField]
        private float _duracionTelegraph = 1.25f;

        [SerializeField]
        private AtaquesArek _ataques;
        private BossAttackScheduler _scheduler;
        private int _indiceEnFase;

        [Header("Visual por fase")]
        [SerializeField]
        private Renderer[] _renderersCuerpo;

        [SerializeField]
        private ParticleSystem _efectoTransicion;

        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColorEmision = Shader.PropertyToID("_EmissionColor");
        private const string KeywordEmision = "_EMISSION";

        private Material[] _materialesCuerpo = Array.Empty<Material>();
        private Coroutine _destello;
        private readonly List<PlayerRoot> _jugadores = new List<PlayerRoot>();

        [Header("Debug UI")]
        [SerializeField]
        private bool _mostrarDebugUI = true;

        private void Awake()
        {
            if (_hitZones == null || _hitZones.Length == 0)
            {
                _hitZones = GetComponentsInChildren<HitZone>();
            }

            PrepararMaterialesCuerpo();
        }

        public override void OnNetworkSpawn()
        {
            Fase.OnValueChanged += HandleFaseChanged;
            AplicarColorFase(Fase.Value);
            Spawned?.Invoke(this);

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
            _maquina.TransicionTerminada += AlTerminarTransicion;

            if (_ojos == null)
                _ojos = GetComponent<OjosArek>();

            _ojosListos = _ojos != null && _ojos.Preparar(_hitZones, _maquina, _fases.Length);
            if (!_ojosListos)
                return;

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

                hz.DanioRegistrado += OnDanioZona;
            }
        }

        public override void OnNetworkDespawn()
        {
            Fase.OnValueChanged -= HandleFaseChanged;

            if (_maquina != null)
                _maquina.TransicionTerminada -= AlTerminarTransicion;

            if (_hitZones == null)
                return;

            foreach (var hz in _hitZones)
            {
                if (hz == null)
                    continue;

                hz.DanioRegistrado -= OnDanioZona;
            }
        }

        private void HandleFaseChanged(int anterior, int actual)
        {
            Debug.Log($"[BossController] Fase cambió de {anterior} a {actual}");
            AplicarColorFase(actual);
            OnFaseChanged?.Invoke(anterior, actual);
        }

        private void OnDanioZona(float cantidad)
        {
            if (!IsServer)
                return;

            if (cantidad <= 0f)
                return;

            if (!_ojosListos)
                return;

            if (Pausado)
                return;

            float porcentajeDanio = cantidad / _vidaMaxima;

            Debug.Log($"[BossController] Daño recibido: {cantidad:F1} ({porcentajeDanio:P2})");

            if (!_ojos.AplicarDanio(porcentajeDanio))
                return;

            Debug.Log($"[BossController] Cambio de fase detectado: {_maquina.FaseActual}");
            EntrarAFase(_maquina.FaseActual);
        }

        private void EntrarAFase(int nuevaFase)
        {
            CambiarFase(nuevaFase);
            _indiceEnFase = 0;
            RestaurarEnergiaJugadores();
            CambioDeFaseRpc(nuevaFase);

            if (!_maquina.EnTransicion)
                AlTerminarTransicion();
        }

        private void AlTerminarTransicion()
        {
            _scheduler?.ReiniciarPorCambioDeFase();
            _indiceEnFase = 0;
        }

        private void RestaurarEnergiaJugadores()
        {
            if (_ataques == null)
            {
                Debug.LogWarning(
                    "[BossController] Sin AtaquesArek: no se pudo restaurar la energía."
                );
                return;
            }

            _ataques.ListarJugadores(_jugadores);
            foreach (var jugador in _jugadores)
                jugador.RestaurarEnergiaPorCambioDeFase();
        }

        private void Update()
        {
            if (!IsServer || _scheduler == null || Pausado)
                return;

            _maquina.Tick(Time.deltaTime);
            _ojos.Tick(Time.deltaTime);

            if (Pausado)
                return;

            var evento = _scheduler.Tick(Time.deltaTime, out var ataque);

            switch (evento)
            {
                case EventoAtaque.Telegraph:
                    TelegraphAtaqueRpc(ataque);
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

        [Rpc(SendTo.ClientsAndHost)]
        private void CambioDeFaseRpc(int fase)
        {
            if (_efectoTransicion != null)
                _efectoTransicion.Play();

            var perfil = PerfilDeFase(fase);
            if (perfil != null && _materialesCuerpo.Length > 0)
            {
                if (_destello != null)
                    StopCoroutine(_destello);
                _destello = StartCoroutine(Destello(perfil));
            }

            JugadorLocal()?.SacudidaCambioDeFase();
        }

        private void CambiarFase(int nuevaFase)
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

        private BossPhaseProfile PerfilDeFase(int fase)
        {
            if (_fases == null)
                return null;

            foreach (var perfil in _fases)
            {
                if (perfil != null && perfil.Fase == fase)
                    return perfil;
            }
            return null;
        }

        private void PrepararMaterialesCuerpo()
        {
            if (_renderersCuerpo == null || _renderersCuerpo.Length == 0)
                _renderersCuerpo = BuscarRenderersCuerpo();

            if (_renderersCuerpo.Length == 0)
            {
                Debug.LogWarning(
                    "[BossController] Sin renderers del cuerpo: no habrá color por fase ni destello."
                );
                return;
            }

            var materiales = new List<Material>();
            foreach (var r in _renderersCuerpo)
            {
                if (r == null)
                    continue;

                var material = r.material;
                if (!material.HasProperty(IdColorBase))
                    Debug.LogWarning(
                        $"[BossController] El shader '{material.shader.name}' de {r.name} no tiene _BaseColor."
                    );
                material.EnableKeyword(KeywordEmision);
                material.SetColor(IdColorEmision, Color.black);
                materiales.Add(material);
            }
            _materialesCuerpo = materiales.ToArray();
        }

        private Renderer[] BuscarRenderersCuerpo()
        {
            var encontrados = new List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                bool esMalla = r is MeshRenderer || r is SkinnedMeshRenderer;
                if (esMalla && r.enabled && r.GetComponent<HitZone>() == null)
                    encontrados.Add(r);
            }
            return encontrados.ToArray();
        }

        private void AplicarColorFase(int fase)
        {
            var perfil = PerfilDeFase(fase);
            if (perfil == null)
                return;

            foreach (var material in _materialesCuerpo)
                material.SetColor(IdColorBase, perfil.ColorFase);
        }

        private IEnumerator Destello(BossPhaseProfile perfil)
        {
            float duracion = perfil.DuracionDestello;
            float transcurrido = 0f;
            while (transcurrido < duracion)
            {
                float fuerza = perfil.IntensidadDestello * (1f - transcurrido / duracion);
                FijarEmision(perfil.ColorDestello * fuerza);
                transcurrido += Time.deltaTime;
                yield return null;
            }

            FijarEmision(Color.black);
            _destello = null;
        }

        private void FijarEmision(Color color)
        {
            foreach (var material in _materialesCuerpo)
                material.SetColor(IdColorEmision, color);
        }

        private PlayerRoot JugadorLocal()
        {
            var cliente = NetworkManager.LocalClient;
            if (cliente == null)
                return null;

            foreach (var objeto in cliente.OwnedObjects)
            {
                var jugador = objeto.GetComponent<PlayerRoot>();
                if (jugador != null)
                    return jugador;
            }
            return null;
        }

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
                if (EnTransicion)
                {
                    GUILayout.Label($"En transición ({_maquina.TiempoTransicionRestante:F1}s)");
                }
                else
                {
                    string etapa = _scheduler.EnTelegraph ? "Telegraph" : "Cooldown";
                    GUILayout.Label($"Ataque actual: {_scheduler.AtaqueActual} ({etapa})");
                    GUILayout.Label($"Siguiente: {SiguienteAtaqueParaDebug()}");
                    GUILayout.Label($"Tiempo restante: {_scheduler.TiempoRestante:F2} s");
                }
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
                    string estado =
                        _ojos != null && i < _ojos.TotalOjos ? _ojos.Estado(i).ToString() : "-";
                    GUILayout.Label(
                        $"[{hz.ZoneIndex:D2}] {estado}: {hz.DanioAcumulado.Value:F0} dmg",
                        GUILayout.Width(180)
                    );

                    if (IsServer)
                    {
                        if (GUILayout.Button("+10", GUILayout.Width(50)))
                        {
                            hz.RegistrarDanio(10f);
                        }

                        if (GUILayout.Button("Golpe", GUILayout.Width(60)))
                        {
                            hz.RegistrarImpacto(
                                10f,
                                NetworkManager.LocalClientId,
                                TipoImpactoOjo.Melee
                            );
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndArea();
        }
    }
}
