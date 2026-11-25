using System.Collections.Generic;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Core;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class AtaqueRayo : AtaqueArekBase
    {
        private const int SegmentosVisualesRayo = 32;
        private const float FrecuenciaCentelleo = 24f;

        [Header("Origen")]
        [SerializeField]
        private Transform _boca;

        [Header("Visuales")]
        [SerializeField]
        private Material _materialAviso;

        [SerializeField]
        private AvisoParpadeo _avisoParpadeo;

        [SerializeField]
        private Material _materialRayo;

        [SerializeField]
        private AtaqueRayoVfxProfile _perfilVfx;

        [Header("Sonido")]
        [SerializeField]
        private SfxBanco _sfxCarga;

        [SerializeField]
        private SfxBanco _sfxDisparoInicio;

        [SerializeField]
        private SfxBanco _sfxDisparoLoop;

        [SerializeField]
        private SfxBanco _sfxDisparoFin;

        private enum FaseRayo
        {
            Aviso,
            Activo,
            Retirada,
            Terminado,
        }

        private AtaqueRayoProfile _perfil;

        private FaseRayo _fase = FaseRayo.Aviso;
        private float _tiempoEnFase;
        private readonly HashSet<ulong> _golpeados = new HashSet<ulong>();

        private Vector3 _origenServidor;
        private Vector3[] _finalesServidor;
        private Vector3[] _iniciosServidor;
        private double _inicioRecorridoServidor;
        private float _duracionRecorrido;

        private readonly List<GameObject> _franjas = new List<GameObject>();
        private readonly List<LineRenderer> _rayos = new List<LineRenderer>();
        private readonly List<LineRenderer> _halos = new List<LineRenderer>();
        private readonly Vector3[] _posicionesNucleo = new Vector3[SegmentosVisualesRayo + 1];
        private readonly Vector3[] _posicionesHalo = new Vector3[SegmentosVisualesRayo + 1];
        private Material _materialNucleoRuntime;
        private Material _materialHaloRuntime;
        private readonly List<ParticleSystem> _particulasImpacto = new List<ParticleSystem>();
        private readonly List<MeshFilter> _mallasRayo = new List<MeshFilter>();
        private readonly List<MeshRenderer> _renderersMallaRayo = new List<MeshRenderer>();

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
            _iniciosServidor = new Vector3[_finalesServidor.Length];
            for (int i = 0; i < _finalesServidor.Length; i++)
                _iniciosServidor[i] = new Vector3(
                    _origenServidor.x,
                    _finalesServidor[i].y,
                    _origenServidor.z
                );
            _fase = FaseRayo.Aviso;
            _tiempoEnFase = 0f;
            _golpeados.Clear();

            AvisoRpc(_origenServidor, _finalesServidor);
            ReproducirSfxEn(_sfxCarga, _origenServidor);
        }

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
            if (PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso)
            {
                if (IsServer && _fase == FaseRayo.Activo)
                {
                    ApagarRpc();
                    DetenerSfxJefe(_sfxDisparoLoop);
                    CambiarFase(FaseRayo.Terminado);
                }
                return;
            }
            if (PauseManager.PausadoGlobal)
                return;
            float dt = DeltaAtaque;

            if (!IsServer || !IsSpawned || _perfil == null)
                return;

            _tiempoEnFase += dt;

            switch (_fase)
            {
                case FaseRayo.Aviso:
                    if (_tiempoEnFase >= _perfil.aviso)
                    {
                        CambiarFase(FaseRayo.Activo);
                        _inicioRecorridoServidor = NetworkManager.ServerTime.Time;
                        _duracionRecorrido =
                            _perfil.largo
                            / (
                                Mathf.Max(0.01f, _perfil.velocidadRecorrido)
                                * MultiplicadorVelocidad
                            );
                        RayoRpc(_inicioRecorridoServidor, _duracionRecorrido);
                        DetenerSfxJefe(_sfxCarga);
                        ReproducirSfxEn(_sfxDisparoInicio, _origenServidor);
                        ReproducirSfxEn(_sfxDisparoLoop, _origenServidor);
                    }
                    break;

                case FaseRayo.Activo:
                    DetectarImpactos();
                    if (
                        NetworkManager.ServerTime.Time - _inicioRecorridoServidor
                        >= _duracionRecorrido
                    )
                    {
                        CambiarFase(FaseRayo.Retirada);
                        ApagarRpc();
                        DetenerSfxJefe(_sfxDisparoLoop);
                        ReproducirSfxEn(_sfxDisparoFin, _origenServidor);
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
            float progresoActual = Mathf.Clamp01(
                (float)(
                    (NetworkManager.ServerTime.Time - _inicioRecorridoServidor)
                    / System.Math.Max(0.01, _duracionRecorrido)
                )
            );
            float progresoAnterior = Mathf.Clamp01(
                (float)(
                    (NetworkManager.ServerTime.Time - Time.deltaTime - _inicioRecorridoServidor)
                    / System.Math.Max(0.01, _duracionRecorrido)
                )
            );
            for (int i = 0; i < _finalesServidor.Length; i++)
            {
                Vector3 anterior = Vector3.Lerp(
                    _iniciosServidor[i],
                    _finalesServidor[i],
                    progresoAnterior
                );
                Vector3 actual = Vector3.Lerp(
                    _iniciosServidor[i],
                    _finalesServidor[i],
                    progresoActual
                );
                Collider[] tocados = Physics.OverlapCapsule(
                    anterior,
                    actual,
                    radio,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide
                );

                foreach (var tocado in tocados)
                {
                    var damageable = tocado.GetComponentInParent<IDamageable>();
                    if (damageable == null)
                        continue;

                    var netObj = tocado.GetComponentInParent<NetworkObject>();
                    ulong id = netObj != null ? netObj.OwnerClientId : 0;
                    if (!_golpeados.Add(id))
                        continue;

                    Debug.Log($"[AREK][Rayo] golpeó a cliente {id}");
                    var jugador = tocado.GetComponentInParent<PlayerRoot>();
                    if (jugador != null)
                        jugador.RecibirDanioDelJefe(
                            _perfil.Dano,
                            _origenServidor,
                            _perfil.EsInstaKill,
                            TipoImpactoBarreraJefe.Rayo
                        );
                    else
                        damageable.RecibirDanio(_perfil.Dano, _perfil.EsInstaKill);
                    if (netObj != null)
                        JugadorGolpeadoRpc(RpcTarget.Single(id, RpcTargetUse.Temp));
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AvisoRpc(Vector3 origen, Vector3[] finales)
        {
            LimpiarVisuales();
            _origenServidor = origen;
            _finalesServidor = finales;
            _iniciosServidor = new Vector3[finales.Length];

            for (int i = 0; i < finales.Length; i++)
            {
                var final = finales[i];
                _iniciosServidor[i] = new Vector3(origen.x, final.y, origen.z);
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
                    AvisoEscalado(_perfil.aviso),
                    renderers,
                    AvisoParpadeo.Forma.FranjaU
                );
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void RayoRpc(double inicioServidor, float duracion)
        {
            _fase = FaseRayo.Activo;
            _inicioRecorridoServidor = inicioServidor;
            _duracionRecorrido = duracion;
            for (int i = 0; i < _rayos.Count; i++)
            {
                LineRenderer rayo = _rayos[i];
                rayo.enabled = true;
                _halos[i].enabled = true;
                _renderersMallaRayo[i].enabled =
                    _perfilVfx != null && _perfilVfx.materialMesh != null;
                Vector3 inicio = ObtenerInicioVisual(i);
                for (int punto = 0; punto <= SegmentosVisualesRayo; punto++)
                    _posicionesNucleo[punto] = _posicionesHalo[punto] = inicio;
                rayo.SetPositions(_posicionesNucleo);
                _halos[i].SetPositions(_posicionesHalo);
                if (_perfilVfx != null && _perfilVfx.particulasImpacto != null)
                {
                    var particulas = Instantiate(
                        _perfilVfx.particulasImpacto,
                        ObtenerFinalVisual(i),
                        Quaternion.identity,
                        transform
                    );
                    particulas.transform.localScale *= _perfilVfx.escalaParticulas;
                    particulas.Play();
                    _particulasImpacto.Add(particulas);
                }
            }
        }

        private void LateUpdate()
        {
            if (_fase != FaseRayo.Activo || _rayos.Count == 0 || _finalesServidor == null)
                return;
            float progreso = Mathf.Clamp01(
                (float)(
                    (NetworkManager.ServerTime.Time - _inicioRecorridoServidor)
                    / System.Math.Max(0.01, _duracionRecorrido)
                )
            );
            float tiempoServidor = (float)NetworkManager.ServerTime.Time;
            for (int i = 0; i < _rayos.Count; i++)
            {
                if (_rayos[i] == null)
                    continue;
                Vector3 inicio = ObtenerInicioVisual(i);
                Vector3 finalVisible = Vector3.Lerp(inicio, ObtenerFinalVisual(i), progreso);
                Vector3 direccion = finalVisible - inicio;
                Vector3 lateral = Vector3.Cross(Vector3.up, direccion).normalized;
                float semilla = (NetworkObjectId % 65521UL) + i * 173.37f;
                float tiempoMuestreo = Mathf.Floor(tiempoServidor * FrecuenciaCentelleo) * 0.173f;
                float pulso = Mathf.Lerp(
                    0.62f,
                    1f,
                    Mathf.PerlinNoise(semilla + 29.1f, tiempoServidor * FrecuenciaCentelleo)
                );
                float intensidadRecorrido = Mathf.Clamp01(progreso * 2f);

                for (int punto = 0; punto <= SegmentosVisualesRayo; punto++)
                {
                    float t = (float)punto / SegmentosVisualesRayo;
                    Vector3 posicion = Vector3.Lerp(inicio, finalVisible, t);
                    if (punto > 0 && punto < SegmentosVisualesRayo)
                    {
                        float envolvente = Mathf.Sin(Mathf.PI * t);
                        float ruidoLateral =
                            Mathf.PerlinNoise(
                                semilla
                                    + punto
                                        * 0.419f
                                        * (_perfilVfx != null ? _perfilVfx.frecuenciaForma : 1f),
                                tiempoMuestreo
                            ) * 2f
                            - 1f;
                        float ruidoVertical = Mathf.PerlinNoise(
                            semilla + 61.7f + punto * 0.283f,
                            tiempoMuestreo
                        );
                        posicion +=
                            lateral
                            * ruidoLateral
                            * Mathf.Min(
                                _perfil.diametro
                                    * (_perfilVfx != null ? _perfilVfx.amplitudOndulacion : 0.12f)
                                    * (_perfilVfx != null ? _perfilVfx.formaOndulada : 1f),
                                0.65f
                            )
                            * envolvente
                            * intensidadRecorrido;
                        posicion +=
                            Vector3.up
                            * (ruidoVertical - 0.5f)
                            * (_perfilVfx != null ? _perfilVfx.ondulacionVertical : 0.7f)
                            * envolvente
                            * intensidadRecorrido;
                    }

                    _posicionesNucleo[punto] = posicion;
                    _posicionesHalo[punto] = posicion;
                }

                _rayos[i].SetPositions(_posicionesNucleo);
                _halos[i].SetPositions(_posicionesHalo);
                ActualizarMallaRayo(i, _posicionesNucleo);
                var colorNucleo = Color.white;
                colorNucleo.a = pulso;
                _rayos[i].startColor = _rayos[i].endColor = colorNucleo;
                var colorHalo = Color.white;
                colorHalo.a = pulso * 0.72f;
                _halos[i].startColor = _halos[i].endColor = colorHalo;
            }
        }

        public override void OnNetworkDespawn()
        {
            DetenerSfxJefe(_sfxCarga);
            DetenerSfxJefe(_sfxDisparoLoop);
            LimpiarVisuales();
            base.OnNetworkDespawn();
        }

        private void OnDestroy() => LimpiarVisuales();

        [Rpc(SendTo.ClientsAndHost)]
        private void ApagarRpc()
        {
            _fase = FaseRayo.Retirada;
            foreach (var rayo in _rayos)
                rayo.enabled = false;
            foreach (var halo in _halos)
                halo.enabled = false;
            foreach (var meshRenderer in _renderersMallaRayo)
                meshRenderer.enabled = false;

            foreach (var franja in _franjas)
                franja.SetActive(false);
        }

        private GameObject CrearFranja(Vector3 origen, Vector3 final)
        {
            Vector3 inicio =
                new Vector3(origen.x, final.y, origen.z) + Vector3.up * DesplazamientoVisualY;
            Vector3 finalVisual = final + Vector3.up * DesplazamientoVisualY;
            Vector3 recorrido = finalVisual - inicio;

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

        private float DesplazamientoVisualY =>
            _perfilVfx != null ? _perfilVfx.desplazamientoVisualY : 0f;

        private Vector3 ObtenerInicioVisual(int indice)
        {
            return new Vector3(_origenServidor.x, _finalesServidor[indice].y, _origenServidor.z)
                + Vector3.up * DesplazamientoVisualY;
        }

        private Vector3 ObtenerFinalVisual(int indice)
        {
            return _finalesServidor[indice] + Vector3.up * DesplazamientoVisualY;
        }

        private LineRenderer CrearRayo(Vector3 origen, Vector3 final)
        {
            if (_materialNucleoRuntime == null)
                _materialNucleoRuntime = CrearMaterialRayo(
                    _perfilVfx != null ? _perfilVfx.materialNucleo : _materialRayo,
                    _perfilVfx != null ? _perfilVfx.colorNucleo : new Color(1.8f, 2.4f, 3f, 1f)
                );
            if (_materialHaloRuntime == null)
                _materialHaloRuntime = CrearMaterialRayo(
                    _perfilVfx != null ? _perfilVfx.materialHalo : _materialRayo,
                    _perfilVfx != null ? _perfilVfx.colorHalo : new Color(2.2f, 1.05f, 0.18f, 0.85f)
                );

            var objetoHalo = new GameObject("RayoHalo");
            objetoHalo.transform.SetParent(transform, false);
            var halo = ConfigurarLinea(
                objetoHalo,
                _materialHaloRuntime,
                Mathf.Clamp(
                    _perfil.diametro * (_perfilVfx != null ? _perfilVfx.anchoHalo : 0.13f),
                    0.01f,
                    8f
                ),
                origen
            );
            _halos.Add(halo);

            var objetoMalla = new GameObject("RayoMallaAllIn");
            objetoMalla.transform.SetParent(transform, false);
            var meshFilter = objetoMalla.AddComponent<MeshFilter>();
            var meshRenderer = objetoMalla.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _perfilVfx != null ? _perfilVfx.materialMesh : null;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = false;
            meshFilter.sharedMesh = new Mesh { name = "RayoDinamico" };
            _mallasRayo.Add(meshFilter);
            _renderersMallaRayo.Add(meshRenderer);

            var objetoNucleo = new GameObject("RayoNucleo");
            objetoNucleo.transform.SetParent(transform, false);
            var linea = ConfigurarLinea(
                objetoNucleo,
                _materialNucleoRuntime,
                Mathf.Clamp(
                    _perfil.diametro * (_perfilVfx != null ? _perfilVfx.anchoNucleo : 0.026f),
                    0.01f,
                    8f
                ),
                origen
            );
            linea.enabled = false;
            halo.enabled = false;
            return linea;
        }

        private void ActualizarMallaRayo(int indice, Vector3[] centro)
        {
            if (
                _perfilVfx == null
                || _perfilVfx.materialMesh == null
                || indice >= _mallasRayo.Count
            )
                return;

            Mesh mesh = _mallasRayo[indice].sharedMesh;
            var vertices = new Vector3[(SegmentosVisualesRayo + 1) * 2];
            var normales = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangulos = new int[SegmentosVisualesRayo * 12];
            Vector3 camara =
                UnityEngine.Camera.main != null
                    ? UnityEngine.Camera.main.transform.position
                    : centro[0] + Vector3.up * 10f;
            float ancho = Mathf.Max(0f, _perfilVfx.anchoMesh);

            for (int p = 0; p <= SegmentosVisualesRayo; p++)
            {
                Vector3 tangente =
                    p == SegmentosVisualesRayo
                        ? centro[p] - centro[p - 1]
                        : centro[p + 1] - centro[p];
                Vector3 anchoDir = Vector3
                    .Cross(tangente.normalized, (camara - centro[p]).normalized)
                    .normalized;
                if (anchoDir.sqrMagnitude < 0.001f)
                    anchoDir = Vector3.Cross(tangente.normalized, Vector3.up).normalized;
                float t = (float)p / SegmentosVisualesRayo;
                float taper = Mathf.Sin(t * Mathf.PI * 0.5f);
                vertices[p * 2] = centro[p] - anchoDir * (ancho * taper);
                vertices[p * 2 + 1] = centro[p] + anchoDir * (ancho * taper);
                Vector3 normal = Vector3.Cross(tangente.normalized, anchoDir).normalized;
                normales[p * 2] = normales[p * 2 + 1] = normal;
                uv[p * 2] = new Vector2(0f, t);
                uv[p * 2 + 1] = new Vector2(1f, t);
                if (p < SegmentosVisualesRayo)
                {
                    int ti = p * 12;
                    int vi = p * 2;
                    triangulos[ti] = vi;
                    triangulos[ti + 1] = vi + 2;
                    triangulos[ti + 2] = vi + 1;
                    triangulos[ti + 3] = vi + 1;
                    triangulos[ti + 4] = vi + 2;
                    triangulos[ti + 5] = vi + 3;
                    triangulos[ti + 6] = vi + 1;
                    triangulos[ti + 7] = vi + 2;
                    triangulos[ti + 8] = vi;
                    triangulos[ti + 9] = vi + 3;
                    triangulos[ti + 10] = vi + 2;
                    triangulos[ti + 11] = vi + 1;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.normals = normales;
            mesh.uv = uv;
            mesh.triangles = triangulos;
            mesh.RecalculateBounds();
            _renderersMallaRayo[indice].enabled = true;
        }

        private Material CrearMaterialRayo(Material materialBase, Color color)
        {
            Shader shader =
                materialBase != null
                    ? materialBase.shader
                    : Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            var material = materialBase != null ? new Material(materialBase) : new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }
            return material;
        }

        private static LineRenderer ConfigurarLinea(
            GameObject objeto,
            Material material,
            float ancho,
            Vector3 posicionInicial
        )
        {
            var linea = objeto.AddComponent<LineRenderer>();
            linea.useWorldSpace = true;
            linea.alignment = LineAlignment.View;
            linea.textureMode = LineTextureMode.Stretch;
            linea.positionCount = SegmentosVisualesRayo + 1;
            linea.startWidth = ancho;
            linea.endWidth = ancho;
            linea.numCapVertices = 2;
            linea.numCornerVertices = 2;
            linea.sharedMaterial = material;
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linea.receiveShadows = false;
            for (int punto = 0; punto <= SegmentosVisualesRayo; punto++)
                linea.SetPosition(punto, posicionInicial);
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

            foreach (var halo in _halos)
                if (halo != null)
                    Destroy(halo.gameObject);

            foreach (var particulas in _particulasImpacto)
                if (particulas != null)
                    Destroy(particulas.gameObject);

            foreach (var meshFilter in _mallasRayo)
                if (meshFilter != null)
                {
                    if (meshFilter.sharedMesh != null)
                        Destroy(meshFilter.sharedMesh);
                    Destroy(meshFilter.gameObject);
                }

            if (_materialNucleoRuntime != null)
                Destroy(_materialNucleoRuntime);
            if (_materialHaloRuntime != null)
                Destroy(_materialHaloRuntime);

            _franjas.Clear();
            _rayos.Clear();
            _halos.Clear();
            _particulasImpacto.Clear();
            _mallasRayo.Clear();
            _renderersMallaRayo.Clear();
            _materialNucleoRuntime = null;
            _materialHaloRuntime = null;
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void JugadorGolpeadoRpc(RpcParams rpcParams = default)
        {
            NotificarGolpeAlJugadorLocal();
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
