using TDOM.Unity.Vfx;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    [RequireComponent(typeof(OjosArek), typeof(BossController), typeof(VfxPool))]
    public sealed class OjosArekVfx : MonoBehaviour
    {
        [SerializeField]
        private VfxBase _vfxOjoDestruido;

        private OjosArek _ojos;
        private BossController _jefe;
        private VfxPool _pool;

        private void Awake()
        {
            _ojos = GetComponent<OjosArek>();
            _jefe = GetComponent<BossController>();
            _pool = GetComponent<VfxPool>();
        }

        private void OnEnable() => _ojos.OjoDestruido += OnOjoDestruido;

        private void OnDisable() => _ojos.OjoDestruido -= OnOjoDestruido;

        private void OnOjoDestruido(int ojo)
        {
            HitZone[] zonas = _jefe.HitZones;
            if (zonas == null || ojo >= zonas.Length || zonas[ojo] == null)
                return;

            Transform zona = zonas[ojo].transform;
            VfxBase instancia = _pool.Pedir(_pool.Indice(_vfxOjoDestruido));
            if (instancia != null)
                instancia.Reproducir(zona.position, Quaternion.LookRotation(zona.up));
        }
    }
}
