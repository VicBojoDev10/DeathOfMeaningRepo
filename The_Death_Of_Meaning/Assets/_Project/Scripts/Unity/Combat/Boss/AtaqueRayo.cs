using System.Collections.Generic;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    /// <summary>
    /// Ataque Rayo (Especial) de AREK en greybox. El servidor elige un patrón de líneas en
    /// abanico frente al jefe, todos ven franjas rojas de aviso en el piso y después los rayos
    /// (LineRenderer) desde la boca. El servidor detecta a los jugadores dentro de cada rayo.
    /// No resta vida: solo detecta y reporta (04. Daño en WIP).
    /// </summary>
    public class AtaqueRayo : NetworkBehaviour
    {
        [Header("Tiempos")]
        [SerializeField]
        private float _aviso = 1.25f;

        [SerializeField]
        private float _activo = 0.6f;

        [SerializeField]
        private float _retirada = 0.2f;

        [Header("Forma")]
        [SerializeField]
        private float _largo = 30f;

        [SerializeField]
        private float _diametro = 2f;

        [SerializeField]
        private int _lineas = 3;

        [SerializeField]
        private float _aberturaGrados = 60f; // abanico frente al jefe

        [Header("Origen")]
        [SerializeField]
        private Transform _boca; // si es null: centro del jefe + 3 m en Y

        [SerializeField]
        private float _alturaBocaPorDefecto = 3f;

        [Header("Visuales")]
        [SerializeField]
        private Material _materialAviso;

        [SerializeField]
        private Material _materialRayo;

        private enum FaseRayo
        {
            Aviso,
            Activo,
            Retirada,
            Terminado,
        }

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

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            _origenServidor =
                _boca != null ? _boca.position : transform.position + Vector3.up * _alturaBocaPorDefecto;
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

            int cantidad = Mathf.Max(1, _lineas);
            float paso = _aberturaGrados / cantidad;
            float desfase = Random.Range(-paso * 0.5f, paso * 0.5f);
            float pisoY = BuscarPiso(origen + adelante * 5f, transform.position.y);

            var finales = new Vector3[cantidad];
            for (int i = 0; i < cantidad; i++)
            {
                float angulo = -_aberturaGrados * 0.5f + paso * (i + 0.5f) + desfase;
                Vector3 direccion = Quaternion.Euler(0f, angulo, 0f) * adelante;
                Vector3 final = origen + direccion * _largo;
                final.y = pisoY;
                finales[i] = final;
            }

            return finales;
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

            if (!IsServer || !IsSpawned)
                return;

            _tiempoEnFase += dt;

            switch (_fase)
            {
                case FaseRayo.Aviso:
                    if (_tiempoEnFase >= _aviso)
                    {
                        CambiarFase(FaseRayo.Activo);
                        RayoRpc();
                    }
                    break;

                case FaseRayo.Activo:
                    DetectarImpactos();
                    if (_tiempoEnFase >= _activo)
                    {
                        CambiarFase(FaseRayo.Retirada);
                        ApagarRpc();
                    }
                    break;

                case FaseRayo.Retirada:
                    if (_tiempoEnFase >= _retirada)
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

            float radio = _diametro * 0.5f;
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

        // Franja roja en el piso, desde debajo de la boca hasta el final del rayo.
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
            franja.transform.localScale = new Vector3(_diametro, 0.02f, recorrido.magnitude);

            var render = franja.GetComponent<Renderer>();
            if (_materialAviso != null)
                render.sharedMaterial = _materialAviso;
            else
                render.material.color = new Color(1f, 0f, 0f, 0.4f);

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
            linea.startWidth = _diametro;
            linea.endWidth = _diametro;

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
