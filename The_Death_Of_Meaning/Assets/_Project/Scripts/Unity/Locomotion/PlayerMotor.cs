using TDOM.Contracts;
using TDOM.Unity.Level;
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

        private PlataformaCircular _plataformaActual;

        public PlataformaCircular PlataformaActual => _plataformaActual;

        public void Apply(
            MotionIntent intent,
            float dt,
            PlataformaCircular plataformaForzada = null
        )
        {
            if (_cc == null)
                return;

            PlataformaCircular plataformaMovimiento =
                plataformaForzada != null ? plataformaForzada : _plataformaActual;
            Vector3 deltaPlataforma =
                plataformaMovimiento != null ? plataformaMovimiento.FrameDelta : Vector3.zero;

            Vector3 desplazamiento =
                (intent.Velocity * dt) + DesplazamientoDeEmpuje(dt) + deltaPlataforma;
            _cc.Move(desplazamiento);

            if (!_cc.isGrounded)
                _plataformaActual = null;
        }

        private Vector3 DesplazamientoDeEmpuje(float dt)
        {
            if (_empujeDuracion <= 0f)
                return Vector3.zero;

            float antes = Mathf.Clamp01(_empujeTiempo / _empujeDuracion);
            _empujeTiempo += dt;
            float despues = Mathf.Clamp01(_empujeTiempo / _empujeDuracion);

            // Avance con salida suave y salto en arco que termina en el punto de partida.
            float avance =
                (1f - (1f - despues) * (1f - despues)) - (1f - (1f - antes) * (1f - antes));
            float arco = Mathf.Sin(despues * Mathf.PI) - Mathf.Sin(antes * Mathf.PI);
            Vector3 delta =
                _empujeDireccion * (_empujeDistancia * avance)
                + Vector3.up * (_empujeAltura * arco);

            if (despues >= 1f)
                _empujeDuracion = 0f;
            return delta;
        }

        public void ProbeGround(LocomotionState estado)
        {
            if (_cc == null)
                return;

            estado.IsGrounded = _cc.isGrounded;
            if (!estado.IsGrounded)
            {
                _plataformaActual = null;
                return;
            }

            // Usamos SphereCast para cubrir toda la base del CharacterController
            // y evitar perder la plataforma si el jugador se para cerca del borde.
            float radioCast = Mathf.Max(0.1f, _cc.radius * 0.85f);
            float distanciaCast = Mathf.Max(0.05f, _cc.bounds.extents.y + 0.2f - radioCast);

            if (
                Physics.SphereCast(
                    _cc.bounds.center,
                    radioCast,
                    Vector3.down,
                    out RaycastHit hit,
                    distanciaCast,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                _plataformaActual = hit.collider.GetComponentInParent<PlataformaCircular>();
            }
            else
            {
                _plataformaActual = null;
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Si el jugador aterriza sobre una plataforma circular desde arriba
            if (hit.normal.y > 0.5f)
            {
                var plataforma = hit.collider.GetComponentInParent<PlataformaCircular>();
                if (plataforma != null)
                {
                    _plataformaActual = plataforma;
                }
            }
        }

        public void ResetPlataforma() => _plataformaActual = null;
    }
}
