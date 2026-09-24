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
        private static InputSnapshot CreateInput(
            Vector2 move = default,
            bool dashPressed = false,
            bool jumpHeld = false,
            bool jumpPressed = false
        )
        {
            return new InputSnapshot(
                move,
                Vector2.zero,
                jumpPressed,
                jumpHeld,
                dashPressed,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false
            );
        }

        [Test]
        public void Tick_aplica_gravedad_real_en_varios_frames()
        {
            var gravity = new GravityModel(-9.81f, -20f, 1f);
            var jump = new JumpResolver(1, 8f, 0.1f, 0.1f);
            var ground = new GroundControlResolver(4.5f, 6.5f, 25f, 30f, 0.3f);
            var run = new SprintResolver();
            var dash = new DashResolver(CrearDashProfile(6f, 0.2f));

            var locomotion = new PlayerLocomotion(gravity, jump, ground, run, dash);
            locomotion.State.IsGrounded = false;
            locomotion.State.Velocity = Vector3.zero;

            const float dt = 1f / 60f;
            const int frames = 10;

            for (int i = 0; i < frames; i++)
            {
                locomotion.Tick(CreateInput(), Quaternion.identity, dt);
            }

            float expectedY = -9.81f * frames * dt;
            Assert.That(locomotion.State.Velocity.y, Is.EqualTo(expectedY).Within(0.02f));
        }

        [Test]
        public void Dash_usa_la_distancia_y_la_duracion_configuradas()
        {
            var dashProfile = CrearDashProfile(6f, 0.2f);
            var gravity = new GravityModel(-9.81f, -20f, 1f);
            var jump = new JumpResolver(1, 8f, 0.1f, 0.1f);
            var ground = new GroundControlResolver(4.5f, 6.5f, 25f, 30f, 0.3f);
            var run = new SprintResolver();
            var locomotion = new PlayerLocomotion(
                gravity,
                jump,
                ground,
                run,
                new DashResolver(dashProfile)
            );

            const float dt = 1f / 60f;
            float elapsed = 0f;
            float traveled = 0f;

            var startInput = CreateInput(move: Vector2.up, dashPressed: true);
            var intent = locomotion.Tick(startInput, Quaternion.identity, dt);

            Assert.That(intent.IgnoreGravity, Is.True);
            Assert.That(locomotion.State.Phase, Is.EqualTo(LocomotionPhase.Dashing));

            locomotion.Tick(CreateInput(move: Vector2.up), Quaternion.identity, dt);
            Assert.That(locomotion.State.Velocity.magnitude, Is.GreaterThan(0f));

            int frames = Mathf.CeilToInt(dashProfile.Duration / dt);
            for (int i = 0; i < frames; i++)
            {
                var velocity = locomotion.State.Velocity;
                locomotion.Tick(CreateInput(move: Vector2.up), Quaternion.identity, dt);
                traveled += velocity.magnitude * dt;
                elapsed += dt;
            }

            Assert.That(traveled, Is.EqualTo(dashProfile.Distance).Within(0.15f));
            Assert.That(elapsed, Is.EqualTo(dashProfile.Duration).Within(0.05f));
        }

        private static DashProfile CrearDashProfile(float distance, float duration)
        {
            var profile = ScriptableObject.CreateInstance<DashProfile>();
            profile.Distance = distance;
            profile.Duration = duration;
            profile.Cooldown = 1f;
            profile.MaxTurnRate = 0f;
            profile.Easing = AnimationCurve.Constant(0f, 1f, 1f);
            return profile;
        }
    }
}
