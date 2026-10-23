using TDOM.Contracts;
using UnityEngine;

namespace TDOM.Unity.Locomotion
{
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField]
        private CharacterController _cc;

        private Vector3 _empujeDireccion;
        private float _empujeDistancia;
        private float _empujeAltura;
        private float _empujeDuracion;
        private float _empujeTiempo;

        /// <summary>
        /// Desplazamiento corto y suave: avanza <paramref name="distancia"/> metros en la
        /// direccion dada y sube/baja <paramref name="altura"/> (vuelve al punto de partida, no
        /// flota). Se suma al movimiento normal y respeta las colisiones.
        /// </summary>
        public void Empujar(
            Vector3 direccionHorizontal,
            float distancia,
            float altura,
            float duracion
        )
        {
            if (duracion <= 0f || (distancia <= 0f && altura <= 0f))
                return;

            direccionHorizontal.y = 0f;
            if (direccionHorizontal.sqrMagnitude < 1e-6f)
                return;

            _empujeDireccion = direccionHorizontal.normalized;
            _empujeDistancia = distancia;
            _empujeAltura = altura;
            _empujeDuracion = duracion;
            _empujeTiempo = 0f;
        }

        public void Apply(MotionIntent intent, float dt)
        {
            _cc.Move(intent.Velocity * dt + DesplazamientoDeEmpuje(dt));
        }

        private Vector3 DesplazamientoDeEmpuje(float dt)
        {
            if (_empujeDuracion <= 0f)
                return Vector3.zero;

            float antes = Mathf.Clamp01(_empujeTiempo / _empujeDuracion);
            _empujeTiempo += dt;
            float despues = Mathf.Clamp01(_empujeTiempo / _empujeDuracion);

            // Avance con salida suave y salto en arco que termina en el punto de partida.
            float avance = (1f - (1f - despues) * (1f - despues)) - (1f - (1f - antes) * (1f - antes));
            float arco = Mathf.Sin(despues * Mathf.PI) - Mathf.Sin(antes * Mathf.PI);
            Vector3 delta = _empujeDireccion * (_empujeDistancia * avance) + Vector3.up * (_empujeAltura * arco);

            if (despues >= 1f)
                _empujeDuracion = 0f;
            return delta;
        }

        public void ProbeGround(LocomotionState estado)
        {
            estado.IsGrounded = _cc.isGrounded;
        }
    }
}
