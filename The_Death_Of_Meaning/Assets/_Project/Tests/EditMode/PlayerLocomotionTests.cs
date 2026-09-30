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

        [Test]
        public void Dash_con_curva_lineal_recorre_la_distancia_configurada()
        {
            var dashProfile = CrearDashProfile(10f, 0.8f);
            dashProfile.Easing = AnimationCurve.Linear(0f, 0f, 1f, 1f);
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

            const float dt = 0.01f;
            float traveled = 0f;

            var startInput = CreateInput(move: Vector2.up, dashPressed: true);
            locomotion.Tick(startInput, Quaternion.identity, dt);
            traveled += locomotion.State.Velocity.magnitude * dt;

            int frames = Mathf.CeilToInt((dashProfile.Duration - dt) / dt);
            for (int i = 0; i < frames; i++)
            {
                locomotion.Tick(CreateInput(move: Vector2.up), Quaternion.identity, dt);
                traveled += locomotion.State.Velocity.magnitude * dt;
            }

            Assert.That(traveled, Is.EqualTo(dashProfile.Distance).Within(0.15f));
        }

        [Test]
        public void Con_el_stick_en_neutral_y_yaw_de_90_el_dash_avanza_sobre_mas_x_y_velocidad_no_es_cero()
        {
            var dashProfile = CrearDashProfile(8f, 0.18f);
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

            var yaw90 = Quaternion.Euler(0f, 90f, 0f);
            var inputNeutral = CreateInput(move: Vector2.zero, dashPressed: true);
            const float dt = 0.016f;

            var intent = locomotion.Tick(inputNeutral, yaw90, dt);

            Assert.That(intent.IgnoreGravity, Is.True);
            Assert.That(locomotion.State.Phase, Is.EqualTo(LocomotionPhase.Dashing));
            Assert.That(locomotion.State.Velocity.magnitude, Is.GreaterThan(0f));
            Assert.That(locomotion.State.Velocity.x, Is.GreaterThan(0f));
            Assert.That(locomotion.State.Velocity.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Con_la_embestida_activa_yaw_de_90_y_stick_hacia_adelante_la_direccion_gira_hacia_mas_x_y_no_hacia_mas_z()
        {
            var dashProfile = ScriptableObject.CreateInstance<DashProfile>();
            dashProfile.Distance = 10f;
            dashProfile.Duration = 0.8f;
            dashProfile.Cooldown = 7f;
            dashProfile.MaxTurnRate = 30f;
            dashProfile.Easing = AnimationCurve.Constant(0f, 1f, 1f);

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

            const float dt = 0.1f;

            var startInput = CreateInput(move: Vector2.up, dashPressed: true);
            locomotion.Tick(startInput, Quaternion.identity, dt);

            var yaw90 = Quaternion.Euler(0f, 90f, 0f);
            var steerInput = CreateInput(move: Vector2.up);
            locomotion.Tick(steerInput, yaw90, dt);

            Assert.That(locomotion.State.Velocity.x, Is.GreaterThan(0f));
            Vector3 direccionEsperada = Vector3.RotateTowards(
                Vector3.forward,
                Vector3.right,
                30f * Mathf.Deg2Rad * dt,
                0f
            );
            float diferenciaAngular = Vector3.Angle(
                direccionEsperada,
                locomotion.State.Velocity.normalized
            );
            Assert.That(diferenciaAngular, Is.LessThan(0.01f));
        }

        [Test]
        public void Con_la_embestida_activa_input_menor_al_deadzone_no_gira()
        {
            var dashProfile = ScriptableObject.CreateInstance<DashProfile>();
            dashProfile.Distance = 10f;
            dashProfile.Duration = 0.8f;
            dashProfile.Cooldown = 7f;
            dashProfile.MaxTurnRate = 30f;
            dashProfile.Easing = AnimationCurve.Constant(0f, 1f, 1f);

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

            const float dt = 0.1f;
            var startInput = CreateInput(move: Vector2.up, dashPressed: true);
            locomotion.Tick(startInput, Quaternion.identity, dt);

            var yaw90 = Quaternion.Euler(0f, 90f, 0f);
            var noiseInput = CreateInput(move: new Vector2(0.05f, 0f));
            locomotion.Tick(noiseInput, yaw90, dt);

            Assert.That(locomotion.State.Velocity.normalized, Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void Saltar_durante_el_gancho_lo_cancela_y_aplica_JumpVelocity()
        {
            var gravity = new GravityModel(-9.81f, -20f, 1f);
            var jump = new JumpResolver(1, 8f, 0.1f, 0.1f);
            var ground = new GroundControlResolver(4.5f, 6.5f, 25f, 30f, 0.3f);
            var run = new SprintResolver();
            var dash = new DashResolver(CrearDashProfile(6f, 0.2f));
            var grapple = new GrappleResolver(30f, 25f, 3f, 1.5f, 2f);

            var locomotion = new PlayerLocomotion(gravity, jump, ground, run, dash, grapple);
            locomotion.State.IsGrounded = true;

            Assert.IsTrue(locomotion.IntentarGancho(Vector3.zero, new Vector3(0f, 10f, 10f)));
            Assert.IsTrue(locomotion.GanchoActivo);

            const float dt = 0.016f;
            locomotion.Tick(CreateInput(), Quaternion.identity, dt);
            Assert.IsTrue(locomotion.GanchoActivo);

            var jumpInput = CreateInput(jumpPressed: true, jumpHeld: true);
            var intent = locomotion.Tick(jumpInput, Quaternion.identity, dt);

            Assert.IsFalse(locomotion.GanchoActivo);
            Assert.That(locomotion.State.Velocity.y, Is.GreaterThan(7f));
            Assert.IsFalse(intent.IgnoreGravity);
        }

        [Test]
        public void Un_personaje_sin_GrappleProfile_Zendre_ignora_IntentarGancho()
        {
            var gravity = new GravityModel(-9.81f, -20f, 1f);
            var jump = new JumpResolver(1, 8f, 0.1f, 0.1f);
            var ground = new GroundControlResolver(4.5f, 6.5f, 25f, 30f, 0.3f);
            var run = new SprintResolver();
            var dash = new DashResolver(CrearDashProfile(6f, 0.2f));

            var locomotion = new PlayerLocomotion(gravity, jump, ground, run, dash, grapple: null);

            bool inicio = locomotion.IntentarGancho(Vector3.zero, new Vector3(0f, 0f, 10f));

            Assert.IsFalse(inicio);
            Assert.IsFalse(locomotion.GanchoActivo);
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
