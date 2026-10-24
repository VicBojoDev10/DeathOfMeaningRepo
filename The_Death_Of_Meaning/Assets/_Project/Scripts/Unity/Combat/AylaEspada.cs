using System;
using TDOM.Data;
using TDOM.Gameplay.Vfx;
using TDOM.Unity.Vfx;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Pone la espada de Ayla en un hueso de su rig (mano derecha por defecto) en tiempo real,
    /// sin tocar los clips de animación: la espada se mueve con el hueso, así que sigue
    /// cualquier animación. Por defecto solo es visible mientras Ayla está en un estado de
    /// ataque del Animator (se detecta localmente, por eso funciona igual en todos los clientes).
    ///
    /// Uso: añadir este componente al GameObject que tiene el Animator de Ayla (VisualRoot)
    /// o a la raíz del jugador, y arrastrar EspadaAyla.fbx (o un prefab suyo) a "Espada Prefab".
    /// Los valores de Posición / Rotación / Escala se aplican cada frame, así que se pueden
    /// ajustar con el juego corriendo.
    /// </summary>
    [DisallowMultipleComponent]
    public class AylaEspada : MonoBehaviour, IOrigenVfxGolpe, ICargaVfx
    {
        [Header("Referencias")]
        [SerializeField]
        private Animator _animator;

        [SerializeField, Tooltip("EspadaAyla.fbx o un prefab hecho a partir de él.")]
        private GameObject _espadaPrefab;

        [Header("Hueso")]
        [SerializeField]
        private HumanBodyBones _huesoHumanoide = HumanBodyBones.RightHand;

        [
            SerializeField,
            Tooltip("Solo se usa si el avatar no es Humanoid o no encuentra el hueso.")
        ]
        private string _nombreHuesoRespaldo = "DEF-hand.R";

        [Header("Ajuste (editable en Play)")]
        [SerializeField, Tooltip("Desplazamiento del puño respecto al hueso, en metros del mundo.")]
        private Vector3 _posicion = Vector3.zero;

        [SerializeField, Tooltip("Rotación de la espada respecto al hueso, en grados.")]
        private Vector3 _rotacion = Vector3.zero;

        [
            SerializeField,
            Min(0.01f),
            Tooltip("Tamaño relativo al del modelo original (1 = tal cual el FBX).")
        ]
        private float _escala = 0.5f;

        [Header("Agarre")]
        [
            SerializeField,
            Tooltip(
                "Parte del modelo que se pone en la mano (centro de su Renderer). Vacío = origen del FBX."
            )
        ]
        private string _nombreMango = "MangoEspada";

        [
            SerializeField,
            Tooltip(
                "Parte del modelo que es la hoja. Su lado mas largo se toma como el eje de la "
                    + "espada para los giros de 'Ajuste por estado'."
            )
        ]
        private string _nombreHoja = "OjaPrincipalEspada";

        [Header("Ataque cargado")]
        [
            SerializeField,
            Tooltip("Hijo de la espada con la silueta que cambia de color mientras se carga.")
        ]
        private string _nombreCarga = "Carga";

        [SerializeField, Tooltip("Hijo de la espada con la ráfaga al soltar el cargado.")]
        private string _nombreLiberacion = "Liberacion";

        [SerializeField, Tooltip("Colores de la silueta y emisión de las partículas del cuerpo.")]
        private CargaEspadaProfile _perfilCarga;

        [SerializeField, Tooltip("Partículas alrededor del cuerpo de Ayla mientras carga.")]
        private ParticleSystem _particulasCuerpo;

        [Header("Visibilidad")]
        [SerializeField, Tooltip("Si está activo, la espada solo se ve mientras Ayla ataca.")]
        private bool _soloVisibleAtacando = true;

        [SerializeField, Min(0f), Tooltip("Segundos que sigue visible tras salir del ataque.")]
        private float _retrasoOcultar = 0.3f;

        [SerializeField]
        private string[] _estadosDeAtaque = { "Attack", "1i", "2IC", "Charged", "1iCargado" };

        [Serializable]
        private struct PerfilEstelaPorEstado
        {
            [Tooltip("Nombre del estado del Animator.")]
            public string Estado;

            [Tooltip("Id del perfil en el componente EstelaEspada (vacio = el de por defecto).")]
            public string IdPerfil;
        }

        [Header("Estela")]
        [
            SerializeField,
            Tooltip(
                "Perfil de estela por estado de ataque. Los estados que no estan aqui usan el "
                    + "perfil por defecto. La estela se dibuja sola mientras Ayla ataca y la "
                    + "espada se mueve rapido (umbrales en el componente EstelaEspada)."
            )
        ]
        private PerfilEstelaPorEstado[] _perfilesEstela =
        {
            new PerfilEstelaPorEstado { Estado = "Charged", IdPerfil = "Cargado" },
        };

        [
            SerializeField,
            Tooltip(
                "Pone la base y la punta de la estela en los extremos de la malla de la hoja al "
                    + "crear la espada. Si se desactiva (o no encuentra la hoja) se usan los "
                    + "marcadores EstelaBase y EstelaPunta del prefab."
            )
        ]
        private bool _ajustarEstelaALaHoja = true;

        [Serializable]
        private struct AjusteEstado
        {
            [Tooltip("Nombre del estado del Animator.")]
            public string Estado;

            [Tooltip(
                "Giro de la espada sobre el eje de su hoja, en grados (el eje se detecta solo "
                    + "del modelo). -90 hace que el golpe vaya de lado a lado con el filo."
            )]
            public float GiroSobreEjeHoja;

            [Tooltip("Rotacion extra adicional, en grados (sobre los ejes de la espada).")]
            public Vector3 RotacionExtra;

            [Tooltip("Desplazamiento extra del puño, en metros del mundo.")]
            public Vector3 PosicionExtra;
        }

        [Header("Ajuste por estado (editable en Play)")]
        [
            SerializeField,
            Tooltip(
                "Correcciones extra mientras el Animator esta en cada estado. Sirven para "
                    + "clips cuyo agarre de la espada no coincide con el resto."
            )
        ]
        private AjusteEstado[] _ajustesPorEstado =
        {
            new AjusteEstado { Estado = "1i", GiroSobreEjeHoja = -90f },
            new AjusteEstado { Estado = "1iCargado", GiroSobreEjeHoja = -90f },
        };

        [SerializeField, Min(0f), Tooltip("Segundos que tarda en pasar de un ajuste al siguiente.")]
        private float _tiempoCambioAjuste = 0.06f;

        private Transform _hueso;
        private Transform _socket;
        private GameObject _visual;
        private EstelaEspada _estela;
        private static readonly int ColorSilueta = Shader.PropertyToID("_Color");
        private static readonly int IntensidadSilueta = Shader.PropertyToID("_Intensidad");

        private GameObject _carga;
        private Renderer[] _renderersCarga = new Renderer[0];
        private MaterialPropertyBlock _bloqueCarga;
        private ParticleSystem _liberacion;
        private bool _cargaActiva;
        private float _segundosCargados;
        private float _segundosMaximos;
        private float _transcurridoCarga;
        private bool _forzarEtapaFinal;
        private float[] _iniciosEtapa = new float[0];
        private float _emisionBaseCuerpo;
        private int[] _hashesAtaque;
        private int[] _hashesAjuste;
        private int[] _hashesPerfilEstela;
        private int _ultimoEstadoObjetivo;
        private Quaternion _rotacionExtraActual = Quaternion.identity;
        private Vector3 _ejeHoja = Vector3.up;
        private Vector3 _posicionExtraActual = Vector3.zero;
        private float _visibleRestante;
        private bool _visibleActual = true;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            _hashesAtaque = new int[_estadosDeAtaque.Length];
            for (int i = 0; i < _estadosDeAtaque.Length; i++)
                _hashesAtaque[i] = Animator.StringToHash(_estadosDeAtaque[i]);

            _hashesAjuste = new int[_ajustesPorEstado.Length];
            for (int i = 0; i < _ajustesPorEstado.Length; i++)
                _hashesAjuste[i] = Animator.StringToHash(_ajustesPorEstado[i].Estado);

            _hashesPerfilEstela = new int[_perfilesEstela.Length];
            for (int i = 0; i < _perfilesEstela.Length; i++)
                _hashesPerfilEstela[i] = Animator.StringToHash(_perfilesEstela[i].Estado);

            PrepararCarga();
        }

        private void PrepararCarga()
        {
            _bloqueCarga = new MaterialPropertyBlock();

            if (_perfilCarga != null && _perfilCarga.etapas != null)
            {
                _iniciosEtapa = new float[_perfilCarga.etapas.Length];
                for (int i = 0; i < _iniciosEtapa.Length; i++)
                    _iniciosEtapa[i] = _perfilCarga.etapas[i].desde;
            }

            if (_particulasCuerpo != null)
                _emisionBaseCuerpo = _particulasCuerpo.emission.rateOverTimeMultiplier;
        }

        private void Start()
        {
            if (_animator == null || _espadaPrefab == null)
            {
                Debug.LogWarning($"[AylaEspada] Falta Animator o Espada Prefab en {name}", this);
                enabled = false;
                return;
            }

            _hueso = BuscarHueso();
            if (_hueso == null)
            {
                Debug.LogWarning($"[AylaEspada] No se encontró el hueso en {name}", this);
                enabled = false;
                return;
            }

            Crear();
            AplicarVisibilidad(!_soloVisibleAtacando);
        }

        private Transform BuscarHueso()
        {
            if (_animator.isHuman)
            {
                Transform t = _animator.GetBoneTransform(_huesoHumanoide);
                if (t != null)
                    return t;
            }

            if (string.IsNullOrEmpty(_nombreHuesoRespaldo))
                return null;

            foreach (Transform t in _animator.GetComponentsInChildren<Transform>(true))
                if (t.name == _nombreHuesoRespaldo)
                    return t;
            return null;
        }

        private void Crear()
        {
            _socket = new GameObject("Espada_Socket").transform;
            _socket.SetParent(_hueso, false);
            _socket.gameObject.layer = _hueso.gameObject.layer;

            _visual = Instantiate(_espadaPrefab, _socket);
            _visual.name = "Espada_Visual";
            _visual.transform.localPosition = Vector3.zero;
            _visual.transform.localRotation = Quaternion.identity;
            _visual.transform.localScale = Vector3.one;

            _estela = _visual.GetComponentInChildren<EstelaEspada>(true);
            _carga = BuscarHijo(_nombreCarga)?.gameObject;
            _liberacion = BuscarHijo(_nombreLiberacion)?.GetComponent<ParticleSystem>();
            if (_carga != null)
            {
                _renderersCarga = _carga.GetComponentsInChildren<Renderer>(true);
                _carga.SetActive(_cargaActiva);
            }
            foreach (Collider c in _visual.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
            foreach (Transform t in _visual.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = _hueso.gameObject.layer;

            // Centra el mango en el origen del socket: así posición/rotación/escala giran
            // alrededor del puño y no alrededor del origen del FBX.
            ActualizarSocket();
            DetectarEjeHoja();
            if (!string.IsNullOrEmpty(_nombreMango))
            {
                foreach (Renderer r in _visual.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.name != _nombreMango)
                        continue;
                    Vector3 centroLocal = _visual.transform.InverseTransformPoint(r.bounds.center);
                    _visual.transform.localPosition = -centroLocal;
                    break;
                }
            }

            AjustarEstelaALaHoja();
        }

        // Los extremos de la estela se sacan de la malla de la hoja (su lado mas largo), para
        // que la estela cubra todo el filo sin depender de marcadores puestos a mano. La base
        // es el extremo mas cercano al mango.
        private void AjustarEstelaALaHoja()
        {
            if (!_ajustarEstelaALaHoja || _estela == null || string.IsNullOrEmpty(_nombreHoja))
                return;

            Vector3 centroMango = _socket.position;
            MeshFilter hoja = null;
            foreach (Renderer r in _visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == _nombreMango)
                    centroMango = r.bounds.center;
                else if (r.name == _nombreHoja)
                    hoja = r.GetComponent<MeshFilter>();
            }

            if (!_estela.AjustarAHoja(hoja, centroMango))
                Debug.LogWarning(
                    "[AylaEspada] No se pudo ajustar la estela a la hoja; se usan los marcadores del prefab.",
                    this
                );
        }

        // El eje de la espada se saca de la malla de la hoja (su dimension mas larga), en vez
        // de suponerlo: asi el giro de 180 grados es sobre el eje correcto sin importar como
        // se importe el FBX.
        private void DetectarEjeHoja()
        {
            _ejeHoja = Vector3.up;
            if (string.IsNullOrEmpty(_nombreHoja))
                return;

            foreach (Renderer r in _visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name != _nombreHoja)
                    continue;

                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                    break;

                Vector3 tam = mf.sharedMesh.bounds.size;
                Vector3 largo;
                if (tam.x >= tam.y && tam.x >= tam.z)
                    largo = Vector3.right;
                else
                    largo = tam.y >= tam.z ? Vector3.up : Vector3.forward;

                Vector3 dir = _visual.transform.InverseTransformDirection(
                    r.transform.TransformDirection(largo)
                );
                if (dir.sqrMagnitude > 1e-6f)
                    _ejeHoja = dir.normalized;
                break;
            }

            Debug.Log($"[AylaEspada] eje de la hoja (espacio de la espada): {_ejeHoja}", this);
        }

        private void LateUpdate()
        {
            if (_socket == null)
                return;

            ActualizarSocket();
            ActualizarCarga();
            ActualizarEstela();

            if (!_soloVisibleAtacando)
            {
                AplicarVisibilidad(true);
                return;
            }

            if (_cargaActiva || EnAtaque())
            {
                _visibleRestante = _retrasoOcultar;
            }
            else
            {
                _visibleRestante -= Time.deltaTime;
            }

            AplicarVisibilidad(_visibleRestante > 0f);
        }

        // Eventos de animacion antiguos (EstelaOn / EstelaOff en los clips). La estela ahora
        // depende del estado de ataque y de la velocidad de la hoja, asi que ya no hacen nada;
        // se dejan para que los clips no avisen de un metodo inexistente. Se pueden quitar de
        // los clips cuando convenga.
        public void EstelaOn(string idPerfil) { }

        public void EstelaOff() { }

        private void ActualizarEstela()
        {
            if (_estela == null)
                return;

            int estado = HashEstadoObjetivo();
            if (estado != _ultimoEstadoObjetivo)
            {
                // Cambio de estado (combo, cancelacion): el tramo nuevo no se une al anterior.
                _ultimoEstadoObjetivo = estado;
                _estela.Cortar();
            }

            _estela.Permitir(EnAtaque(), IdPerfilEstela(estado));
        }

        private string IdPerfilEstela(int hashEstado)
        {
            for (int i = 0; i < _hashesPerfilEstela.Length; i++)
                if (_hashesPerfilEstela[i] == hashEstado)
                    return _perfilesEstela[i].IdPerfil;
            return string.Empty;
        }

        // Estado al que va el Animator (si esta en transicion, el de destino).
        private int HashEstadoObjetivo()
        {
            if (_animator == null || _animator.layerCount == 0)
                return 0;

            return _animator.IsInTransition(0)
                ? _animator.GetNextAnimatorStateInfo(0).shortNameHash
                : _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        }

        public void SetCarga(bool activa, float segundosCargados, float segundosMaximos)
        {
            _cargaActiva = activa;
            _segundosCargados = segundosCargados;
            _segundosMaximos = segundosMaximos;
            _transcurridoCarga = 0f;

            if (_carga != null)
                _carga.SetActive(activa);
            AlternarParticulasCuerpo(activa);
            if (!activa || _visual == null)
                return;

            ActualizarCarga();
            _visibleRestante = _retrasoOcultar;
            AplicarVisibilidad(true);
        }

        public int EtapaDe(float progreso) => EtapasCarga.Indice(_iniciosEtapa, progreso);

        public void ForzarEtapaFinal(bool activa) => _forzarEtapaFinal = activa;

        private int EtapaActual(float progreso) =>
            _forzarEtapaFinal ? _iniciosEtapa.Length - 1 : EtapaDe(progreso);

        private void ActualizarCarga()
        {
            if (!_cargaActiva)
                return;

            _transcurridoCarga += Time.deltaTime;
            float progreso = EtapasCarga.Progreso(
                _segundosCargados,
                _transcurridoCarga,
                _segundosMaximos
            );
            PintarSilueta(EtapaActual(progreso));
            EscalarEmisionCuerpo(progreso);
        }

        private void PintarSilueta(int etapa)
        {
            if (etapa == EtapasCarga.SinEtapa)
                return;

            CargaEspadaProfile.Etapa datos = _perfilCarga.etapas[etapa];
            foreach (Renderer r in _renderersCarga)
            {
                r.GetPropertyBlock(_bloqueCarga);
                _bloqueCarga.SetColor(ColorSilueta, datos.color);
                _bloqueCarga.SetFloat(IntensidadSilueta, datos.intensidad);
                r.SetPropertyBlock(_bloqueCarga);
            }
        }

        private void EscalarEmisionCuerpo(float progreso)
        {
            if (_particulasCuerpo == null || _perfilCarga == null)
                return;

            ParticleSystem.EmissionModule emision = _particulasCuerpo.emission;
            emision.rateOverTimeMultiplier =
                _emisionBaseCuerpo * _perfilCarga.emisionCuerpo.Evaluate(progreso);
        }

        private void AlternarParticulasCuerpo(bool activa)
        {
            if (_particulasCuerpo == null)
                return;

            if (activa)
                _particulasCuerpo.Play(true);
            else
                _particulasCuerpo.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public void EspadaBurst()
        {
            if (_liberacion == null)
                return;

            _liberacion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _liberacion.Play(true);
        }

        private Transform BuscarHijo(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
                return null;

            foreach (Transform t in _visual.GetComponentsInChildren<Transform>(true))
                if (t.name == nombre)
                    return t;
            return null;
        }

        public bool Hoja(out Vector3 baseHoja, out Vector3 punta)
        {
            if (_estela != null)
                return _estela.Hoja(out baseHoja, out punta);

            baseHoja = Vector3.zero;
            punta = Vector3.zero;
            return false;
        }

        public bool Estela(out Vector3 centro, out Quaternion rotacion)
        {
            if (_estela != null)
                return _estela.Marco(out centro, out rotacion);

            centro = Vector3.zero;
            rotacion = Quaternion.identity;
            return false;
        }

        private void ActualizarSocket()
        {
            // El socket compensa la escala del hueso para que _escala y _posicion
            // sean siempre medidas del mundo, aunque el rig venga escalado.
            Vector3 p = _hueso.lossyScale;
            Vector3 inv = new Vector3(Inv(p.x), Inv(p.y), Inv(p.z));
            _socket.localScale = inv * _escala;

            ResolverAjusteObjetivo(out Quaternion rotacionObjetivo, out Vector3 posicionObjetivo);
            float k =
                _tiempoCambioAjuste > 0f
                    ? 1f - Mathf.Exp(-Time.deltaTime / (_tiempoCambioAjuste * 0.35f))
                    : 1f;
            _rotacionExtraActual = Quaternion.Slerp(_rotacionExtraActual, rotacionObjetivo, k);
            _posicionExtraActual = Vector3.Lerp(_posicionExtraActual, posicionObjetivo, k);

            // La rotacion extra se aplica sobre el eje de la propia espada (despues de la base).
            _socket.localRotation = Quaternion.Euler(_rotacion) * _rotacionExtraActual;
            _socket.localPosition = Vector3.Scale(_posicion + _posicionExtraActual, inv);
        }

        // Ajuste del estado al que va el Animator (si esta en transicion, el de destino).
        private void ResolverAjusteObjetivo(out Quaternion rotacion, out Vector3 posicion)
        {
            rotacion = Quaternion.identity;
            posicion = Vector3.zero;
            if (_animator == null || _animator.layerCount == 0 || _hashesAjuste.Length == 0)
                return;

            int hash = _animator.IsInTransition(0)
                ? _animator.GetNextAnimatorStateInfo(0).shortNameHash
                : _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            for (int i = 0; i < _hashesAjuste.Length; i++)
            {
                if (_hashesAjuste[i] != hash)
                    continue;
                rotacion =
                    Quaternion.AngleAxis(_ajustesPorEstado[i].GiroSobreEjeHoja, _ejeHoja)
                    * Quaternion.Euler(_ajustesPorEstado[i].RotacionExtra);
                posicion = _ajustesPorEstado[i].PosicionExtra;
                return;
            }
        }

        // Se compensa solo la magnitud: si el modelo esta espejado (escala X negativa para
        // espejar el ataque), la espada hereda el reflejo y queda en el lado correcto.
        private static float Inv(float v) => Mathf.Abs(v) < 1e-5f ? 1f : 1f / Mathf.Abs(v);

        private bool EnAtaque()
        {
            if (_animator.layerCount == 0)
                return false;

            if (EsAtaque(_animator.GetCurrentAnimatorStateInfo(0).shortNameHash))
                return true;

            return _animator.IsInTransition(0)
                && EsAtaque(_animator.GetNextAnimatorStateInfo(0).shortNameHash);
        }

        private bool EsAtaque(int hash)
        {
            for (int i = 0; i < _hashesAtaque.Length; i++)
                if (_hashesAtaque[i] == hash)
                    return true;
            return false;
        }

        private void AplicarVisibilidad(bool visible)
        {
            if (_visual == null || visible == _visibleActual)
                return;
            _visibleActual = visible;
            _visual.SetActive(visible);
        }

        private void OnDestroy()
        {
            if (_socket != null)
                Destroy(_socket.gameObject);
        }
    }
}
