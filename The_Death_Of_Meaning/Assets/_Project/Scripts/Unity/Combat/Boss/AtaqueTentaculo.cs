using System.Collections.Generic;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    /// <summary>
    /// Ataque Tentáculo (Pesado) de AREK en greybox. El servidor elige un punto aleatorio
    /// frente al jefe, muestra un círculo de aviso, deja caer el tentáculo y detecta a los
    /// jugadores dentro del área. No resta vida: solo detecta y reporta (04. Daño en WIP).
    /// </summary>
    public class AtaqueTentaculo : NetworkBehaviour
    {
        [Header("Tiempos")]
        [SerializeField]
        private float _aviso = 1.0f;

        [SerializeField]
        private float _caida = 0.15f;

        [SerializeField]
        private float _retirada = 0.5f;

        [Header("Área")]
        [SerializeField]
        private float _radioArea = 4f;

        [SerializeField]
        private float _radioArena = 25f;

        [SerializeField]
        private float _alturaCaida = 15f;

        [SerializeField]
        private float _altoTentaculo = 6f;

        [Header("Visuales")]
        [SerializeField]
        private Transform _discoAviso;

        [SerializeField]
        private Transform _tentaculo;

        private enum FaseTentaculo
        {
            Aviso,
            Caida,
            Retirada,
            Terminado,
        }

        private FaseTentaculo _fase = FaseTentaculo.Aviso;
        private float _tiempoEnFase;
        private readonly HashSet<ulong> _golpeados = new HashSet<ulong>();

        // Animación local de la caída (corre en todos los clientes a partir de CaidaRpc).
        private float _tiempoCaidaLocal = -1f;

        // Flash rojo de pantalla en el cliente golpeado.
        private float _flashGolpe;

        private void Awake()
        {
            // Los visuales son solo decoración: sin colliders para no empujar jugadores.
            foreach (var colisionador in GetComponentsInChildren<Collider>(true))
                colisionador.enabled = false;

            if (_discoAviso != null)
            {
                _discoAviso.localScale = new Vector3(_radioArea * 2f, 0.02f, _radioArea * 2f);
                _discoAviso.localPosition = new Vector3(0f, 0.05f, 0f);
                _discoAviso.gameObject.SetActive(false);
            }

            if (_tentaculo != null)
            {
                // El cilindro primitivo de Unity mide 2 de alto con escala 1.
                _tentaculo.localScale = new Vector3(1.5f, _altoTentaculo * 0.5f, 1.5f);
                ColocarTentaculo(_alturaCaida);
                _tentaculo.gameObject.SetActive(false);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            transform.SetPositionAndRotation(ElegirPuntoFrenteAlJefe(), Quaternion.identity);
            _fase = FaseTentaculo.Aviso;
            _tiempoEnFase = 0f;
            _golpeados.Clear();

            AvisoRpc();
        }

        // Se spawnea en la posición y rotación del jefe; el punto cae en su semicírculo frontal.
        private Vector3 ElegirPuntoFrenteAlJefe()
        {
            Vector3 adelante = transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.0001f)
                adelante = Vector3.forward;
            adelante.Normalize();

            float angulo = Random.Range(-90f, 90f);
            // Raíz cuadrada para repartir los puntos parejo por el área, y nunca debajo del jefe.
            float distancia = Mathf.Lerp(_radioArea, _radioArena, Mathf.Sqrt(Random.value));

            Vector3 punto = transform.position + Quaternion.Euler(0f, angulo, 0f) * adelante * distancia;
            punto.y = AlturaDelPiso(punto);
            return punto;
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
                case FaseTentaculo.Aviso:
                    if (_tiempoEnFase >= _aviso)
                    {
                        CambiarFase(FaseTentaculo.Caida);
                        CaidaRpc();
                    }
                    break;

                case FaseTentaculo.Caida:
                    if (_tiempoEnFase >= _caida)
                    {
                        DetectarImpactos();
                        CambiarFase(FaseTentaculo.Retirada);
                    }
                    break;

                case FaseTentaculo.Retirada:
                    if (_tiempoEnFase >= _retirada)
                    {
                        CambiarFase(FaseTentaculo.Terminado);
                        NetworkObject.Despawn(true);
                    }
                    break;
            }
        }

        private void CambiarFase(FaseTentaculo nueva)
        {
            _fase = nueva;
            _tiempoEnFase = 0f;
        }

        private void AnimarCaidaLocal(float dt)
        {
            if (_tiempoCaidaLocal < 0f || _tentaculo == null)
                return;

            _tiempoCaidaLocal += dt;
            float t = Mathf.Clamp01(_tiempoCaidaLocal / _caida);
            ColocarTentaculo(Mathf.Lerp(_alturaCaida, 0f, t));
        }

        // Coloca la base del tentáculo a la altura indicada sobre el centro del área.
        private void ColocarTentaculo(float alturaBase)
        {
            if (_tentaculo != null)
                _tentaculo.localPosition = new Vector3(0f, alturaBase + _altoTentaculo * 0.5f, 0f);
        }

        private void DetectarImpactos()
        {
            Collider[] dentro = Physics.OverlapSphere(
                transform.position,
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
                    continue; // cada jugador solo una vez

                Debug.Log($"[AREK][Tentaculo] golpeó a cliente {id}");
                JugadorGolpeadoRpc(RpcTarget.Single(id, RpcTargetUse.Temp));
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AvisoRpc()
        {
            if (_discoAviso != null)
                _discoAviso.gameObject.SetActive(true);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void CaidaRpc()
        {
            if (_tentaculo != null)
                _tentaculo.gameObject.SetActive(true);

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
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _radioArea);
        }
    }
}
