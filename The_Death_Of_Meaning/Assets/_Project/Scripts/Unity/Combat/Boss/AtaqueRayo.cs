using System.Collections.Generic;
using TDOM.Data;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    /// <summary>
    /// Ataque Rayo (Especial) de AREK en greybox. El servidor elige un patrón de líneas en
    /// abanico frente al jefe, todos ven franjas de aviso parpadeantes en el piso y después los
    /// rayos (LineRenderer) desde la boca. El servidor detecta a los jugadores dentro de cada rayo.
    /// No resta vida: solo detecta y reporta (04. Daño en WIP).
    /// </summary>
    public class AtaqueRayo : AtaqueArekBase
    {
        [Header("Origen")]
        [SerializeField]
        private Transform _boca; // si es null: centro del jefe + alturaBocaPorDefecto del perfil

        [Header("Visuales")]
        [SerializeField]
        private Material _materialAviso; // AvisoAtaque.mat

        [SerializeField]
        private AvisoParpadeo _avisoParpadeo;

        [SerializeField]
        private Material _materialRayo;

        private enum FaseRayo
        {
            Aviso,
            Activo,
            Retirada,
            Terminado,
        }

        // Tuning: lo lee de AtaquesArek (BossArek.prefab) al spawnear.
        private AtaqueRayoProfile _perfil;

        private FaseRayo _fase = FaseRayo.Aviso;
        private float _tiempoEnFase;
        private readonly HashSet<ulong> _golpeados = new HashSet<ulong>();

        // Solo servidor: geometría del patrón para la detección.
        private Vector3 _origenServidor;
        private Vector3[] _finalesServidor;

        // Todos los clientes: visuales creados a partir de AvisoRpc.
        private readonly List<GameObject> _franjas = new List<GameObject>();
        private readonly List<LineRenderer> _rayos = new List<LineRenderer>();

        private float _flashGolpe;

        /// <summary>Lo llama AtaquesArek antes de Spawn para indicar la boca del jefe.</summary>
        public void AsignarBoca(Transform boca)
        {
            _boca = boca;
        }

        private void Awake()
        {
            if (_avisoParpadeo == null)
                _avisoParpadeo = GetComponent<AvisoParpadeo>();
        }

        protected override void AlSpawnear()
        {
            _perfil = Lanzador != null ? Lanzador.PerfilRayo : null;
            if (_perfil == null)
            {
                Debug.LogError("[AtaqueRayo] Falta el AtaqueRayoProfile en AtaquesArek.");
                return;
            }

            if (!IsServer)
                return;

            _origenServidor =
                _boca != null
                    ? _boca.position
                    : transform.position + Vector3.up * _perfil.alturaBocaPorDefecto;
            _finalesServidor = CalcularFinales(_origenServidor);
            _fase = FaseRayo.Aviso;
            _tiempoEnFase = 0f;
            _golpeados.Clear();

            AvisoRpc(_origenServidor, _finalesServidor);
        }

        // Reparte _lineas direcciones en el abanico, con un desfase aleatorio común para que el
        // patrón cambie entre lanzamientos sin cerrar los huecos entre franjas.
        private Vector3[] CalcularFinales(Vector3 origen)
        {
            Vector3 adelante = transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.0001f)
                adelante = Vector3.forward;
            adelante.Normalize();

            int cantidad = Mathf.Max(1, _perfil.lineas);
            float paso = _perfil.aberturaGrados / cantidad;
            float desfase = Random.Range(-paso * 0.5f, paso * 0.5f);

            // El abanico se centra en el objetivo, pero ningún rayo sale más allá del límite
            // frontal: nunca hacia atrás de la boca.
            float limite = Mathf.Abs(_perfil.limiteFrontalGrados);
            float centro = Mathf.Clamp(AnguloHaciaObjetivo(adelante), -limite, limite);

            var finales = new Vector3[cantidad];
            for (int i = 0; i < cantidad; i++)
            {
                float angulo = centro - _perfil.aberturaGrados * 0.5f + paso * (i + 0.5f) + desfase;
                angulo = Mathf.Clamp(angulo, -limite, limite);
                Vector3 direccion = Quaternion.Euler(0f, angulo, 0f) * adelante;
                Vector3 final = origen + direccion * _perfil.largo;
                final.y = BuscarPiso(final, transform.position.y);
                finales[i] = final;
            }

            return finales;
        }

        // Grados (en Y) del frente del jefe hacia el objetivo elegido; 0 si no hay objetivo.
        private float AnguloHaciaObjetivo(Vector3 adelante)
        {
            if (
                Lanzador == null
                || !Lanzador.ObjetivoActual.HasValue
                || !Lanzador.TryPosicionJugador(Lanzador.ObjetivoActual.Value, out var posicion)
            )
                return 0f;

            Vector3 hacia = posicion - transform.position;
            hacia.y = 0f;
            if (hacia.sqrMagnitude < 0.0001f)
                return 0f;

            return Vector3.SignedAngle(adelante, hacia.normalized, Vector3.up);
        }

        // Altura del piso: el punto más bajo que encuentra un rayo hacia abajo (ignora jefe y jugadores).
        private static float BuscarPiso(Vector3 punto, float alturaSiNoHayPiso)
        {
            var impactos = Physics.RaycastAll(
                punto + Vector3.up * 30f,
                Vector3.down,
                100f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            float piso = alturaSiNoHayPiso;
            bool encontrado = false;
            foreach (var impacto in impactos)
            {
                if (!encontrado || impacto.point.y < piso)
                {
                    piso = impacto.point.y;
                    encontrado = true;
                }
            }
            return piso;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_flashGolpe > 0f)
                _flashGolpe -= dt;

            if (!IsServer || !IsSpawned || _perfil == null)
                return;

            _tiempoEnFase += dt;

            switch (_fase)
            {
                case FaseRayo.Aviso:
                    if (_tiempoEnFase >= _perfil.aviso)
                    {
                        CambiarFase(FaseRayo.Activo);
                        RayoRpc();
                    }
                    break;

                case FaseRayo.Activo:
                    DetectarImpactos();
                    if (_tiempoEnFase >= _perfil.activo)
                    {
                        CambiarFase(FaseRayo.Retirada);
                        ApagarRpc();
                    }
                    break;

                case FaseRayo.Retirada:
                    if (_tiempoEnFase >= _perfil.retirada)
                    {
                        CambiarFase(FaseRayo.Terminado);
                        NetworkObject.Despawn(true);
                    }
                    break;
            }
        }

        private void CambiarFase(FaseRayo nueva)
        {
            _fase = nueva;
            _tiempoEnFase = 0f;
        }

        private void DetectarImpactos()
        {
            if (_finalesServidor == null)
                return;

            float radio = _perfil.diametro * 0.5f;
            foreach (var final in _finalesServidor)
            {
                Collider[] tocados = Physics.OverlapCapsule(
                    _origenServidor,
                    final,
                    radio,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide
                );

                foreach (var tocado in tocados)
                {
                    var jugador = tocado.GetComponentInParent<PlayerRoot>();
                    if (jugador == null || !_golpeados.Add(jugador.OwnerClientId))
                        continue; // cada jugador solo una vez por ataque

                    ulong id = jugador.OwnerClientId;
                    Debug.Log($"[AREK][Rayo] golpeó a cliente {id}");
                    JugadorGolpeadoRpc(RpcTarget.Single(id, RpcTargetUse.Temp));
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AvisoRpc(Vector3 origen, Vector3[] finales)
        {
            LimpiarVisuales();

            foreach (var final in finales)
            {
                _franjas.Add(CrearFranja(origen, final));
                _rayos.Add(CrearRayo(origen, final));
            }

            if (_avisoParpadeo != null && _perfil != null)
            {
                var renderers = new List<Renderer>(_franjas.Count);
                foreach (var franja in _franjas)
                    renderers.Add(franja.GetComponent<Renderer>());

                _avisoParpadeo.Iniciar(
                    Lanzador.PerfilAviso,
                    _perfil.aviso,
                    renderers,
                    AvisoParpadeo.Forma.FranjaU
                );
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void RayoRpc()
        {
            foreach (var rayo in _rayos)
                rayo.enabled = true;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ApagarRpc()
        {
            foreach (var rayo in _rayos)
                rayo.enabled = false;

            foreach (var franja in _franjas)
                franja.SetActive(false);
        }

        // Franja de aviso en el piso, desde debajo de la boca hasta el final del rayo.
        private GameObject CrearFranja(Vector3 origen, Vector3 final)
        {
            Vector3 inicio = new Vector3(origen.x, final.y, origen.z);
            Vector3 recorrido = final - inicio;

            var franja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            franja.name = "FranjaAviso";
            Destroy(franja.GetComponent<Collider>());
            franja.transform.SetParent(transform, true);
            franja.transform.SetPositionAndRotation(
                inicio + recorrido * 0.5f + Vector3.up * 0.05f,
                Quaternion.LookRotation(recorrido.normalized, Vector3.up)
            );
            franja.transform.localScale = new Vector3(_perfil.diametro, 0.02f, recorrido.magnitude);

            var render = franja.GetComponent<Renderer>();
            if (_materialAviso != null)
                render.sharedMaterial = _materialAviso;
            else
                Debug.LogWarning("[AtaqueRayo] Falta asignar _materialAviso (AvisoAtaque.mat).");

            return franja;
        }

        // Rayo con LineRenderer desde la boca hasta el piso; se enciende con RayoRpc.
        private LineRenderer CrearRayo(Vector3 origen, Vector3 final)
        {
            var objetoRayo = new GameObject("Rayo");
            objetoRayo.transform.SetParent(transform, false);

            var linea = objetoRayo.AddComponent<LineRenderer>();
            linea.useWorldSpace = true;
            linea.positionCount = 2;
            linea.SetPosition(0, origen);
            linea.SetPosition(1, final);
            linea.startWidth = _perfil.diametro;
            linea.endWidth = _perfil.diametro;

            if (_materialRayo != null)
            {
                linea.sharedMaterial = _materialRayo;
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");
                if (shader != null)
                    linea.material = new Material(shader) { color = new Color(1f, 0.85f, 0.2f) };
            }

            linea.enabled = false;
            return linea;
        }

        private void LimpiarVisuales()
        {
            foreach (var franja in _franjas)
                if (franja != null)
                    Destroy(franja);

            foreach (var rayo in _rayos)
                if (rayo != null)
                    Destroy(rayo.gameObject);

            _franjas.Clear();
            _rayos.Clear();
        }

        // Mismo feedback que la Mano y el Tentáculo: impacto de cámara y flashes.
        [Rpc(SendTo.SpecifiedInParams)]
        private void JugadorGolpeadoRpc(RpcParams rpcParams = default)
        {
            _flashGolpe = 0.25f;

            var propio = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (propio == null)
                return;

            var camaraJugador = propio.GetComponentInChildren<PlayerCameraRig>();
            if (camaraJugador != null)
                camaraJugador.Impacto(2.5f);

            var flashDebug = propio.GetComponentInChildren<CombatDebugFeedback>();
            if (flashDebug != null)
                flashDebug.FlashActive(0.2f);
        }

        private void OnGUI()
        {
            if (_flashGolpe <= 0f)
                return;

            Color previo = GUI.color;
            GUI.color = new Color(1f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previo;
        }

        private void OnDrawGizmosSelected()
        {
            if (_finalesServidor == null)
                return;

            Gizmos.color = Color.yellow;
            foreach (var final in _finalesServidor)
                Gizmos.DrawLine(_origenServidor, final);
        }
    }
}
