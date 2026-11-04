using UnityEngine;

namespace TDOM.Unity.Vfx
{
    /// <summary>
    /// Burbuja de lava del origen de la Mano de AREK (TW-180): un quad con el shader
    /// TDOM/LavaBurbuja. Local a cada máquina; el ataque le pasa la intensidad (0 a 1) cada
    /// frame y la burbuja se enciende, crece y se apaga con ella.
    /// </summary>
    public sealed class BurbujaLava : MonoBehaviour
    {
        private static readonly int IdProgreso = Shader.PropertyToID("_Progreso");

        [SerializeField]
        private Renderer _renderer;

        private MaterialPropertyBlock _bloque;
        private float _tamano = 1f;
        private float _intensidad = -1f;

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();

            _bloque = new MaterialPropertyBlock();
            Establecer(0f);
        }

        /// <summary>Tamaño de la burbuja en metros; se apoya en el piso del origen.</summary>
        public void Configurar(float tamano)
        {
            _tamano = Mathf.Max(0.01f, tamano);
            transform.localPosition = new Vector3(0f, _tamano * 0.4f, 0f);
            _intensidad = -1f;
        }

        public void Establecer(float intensidad)
        {
            intensidad = Mathf.Clamp01(intensidad);
            if (_renderer == null || Mathf.Approximately(intensidad, _intensidad))
                return;

            _intensidad = intensidad;
            bool visible = intensidad > 0.001f;
            if (_renderer.enabled != visible)
                _renderer.enabled = visible;

            if (!visible)
                return;

            transform.localScale = Vector3.one * (_tamano * Mathf.Lerp(0.6f, 1f, intensidad));
            _renderer.GetPropertyBlock(_bloque);
            _bloque.SetFloat(IdProgreso, intensidad);
            _renderer.SetPropertyBlock(_bloque);
        }
    }
}
