using TDOM.Contracts;

namespace TDOM.Gameplay
{
    public sealed class JumpResolver
    {
        private readonly int _maxSaltos;
        private readonly float _velocidadSalto;
        private readonly float _coyoteTime;
        private readonly float _bufferTime;

        public JumpResolver(int maxSaltos, float velocidadSalto, float coyoteTime, float bufferTime)
        {
            _maxSaltos = maxSaltos;
            _velocidadSalto = velocidadSalto;
            _coyoteTime = coyoteTime;
            _bufferTime = bufferTime;
        }

        public bool PuedeSaltar(LocomotionState estado, bool permitirSaltoAereo = true)
        {
            if (estado.IsGrounded)
                return true;
            if (!permitirSaltoAereo && EsSaltoAereo(estado))
                return false;
            bool tieneSaltos = estado.JumpsUsed < _maxSaltos;
            bool enCoyote = estado.CoyoteTimer > 0f && estado.JumpsUsed == 0;
            return tieneSaltos || enCoyote;
        }

        // Un salto que se hace sin tocar el suelo y ya habiendo gastado el primero (doble salto).
        private static bool EsSaltoAereo(LocomotionState estado)
        {
            return !estado.IsGrounded && estado.JumpsUsed >= 1;
        }

        /// <param name="permitirSaltoAereo">
        /// Falso = el doble salto no se ejecuta (por ejemplo, sin energia); el salto normal
        /// desde el suelo o durante el coyote time sigue funcionando.
        /// </param>
        public void Tick(
            LocomotionState estado,
            InputSnapshot input,
            float dt,
            bool permitirSaltoAereo = true
        )
        {
            if (estado.IsGrounded)
            {
                estado.JumpsUsed = 0;
                estado.CoyoteTimer = _coyoteTime;
            }
            else
            {
                estado.CoyoteTimer -= dt;
                if (estado.CoyoteTimer <= 0f && estado.JumpsUsed == 0)
                    estado.JumpsUsed = 1;
            }

            if (input.JumpPressed)
                estado.BufferTimer = _bufferTime;
            else
                estado.BufferTimer -= dt;

            bool tieneSaltos = estado.JumpsUsed < _maxSaltos;
            bool enCoyote = estado.CoyoteTimer > 0f && estado.JumpsUsed == 0;

            if (
                estado.BufferTimer > 0f
                && (tieneSaltos || enCoyote)
                && (permitirSaltoAereo || !EsSaltoAereo(estado))
            )
            {
                estado.Velocity.y = _velocidadSalto;
                estado.JumpsUsed++;
                estado.CoyoteTimer = 0f;
                estado.BufferTimer = 0f;
            }
        }
    }
}
