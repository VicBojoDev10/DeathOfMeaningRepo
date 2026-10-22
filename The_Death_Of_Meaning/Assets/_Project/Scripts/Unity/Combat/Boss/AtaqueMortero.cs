using System.Collections.Generic;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Gameplay.Core;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    /// <summary>
    /// Ataque Mortero (InstaKill) de AREK en greybox: tres disparos simultáneos. El servidor
    /// coloca 3 puntos sobre el o los objetivos (separados al menos 2 × radioArea), todos ven
    /// un disco de aviso en cada punto, después cae una "piedra" en cada uno y el servidor
    /// detecta a los jugadores dentro. No resta vida: solo detecta y reporta (04. Daño en WIP).
    /// TW-126: al impactar, cada piedra deja una ColumnaMortero (la spawnea AtaquesArek) y todos
    /// ven polvo y sienten una sacudida de cámara según la distancia.
    /// </summary>
    public class AtaqueMortero : AtaqueArekBase
    {
        [Header("Visuales (plantillas, se clonan una vez por disparo)")]
        [SerializeField]
        private Transform _discoAviso;

        [SerializeField]
        private Transform _piedra;

        [Header("Aviso")]
        [SerializeField]
        private AvisoParpadeo _avisoParpadeo;

        [Header("Impacto (plantilla, se clona una vez por disparo)")]
        [SerializeField]
        private ParticleSystem _polvo;

        private enum FaseMortero
        {
            Aviso,
            Caida,
            Retirada,
            Terminado,
        }

        // Tuning: lo lee de AtaquesArek (BossArek.prefab) al spawnear.
        private AtaqueMorteroProfile _perfil;

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
            if (_polvo != null)
                _polvo.gameObject.SetActive(false);

            if (_avisoParpadeo == null)
                _avisoParpadeo = GetComponent<AvisoParpadeo>();
        }

        protected override void AlSpawnear()
        {
            _perfil = Lanzador != null ? Lanzador.PerfilMortero : null;
            if (_perfil == null)
            {
                Debug.LogError("[AtaqueMortero] Falta el AtaqueMorteroProfile en AtaquesArek.");
                return;
            }

            if (!IsServer)
                return;

            _puntosServidor = TryPuntosSobreObjetivos(out var puntos) ? puntos : ElegirPuntos();
            _fase = FaseMortero.Aviso;
            _tiempoEnFase = 0f;
            _golpeados.Clear();

            AvisoRpc(_puntosServidor);
        }

        // Con objetivo elegido por AtaquesArek: el primer disparo cae sobre él, uno sobre cada otro
        // jugador en rango y el resto pegados, sin solaparse (MorteroPlacement).
        private bool TryPuntosSobreObjetivos(out Vector3[] puntos)
        {
            puntos = null;
            if (Lanzador == null || !Lanzador.ObjetivoActual.HasValue)
                return false;

            ulong objetivoId = Lanzador.ObjetivoActual.Value;
            if (!Lanzador.TryPosicionJugador(objetivoId, out Vector3 posicionObjetivo))
                return false;

            var otros = new List<Vector2>();
            var jugadores = new List<PlayerRoot>();
            Lanzador.ListarJugadores(jugadores);
            foreach (var jugador in jugadores)
            {
                if (jugador.OwnerClientId == objetivoId)
                    continue;

                Vector3 posicion = jugador.transform.position;
                Vector3 hacia = posicion - transform.position;
                hacia.y = 0f;
                if (hacia.magnitude <= _perfil.Rango)
                    otros.Add(new Vector2(posicion.x, posicion.z));
            }

            var enPlano = MorteroPlacement.Colocar(
                new Vector2(posicionObjetivo.x, posicionObjetivo.z),
                otros,
                _perfil.disparos,
                _perfil.radioArea,
                Random.Range(0f, 360f)
            );

            puntos = new Vector3[enPlano.Count];
            for (int i = 0; i < enPlano.Count; i++)
            {
                var punto = new Vector3(enPlano[i].x, posicionObjetivo.y, enPlano[i].y);
                punto.y = AlturaDelPiso(punto);
                puntos[i] = punto;
            }

            return true;
        }

        // Sin objetivo (botón de debug): puntos al azar en el semicírculo frontal del jefe.
        private Vector3[] ElegirPuntos()
        {
            Vector3 adelante = transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.0001f)
                adelante = Vector3.forward;
            adelante.Normalize();

            int cantidad = Mathf.Max(1, _perfil.disparos);
            float separacionMinima = 2f * _perfil.radioArea;
            var puntos = new List<Vector3>(cantidad);

            for (int i = 0; i < cantidad; i++)
            {
                Vector3 candidato = PuntoAleatorio(adelante);
                for (int intento = 1; intento < _perfil.intentosPorPunto; intento++)
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
            float distancia = Mathf.Lerp(
                _perfil.radioArea,
                _perfil.radioArena,
                Mathf.Sqrt(Random.value)
            );

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
            if (
                PauseManager.PausadoGlobal
                || (PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso)
            )
                return;
            if (_perfil == null)
                return;

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
                    if (_tiempoEnFase >= _perfil.aviso)
                    {
                        CambiarFase(FaseMortero.Caida);
                        CaidaRpc();
                    }
                    break;

                case FaseMortero.Caida:
                    if (_tiempoEnFase >= _perfil.caida)
                    {
                        DetectarImpactos();
                        SpawnearColumnas();
                        ImpactoRpc();
                        CambiarFase(FaseMortero.Retirada);
                    }
                    break;

                case FaseMortero.Retirada:
                    if (_tiempoEnFase >= _perfil.retirada)
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
            float t = Mathf.Clamp01(_tiempoCaidaLocal / _perfil.caida);
            float altura = Mathf.Lerp(_perfil.alturaCaida, 0f, t);

            for (int i = 0; i < _piedras.Count; i++)
                ColocarPiedra(_piedras[i], _puntosLocales[i], altura);
        }

        // Coloca la base de la piedra a la altura indicada sobre su punto de impacto.
        private void ColocarPiedra(Transform piedra, Vector3 punto, float alturaBase)
        {
            piedra.position = punto + Vector3.up * (alturaBase + _perfil.altoPiedra * 0.5f);
        }

        private void DetectarImpactos()
        {
            if (_puntosServidor == null)
                return;

            foreach (var punto in _puntosServidor)
            {
                Collider[] dentro = Physics.OverlapSphere(
                    punto,
                    _perfil.radioArea,
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

                    Debug.Log($"[AREK][Mortero] golpeó a cliente {id}");
                    float dano = _perfil != null ? _perfil.Dano : 9999f;
                    damageable.RecibirDanio(dano, esInstaKill: true);
                    if (netObj != null)
                        JugadorGolpeadoRpc(RpcTarget.Single(id, RpcTargetUse.Temp));
                }
            }
        }

        // TW-126: la columna queda con la base en el piso que ya se calculó para cada punto.
        private void SpawnearColumnas()
        {
            if (_puntosServidor == null || Lanzador == null)
                return;

            foreach (var punto in _puntosServidor)
                Lanzador.SpawnearColumna(punto);
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
                    disco.localScale = new Vector3(
                        _perfil.radioArea * 2f,
                        0.02f,
                        _perfil.radioArea * 2f
                    );
                    disco.position = punto + Vector3.up * 0.05f;
                    disco.gameObject.SetActive(true);
                    _discos.Add(disco);
                }

                if (_piedra != null)
                {
                    var piedra = Instantiate(_piedra, transform);
                    // El cilindro primitivo de Unity mide 2 de alto con escala 1.
                    piedra.localScale = new Vector3(1.5f, _perfil.altoPiedra * 0.5f, 1.5f);
                    ColocarPiedra(piedra, punto, _perfil.alturaCaida);
                    _piedras.Add(piedra);
                }
            }

            // Un solo componente para los 3 discos.
            if (_avisoParpadeo != null)
            {
                var renderers = new List<Renderer>(_discos.Count);
                foreach (var disco in _discos)
                    renderers.Add(disco.GetComponent<Renderer>());

                _avisoParpadeo.Iniciar(
                    Lanzador.PerfilAviso,
                    _perfil.aviso,
                    renderers,
                    AvisoParpadeo.Forma.Disco
                );
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void CaidaRpc()
        {
            foreach (var piedra in _piedras)
                piedra.gameObject.SetActive(true);

            _tiempoCaidaLocal = 0f;
        }

        // TW-126: la piedra se esconde (la reemplaza la columna que sube), sale polvo en cada punto
        // y la cámara del jugador local se sacude según qué tan cerca cayó la piedra más próxima.
        [Rpc(SendTo.ClientsAndHost)]
        private void ImpactoRpc()
        {
            _tiempoCaidaLocal = -1f;
            foreach (var piedra in _piedras)
                piedra.gameObject.SetActive(false);

            foreach (var punto in _puntosLocales)
                LanzarPolvo(punto);

            SacudirCamaraLocal();
        }

        // Sin padre: el ataque se despawnea antes de que termine el polvo.
        private void LanzarPolvo(Vector3 punto)
        {
            if (_polvo == null)
                return;

            ParticleSystem polvo = Instantiate(_polvo, punto, _polvo.transform.rotation);
            polvo.gameObject.SetActive(true);
            var principal = polvo.main;
            principal.stopAction = ParticleSystemStopAction.Destroy;
            polvo.Play(true);
        }

        private void SacudirCamaraLocal()
        {
            if (_perfil.radioSacudida <= 0f)
                return;

            var jugador = BuscarJugadorLocal();
            if (jugador == null)
                return;

            var rig = jugador.GetComponentInChildren<PlayerCameraRig>(true);
            if (rig == null)
                return;

            float distanciaMinima = float.MaxValue;
            foreach (var punto in _puntosLocales)
                distanciaMinima = Mathf.Min(
                    distanciaMinima,
                    Vector3.Distance(jugador.transform.position, punto)
                );

            float cercania = 1f - Mathf.Clamp01(distanciaMinima / _perfil.radioSacudida);
            if (cercania > 0f)
                rig.Impacto(_perfil.sacudidaMaxima * cercania);
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
            if (_puntosServidor == null || _perfil == null)
                return;

            Gizmos.color = Color.red;
            foreach (var punto in _puntosServidor)
                Gizmos.DrawWireSphere(punto, _perfil.radioArea);
        }
    }
}
