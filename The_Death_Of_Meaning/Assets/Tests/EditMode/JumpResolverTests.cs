/*using NUnit.Framework;
using UnityEngine;
using Tdom.Core.Locomotion;

namespace Tdom.Tests.EditMode
{
    /// <summary>
    /// Tests del JumpResolver.
    ///
    /// Este archivo muestra el caso realista: una clase con varias reglas
    /// que interactúan entre sí. Cada test verifica UNA regla.
    ///
    /// Los tres últimos tests cubren bugs concretos que ya han costado
    /// tiempo en otros proyectos. Si alguno se pone en rojo, no lo borres:
    /// está atrapando exactamente lo que debe atrapar.
    /// </summary>
    public sealed class JumpResolverTests
    {
        private const float Dt = 1f / 60f;

        // --- Helpers -----------------------------------------------------

        private static JumpResolver CrearAyla() =>
            new JumpResolver(maxSaltos: 2, velocidadSalto: 6f,
                             coyoteTime: 0.12f, bufferTime: 0.15f);

        private static JumpResolver CrearZendre() =>
            new JumpResolver(maxSaltos: 1, velocidadSalto: 5f,
                             coyoteTime: 0.12f, bufferTime: 0.15f);

        private static InputSnapshot ConSalto() =>
            new InputSnapshot(jumpPressed: true, jumpHeld: true);

        private static InputSnapshot SinSalto() =>
            new InputSnapshot(jumpPressed: false, jumpHeld: false);

        private static LocomotionState EnElAire() =>
            new LocomotionState { IsGrounded = false, JumpsUsed = 0 };

        private static LocomotionState EnSuelo() =>
            new LocomotionState { IsGrounded = true, JumpsUsed = 0 };

        // --- Reglas básicas ----------------------------------------------

        [Test]
        public void Ayla_puede_saltar_dos_veces()
        {
            var resolver = CrearAyla();
            var estado   = EnSuelo();

            resolver.Tick(estado, ConSalto(), Dt);
            estado.IsGrounded = false;
            resolver.Tick(estado, ConSalto(), Dt);

            Assert.AreEqual(2, estado.JumpsUsed);
        }

        [Test]
        public void Zendre_solo_puede_saltar_una_vez()
        {
            var resolver = CrearZendre();
            var estado   = EnSuelo();

            resolver.Tick(estado, ConSalto(), Dt);
            estado.IsGrounded = false;
            resolver.Tick(estado, ConSalto(), Dt);
            resolver.Tick(estado, ConSalto(), Dt);

            Assert.AreEqual(1, estado.JumpsUsed);
        }

        [Test]
        public void Tocar_suelo_reinicia_el_contador_de_saltos()
        {
            var resolver = CrearAyla();
            var estado   = EnElAire();
            estado.JumpsUsed = 2;

            estado.IsGrounded = true;
            resolver.Tick(estado, SinSalto(), Dt);

            Assert.AreEqual(0, estado.JumpsUsed);
        }

        // --- Coyote time -------------------------------------------------

        [Test]
        public void Coyote_time_permite_saltar_justo_despues_del_borde()
        {
            var resolver = CrearZendre();
            var estado   = EnSuelo();

            // Un frame en suelo para cargar el coyote timer
            resolver.Tick(estado, SinSalto(), Dt);

            // Se cae del borde
            estado.IsGrounded = false;
            resolver.Tick(estado, SinSalto(), Dt);

            // Salta 2 frames tarde: debe funcionar
            resolver.Tick(estado, ConSalto(), Dt);

            Assert.AreEqual(1, estado.JumpsUsed);
        }

        [Test]
        public void Coyote_time_expirado_ya_no_permite_saltar()
        {
            var resolver = CrearZendre();
            var estado   = EnSuelo();
            resolver.Tick(estado, SinSalto(), Dt);

            estado.IsGrounded = false;
            // 20 frames = 0.33 s, muy por encima de los 0.12 s
            for (int i = 0; i < 20; i++)
                resolver.Tick(estado, SinSalto(), Dt);

            resolver.Tick(estado, ConSalto(), Dt);

            Assert.AreEqual(0, estado.JumpsUsed);
        }

        /// <summary>
        /// BUG CONOCIDO: si el coyote timer no se pone en cero al saltar,
        /// Zendre consigue un doble salto accidental durante la ventana.
        /// </summary>
        [Test]
        public void Zendre_no_obtiene_doble_salto_dentro_de_la_ventana_de_coyote()
        {
            var resolver = CrearZendre();
            var estado   = EnSuelo();
            resolver.Tick(estado, SinSalto(), Dt);

            estado.IsGrounded = false;
            resolver.Tick(estado, ConSalto(), Dt);   // salto 1
            resolver.Tick(estado, ConSalto(), Dt);   // no debe contar

            Assert.AreEqual(1, estado.JumpsUsed);
        }

        // --- Jump buffer -------------------------------------------------

        [Test]
        public void El_buffer_ejecuta_el_salto_al_aterrizar()
        {
            var resolver = CrearZendre();
            var estado   = EnElAire();
            estado.JumpsUsed = 1;
            estado.CoyoteTimer = 0f;

            // Presiona justo antes de tocar el suelo
            resolver.Tick(estado, ConSalto(), Dt);
            Assert.AreEqual(1, estado.JumpsUsed, "todavía no debe saltar");

            // Aterriza en el siguiente frame, sin volver a presionar
            estado.IsGrounded = true;
            resolver.Tick(estado, SinSalto(), Dt);

            Assert.AreEqual(1, estado.JumpsUsed,
                "el salto bufferizado debe haberse ejecutado al aterrizar");
        }

        // --- Velocidad ---------------------------------------------------

        /// <summary>
        /// BUG CONOCIDO: si el salto SUMA en vez de ASIGNAR velocity.y,
        /// saltar mientras caes da un impulso débil y el doble salto se
        /// siente inconsistente.
        /// </summary>
        [Test]
        public void El_doble_salto_asigna_la_velocidad_no_la_suma()
        {
            var resolver = CrearAyla();
            var estado   = EnElAire();
            estado.JumpsUsed = 1;
            estado.Velocity  = new Vector3(0f, -12f, 0f);   // cayendo rápido

            resolver.Tick(estado, ConSalto(), Dt);

            Assert.AreEqual(6f, estado.Velocity.y, 0.01f,
                "debe quedar en 6, no en -6 (que sería -12 + 6 * ...)");
        }
    }
}
*/