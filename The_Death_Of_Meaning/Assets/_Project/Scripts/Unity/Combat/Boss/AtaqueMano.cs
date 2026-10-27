using System.Collections.Generic;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Gameplay.Core;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using TDOM.Unity.Vfx;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    /// <summary>
    /// Ataque Mano (Básico) de AREK. Una burbuja de lava aparece en el origen y la columna sale
    /// del jefe hasta estirarse por completo; entonces barre 180° (en un sentido que alterna en
    /// cada ataque) y se retrae rápido. El abanico de aviso queda fijo en el piso cubriendo todo
    /// el barrido. La estela del barrido es solo visual: el daño lo hace la columna, y solo
    /// mientras barre.
    /// El servidor manda la rotación (NetworkTransform) y el daño; cada máquina corre la misma
    /// línea de tiempo (LineaTiempoManotazo) para la columna, la burbuja y la estela.
    /// </summary>
    public class AtaqueMano : AtaqueArekBase
    {
        private const int SegmentosArco = 36;

        [Header("Visuales")]
        [SerializeField]
        private Renderer _manoRenderer;

        [SerializeField]
        private Transform _manoVisual;

        [Header("Aviso")]
        [SerializeField]
        private Material _materialAviso; // AvisoAtaque.mat

        [SerializeField]
        private AvisoParpadeo _avisoParpadeo;

        [Header("Burbuja y estela (TW-180)")]
        [SerializeField]
        private BurbujaLava _burbuja;

        [SerializeField]
        private EstelaEspada _estela;

        [SerializeField]
        private Transform _baseEstela;

        [SerializeField]
        private Transform _puntaEstela;

        // Tuning: lo lee de AtaquesArek (BossArek.prefab) al spawnear.
        private AtaqueManoProfile _perfil;
        private LineaTiempoManotazo _lineaTiempo;

        private float _tiempo;
        private bool _iniciado;
        private bool _terminado;
        private bool _estelaPermitida;
        private int _sentido = 1;
        private Quaternion _rotacionBase = Quaternion.identity;
        private readonly HashSet<ulong> _jugadoresGolpeados = new HashSet<ulong>();

        // Flash de pantalla en el cliente golpeado
        private static float _flashRojoTimer = 0f;

        // Todos los clientes: abanico de aviso en el piso, fuera de la jerarquía porque la
        // columna rota y el abanico debe quedarse fijo.
        private GameObject _arco;
        private LineRenderer _arcLineRenderer;

        /// <summary>Solo servidor: sentido del barrido (+1 o -1), antes del Spawn.</summary>
        public void AsignarSentido(int sentido)
        {
            _sentido = sentido < 0 ? -1 : 1;
        }

        private void Awake()
        {
            if (_manoRenderer == null)
                _manoRenderer = GetComponentInChildren<Renderer>();

            if (_manoVisual == null && _manoRenderer != null)
                _manoVisual = _manoRenderer.transform;

            if (_avisoParpadeo == null)
                _avisoParpadeo = GetComponent<AvisoParpadeo>();
        }

        // La columna sale del pivote: su largo crece de 0 al radio del perfil y el brazo conserva
        // sus proporciones (grosor según el largo, con tope). Solo es visual: el daño usa
        // ancho, alto y radio del perfil.
        private void ActualizarColumna(float longitud)
        {
            float largo = longitud * _perfil.radio;
            bool visible = largo > 0.01f;

            if (_manoRenderer != null && _manoRenderer.enabled != visible)
                _manoRenderer.enabled = visible;

            float grosorAncho = GeometriaManotazo.Grosor(
                largo,
                GeometriaManotazo.ProporcionAncho,
                _perfil.grosorBrazo,
                _perfil.ancho * _perfil.grosorMaximo
            );
            float grosorAlto = GeometriaManotazo.Grosor(
                largo,
                GeometriaManotazo.ProporcionAlto,
                _perfil.grosorBrazo,
                _perfil.alto * _perfil.grosorMaximo
            );

            if (_manoVisual != null)
            {
                _manoVisual.localScale = new Vector3(
                    Mathf.Max(grosorAncho, 0.001f),
                    Mathf.Max(grosorAlto, 0.001f),
                    Mathf.Max(largo, 0.001f)
                );
                _manoVisual.localPosition = new Vector3(0f, grosorAlto * 0.5f, largo * 0.5f);
            }

            // La estela va por el centro del brazo.
            if (_baseEstela != null)
                _baseEstela.localPosition = new Vector3(0f, grosorAlto * 0.5f, 0f);
            if (_puntaEstela != null)
                _puntaEstela.localPosition = new Vector3(0f, grosorAlto * 0.5f, largo);
        }

        private void ActualizarVisual(EstadoManotazo estado)
        {
            ActualizarColumna(estado.Longitud);

            if (_burbuja != null)
                _burbuja.Establecer(estado.Burbuja);

            // La estela solo deja rastro mientras barre; al salir del barrido se corta el tramo.
            bool barriendo = estado.Fase == FaseManotazo.Barrido;
            if (_estela != null)
            {
                _estela.Permitir(barriendo);
                if (_estelaPermitida && !barriendo)
                    _estela.Cortar();
            }
            _estelaPermitida = barriendo;
        }

        protected override void AlSpawnear()
        {
            _perfil = Lanzador != null ? Lanzador.PerfilMano : null;
            if (_perfil == null)
            {
                Debug.LogError("[AtaqueMano] Falta el AtaqueManoProfile en AtaquesArek.");
                return;
            }

            _lineaTiempo = new LineaTiempoManotazo(
                _perfil.aviso,
                _perfil.fraccionBurbuja,
                _perfil.barrido,
                _perfil.retirada
            );

            if (_burbuja != null)
                _burbuja.Configurar(_perfil.tamanoBurbuja);
            ActualizarVisual(_lineaTiempo.Evaluar(0f, _sentido));

            if (IsServer)
            {
                // Sale a la altura del piso (más alturaOrigen), no a la del pivote del jefe.
                Vector3 origen = transform.position;
                float piso = AlturaDelPiso(origen);
                origen.y = piso + _perfil.alturaOrigen;
                transform.position = origen;

                _tiempo = 0f;
                _iniciado = true;
                _rotacionBase = transform.rotation;
                transform.rotation = _rotacionBase * Quaternion.Euler(0f, -90f * _sentido, 0f);
                _jugadoresGolpeados.Clear();

                AvisoRpc(origen, piso, _rotacionBase, _sentido);
            }
        }

        public override void OnNetworkDespawn()
        {
            DestruirArco();
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            DestruirArco();
            base.OnDestroy();
        }

        private void DestruirArco()
        {
            if (_arco != null)
                Destroy(_arco);
        }

        // Busca el piso debajo del punto; si hay varios golpes (jefe, jugadores), el más bajo es el piso.
        private static float AlturaDelPiso(Vector3 punto)
        {
            var golpes = Physics.RaycastAll(
                punto + Vector3.up * 30f,
                Vector3.down,
                100f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            if (golpes.Length == 0)
                return punto.y;

            float alturaMinima = float.MaxValue;
            foreach (var golpe in golpes)
                alturaMinima = Mathf.Min(alturaMinima, golpe.point.y);

            return alturaMinima;
        }

        private void Update()
        {
            if (
                PauseManager.PausadoGlobal
                || (PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso)
            )
                return;
            if (!_iniciado || _perfil == null)
                return;

            _tiempo += Time.deltaTime;
            EstadoManotazo estado = _lineaTiempo.Evaluar(_tiempo, _sentido);
            ActualizarVisual(estado);

            if (!IsServer)
                return;

            transform.rotation = _rotacionBase * Quaternion.Euler(0f, estado.Angulo, 0f);

            // Daño solo en el barrido, con la columna completa.
            if (estado.DanoActivo)
                DetectarImpactos();

            // Tras retraerse espera a que la estela se desvanezca antes de despawnear.
            if (!_terminado && _tiempo >= _lineaTiempo.Duracion + _perfil.retrasoDespawn)
            {
                _terminado = true;
                var no = GetComponent<NetworkObject>();
                if (no != null && no.IsSpawned)
                    no.Despawn(true);
                else
                    Destroy(gameObject);
            }
        }

        // Caja de daño (ver GeometriaManotazo.BandaDano): a ras del piso o pared, según el perfil.
        private Vector3 MitadCajaDano() =>
            new Vector3(_perfil.ancho * 0.5f, Banda().Mitad, _perfil.radio * 0.5f);

        private Vector3 CentroCajaDano() =>
            transform.position
            + transform.rotation * new Vector3(0f, Banda().Centro, _perfil.radio * 0.5f);

        private (float Centro, float Mitad) Banda() =>
            GeometriaManotazo.BandaDano(_perfil.alto, _perfil.alturaOrigen, _perfil.danoAlPiso);

        private void DetectarImpactos()
        {
            Vector3 halfExtents = MitadCajaDano();
            Vector3 center = CentroCajaDano();
            Quaternion orientation = transform.rotation;

            Collider[] hits = Physics.OverlapBox(
                center,
                halfExtents,
                orientation,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    var netObj = hit.GetComponentInParent<NetworkObject>();
                    ulong clientId = netObj != null ? netObj.OwnerClientId : 0;
                    if (!_jugadoresGolpeados.Contains(clientId))
                    {
                        _jugadoresGolpeados.Add(clientId);
                        Debug.Log($"[AREK][Mano] golpeó a cliente {clientId}");
                        damageable.RecibirDanio(_perfil.Dano, _perfil.EsInstaKill);
                        if (netObj != null)
                            JugadorGolpeadoRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
                    }
                }
            }
        }

        // Llega a todas las máquinas con lo que necesitan para dibujar el aviso y llevar la
        // misma línea de tiempo: origen, piso (el abanico va ahí), rotación base y sentido.
        [Rpc(SendTo.ClientsAndHost)]
        private void AvisoRpc(Vector3 origen, float piso, Quaternion rotacionBase, int sentido)
        {
            if (_perfil == null)
                return;

            _sentido = sentido;

            if (!IsServer)
            {
                _rotacionBase = rotacionBase;
                _tiempo = 0f;
                _iniciado = true;
            }

            // El brazo usa el material del jefe (Shader Graph): solo se tiñe si tiene _Color.
            if (_manoRenderer != null && _manoRenderer.sharedMaterial.HasProperty("_Color"))
                _manoRenderer.material.color = Color.red;

            CrearArcoTelegraph(new Vector3(origen.x, piso, origen.z), rotacionBase);

            if (_avisoParpadeo != null && _perfil != null)
                _avisoParpadeo.Iniciar(
                    Lanzador.PerfilAviso,
                    _perfil.aviso,
                    new Renderer[] { _arcLineRenderer },
                    AvisoParpadeo.Forma.FranjaV
                );
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void JugadorGolpeadoRpc(RpcParams rpcParams = default)
        {
            _flashRojoTimer = 0.25f;
            NotificarGolpeAlJugadorLocal();
        }

        // Abanico de -90° a +90° sobre el piso, en coordenadas del mundo: no rota con la columna.
        private void CrearArcoTelegraph(Vector3 origen, Quaternion rotacionBase)
        {
            DestruirArco();

            _arco = new GameObject("ArcoAvisoMano");
            _arcLineRenderer = _arco.AddComponent<LineRenderer>();
            _arcLineRenderer.useWorldSpace = true;
            _arcLineRenderer.loop = false;
            _arcLineRenderer.startWidth = 0.4f;
            _arcLineRenderer.endWidth = 0.4f;

            Vector3 piso = origen + Vector3.up * 0.05f;
            Vector3[] posiciones = new Vector3[SegmentosArco + 2];
            posiciones[0] = piso;

            for (int i = 0; i <= SegmentosArco; i++)
            {
                float angulo = Mathf.Lerp(-90f, 90f, (float)i / SegmentosArco);
                Quaternion giro = rotacionBase * Quaternion.Euler(0f, angulo, 0f);
                posiciones[i + 1] = piso + giro * Vector3.forward * _perfil.radio;
            }

            _arcLineRenderer.positionCount = posiciones.Length;
            _arcLineRenderer.SetPositions(posiciones);

            if (_materialAviso != null)
                _arcLineRenderer.sharedMaterial = _materialAviso;
            else
                Debug.LogWarning("[AtaqueMano] Falta asignar _materialAviso (AvisoAtaque.mat).");
        }

        private void OnGUI()
        {
            if (_flashRojoTimer > 0f)
            {
                _flashRojoTimer -= Time.deltaTime;
                Color prev = GUI.color;
                GUI.color = new Color(1f, 0f, 0f, 0.4f);
                GUI.DrawTexture(
                    new Rect(0, 0, Screen.width, Screen.height),
                    Texture2D.whiteTexture
                );
                GUI.color = prev;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_perfil == null)
                return;

            Gizmos.color = Color.red;
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(CentroCajaDano(), transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, MitadCajaDano() * 2f);
            Gizmos.matrix = prev;
        }
    }
}
