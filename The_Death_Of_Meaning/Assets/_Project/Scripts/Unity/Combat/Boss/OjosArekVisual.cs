using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    [RequireComponent(typeof(BossController))]
    public sealed class OjosArekVisual : MonoBehaviour
    {
        [SerializeField]
        private BossController _jefe;

        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColorEmision = Shader.PropertyToID("_EmissionColor");
        private const string KeywordEmision = "_EMISSION";

        private Material[] _materialesOjos;

        private void Awake()
        {
            if (_jefe == null)
                _jefe = GetComponent<BossController>();

            PrepararMateriales();
        }

        private void OnEnable() => _jefe.EstadoOjoCambiado += Pintar;

        private void OnDisable() => _jefe.EstadoOjoCambiado -= Pintar;

        private void PrepararMateriales()
        {
            var zonas = _jefe.HitZones;
            if (zonas == null || zonas.Length == 0)
                zonas = GetComponentsInChildren<HitZone>();

            _materialesOjos = new Material[zonas.Length];

            for (int ojo = 0; ojo < zonas.Length; ojo++)
            {
                var render = zonas[ojo] != null ? zonas[ojo].GetComponent<Renderer>() : null;
                if (render == null)
                    continue;

                _materialesOjos[ojo] = render.material;
                _materialesOjos[ojo].EnableKeyword(KeywordEmision);
            }
        }

        private void Pintar(int ojo, EstadoOjo estado)
        {
            var perfil = _jefe.PerfilOjos;
            if (perfil == null || ojo < 0 || ojo >= _materialesOjos.Length)
                return;

            var material = _materialesOjos[ojo];
            if (material == null)
                return;

            Color color = ColorDe(perfil, estado);
            material.SetColor(IdColorBase, color);
            material.SetColor(IdColorEmision, color * perfil.IntensidadEmision);
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
