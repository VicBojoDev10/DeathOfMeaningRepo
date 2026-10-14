using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public readonly struct LockOnCandidate
    {
        public int Id { get; }
        public Vector3 Position { get; }

        public LockOnCandidate(int id, Vector3 position)
        {
            Id = id;
            Position = position;
        }
    }
}
