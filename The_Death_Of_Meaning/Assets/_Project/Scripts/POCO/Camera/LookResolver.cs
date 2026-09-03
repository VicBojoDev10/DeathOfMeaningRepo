using UnityEngine;

namespace TDOM.Gameplay
{
    [CreateAssetMenu(fileName = "LookResolver", menuName = "Scriptable Objects/LookResolver")]
    public class LookResolver : ScriptableObject
    {
        private readonly float _sensibilidad;
        private readonly float _pitchMin;   // -85
        private readonly float _pitchMax;   //  85
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public Quaternion YawRotation => Quaternion.Euler(0f, Yaw, 0f);
        public void Tick(Vector2 look, float dt)
        {
            // El gamepad entrega POSICIÓN del stick, no velocidad:
            // aquí sí se multiplica por dt
            Yaw += look.x * _sensibilidad * dt;
            Pitch -= look.y * _sensibilidad * dt;
            Pitch = Mathf.Clamp(Pitch, _pitchMin, _pitchMax);
        }
    }
}
