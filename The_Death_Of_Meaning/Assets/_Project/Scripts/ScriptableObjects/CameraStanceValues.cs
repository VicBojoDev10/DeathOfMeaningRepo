using System;

namespace TDOM.Data
{
    [Serializable]
    public sealed class CameraStanceValues
    {
        public float Distance;
        public float ShoulderOffsetX;
        public float PivotHeight;
        public float FieldOfView;

        public CameraStanceValues(
            float distance,
            float shoulderOffsetX,
            float pivotHeight,
            float fieldOfView
        )
        {
            Distance = distance;
            ShoulderOffsetX = shoulderOffsetX;
            PivotHeight = pivotHeight;
            FieldOfView = fieldOfView;
        }
    }
}
