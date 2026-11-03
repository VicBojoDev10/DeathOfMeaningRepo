using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public readonly struct AimSolution
    {
        public Vector3 TargetPoint { get; }
        public Vector3 Direction { get; }
        public bool HasHit { get; }

        public AimSolution(Vector3 targetPoint, Vector3 direction, bool hasHit)
        {
            TargetPoint = targetPoint;
            Direction = direction;
            HasHit = hasHit;
        }
    }
}
