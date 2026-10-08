using System.Collections.Generic;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace TDOM.Unity
{
    /// <summary>
    /// Ataque Mortero (InstaKill) de AREK en greybox: tres disparos simultáneos. El servidor
    /// elige 3 puntos aleatorios frente al jefe (separados al menos 2 × _radioArea), todos ven
    /// un disco de aviso en cada punto, después cae una "piedra" en cada uno y el servidor
    /// detecta a los jugadores dentro. No resta vida: solo detecta y reporta (04. Daño en WIP).
    /// </summary>
    public class AtaqueMortero : NetworkBehaviour
    {
        [Header("Tiempos")]
        [SerializeField]
        private float _aviso = 1.0f;

        [SerializeField]
        private float _caida = 0.15f;

        [SerializeField]
        private float _retirada = 0.5f;

        [Header("Disparos")]
        [SerializeField]
        private int _disparos = 3;

        [SerializeField]
        private float _radioArea = 4f;

        [SerializeField]
        private float _radioArena = 25f;

        [SerializeField]
        private int _intentosPorPunto = 30;

        [SerializeField]
        private float _alturaCaida = 15f;

        [SerializeField]
        [FormerlySerializedAs("_altoTentaculo")]
        private float _altoPiedra = 6f;

        [Header("Visuales (plantillas, se clonan una vez por disparo)")]
        [SerializeField]
        private Transform _discoAviso;

        [SerializeField]
        [FormerlySerializedAs("_tentaculo")]
        private Transform _piedra;

        private enum FaseMortero
        {
            Aviso,
            Caida,
            Retirada,
            Terminado,
        }

        private FaseMortero _fase = FaseMortero.Aviso;
        private float _tiempoEnFase;
        private readonly HashSet<ulong> _golpeados = new HashSet<ulong>();

        // Solo servidor: puntos de impacto para la detección.
        private Vector3[] _puntosServidor;

        // Todos los clientes: clones visuales creados a partir de AvisoRpc.
        private readonly List<Transform> _discos = new List<Transform>();
        private readonly List<Transform> _piedras = new List<Transform>();
        private readonly List<Vector3> _puntosLocales = new List<Vector3>();
        private float _tiempoCaidaLocal = -1f;

        private float _flashGolpe;

        private void Awake()
        {
            // Los visuales son solo decoración: sin colliders para no empujar jugadores.
            foreach (var colisionador in GetComponentsInChildren<Collider>(true))
                colisionador.enabled = false;

            // Las plantillas no se ven; solo sus clones.
            if (_discoAviso != null)
                _discoAviso.gameObject.SetActive(false);
            if (_piedra != null)
                _piedra.gameObject.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            _puntosServidor = ElegirPuntos();
            _fase = FaseMortero.Aviso;
            _tiempoEnFase = 0f;
            _golpeados.Clear();

            AvisoRpc(_puntosServidor);
        }

        // Se spawnea en la posición y rotación del jefe; los puntos caen en su semicírculo frontal.
        private Vector3[] ElegirPuntos()
        {
            Vector3 adelante = transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.0001f)
                adelante = Vector3.forward;
            adelante.Normalize();

            int cantidad = Mathf.Max(1, _disparos);
            float separacionMinima = 2f * _radioArea;
            var puntos = new List<Vector3>(cantidad);

            for (int i = 0; i < cantidad; i++)
            {
                Vector3 candidato = PuntoAleatorio(adelante);
                for (int intento = 1; intento < _intentosPorPunto; intento++)
                {
                    if (EstaSeparado(candidato, puntos, separacionMinima))
                        break;
                    candidato = PuntoAleatorio(adelante);
                }
                // Si la arena es muy chica y no se logra la separación, se acepta el último.
                puntos.Add(candidato);
            }

            return puntos.ToArray();
        }

        private Vector3 PuntoAleatorio(Vector3 adelante)
        {
            float angulo = Random.Range(-90f, 90f);
            // Raíz cuadrada para repartir parejo por el área, y nunca debajo del jefe.
            float distancia = Mathf.Lerp(_radioArea, _radioArena, Mathf.Sqrt(Random.value));

            Vector3 punto =
                transform.position + Quaternion.Euler(0f, angulo, 0f) * adelante * distancia;
            punto.y = AlturaDelPiso(punto);
            return punto;
        }

        private static bool EstaSeparado(Vector3 candidato, List<Vector3> puntos, float minimo)
        {
            foreach (var punto in puntos)
            {
                Vector2 a = new Vector2(candidato.x, candidato.z);
                Vector2 b = new Vector2(punto.x, punto.z);
                if (Vector2.Distance(a, b) < minimo)
                    return false;
            }
            return true;
        }

        // Busca el piso debajo del punto; si hay varios golpes (jefe, jugadores), el más bajo es el piso.
        private float AlturaDelPiso(Vector3 punto)
        {
            var golpes = Physics.RaycastAll(
                punto + Vector3.up * 30f,
                Vector3.down,
                100f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            if (golpes.Length == 0)
                return transform.position.y;

            float alturaMinima = float.MaxValue;
            foreach (var golpe in golpes)
                alturaMinima = Mathf.Min(alturaMinima, golpe.point.y);

            return alturaMinima;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            AnimarCaidaLocal(dt);

            if (_flashGolpe > 0f)
                _flashGolpe -= dt;

            if (!IsServer || !IsSpawned)
                return;

            _tiempoEnFase += dt;

            switch (_fase)
            {
                case FaseMortero.Aviso:
                    if (_tiempoEnFase >= _aviso)
                    {
                        CambiarFase(FaseMortero.Caida);
                        CaidaRpc();
                    }
                    break;

                case FaseMortero.Caida:
                    if (_tiempoEnFase >= _caida)
                    {
                        DetectarImpactos();
                        CambiarFase(FaseMortero.Retirada);
                    }
                    break;

                case FaseMortero.Retirada:
                    if (_tiempoEnFase >= _retirada)
                    {
                        CambiarFase(FaseMortero.Terminado);
                        NetworkObject.Despawn(true);
                    }
                    break;
            }
        }

        private void CambiarFase(FaseMortero nueva)
        {
            _fase = nueva;
            _tiempoEnFase = 0f;
        }

        private void AnimarCaidaLocal(float dt)
        {
            if (_tiempoCaidaLocal < 0f)
                return;

            _tiempoCaidaLocal += dt;
            float t = Mathf.Clamp01(_tiempoCaidaLocal / _caida);
            float altura = Mathf.Lerp(_alturaCaida, 0f, t);

            for (int i = 0; i < _piedras.Count; i++)
                ColocarPiedra(_piedras[i], _puntosLocales[i], altura);
        }

        // Coloca la base de la piedra a la altura indicada sobre su punto de impacto.
        private void ColocarPiedra(Transform piedra, Vector3 punto, float alturaBase)
        {
            piedra.position = punto + Vector3.up * (alturaBase + _altoPiedra * 0.5f);
        }

        private void DetectarImpactos()
        {
            if (_puntosServidor == null)
                return;

            foreach (var punto in _puntosServidor)
            {
                Collider[] dentro = Physics.OverlapSphere(
                    punto,
                    _radioArea,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide
                );

                foreach (var colisionador in dentro)
                {
                    var jugador = colisionador.GetComponentInParent<PlayerRoot>();
                    if (jugador == null)
                        continue;

                    ulong id = jugador.OwnerClientId;
                    if (!_golpeados.Add(id))
                        continue; // cada jugador solo una vez por ataque, aunque esté en dos círculos

                    Debug.Log($"[AREK][Mortero] golpeó a cliente {id}");
                    JugadorGolpeadoRpc(RpcTarget.Single(id, RpcTargetUse.Temp));
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AvisoRpc(Vector3[] puntos)
        {
            _puntosLocales.Clear();
            _puntosLocales.AddRange(puntos);

            foreach (var punto in puntos)
            {
                if (_discoAviso != null)
                {
                    var disco = Instantiate(_discoAviso, transform);
                    disco.localScale = new Vector3(_radioArea * 2f, 0.02f, _radioArea * 2f);
                    disco.position = punto + Vector3.up * 0.05f;
                    disco.gameObject.SetActive(true);
                    _discos.Add(disco);
                }

                if (_piedra != null)
                {
                    var piedra = Instantiate(_piedra, transform);
                    // El cilindro primitivo de Unity mide 2 de alto con escala 1.
                    piedra.localScale = new Vector3(1.5f, _altoPiedra * 0.5f, 1.5f);
                    ColocarPiedra(piedra, punto, _alturaCaida);
                    _piedras.Add(piedra);
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void CaidaRpc()
        {
            foreach (var piedra in _piedras)
                piedra.gameObject.SetActive(true);

            _tiempoCaidaLocal = 0f;
        }

        // Mismo feedback que la Mano: impacto de cámara, flash de debug y flash rojo de pantalla.
        [Rpc(SendTo.SpecifiedInParams)]
        private void JugadorGolpeadoRpc(RpcParams rpcParams = default)
        {
            _flashGolpe = 0.25f;

            var jugadorLocal = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (jugadorLocal == null)
                return;

            var rigCamara = jugadorLocal.GetComponentInChildren<PlayerCameraRig>();
            if (rigCamara != null)
                rigCamara.Impacto(2.5f);

            var feedbackDebug = jugadorLocal.GetComponentInChildren<CombatDebugFeedback>();
            if (feedbackDebug != null)
                feedbackDebug.FlashActive(0.2f);
        }

        private void OnGUI()
        {
            if (_flashGolpe <= 0f)
                return;

            Color colorAnterior = GUI.color;
            GUI.color = new Color(1f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = colorAnterior;
        }

        private void OnDrawGizmosSelected()
        {
            if (_puntosServidor == null)
                return;

            Gizmos.color = Color.red;
            foreach (var punto in _puntosServidor)
                Gizmos.DrawWireSphere(punto, _radioArea);
        }
    }
}
