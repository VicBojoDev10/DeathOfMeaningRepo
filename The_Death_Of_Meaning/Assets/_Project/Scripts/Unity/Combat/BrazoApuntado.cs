using TDOM.Unity.Player;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class BrazoApuntado : MonoBehaviour
    {
        private static readonly int AimHash = Animator.StringToHash("Aim");
        private static readonly int ShootHash = Animator.StringToHash("Shoot");

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private Transform _brazo;

        [SerializeField]
        private Transform _antebrazo;

        [SerializeField]
        private Transform _mano;

        [SerializeField]
        [Range(0f, 1f)]
        private float _pesoApuntado = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _pesoEnderezado = 1f;

        [SerializeField]
        private float _velocidadMezcla = 10f;

        [SerializeField]
        private float _distanciaSinObjetivo = 25f;

        private PlayerRoot _root;
        private float _mezcla;

        private void Awake()
        {
            _root = GetComponentInParent<PlayerRoot>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        private void LateUpdate()
        {
            if (_animator == null || _brazo == null || _mano == null)
                return;

            float destino = Apuntando ? 1f : 0f;
            _mezcla = Mathf.MoveTowards(_mezcla, destino, _velocidadMezcla * Time.deltaTime);
            if (_mezcla <= 0f)
                return;

            Enderezar(_mezcla * _pesoEnderezado);
            Alinear(_brazo, _mano.position, PuntoObjetivo(), _mezcla * _pesoApuntado);
        }

        private bool Apuntando => _animator.GetBool(AimHash) || _animator.GetBool(ShootHash);

        private void Enderezar(float peso)
        {
            if (_antebrazo == null || peso <= 0f)
                return;

            Vector3 direccionBrazo = _antebrazo.position - _brazo.position;
            Alinear(_antebrazo, _mano.position, _antebrazo.position + direccionBrazo, peso);
        }

        private Vector3 PuntoObjetivo()
        {
            if (_root != null && _root.IsOwner && _root.CameraTransform != null)
                return _root.ResolverApuntado(_mano.position).TargetPoint;

            return _brazo.position + transform.forward * _distanciaSinObjetivo;
        }

        private static void Alinear(Transform hueso, Vector3 extremo, Vector3 objetivo, float peso)
        {
            Vector3 actual = extremo - hueso.position;
            Vector3 deseada = objetivo - hueso.position;
            if (actual.sqrMagnitude <= 0f || deseada.sqrMagnitude <= 0f)
                return;

            Quaternion correccion = Quaternion.FromToRotation(actual, deseada);
            hueso.rotation =
                Quaternion.Slerp(Quaternion.identity, correccion, peso) * hueso.rotation;
        }
    }
}
