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
    public class AylaEspada : MonoBehaviour
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

        [Header("Visibilidad")]
        [SerializeField, Tooltip("Si está activo, la espada solo se ve mientras Ayla ataca.")]
        private bool _soloVisibleAtacando = true;

        [SerializeField, Min(0f), Tooltip("Segundos que sigue visible tras salir del ataque.")]
        private float _retrasoOcultar = 0.3f;

        [SerializeField]
        private string[] _estadosDeAtaque = { "Attack", "1i", "2IC", "Charged" };

        private Transform _hueso;
        private Transform _socket;
        private GameObject _visual;
        private int[] _hashesAtaque;
        private float _visibleRestante;
        private bool _visibleActual = true;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            _hashesAtaque = new int[_estadosDeAtaque.Length];
            for (int i = 0; i < _estadosDeAtaque.Length; i++)
                _hashesAtaque[i] = Animator.StringToHash(_estadosDeAtaque[i]);
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

            foreach (Collider c in _visual.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
            foreach (Transform t in _visual.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = _hueso.gameObject.layer;

            // Centra el mango en el origen del socket: así posición/rotación/escala giran
            // alrededor del puño y no alrededor del origen del FBX.
            ActualizarSocket();
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
        }

        private void LateUpdate()
        {
            if (_socket == null)
                return;

            ActualizarSocket();

            if (!_soloVisibleAtacando)
            {
                AplicarVisibilidad(true);
                return;
            }

            if (EnAtaque())
                _visibleRestante = _retrasoOcultar;
            else
                _visibleRestante -= Time.deltaTime;

            AplicarVisibilidad(_visibleRestante > 0f);
        }

        private void ActualizarSocket()
        {
            // El socket compensa la escala del hueso para que _escala y _posicion
            // sean siempre medidas del mundo, aunque el rig venga escalado.
            Vector3 p = _hueso.lossyScale;
            Vector3 inv = new Vector3(Inv(p.x), Inv(p.y), Inv(p.z));
            _socket.localScale = inv * _escala;
            _socket.localRotation = Quaternion.Euler(_rotacion);
            _socket.localPosition = Vector3.Scale(_posicion, inv);
        }

        private static float Inv(float v) => Mathf.Abs(v) < 1e-5f ? 1f : 1f / v;

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
