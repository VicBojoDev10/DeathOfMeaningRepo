using System.Collections.Generic;
using TDOM.Data;
using TDOM.Gameplay.Core;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    /// <summary>
    /// Ataque Tentáculo (Pesado) de AREK en greybox: como la Mano, pero vertical. El tentáculo se
    /// levanta junto al jefe, todos ven una franja de aviso parpadeante en el piso y después azota
    /// de arriba hacia abajo sobre ella. Se esquiva saliéndose de la franja hacia un lado, no
    /// saltando.
    /// No resta vida: solo detecta y reporta (04. Daño en WIP).
    /// </summary>
    public class AtaqueTentaculo : AtaqueArekBase
    {
        [Header("Visuales")]
        [SerializeField]
        private Transform _tentaculoVisual;

        [SerializeField]
        private Renderer _tentaculoRenderer;

        [Header("Aviso")]
        [SerializeField]
        private Material _materialAviso; // AvisoAtaque.mat

        [SerializeField]
        private AvisoParpadeo _avisoParpadeo;

        private enum FaseTentaculo
        {
            Aviso,
            Azote,
            Retirada,
            Terminado,
        }

        // Tuning: lo lee de AtaquesArek (BossArek.prefab) al spawnear.
        private AtaqueTentaculoProfile _perfil;

        private FaseTentaculo _fase = FaseTentaculo.Aviso;
        private float _tiempoEnFase;
        private Quaternion _rotacionBase = Quaternion.identity;
        private readonly HashSet<ulong> _golpeados = new HashSet<ulong>();

        // Todos los clientes: franja de aviso en el piso (fuera de la jerarquía porque el tentáculo rota).
        private GameObject _franja;

        private float _flashGolpe;

        private void Awake()
        {
            if (_tentaculoVisual == null)
                _tentaculoVisual = transform.Find("Tentaculo");

            if (_tentaculoRenderer == null && _tentaculoVisual != null)
                _tentaculoRenderer = _tentaculoVisual.GetComponent<Renderer>();

            if (_avisoParpadeo == null)
                _avisoParpadeo = GetComponent<AvisoParpadeo>();

            // Los visuales son solo decoración: sin colliders para no empujar jugadores.
            foreach (var colisionador in GetComponentsInChildren<Collider>(true))
                colisionador.enabled = false;
        }

        // El pivote es la base del tentáculo; el bloque se extiende hacia +Z local.
        private void ConfigurarGeometriaVisual()
        {
            if (_tentaculoVisual == null)
                return;

            // El cilindro primitivo de Unity mide 2 de alto con escala 1 y su eje largo es Y;
            // se acuesta sobre Z para que el largo quede a lo largo de la franja.
            _tentaculoVisual.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _tentaculoVisual.localScale = new Vector3(
                _perfil.ancho,
                _perfil.largo * 0.5f,
                _perfil.alto
            );
            _tentaculoVisual.localPosition = new Vector3(
                0f,
                _perfil.alto * 0.5f,
                _perfil.largo * 0.5f
            );
        }

        protected override void AlSpawnear()
        {
            _perfil = Lanzador != null ? Lanzador.PerfilTentaculo : null;
            if (_perfil == null)
            {
                Debug.LogError("[AtaqueTentaculo] Falta el AtaqueTentaculoProfile en AtaquesArek.");
                return;
            }

            ConfigurarGeometriaVisual();

            if (!IsServer)
                return;

            Vector3 origen = transform.position;
            origen.y = AlturaDelPiso(origen);
            transform.position = origen;

            Vector3 direccion = ElegirDireccion();
            _rotacionBase = Quaternion.LookRotation(direccion, Vector3.up);
            transform.rotation = RotacionConInclinacion(-90f);

            _fase = FaseTentaculo.Aviso;
            _tiempoEnFase = 0f;
            _golpeados.Clear();

            AvisoRpc(origen, direccion);
        }

        public override void OnNetworkDespawn()
        {
            if (_franja != null)
                Destroy(_franja);
        }

        // Hacia el objetivo elegido por AtaquesArek (en XZ) ± desvioMaximo. Sin objetivo (botón de
        // debug): hacia un jugador al azar, o al azar en el frente de 180° si no hay jugadores.
        private Vector3 ElegirDireccion()
        {
            Vector3 adelante = transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.0001f)
                adelante = Vector3.forward;
            adelante.Normalize();

            if (TryPosicionObjetivo(out Vector3 posicion))
            {
                Vector3 haciaJugador = posicion - transform.position;
                haciaJugador.y = 0f;

                if (haciaJugador.sqrMagnitude > 0.0001f)
                {
                    float desvio = Random.Range(-_perfil.desvioMaximo, _perfil.desvioMaximo);
                    return Quaternion.Euler(0f, desvio, 0f) * haciaJugador.normalized;
                }
            }

            return Quaternion.Euler(0f, Random.Range(-90f, 90f), 0f) * adelante;
        }

        private bool TryPosicionObjetivo(out Vector3 posicion)
        {
            posicion = default;

            if (Lanzador != null && Lanzador.ObjetivoActual.HasValue)
                return Lanzador.TryPosicionJugador(Lanzador.ObjetivoActual.Value, out posicion);

            if (Lanzador == null)
                return false;

            var jugadores = new List<PlayerRoot>();
            Lanzador.ListarJugadores(jugadores);
            if (jugadores.Count == 0)
                return false;

            posicion = jugadores[Random.Range(0, jugadores.Count)].transform.position;
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
                return punto.y;

            float alturaMinima = float.MaxValue;
            foreach (var golpe in golpes)
                alturaMinima = Mathf.Min(alturaMinima, golpe.point.y);

            return alturaMinima;
        }

        // -90° = levantado en vertical; 0° = acostado sobre la franja.
        private Quaternion RotacionConInclinacion(float inclinacion)
        {
            return _rotacionBase * Quaternion.Euler(inclinacion, 0f, 0f);
        }

        private void Update()
        {
            if (
                PauseManager.PausadoGlobal
                || (PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso)
            )
                return;
            float dt = Time.deltaTime;

            if (_flashGolpe > 0f)
                _flashGolpe -= dt;

            if (!IsServer || !IsSpawned || _perfil == null)
                return;

            _tiempoEnFase += dt;

            switch (_fase)
            {
                case FaseTentaculo.Aviso:
                    if (_tiempoEnFase >= _perfil.aviso)
                        CambiarFase(FaseTentaculo.Azote);
                    break;

                case FaseTentaculo.Azote:
                    float t = Mathf.Clamp01(_tiempoEnFase / _perfil.azote);
                    transform.rotation = RotacionConInclinacion(Mathf.Lerp(-90f, 0f, t));

                    // Golpe seco: se detecta una sola vez, al terminar el azote.
                    if (_tiempoEnFase >= _perfil.azote)
                    {
                        DetectarImpactos();
                        CambiarFase(FaseTentaculo.Retirada);
                    }
                    break;

                case FaseTentaculo.Retirada:
                    if (_tiempoEnFase >= _perfil.retirada)
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

        // Caja con la forma de la franja, pegada al piso y con la altura del perfil: saltar no salva.
        private void DetectarImpactos()
        {
            Vector3 mitad = new Vector3(
                _perfil.ancho * 0.5f,
                _perfil.alto * 0.5f,
                _perfil.largo * 0.5f
            );
            Vector3 centro =
                transform.position
                + _rotacionBase * new Vector3(0f, _perfil.alto * 0.5f, _perfil.largo * 0.5f);

            Collider[] dentro = Physics.OverlapBox(
                centro,
                mitad,
                _rotacionBase,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );

            foreach (var colisionador in dentro)
            {
                var damageable = colisionador.GetComponentInParent<IDamageable>();
                if (damageable == null)
                    continue;

                var netObj = colisionador.GetComponentInParent<NetworkObject>();
                ulong id = netObj != null ? netObj.OwnerClientId : 0;
                if (!_golpeados.Add(id))
                    continue;

                Debug.Log($"[AREK][Tentaculo] golpeó a cliente {id}");
                damageable.RecibirDanio(_perfil.Dano, _perfil.EsInstaKill);
                if (netObj != null)
                    JugadorGolpeadoRpc(RpcTarget.Single(id, RpcTargetUse.Temp));
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AvisoRpc(Vector3 origen, Vector3 direccion)
        {
            if (_tentaculoRenderer != null)
                _tentaculoRenderer.material.color = Color.red;

            CrearFranja(origen, direccion);

            if (_avisoParpadeo != null && _perfil != null)
                _avisoParpadeo.Iniciar(
                    Lanzador.PerfilAviso,
                    _perfil.aviso,
                    new[] { _franja.GetComponent<Renderer>() },
                    AvisoParpadeo.Forma.FranjaV
                );
        }

        // Franja de aviso parpadeante de largo × ancho en el piso, en la dirección del azote.
        private void CrearFranja(Vector3 origen, Vector3 direccion)
        {
            if (_franja != null)
                Destroy(_franja);

            _franja = new GameObject("FranjaAvisoTentaculo");
            // Con TransformZ la línea mira hacia el Z del objeto; apuntándolo hacia arriba queda acostada.
            _franja.transform.rotation = Quaternion.LookRotation(Vector3.up, direccion);

            var linea = _franja.AddComponent<LineRenderer>();
            linea.useWorldSpace = true;
            linea.alignment = LineAlignment.TransformZ;
            linea.positionCount = 2;
            linea.SetPosition(0, origen + Vector3.up * 0.05f);
            linea.SetPosition(1, origen + direccion * _perfil.largo + Vector3.up * 0.05f);
            linea.startWidth = _perfil.ancho;
            linea.endWidth = _perfil.ancho;

            if (_materialAviso != null)
                linea.sharedMaterial = _materialAviso;
            else
                Debug.LogWarning(
                    "[AtaqueTentaculo] Falta asignar _materialAviso (AvisoAtaque.mat)."
                );
        }

        // Mismo feedback que la Mano: impacto de cámara, flash de debug y flash rojo de pantalla.
        [Rpc(SendTo.SpecifiedInParams)]
        private void JugadorGolpeadoRpc(RpcParams rpcParams = default)
        {
            _flashGolpe = 0.25f;
            NotificarGolpeAlJugadorLocal();
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
            if (_perfil == null)
                return;

            Gizmos.color = Color.red;
            Vector3 mitad = new Vector3(
                _perfil.ancho * 0.5f,
                _perfil.alto * 0.5f,
                _perfil.largo * 0.5f
            );
            Vector3 centro =
                transform.position
                + _rotacionBase * new Vector3(0f, _perfil.alto * 0.5f, _perfil.largo * 0.5f);

            Matrix4x4 anterior = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(centro, _rotacionBase, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, mitad * 2f);
            Gizmos.matrix = anterior;
        }
    }
}
