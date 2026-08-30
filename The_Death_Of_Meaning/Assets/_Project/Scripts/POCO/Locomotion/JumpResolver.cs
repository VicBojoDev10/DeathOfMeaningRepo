using TDOM.Contracts;

namespace TDOM.Gameplay.Locomotion
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

        public void Tick(LocomotionState estado, InputSnapshot input, float dt)
        {
            if (estado.IsGrounded)
            {
                estado.JumpsUsed = 0;
                estado.CoyoteTimer = _coyoteTime;
            }
            else
            {
                estado.CoyoteTimer -= dt;
            }

            if (input.JumpPressed)
                estado.BufferTimer = _bufferTime;
            else
                estado.BufferTimer -= dt;

            bool tieneSaltos = estado.JumpsUsed < _maxSaltos;
            bool enCoyote = estado.CoyoteTimer > 0f && estado.JumpsUsed == 0;

            if (estado.BufferTimer > 0f && (tieneSaltos || enCoyote))
            {
                estado.Velocity.y = _velocidadSalto;
                estado.JumpsUsed++;
                estado.CoyoteTimer = 0f;
                estado.BufferTimer = 0f;
            }
        }
        //Esto si es un comentario Juas Juas
    }
}
