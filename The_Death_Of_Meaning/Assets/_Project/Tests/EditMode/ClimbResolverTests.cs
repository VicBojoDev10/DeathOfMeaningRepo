using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay;
using TDOM.Gameplay.Locomotion;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public sealed class ClimbResolverTests
    {
        private const float DeltaTime = 1f / 60f;

        [Test]
        public void Solo_inicia_al_acercarse_a_un_borde_marcado_y_cercano()
        {
            var resolver = CrearResolver();

            Assert.That(
                resolver.IntentarIniciar(
                    Vector3.zero,
                    Vector3.up,
                    0.5f,
                    acercandose: true,
                    bordeMarcado: true
                ),
                Is.True
            );

            Assert.That(
                CrearResolver()
                    .IntentarIniciar(
                        Vector3.zero,
                        Vector3.up,
                        0.9f,
                        acercandose: true,
                        bordeMarcado: true
                    ),
                Is.False
            );
            Assert.That(
                CrearResolver()
                    .IntentarIniciar(
                        Vector3.zero,
                        Vector3.up,
                        0.5f,
                        acercandose: false,
                        bordeMarcado: true
                    ),
                Is.False
            );
            Assert.That(
                CrearResolver()
                    .IntentarIniciar(
                        Vector3.zero,
                        Vector3.up,
                        0.5f,
                        acercandose: true,
                        bordeMarcado: false
                    ),
                Is.False
            );
        }

        [Test]
        public void Avanza_gradualmente_y_suspende_la_gravedad()
        {
            var resolver = CrearResolver();
            var estado = new LocomotionState();
            Assert.That(
                resolver.IntentarIniciar(
                    Vector3.zero,
                    Vector3.up,
                    0.5f,
                    acercandose: true,
                    bordeMarcado: true
                ),
                Is.True
            );

            MotionIntent intent = resolver.Tick(estado, Vector3.zero, DeltaTime);

            Assert.That(resolver.Activo, Is.True);
            Assert.That(estado.Phase, Is.EqualTo(LocomotionPhase.Climbing));
            Assert.That(estado.Velocity.y, Is.GreaterThan(0f));
            Assert.That(intent.IgnoreGravity, Is.True);
            Assert.That(intent.Velocity.y * DeltaTime, Is.LessThan(1f));
        }

        [Test]
        public void Locomocion_escalando_ignora_dash_y_gravedad()
        {
            var dashProfile = ScriptableObject.CreateInstance<DashProfile>();
            dashProfile.Distance = 2f;
            dashProfile.Duration = 0.2f;
            dashProfile.Cooldown = 0.5f;
            dashProfile.MaxTurnRate = 90f;
            var locomocion = new PlayerLocomotion(
                new GravityModel(-9f, -20f, 1f),
                new JumpResolver(1, 8f, 0.1f, 0.1f),
                new GroundControlResolver(4f, 6f, 10f, 8f, 0.5f),
                new SprintResolver(),
                new DashResolver(dashProfile),
                climb: CrearResolver()
            );
            Assert.That(
                locomocion.IntentarEscalar(
                    0.5f,
                    Vector3.zero,
                    Vector3.up,
                    acercandose: true,
                    bordeMarcado: true
                ),
                Is.True
            );

            MotionIntent intent = locomocion.Tick(
                new InputSnapshot { DashPressed = true },
                Quaternion.identity,
                DeltaTime,
                posicion: Vector3.zero
            );

            Assert.That(intent.IgnoreGravity, Is.True);
            Assert.That(locomocion.DashActivo, Is.False);
            Assert.That(locomocion.DashIniciado, Is.False);
            Assert.That(locomocion.State.Phase, Is.EqualTo(LocomotionPhase.Climbing));
            Object.DestroyImmediate(dashProfile);
        }

        [Test]
        public void Personaje_sin_capacidad_de_escalar_no_crea_resolver()
        {
            var definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            var movement = ScriptableObject.CreateInstance<MovementProfile>();
            var dash = ScriptableObject.CreateInstance<DashProfile>();
            definition.Movement = movement;
            definition.Dash = dash;
            definition.CanClimb = false;

            var locomocion = new PlayerLocomotion(definition);

            Assert.That(locomocion.PuedeEscalar, Is.False);
            Assert.That(
                locomocion.IntentarEscalar(
                    0.5f,
                    Vector3.zero,
                    Vector3.up,
                    acercandose: true,
                    bordeMarcado: true
                ),
                Is.False
            );

            Object.DestroyImmediate(dash);
            Object.DestroyImmediate(movement);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Limita_la_duracion_maxima_a_dos_segundos()
        {
            var resolver = new ClimbResolver(0.5f, 0.8f, 0.5f, 2.5f, 2f);
            var estado = new LocomotionState();
            Vector3 posicion = Vector3.zero;
            Vector3 destino = Vector3.up * 2f;
            Assert.That(
                resolver.IntentarIniciar(
                    posicion,
                    destino,
                    0.5f,
                    acercandose: true,
                    bordeMarcado: true
                ),
                Is.True
            );

            for (int frame = 0; frame < 119; frame++)
            {
                MotionIntent intent = resolver.Tick(estado, posicion, DeltaTime);
                posicion += intent.Velocity * DeltaTime;
            }

            Assert.That(resolver.Activo, Is.True);
            MotionIntent finalIntent = resolver.Tick(estado, posicion, DeltaTime);
            posicion += finalIntent.Velocity * DeltaTime;

            Assert.That(resolver.Activo, Is.False);
            Assert.That(posicion, Is.EqualTo(destino));
            Assert.That(estado.Phase, Is.EqualTo(LocomotionPhase.Grounded));
        }

        [Test]
        public void Repite_la_misma_trayectoria_con_delta_fijo()
        {
            Vector3 primera = SimularEscalada();
            Vector3 segunda = SimularEscalada();

            Assert.That(primera, Is.EqualTo(segunda));
        }

        private static ClimbResolver CrearResolver() => new ClimbResolver(2f, 0.8f, 0.5f, 2.5f, 2f);

        private static Vector3 SimularEscalada()
        {
            var resolver = CrearResolver();
            var estado = new LocomotionState();
            Vector3 posicion = Vector3.zero;
            Vector3 destino = new Vector3(1f, 1f, 0f);
            resolver.IntentarIniciar(
                posicion,
                destino,
                0.5f,
                acercandose: true,
                bordeMarcado: true
            );

            for (int frame = 0; frame < 60 && resolver.Activo; frame++)
            {
                MotionIntent intent = resolver.Tick(estado, posicion, DeltaTime);
                posicion += intent.Velocity * DeltaTime;
            }

            return posicion;
        }
    }
}
