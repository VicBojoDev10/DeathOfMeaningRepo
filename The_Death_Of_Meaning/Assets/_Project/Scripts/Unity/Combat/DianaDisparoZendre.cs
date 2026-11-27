using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>Recibe los disparos de Zendre y desbloquea su muro al acumular daño.</summary>
    public sealed class DianaDisparoZendre : NetworkBehaviour
    {
        private const float DuracionFlash = 0.25f;
        private const float IntervaloParpadeo = 0.05f;
        private const float AmplitudSacudida = 0.035f;
        private const float DuracionDesaparicion = 0.35f;
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColor = Shader.PropertyToID("_Color");

        [SerializeField, Min(1f)]
        private float _vidaMaxima = 100f;

        [SerializeField]
        private GameObject _muroDisparoZendre;

        private readonly NetworkVariable<float> _danioAcumulado = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        private readonly NetworkVariable<bool> _destruida = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private Renderer[] _renderers;
        private Material[][] _materiales;
        private Color[][] _coloresBase;
        private Vector3 _posicionLocalInicial;
        private Vector3 _posicionMuroInicial;
        private Vector3 _escalaMuroInicial;
        private Coroutine _flash;
        private Coroutine _desaparicion;

        private void Awake()
        {
            _posicionLocalInicial = transform.localPosition;
            if (_muroDisparoZendre == null)
                return;

            _posicionMuroInicial = _muroDisparoZendre.transform.localPosition;
            _escalaMuroInicial = _muroDisparoZendre.transform.localScale;
        }

        public override void OnNetworkSpawn()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            PrepararMateriales();
            RestaurarAlReiniciarPartida();
            _destruida.OnValueChanged += AlCambiarDestruida;
            AplicarEstado(_destruida.Value);
        }

        public override void OnNetworkDespawn()
        {
            _destruida.OnValueChanged -= AlCambiarDestruida;
            DetenerAnimaciones();
            LiberarMateriales();
        }

        public void RecibirDisparoZendre(float dano)
        {
            if (!IsServer || _destruida.Value || dano <= 0f)
                return;

            FeedbackGolpeRpc();
            _danioAcumulado.Value = Mathf.Min(_vidaMaxima, _danioAcumulado.Value + dano);
            if (_danioAcumulado.Value >= _vidaMaxima)
                _destruida.Value = true;
        }

        public void RestaurarAlReiniciarPartida()
        {
            if (!IsServer)
                return;

            DetenerAnimaciones();
            transform.localPosition = _posicionLocalInicial;
            PintarColorBase();
            _danioAcumulado.Value = 0f;
            _destruida.Value = false;
            AplicarEstado(false);
        }

        private void AlCambiarDestruida(bool anterior, bool actual) => AplicarEstado(actual);

        [Rpc(SendTo.ClientsAndHost)]
        private void FeedbackGolpeRpc()
        {
            if (_destruida.Value)
                return;
            if (_flash != null)
                StopCoroutine(_flash);
            _flash = StartCoroutine(AnimarGolpe());
        }

        private IEnumerator AnimarGolpe()
        {
            float transcurrido = 0f;
            float siguienteParpadeo = 0f;
            bool rojo = true;
            while (transcurrido < DuracionFlash)
            {
                if (transcurrido >= siguienteParpadeo)
                {
                    Pintar(rojo ? Color.red : ColorBasePromedio());
                    rojo = !rojo;
                    siguienteParpadeo += IntervaloParpadeo;
                }
                transform.localPosition =
                    _posicionLocalInicial + Random.insideUnitSphere * AmplitudSacudida;
                transcurrido += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = _posicionLocalInicial;
            PintarColorBase();
            _flash = null;
        }

        private void AplicarEstado(bool destruida)
        {
            if (_muroDisparoZendre == null)
                return;

            if (!destruida)
            {
                if (_desaparicion != null)
                    StopCoroutine(_desaparicion);
                _desaparicion = null;
                _muroDisparoZendre.SetActive(true);
                Transform muroRestaurado = _muroDisparoZendre.transform;
                muroRestaurado.localPosition = _posicionMuroInicial;
                muroRestaurado.localScale = _escalaMuroInicial;
                foreach (Collider c in _muroDisparoZendre.GetComponentsInChildren<Collider>(true))
                    if (c != null)
                        c.enabled = true;
                Renderer[] renderersRestaurados =
                    _muroDisparoZendre.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderersRestaurados)
                    if (r != null)
                        r.enabled = true;
                PintarMuro(ColorBasePromedio(), renderersRestaurados);
                return;
            }

            if (_desaparicion != null)
                StopCoroutine(_desaparicion);
            _desaparicion = StartCoroutine(EncogerMuro());
        }

        private IEnumerator EncogerMuro()
        {
            Transform muro = _muroDisparoZendre.transform;
            Vector3 posicionInicial = muro.localPosition;
            Collider[] colliders = _muroDisparoZendre.GetComponentsInChildren<Collider>(true);
            Renderer[] renderers = _muroDisparoZendre.GetComponentsInChildren<Renderer>(true);
            foreach (Collider c in colliders)
                if (c != null)
                    c.enabled = false;

            float transcurrido = 0f;
            float siguienteParpadeo = 0f;
            bool rojo = true;
            while (transcurrido < DuracionDesaparicion)
            {
                if (transcurrido >= siguienteParpadeo)
                {
                    PintarMuro(rojo ? Color.red : ColorBasePromedio(), renderers);
                    rojo = !rojo;
                    siguienteParpadeo += IntervaloParpadeo;
                }
                float progreso = Mathf.Clamp01(transcurrido / DuracionDesaparicion);
                muro.localScale = Vector3.Lerp(_escalaMuroInicial, Vector3.zero, progreso);
                muro.localPosition = posicionInicial + Random.insideUnitSphere * AmplitudSacudida;
                transcurrido += Time.deltaTime;
                yield return null;
            }
            _muroDisparoZendre.SetActive(false);
            _desaparicion = null;
        }

        private void PrepararMateriales()
        {
            _materiales = new Material[_renderers.Length][];
            _coloresBase = new Color[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;
                _materiales[i] = _renderers[i].materials;
                _coloresBase[i] = new Color[_materiales[i].Length];
                for (int j = 0; j < _materiales[i].Length; j++)
                    _coloresBase[i][j] = ObtenerColor(_materiales[i][j]);
            }
        }

        private static Color ObtenerColor(Material material)
        {
            if (material == null)
                return Color.white;
            if (material.HasProperty(IdColorBase))
                return material.GetColor(IdColorBase);
            if (material.HasProperty(IdColor))
                return material.GetColor(IdColor);
            return Color.white;
        }

        private Color ColorBasePromedio()
        {
            if (_coloresBase == null)
                return Color.white;
            foreach (Color[] colores in _coloresBase)
                if (colores != null && colores.Length > 0)
                    return colores[0];
            return Color.white;
        }

        private void PintarColorBase()
        {
            if (_materiales == null || _coloresBase == null)
                return;
            for (int i = 0; i < _materiales.Length; i++)
                if (_materiales[i] != null)
                    for (int j = 0; j < _materiales[i].Length; j++)
                        PintarMaterial(_materiales[i][j], _coloresBase[i][j]);
        }

        private void Pintar(Color color)
        {
            if (_materiales == null)
                return;
            foreach (Material[] grupo in _materiales)
                if (grupo != null)
                    foreach (Material material in grupo)
                        PintarMaterial(material, color);
        }

        private static void PintarMuro(Color color, Renderer[] renderers)
        {
            foreach (Renderer renderer in renderers)
                if (renderer != null)
                    foreach (Material material in renderer.materials)
                        PintarMaterial(material, color);
        }

        private static void PintarMaterial(Material material, Color color)
        {
            if (material == null)
                return;
            if (material.HasProperty(IdColorBase))
                material.SetColor(IdColorBase, color);
            if (material.HasProperty(IdColor))
                material.SetColor(IdColor, color);
        }

        private void DetenerAnimaciones()
        {
            if (_flash != null)
                StopCoroutine(_flash);
            if (_desaparicion != null)
                StopCoroutine(_desaparicion);
            _flash = null;
            _desaparicion = null;
        }

        private void LiberarMateriales()
        {
            if (_materiales == null)
                return;
            foreach (Material[] grupo in _materiales)
                if (grupo != null)
                    foreach (Material material in grupo)
                        if (material != null)
                            Destroy(material);
            _materiales = null;
            _coloresBase = null;
        }
    }
}
