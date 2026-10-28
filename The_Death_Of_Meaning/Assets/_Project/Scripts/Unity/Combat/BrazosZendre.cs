using UnityEngine;

namespace TDOM.Unity
{
    public class BrazosZendre : MonoBehaviour
    {

        [Header("Referencias")]
        [SerializeField]
        private Animator _animator;

        [SerializeField, Tooltip("Prefabs del brazo izquierdo de Zendre")]
        private GameObject _brazoLeftPrefab;
        [SerializeField, Tooltip("Prefabs del brazo derecho de Zendre")]
        private GameObject _brazoRightPrefab;

        [Header("Hueso")] [SerializeField] private HumanBodyBones _leftHandHuesoHumanoide = HumanBodyBones.LeftHand;
        [Header("Hueso")] [SerializeField] private HumanBodyBones _rightHandHuesoHumanoide = HumanBodyBones.RightHand;
    }
}
