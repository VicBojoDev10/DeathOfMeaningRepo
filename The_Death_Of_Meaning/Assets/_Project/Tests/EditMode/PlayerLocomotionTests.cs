using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay;
using TDOM.Gameplay.Locomotion;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public sealed class PlayerLocomotionTests
    {
        [SerializeField]
        private PlayerLocomotion _locomotion;

        [SerializeField]
        private readonly GravityModel _gravedad;

        [SerializeField]
        private readonly JumpResolver _salto;

        [SerializeField]
        private readonly GroundControlResolver _suelo;

        [SerializeField]
        private readonly SprintResolver _correr;

        [SerializeField]
        private readonly DashResolver _dash;

        [SerializeField]
        private readonly CharacterDefinition _definicion;

        public LocomotionState State { get; } = new();

        private Vector3 DireccionDeDash(InputSnapshot input, Quaternion yaw)
        {
            Vector3 direction = yaw * new Vector3(input.Move.x, 0f, input.Move.y);
            return direction.normalized;
        }

        private MotionIntent Tick(InputSnapshot input, Quaternion yaw, float dt)
        {
            _correr.Tick(input);
            if (input.DashPressed)
            {
                Vector3 dir = DireccionDeDash(input, yaw);
                _dash.TryIniciar(dir);
            }
            _dash.Tick(State, input.Move, dt);
            if (_dash.Activo)
                return new MotionIntent(State.Velocity, ignoreGravity: true);
            _salto.Tick(State, input, dt);
            _suelo.Tick(State, input.Move, yaw, _correr.Corriendo, dt);
            _gravedad.Aplicar(State, input, dt);
            return new MotionIntent(State.Velocity, ignoreGravity: false);
        }

        [Test]
        public void DireccionDeDash_usa_el_yaw_para_rotar_el_movimiento()
        {
            var input = new InputSnapshot(false, false) { Move = new Vector2(1f, 0f) };
            var yaw = Quaternion.Euler(0f, -90f, 0f);

            Vector3 result = DireccionDeDash(input, yaw);

            Assert.That(result.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(result.z, Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
