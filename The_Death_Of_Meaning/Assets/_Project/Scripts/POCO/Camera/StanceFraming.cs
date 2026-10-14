using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public readonly struct StanceFraming
    {
        public float Distance { get; }
        public float ShoulderOffset { get; }
        public float PivotHeight { get; }
        public float FieldOfView { get; }

        public StanceFraming(
            float distance,
            float shoulderOffset,
            float pivotHeight,
            float fieldOfView
        )
        {
            Distance = distance;
            ShoulderOffset = shoulderOffset;
            PivotHeight = pivotHeight;
            FieldOfView = fieldOfView;
        }

        public static StanceFraming Lerp(StanceFraming desde, StanceFraming hacia, float t)
        {
            return new StanceFraming(
                Mathf.Lerp(desde.Distance, hacia.Distance, t),
                Mathf.Lerp(desde.ShoulderOffset, hacia.ShoulderOffset, t),
                Mathf.Lerp(desde.PivotHeight, hacia.PivotHeight, t),
                Mathf.Lerp(desde.FieldOfView, hacia.FieldOfView, t)
            );
        }
    }
}
