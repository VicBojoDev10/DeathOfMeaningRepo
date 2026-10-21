using TDOM.Data;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace TDOM.Unity
{
    /// <summary>
    /// TW-126: columna que deja cada piedra del Mortero en su punto de impacto. El servidor la
    /// spawnea (AtaquesArek) con la base en el piso y una altura al azar; en todas las máquinas
    /// el cuerpo sube desde el suelo y, al quitarla, se hunde antes de despawnearse.
    /// </summary>
    public class ColumnaMortero : NetworkBehaviour
    {
        [SerializeField]
        private AtaqueMorteroProfile _perfil;

        [Tooltip("Cilindro con el MeshCollider convexo; es lo que sube y se hunde.")]
        [SerializeField]
        private Transform _cuerpo;

        [Tooltip("Disco plano opcional pegado al piso (grieta), sin collider.")]
        [SerializeField]
        private Transform _grieta;

        [SerializeField]
        private NavMeshObstacle _obstaculo;

        private enum Estado
        {
            Subiendo,
            Quieta,
            Hundiendo,
        }

        // La elige el servidor antes de Spawn; llega a los clientes en el spawn.
        private readonly NetworkVariable<float> _altura = new NetworkVariable<float>();

        private Estado _estado = Estado.Subiendo;
        private float _tiempoEnEstado;

        /// <summary>Solo servidor, antes de Spawn.</summary>
        public void AsignarAltura(float altura)
        {
            _altura.Value = altura;
        }

        public override void OnNetworkSpawn()
        {
            if (_perfil == null)
            {
                Debug.LogError("[ColumnaMortero] Falta el AtaqueMorteroProfile en el prefab.");
                return;
            }

            float diametro = _perfil.diametroColumna;
            float altura = _altura.Value;

            if (_cuerpo != null)
                // El cilindro primitivo de Unity mide 2 de alto con escala 1.
                _cuerpo.localScale = new Vector3(diametro, altura * 0.5f, diametro);

            if (_grieta != null)
            {
                foreach (var colisionador in _grieta.GetComponentsInChildren<Collider>(true))
                    colisionador.enabled = false;

                _grieta.localScale = new Vector3(
                    _perfil.diametroGrieta,
                    0.005f,
                    _perfil.diametroGrieta
                );
                _grieta.localPosition = Vector3.up * 0.02f;
            }

            if (_obstaculo != null)
            {
                _obstaculo.shape = NavMeshObstacleShape.Capsule;
                _obstaculo.radius = diametro * 0.5f;
                _obstaculo.height = altura;
                _obstaculo.center = Vector3.up * (altura * 0.5f);
                _obstaculo.carving = true;
            }

            CambiarEstado(Estado.Subiendo);
            ColocarCuerpo(0f);

            if (IsServer)
                SacarJugadoresAtrapados();
        }

        private void SacarJugadoresAtrapados()
        {
            Vector3 centro = transform.position;
            float altura = _altura.Value;
            float radioColumna = _perfil.diametroColumna * 0.5f;

            foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (objeto == null || !objeto.TryGetComponent(out PlayerRoot jugador))
                    continue;

                Vector3 posicion = jugador.transform.position;
                if (posicion.y > centro.y + altura)
                    continue;

                float radioJugador = jugador.TryGetComponent(out CharacterController controlador)
                    ? controlador.radius
                    : 0f;
                float distanciaBorde = radioColumna + radioJugador + _perfil.margenSacarJugador;

                Vector3 hacia = posicion - centro;
                hacia.y = 0f;
                if (hacia.magnitude >= distanciaBorde)
                    continue;

                Vector3 direccion =
                    hacia.sqrMagnitude > 0.0001f ? hacia.normalized : Vector3.forward;
                Vector3 destino = centro + direccion * distanciaBorde;
                destino.y = posicion.y;

                SacarJugadorRpc(
                    destino,
                    RpcTarget.Single(jugador.OwnerClientId, RpcTargetUse.Temp)
                );
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SacarJugadorRpc(Vector3 destino, RpcParams rpcParams = default)
        {
            var jugador = AtaqueArekBase.BuscarJugadorLocal();
            if (jugador == null)
                return;

            var controlador = jugador.GetComponent<CharacterController>();
            bool controladorActivo = controlador != null && controlador.enabled;
            if (controladorActivo)
                controlador.enabled = false;

            try
            {
                Transform cuerpo = jugador.transform;
                var networkTransform = jugador.GetComponent<ClientNetworkTransform>();
                if (networkTransform != null)
                    networkTransform.Teleport(destino, cuerpo.rotation, cuerpo.localScale);
                else
                    cuerpo.position = destino;
            }
            finally
            {
                if (controladorActivo)
                    controlador.enabled = true;
            }
        }

        /// <summary>Solo servidor: la columna se hunde en todas las máquinas y luego se despawnea.</summary>
        public void Hundir()
        {
            if (!IsServer || !IsSpawned || _estado == Estado.Hundiendo)
                return;

            HundirRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void HundirRpc()
        {
            CambiarEstado(Estado.Hundiendo);
        }

        private void CambiarEstado(Estado nuevo)
        {
            _estado = nuevo;
            _tiempoEnEstado = 0f;

            if (_obstaculo != null)
                _obstaculo.enabled = nuevo == Estado.Quieta;
        }

        private void Update()
        {
            if (
                PauseManager.PausadoGlobal
                || (PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso)
            )
                return;
            if (_perfil == null || !IsSpawned)
                return;

            _tiempoEnEstado += Time.deltaTime;

            switch (_estado)
            {
                case Estado.Subiendo:
                    float subida = Progreso(_perfil.duracionAscenso);
                    ColocarCuerpo(subida);
                    if (subida >= 1f)
                        CambiarEstado(Estado.Quieta);
                    break;

                case Estado.Hundiendo:
                    float hundida = Progreso(_perfil.duracionHundimiento);
                    ColocarCuerpo(1f - hundida);
                    if (hundida >= 1f && IsServer)
                        NetworkObject.Despawn(true);
                    break;
            }
        }

        private float Progreso(float duracion)
        {
            return duracion > 0f ? Mathf.Clamp01(_tiempoEnEstado / duracion) : 1f;
        }

        // 0 = todo el cuerpo bajo el piso, 1 = la base en el piso.
        private void ColocarCuerpo(float salida)
        {
            if (_cuerpo == null)
                return;

            float altura = _altura.Value;
            _cuerpo.localPosition = Vector3.up * (altura * (salida - 0.5f));
        }
    }
}
