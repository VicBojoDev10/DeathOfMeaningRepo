using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>Bloquea el pasillo hasta que Ayla lo golpea con melee.</summary>
    public sealed class ParedMeleeAyla : NetworkBehaviour
    {
        private const float DuracionFlashGolpe = 0.25f;
        private const float IntervaloParpadeo = 0.05f;
        private const float AmplitudSacudida = 0.035f;
        private const float DuracionDesaparicion = 0.35f;
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColor = Shader.PropertyToID("_Color");

        [SerializeField, Min(1f)]
        private float _vidaMaxima = 100f;

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
        private Collider[] _colliders;
        private Material[][] _materiales;
        private Color[][] _coloresBase;
        private Vector3 _posicionInicial;
        private Vector3 _escalaInicial;
        private Coroutine _flashGolpe;
        private Coroutine _encogimiento;

        public Collider[] Colliders
        {
            get
            {
                PrepararComponentes();
                return _colliders;
            }
        }

        public override void OnNetworkSpawn()
        {
            PrepararComponentes();
            _posicionInicial = transform.position;
            _escalaInicial = transform.localScale;
            PrepararMateriales();
            _destruida.OnValueChanged += AlCambiarEstado;
            AplicarEstado(_destruida.Value);
        }

        public override void OnNetworkDespawn()
        {
            _destruida.OnValueChanged -= AlCambiarEstado;
            if (_flashGolpe != null)
                StopCoroutine(_flashGolpe);
            if (_encogimiento != null)
                StopCoroutine(_encogimiento);
            LiberarMateriales();
        }

        public void RecibirMeleeDeAyla(float dano)
        {
            if (!IsServer || _destruida.Value || dano <= 0f)
                return;

            FeedbackGolpeRpc();
            _danioAcumulado.Value = Mathf.Min(_vidaMaxima, _danioAcumulado.Value + dano);
            if (_danioAcumulado.Value < _vidaMaxima)
                return;

            _destruida.Value = true;
            Debug.Log(
                $"[ParedMeleeAyla] Pasillo desbloqueado al recibir {_danioAcumulado.Value:F1} de daño melee de Ayla."
            );
        }

        public void RestaurarAlReiniciarPartida()
        {
            if (!IsServer)
                return;

            _danioAcumulado.Value = 0f;
            _destruida.Value = false;
        }

        private void AlCambiarEstado(bool anterior, bool actual) => AplicarEstado(actual);

        private void AplicarEstado(bool destruida)
        {
            PrepararComponentes();
            if (destruida)
            {
                IniciarEncogimiento();
                return;
            }

            DetenerAnimacionesVisuales();
            transform.position = _posicionInicial;
            transform.localScale = _escalaInicial;
            PintarColorBase();
            foreach (Renderer renderer in _renderers)
                if (renderer != null)
                    renderer.enabled = true;
            foreach (Collider collider in _colliders)
                if (collider != null)
                    collider.enabled = true;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void FeedbackGolpeRpc()
        {
            if (_destruida.Value)
                return;

            if (_flashGolpe != null)
                StopCoroutine(_flashGolpe);
            transform.position = _posicionInicial;
            PintarColorBase();
            _flashGolpe = StartCoroutine(AnimarGolpe());
        }

        private IEnumerator AnimarGolpe()
        {
            float transcurrido = 0f;
            bool rojo = true;
            float siguienteParpadeo = 0f;
            while (transcurrido < DuracionFlashGolpe)
            {
                if (transcurrido >= siguienteParpadeo)
                {
                    if (rojo)
                        Pintar(Color.red);
                    else
                        PintarColorBase();
                    rojo = !rojo;
                    siguienteParpadeo += IntervaloParpadeo;
                }

                transform.position =
                    _posicionInicial + Random.insideUnitSphere * AmplitudSacudida;
                transcurrido += Time.deltaTime;
                yield return null;
            }

            transform.position = _posicionInicial;
            PintarColorBase();
            _flashGolpe = null;
        }

        private void IniciarEncogimiento()
        {
            DetenerAnimacionesVisuales();
            transform.position = _posicionInicial;
            transform.localScale = _escalaInicial;
            PintarColorBase();
            foreach (Collider collider in _colliders)
                if (collider != null)
                    collider.enabled = false;
            foreach (Renderer renderer in _renderers)
                if (renderer != null)
                    renderer.enabled = true;
            _encogimiento = StartCoroutine(EncogerHastaDesaparecer());
        }

        private IEnumerator EncogerHastaDesaparecer()
        {
            float transcurrido = 0f;
            float siguienteParpadeo = 0f;
            bool rojo = true;
            while (transcurrido < DuracionDesaparicion)
            {
                if (transcurrido >= siguienteParpadeo)
                {
                    if (rojo)
                        Pintar(Color.red);
                    else
                        PintarColorBase();
                    rojo = !rojo;
                    siguienteParpadeo += IntervaloParpadeo;
                }

                float progreso = Mathf.Clamp01(transcurrido / DuracionDesaparicion);
                transform.localScale = Vector3.Lerp(_escalaInicial, Vector3.zero, progreso);
                transform.position =
                    _posicionInicial + Random.insideUnitSphere * AmplitudSacudida;
                transcurrido += Time.deltaTime;
                yield return null;
            }

            transform.position = _posicionInicial;
            transform.localScale = Vector3.zero;
            PintarColorBase();
            foreach (Renderer renderer in _renderers)
                if (renderer != null)
                    renderer.enabled = false;
            _encogimiento = null;
        }

        private void DetenerAnimacionesVisuales()
        {
            if (_flashGolpe != null)
            {
                StopCoroutine(_flashGolpe);
                _flashGolpe = null;
            }
            if (_encogimiento != null)
            {
                StopCoroutine(_encogimiento);
                _encogimiento = null;
            }
        }

        private void PrepararMateriales()
        {
            if (_materiales != null)
                return;

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

        private void PintarColorBase()
        {
            if (_materiales == null || _coloresBase == null)
                return;

            for (int i = 0; i < _materiales.Length; i++)
                for (int j = 0; _materiales[i] != null && j < _materiales[i].Length; j++)
                    PintarMaterial(_materiales[i][j], _coloresBase[i][j]);
        }

        private void Pintar(Color color)
        {
            if (_materiales == null)
                return;

            foreach (Material[] materiales in _materiales)
                if (materiales != null)
                    foreach (Material material in materiales)
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

        private void LiberarMateriales()
        {
            if (_materiales == null)
                return;

            foreach (Material[] materiales in _materiales)
                if (materiales != null)
                    foreach (Material material in materiales)
                        if (material != null)
                            Destroy(material);
            _materiales = null;
            _coloresBase = null;
        }

        private void PrepararComponentes()
        {
            if (_renderers == null)
                _renderers = GetComponentsInChildren<Renderer>(true);
            if (_colliders == null)
                _colliders = GetComponentsInChildren<Collider>(true);
        }
    }
}
