using UnityEngine;

namespace TDOM.Unity
{
    public class BrazosZendre : MonoBehaviour
    {
        private static readonly int AttackStateHash = Animator.StringToHash("Attack");
        private static readonly int ChargedStateHash = Animator.StringToHash("Charged");
        private static readonly int LightComboStateHash = Animator.StringToHash("1iC");
        private static readonly int LightChargedComboStateHash = Animator.StringToHash("2IC");

        private const string LeftHandBoneName = "DEF-hand.L";
        private const string RightHandBoneName = "DEF-hand.R";

        [Header("Referencias")]
        [SerializeField]
        private Animator _animator;

        [SerializeField, Tooltip("Modelo del brazo izquierdo. Se conserva su escala y materiales.")]
        private GameObject _brazoLeftPrefab;

        [SerializeField, Tooltip("Modelo del brazo derecho. Se conserva su escala y materiales.")]
        private GameObject _brazoRightPrefab;

        [SerializeField, Tooltip("Opcional. Si se deja vacío, se busca DEF-hand.L en el rig Generic.")]
        private Transform _leftHand;

        [SerializeField, Tooltip("Opcional. Si se deja vacío, se busca DEF-hand.R en el rig Generic.")]
        private Transform _rightHand;

        [Header("Offsets locales de las manos")]
        [SerializeField, Tooltip("Ajuste de posición local adicional para el brazo izquierdo.")]
        private Vector3 _leftPositionOffset;

        [SerializeField, Tooltip("Ajuste de rotación local adicional para el brazo izquierdo.")]
        private Vector3 _leftRotationOffset;

        [SerializeField, Tooltip("Ajuste de posición local adicional para el brazo derecho.")]
        private Vector3 _rightPositionOffset;

        [SerializeField, Tooltip("Ajuste de rotación local adicional para el brazo derecho.")]
        private Vector3 _rightRotationOffset;

        private Transform _leftArmTransform;
        private Transform _rightArmTransform;
        private Vector3 _leftAuthoredPositionOffset;
        private Quaternion _leftAuthoredRotationOffset;
        private Vector3 _rightAuthoredPositionOffset;
        private Quaternion _rightAuthoredRotationOffset;
        private bool _reportedMissingRig;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();

            _leftArmTransform = _brazoLeftPrefab != null ? _brazoLeftPrefab.transform : null;
            _rightArmTransform = _brazoRightPrefab != null ? _brazoRightPrefab.transform : null;

            if (_animator != null)
            {
                _leftHand = ResolveHand(_leftHand, LeftHandBoneName);
                _rightHand = ResolveHand(_rightHand, RightHandBoneName);
            }

            CacheAuthoredOffset(_leftHand, _leftArmTransform, out _leftAuthoredPositionOffset, out _leftAuthoredRotationOffset);
            CacheAuthoredOffset(_rightHand, _rightArmTransform, out _rightAuthoredPositionOffset, out _rightAuthoredRotationOffset);

            SetArmsActive(false);
            if (_animator == null && !_reportedMissingRig)
            {
                Debug.LogWarning(
                    $"[{nameof(BrazosZendre)}] No hay Animator asignado en {name}; no se pueden detectar las animaciones de ataque.",
                    this
                );
                _reportedMissingRig = true;
            }
        }

        private void LateUpdate()
        {
            if (_animator == null || _animator.layerCount == 0)
            {
                SetArmsActive(false);
                return;
            }

            bool attackActive = IsAttackState(_animator.GetCurrentAnimatorStateInfo(0).shortNameHash);
            if (!attackActive && _animator.IsInTransition(0))
                attackActive = IsAttackState(_animator.GetNextAnimatorStateInfo(0).shortNameHash);

            SetArmsActive(attackActive);
            if (!attackActive)
                return;

            FollowHand(
                _leftHand,
                _leftArmTransform,
                _leftAuthoredPositionOffset + _leftPositionOffset,
                _leftAuthoredRotationOffset * Quaternion.Euler(_leftRotationOffset)
            );
            FollowHand(
                _rightHand,
                _rightArmTransform,
                _rightAuthoredPositionOffset + _rightPositionOffset,
                _rightAuthoredRotationOffset * Quaternion.Euler(_rightRotationOffset)
            );
        }

        private static bool IsAttackState(int stateHash)
        {
            return stateHash == AttackStateHash
                || stateHash == ChargedStateHash
                || stateHash == LightComboStateHash
                || stateHash == LightChargedComboStateHash;
        }

        private static void CacheAuthoredOffset(
            Transform hand,
            Transform arm,
            out Vector3 positionOffset,
            out Quaternion rotationOffset
        )
        {
            if (hand == null || arm == null)
            {
                positionOffset = Vector3.zero;
                rotationOffset = Quaternion.identity;
                return;
            }

            positionOffset = hand.InverseTransformPoint(arm.position);
            rotationOffset = Quaternion.Inverse(hand.rotation) * arm.rotation;
        }

        private static void FollowHand(
            Transform hand,
            Transform arm,
            Vector3 localPositionOffset,
            Quaternion localRotationOffset
        )
        {
            if (hand == null || arm == null)
                return;

            // Solo cambia la posición y rotación; la escala original del brazo queda intacta.
            arm.SetPositionAndRotation(
                hand.TransformPoint(localPositionOffset),
                hand.rotation * localRotationOffset
            );
        }

        private void SetArmsActive(bool active)
        {
            SetActiveIfNeeded(_brazoLeftPrefab, active);
            SetActiveIfNeeded(_brazoRightPrefab, active);
        }

        private static void SetActiveIfNeeded(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == targetName)
                    return child;

                Transform match = FindDescendant(child, targetName);
                if (match != null)
                    return match;
            }

            return null;
        }

        private Transform ResolveHand(Transform inspectorReference, string boneName)
        {
            Transform rigReference = FindDescendant(_animator.transform, boneName);
            if (rigReference != null)
            {
                if (inspectorReference != null && inspectorReference != rigReference)
                {
                    Debug.LogWarning(
                        $"[{nameof(BrazosZendre)}] La referencia manual '{inspectorReference.name}' no coincide con el hueso Generic '{boneName}'. Se usará el hueso del rig.",
                        this
                    );
                }

                return rigReference;
            }

            if (inspectorReference != null)
                return inspectorReference;

            Debug.LogWarning(
                $"[{nameof(BrazosZendre)}] No se encontró '{boneName}' en el rig ni hay referencia asignada en el Inspector.",
                this
            );
            return null;
        }
    }
}
