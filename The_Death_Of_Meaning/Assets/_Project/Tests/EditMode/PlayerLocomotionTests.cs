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
            bool jumpPressed = false,
            bool sprintPressed = false
        )
        {
            return new InputSnapshot(
                move,
                Vector2.zero,
                jumpPressed,
                jumpHeld,
                dashPressed,
                sprintPressed,
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
            Assert.That(locomotion.State.Phase, Is.Not.EqualTo(LocomotionPhase.Grappling));
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

        [Test]
        public void IntentarGancho_si_hay_un_ataque_activo_devuelve_false()
        {
            var gravity = new GravityModel(-9.81f, -20f, 1f);
            var jump = new JumpResolver(1, 8f, 0.1f, 0.1f);
            var ground = new GroundControlResolver(4.5f, 6.5f, 25f, 30f, 0.3f);
            var run = new SprintResolver();
            var dash = new DashResolver(CrearDashProfile(6f, 0.2f));
            var grapple = new GrappleResolver(30f, 25f, 3f, 1.5f, 2f);

            var locomotion = new PlayerLocomotion(gravity, jump, ground, run, dash, grapple);

            locomotion.State.Phase = LocomotionPhase.Attacking;
            bool inicio = locomotion.IntentarGancho(Vector3.zero, new Vector3(0f, 0f, 10f));
            Assert.IsFalse(inicio);
            Assert.IsFalse(locomotion.GanchoActivo);

            Assert.IsFalse(locomotion.GanchoActivo);
        }

        [Test]
        public void Tick_con_blockMove_actualiza_fase_a_Attacking_y_bloquea_IntentarGancho()
        {
            var gravity = new GravityModel(-9.81f, -20f, 1f);
            var jump = new JumpResolver(1, 8f, 0.1f, 0.1f);
            var ground = new GroundControlResolver(4.5f, 6.5f, 25f, 30f, 0.3f);
            var run = new SprintResolver();
            var dash = new DashResolver(CrearDashProfile(6f, 0.2f));
            var grapple = new GrappleResolver(30f, 25f, 3f, 1.5f, 2f);

            var locomotion = new PlayerLocomotion(gravity, jump, ground, run, dash, grapple);
            locomotion.State.IsGrounded = true;

            const float dt = 0.016f;
            locomotion.Tick(CreateInput(), Quaternion.identity, dt, blockMove: true);

            Assert.That(locomotion.State.Phase, Is.EqualTo(LocomotionPhase.Attacking));
            Assert.IsFalse(locomotion.IntentarGancho(Vector3.zero, new Vector3(0f, 0f, 10f)));
            Assert.IsFalse(locomotion.GanchoActivo);

            locomotion.Tick(CreateInput(), Quaternion.identity, dt, blockMove: false);
            Assert.That(locomotion.State.Phase, Is.EqualTo(LocomotionPhase.Grounded));
            Assert.IsTrue(locomotion.IntentarGancho(Vector3.zero, new Vector3(0f, 0f, 10f)));
            Assert.IsTrue(locomotion.GanchoActivo);
        }

        private static PlayerLocomotion CrearLocomotion(int maxJumps = 1)
        {
            var gravity = new GravityModel(-9.81f, -20f, 1f);
            var jump = new JumpResolver(maxJumps, 8f, 0.1f, 0.1f);
            var ground = new GroundControlResolver(4.5f, 6.5f, 25f, 30f, 0.3f);
            var run = new SprintResolver();
            var dash = new DashResolver(CrearDashProfile(6f, 0.2f));
            return new PlayerLocomotion(gravity, jump, ground, run, dash);
        }

        [Test]
        public void DashIniciado_es_true_exactamente_en_el_frame_del_dash_y_false_en_el_siguiente()
        {
            var locomotion = CrearLocomotion();
            const float dt = 1f / 60f;

            Assert.That(locomotion.DashIniciado, Is.False);

            locomotion.Tick(
                CreateInput(move: Vector2.up, dashPressed: true),
                Quaternion.identity,
                dt
            );
            Assert.That(locomotion.DashIniciado, Is.True);

            locomotion.Tick(CreateInput(move: Vector2.up), Quaternion.identity, dt);
            Assert.That(locomotion.State.Phase, Is.EqualTo(LocomotionPhase.Dashing));
            Assert.That(locomotion.DashIniciado, Is.False);
        }

        [Test]
        public void SaltoAereo_es_false_en_el_primer_salto_de_Ayla_y_true_en_el_segundo()
        {
            var locomotion = CrearLocomotion(maxJumps: 2);
            locomotion.State.IsGrounded = true;
            const float dt = 1f / 60f;

            locomotion.Tick(
                CreateInput(jumpPressed: true, jumpHeld: true),
                Quaternion.identity,
                dt
            );
            Assert.That(
                locomotion.State.Velocity.y,
                Is.GreaterThan(0f),
                "el primer salto debe ejecutarse"
            );
            Assert.That(locomotion.SaltoAereo, Is.False);

            locomotion.State.IsGrounded = false;
            for (int i = 0; i < 5; i++)
            {
                locomotion.Tick(CreateInput(), Quaternion.identity, dt);
                Assert.That(locomotion.SaltoAereo, Is.False);
            }

            locomotion.Tick(
                CreateInput(jumpPressed: true, jumpHeld: true),
                Quaternion.identity,
                dt
            );
            Assert.That(locomotion.SaltoAereo, Is.True);

            locomotion.Tick(CreateInput(jumpHeld: true), Quaternion.identity, dt);
            Assert.That(locomotion.SaltoAereo, Is.False);
        }

        [Test]
        public void Aterrizaje_es_true_al_tocar_el_suelo_despues_de_caer_desde_10_m()
        {
            var locomotion = CrearLocomotion();
            locomotion.State.IsGrounded = false;
            locomotion.State.Velocity = Vector3.zero;
            const float dt = 1f / 60f;

            float altura = 10f;
            int guarda = 0;
            while (altura > 0f && guarda++ < 600)
            {
                locomotion.Tick(CreateInput(), Quaternion.identity, dt);
                Assert.That(locomotion.Aterrizaje, Is.False, "en el aire no hay aterrizaje");
                altura += locomotion.State.Velocity.y * dt;
            }

            Assert.That(
                locomotion.State.Velocity.y,
                Is.LessThan(PlayerLocomotion.UmbralAterrizajeFuerte),
                "la caída de 10 m debe superar el umbral"
            );

            locomotion.State.IsGrounded = true;
            locomotion.Tick(CreateInput(), Quaternion.identity, dt);
            Assert.That(locomotion.Aterrizaje, Is.True);

            locomotion.Tick(CreateInput(), Quaternion.identity, dt);
            Assert.That(locomotion.Aterrizaje, Is.False);
        }

        [Test]
        public void Aterrizaje_es_false_al_bajar_un_escalon()
        {
            var locomotion = CrearLocomotion();
            locomotion.State.IsGrounded = true;
            const float dt = 1f / 60f;

            locomotion.Tick(CreateInput(), Quaternion.identity, dt);

            locomotion.State.IsGrounded = false;
            for (int i = 0; i < 3; i++)
            {
                locomotion.Tick(CreateInput(), Quaternion.identity, dt);
                Assert.That(locomotion.Aterrizaje, Is.False);
            }

            locomotion.State.IsGrounded = true;
            locomotion.Tick(CreateInput(), Quaternion.identity, dt);

            Assert.That(
                locomotion.State.Velocity.y,
                Is.GreaterThan(PlayerLocomotion.UmbralAterrizajeFuerte)
            );
            Assert.That(locomotion.Aterrizaje, Is.False);
        }

        [Test]
        [TestCase(1f / 60f)]
        [TestCase(1f / 30f)]
        public void frena_en_menos_de_cero_punto_quince_segundos_con_friccion_80(float dt)
        {
            var ground = new GroundControlResolver(10f, 20f, 60f, 80f, 2.5f);
            var state = new LocomotionState
            {
                IsGrounded = true,
                Velocity = new Vector3(10f, 0f, 0f),
            };

            float elapsed = 0f;
            int limit = 100;

            while (state.Velocity.sqrMagnitude > 0.001f && limit-- > 0)
            {
                ground.Tick(state, Vector2.zero, Quaternion.identity, false, dt);
                elapsed += dt;
            }

            Assert.That(elapsed, Is.LessThanOrEqualTo(0.15f + dt));
            Assert.That(state.Velocity.sqrMagnitude, Is.LessThan(0.001f));
        }

        private const float VelocidadBase = 4.5f;
        private const float VelocidadCorrer = 6.5f;
        private const float MultiplicadorPistola = 0.3f;
        private const float VelocidadMaximaPistola = VelocidadBase * MultiplicadorPistola;
        private const float DtFijo = 1f / 60f;
        private const int FramesParaVelocidadEstable = 120;
        private const float Tolerancia = 0.001f;

        private static PlayerLocomotion CrearLocomotionConPistola()
        {
            var gravity = new GravityModel(-9.81f, -20f, 1f);
            var jump = new JumpResolver(1, 8f, 0.1f, 0.1f);
            var ground = new GroundControlResolver(VelocidadBase, VelocidadCorrer, 25f, 30f, 0.3f);
            var run = new SprintResolver();
            var dash = new DashResolver(CrearDashProfile(6f, 0.2f));
            var locomotion = new PlayerLocomotion(
                gravity,
                jump,
                ground,
                run,
                dash,
                multiplicadorVelocidadPistola: MultiplicadorPistola
            );
            locomotion.State.IsGrounded = true;
            return locomotion;
        }

        private static float VelocidadHorizontal(PlayerLocomotion locomotion)
        {
            Vector3 velocidad = locomotion.State.Velocity;
            return new Vector2(velocidad.x, velocidad.z).magnitude;
        }

        private static void Avanzar(
            PlayerLocomotion locomotion,
            InputSnapshot input,
            int frames,
            bool modoPistola
        )
        {
            for (int i = 0; i < frames; i++)
            {
                locomotion.Tick(input, Quaternion.identity, DtFijo, modoPistola: modoPistola);
            }
        }

        [Test]
        public void En_modo_pistola_con_el_stick_al_maximo_la_velocidad_no_supera_la_configurada()
        {
            var locomotion = CrearLocomotionConPistola();
            var input = CreateInput(move: Vector2.up);

            for (int i = 0; i < FramesParaVelocidadEstable; i++)
            {
                locomotion.Tick(input, Quaternion.identity, DtFijo, modoPistola: true);
                Assert.That(
                    VelocidadHorizontal(locomotion),
                    Is.LessThanOrEqualTo(VelocidadMaximaPistola + Tolerancia)
                );
            }

            Assert.That(
                VelocidadHorizontal(locomotion),
                Is.EqualTo(VelocidadMaximaPistola).Within(Tolerancia)
            );
        }

        [Test]
        public void En_modo_pistola_el_personaje_se_mueve_y_no_queda_en_fase_Attacking()
        {
            var locomotion = CrearLocomotionConPistola();

            Avanzar(locomotion, CreateInput(move: Vector2.up), 1, modoPistola: true);

            Assert.That(VelocidadHorizontal(locomotion), Is.GreaterThan(0f));
            Assert.That(locomotion.State.Phase, Is.Not.EqualTo(LocomotionPhase.Attacking));
        }

        [Test]
        public void En_modo_pistola_no_se_puede_iniciar_sprint()
        {
            var locomotion = CrearLocomotionConPistola();
            var input = CreateInput(move: Vector2.up, sprintPressed: true);

            Avanzar(locomotion, input, FramesParaVelocidadEstable, modoPistola: true);

            Assert.That(locomotion.Corriendo, Is.False);
            Assert.That(
                VelocidadHorizontal(locomotion),
                Is.LessThanOrEqualTo(VelocidadMaximaPistola + Tolerancia)
            );
        }

        [Test]
        public void Entrar_en_modo_pistola_corta_el_sprint_en_curso()
        {
            var locomotion = CrearLocomotionConPistola();
            Avanzar(
                locomotion,
                CreateInput(move: Vector2.up, sprintPressed: true),
                1,
                modoPistola: false
            );
            Assert.That(locomotion.Corriendo, Is.True);

            Avanzar(locomotion, CreateInput(move: Vector2.up), 1, modoPistola: true);

            Assert.That(locomotion.Corriendo, Is.False);
        }

        [Test]
        public void En_modo_pistola_no_se_puede_iniciar_la_embestida()
        {
            var locomotion = CrearLocomotionConPistola();
            var input = CreateInput(move: Vector2.up, dashPressed: true);

            var intent = locomotion.Tick(input, Quaternion.identity, DtFijo, modoPistola: true);

            Assert.That(locomotion.DashIniciado, Is.False);
            Assert.That(intent.IgnoreGravity, Is.False);
            Assert.That(locomotion.State.Phase, Is.Not.EqualTo(LocomotionPhase.Dashing));
        }

        [Test]
        public void Al_salir_del_modo_pistola_la_velocidad_vuelve_a_la_normal()
        {
            var locomotion = CrearLocomotionConPistola();
            var input = CreateInput(move: Vector2.up);

            Avanzar(locomotion, input, FramesParaVelocidadEstable, modoPistola: true);
            Avanzar(locomotion, input, FramesParaVelocidadEstable, modoPistola: false);

            Assert.That(
                VelocidadHorizontal(locomotion),
                Is.EqualTo(VelocidadBase).Within(Tolerancia)
            );
        }

        [Test]
        public void Con_blockMove_el_personaje_no_avanza_aunque_el_stick_este_al_maximo()
        {
            var locomotion = CrearLocomotionConPistola();
            var input = CreateInput(move: Vector2.up);

            for (int i = 0; i < FramesParaVelocidadEstable; i++)
            {
                locomotion.Tick(input, Quaternion.identity, DtFijo, blockMove: true);
            }

            Assert.That(VelocidadHorizontal(locomotion), Is.EqualTo(0f).Within(Tolerancia));
            Assert.That(locomotion.State.Phase, Is.EqualTo(LocomotionPhase.Attacking));
        }

        [Test]
        public void El_modo_pistola_es_determinista_con_dt_fijo()
        {
            var primera = CrearLocomotionConPistola();
            var segunda = CrearLocomotionConPistola();
            var input = CreateInput(move: new Vector2(0.6f, 0.8f));

            for (int i = 0; i < FramesParaVelocidadEstable; i++)
            {
                bool modoPistola = i % 2 == 0;
                primera.Tick(input, Quaternion.identity, DtFijo, modoPistola: modoPistola);
                segunda.Tick(input, Quaternion.identity, DtFijo, modoPistola: modoPistola);
            }

            Assert.That(primera.State.Velocity, Is.EqualTo(segunda.State.Velocity));
        }

        [Test]
        public void Abatido_cancela_el_dash_y_el_movimiento_inmediatamente()
        {
            var locomotion = CrearLocomotionConPistola();
            locomotion.Tick(
                CreateInput(move: Vector2.up, dashPressed: true),
                Quaternion.identity,
                DtFijo
            );
            Assert.That(locomotion.State.Phase, Is.EqualTo(LocomotionPhase.Dashing));

            var intent = locomotion.TickIncapacitado(DtFijo);

            Assert.That(VelocidadHorizontal(locomotion), Is.Zero);
            Assert.That(locomotion.DashIniciado, Is.False);
            Assert.That(intent.IgnoreGravity, Is.False);
        }

        [Test]
        public void Abatido_no_reproduce_un_salto_bufferizado_ni_congela_la_caida()
        {
            var locomotion = CrearLocomotionConPistola();
            locomotion.State.IsGrounded = false;
            locomotion.State.Velocity = new Vector3(5f, -3f, 5f);
            locomotion.State.BufferTimer = 1f;
            locomotion.State.CoyoteTimer = 1f;

            locomotion.TickIncapacitado(DtFijo);

            Assert.That(VelocidadHorizontal(locomotion), Is.Zero);
            Assert.That(locomotion.State.Velocity.y, Is.LessThan(-3f));
            Assert.That(locomotion.SaltoEnCurso, Is.False);
            Assert.That(locomotion.State.Phase, Is.EqualTo(LocomotionPhase.Airborne));
        }

        [Test]
        public void Abatido_detiene_el_sprint_y_permite_moverse_tras_revivir()
        {
            var locomotion = CrearLocomotionConPistola();
            locomotion.Tick(
                CreateInput(move: Vector2.up, sprintPressed: true),
                Quaternion.identity,
                DtFijo
            );
            locomotion.TickIncapacitado(DtFijo);

            Assert.That(locomotion.Corriendo, Is.False);
            Assert.That(VelocidadHorizontal(locomotion), Is.Zero);

            locomotion.Tick(CreateInput(move: Vector2.up), Quaternion.identity, DtFijo);
            Assert.That(VelocidadHorizontal(locomotion), Is.GreaterThan(0f));
        }

        [Test]
        public void El_movimiento_abatido_es_determinista_con_dt_fijo()
        {
            var primera = CrearLocomotionConPistola();
            var segunda = CrearLocomotionConPistola();
            primera.State.IsGrounded = false;
            segunda.State.IsGrounded = false;

            for (int i = 0; i < FramesParaVelocidadEstable; i++)
            {
                primera.TickIncapacitado(DtFijo);
                segunda.TickIncapacitado(DtFijo);
            }

            Assert.That(primera.State.Velocity, Is.EqualTo(segunda.State.Velocity));
            Assert.That(VelocidadHorizontal(primera), Is.Zero);
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
