using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    [RequireComponent(typeof(OjosArek), typeof(BossController))]
    public sealed class OjosArekVisual : MonoBehaviour
    {
        [SerializeField]
        private OjosArek _ojos;

        [SerializeField]
        private BossController _jefe;

        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        private Renderer[] _renderersOjos;
        private MaterialPropertyBlock _bloque;

        private void Awake()
        {
            if (_ojos == null)
                _ojos = GetComponent<OjosArek>();
            if (_jefe == null)
                _jefe = GetComponent<BossController>();

            _bloque = new MaterialPropertyBlock();
            PrepararRenderers();
        }

        private void OnEnable() => _ojos.EstadoOjoCambiado += Pintar;

        private void OnDisable() => _ojos.EstadoOjoCambiado -= Pintar;

        private void PrepararRenderers()
        {
            var zonas = _jefe.HitZones;
            if (zonas == null || zonas.Length == 0)
                zonas = GetComponentsInChildren<HitZone>();

            _renderersOjos = new Renderer[zonas.Length];
            for (int ojo = 0; ojo < zonas.Length; ojo++)
                _renderersOjos[ojo] =
                    zonas[ojo] != null ? zonas[ojo].GetComponent<Renderer>() : null;
        }

        private void Pintar(int ojo, EstadoOjo estado)
        {
            var perfil = _ojos.Perfil;
            if (perfil == null || ojo < 0 || ojo >= _renderersOjos.Length)
                return;

            var render = _renderersOjos[ojo];
            if (render == null)
                return;

            render.GetPropertyBlock(_bloque);
            _bloque.SetColor(IdColorBase, ColorDe(perfil, estado));
            render.SetPropertyBlock(_bloque);
        }

        private static Color ColorDe(WeakPointProfile perfil, EstadoOjo estado)
        {
            switch (estado)
            {
                case EstadoOjo.Brillando:
                    return perfil.ColorBrillando;
                case EstadoOjo.Abierto:
                    return perfil.ColorAbierto;
                case EstadoOjo.Enfriando:
                    return perfil.ColorEnfriando;
                default:
                    return perfil.ColorCerrado;
            }
        }
    }
}
