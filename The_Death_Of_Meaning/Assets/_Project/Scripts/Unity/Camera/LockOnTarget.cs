using System.Collections.Generic;
using UnityEngine;

namespace TDOM.Unity.Camera
{
    [DisallowMultipleComponent]
    public sealed class LockOnTarget : MonoBehaviour
    {
        private static readonly List<LockOnTarget> _activos = new List<LockOnTarget>();
        private static int _ultimoId;

        [SerializeField]
        private Transform _puntoDeMira;

        private int _id;

        public static IReadOnlyList<LockOnTarget> Activos => _activos;

        public int Id => _id;
        public Vector3 PuntoDeMira =>
            _puntoDeMira != null ? _puntoDeMira.position : transform.position;

        private void Awake()
        {
            _id = ++_ultimoId;
        }

        private void OnEnable()
        {
            _activos.Add(this);
        }

        private void OnDisable()
        {
            _activos.Remove(this);
        }
    }
}
