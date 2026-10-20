using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay.Camera;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class LockOnSwitcherTests
    {
        private const float Alcance = 60f;
        private const float UmbralCambio = 0.7f;
        private const float UmbralRearme = 0.3f;
        private const float AnguloMaximo = 60f;
        private const int IdActual = 1;
        private const int IdDerecha = 2;
        private const int IdIzquierda = 3;
        private const int IdArriba = 4;
        private const int IdLejano = 5;

        private static readonly Vector3 Origen = Vector3.zero;
        private static readonly Vector3 PuntoActual = new Vector3(0f, 0f, 20f);
        private static readonly Vector2 StickDerecha = Vector2.right;
        private static readonly Vector2 StickIzquierda = Vector2.left;
        private static readonly Vector2 StickArriba = Vector2.up;
        private static readonly Vector2 StickSuelto = Vector2.zero;

        private static LockOnSwitcher CrearSwitcher() =>
            new LockOnSwitcher(
                new LockOnSwitchSettings(Alcance, UmbralCambio, UmbralRearme, AnguloMaximo)
            );

        private static List<LockOnCandidate> Candidatos() =>
            new List<LockOnCandidate>
            {
                new LockOnCandidate(IdActual, PuntoActual),
                new LockOnCandidate(IdDerecha, new Vector3(8f, 0f, 20f)),
                new LockOnCandidate(IdIzquierda, new Vector3(-8f, 0f, 20f)),
                new LockOnCandidate(IdArriba, new Vector3(0f, 10f, 20f)),
            };

        private static bool Cambiar(
            LockOnSwitcher switcher,
            Vector2 stick,
            List<LockOnCandidate> candidatos,
            out LockOnCandidate elegido
        ) => switcher.TryCambiar(stick, Origen, PuntoActual, IdActual, candidatos, out elegido);

        [Test]
        public void Mover_el_stick_a_la_derecha_elige_el_objetivo_de_la_derecha()
        {
            Assert.IsTrue(Cambiar(CrearSwitcher(), StickDerecha, Candidatos(), out var elegido));
            Assert.AreEqual(IdDerecha, elegido.Id);
        }

        [Test]
        public void Mover_el_stick_a_la_izquierda_elige_el_objetivo_de_la_izquierda()
        {
            Assert.IsTrue(Cambiar(CrearSwitcher(), StickIzquierda, Candidatos(), out var elegido));
            Assert.AreEqual(IdIzquierda, elegido.Id);
        }

        [Test]
        public void Mover_el_stick_arriba_elige_el_objetivo_de_arriba()
        {
            Assert.IsTrue(Cambiar(CrearSwitcher(), StickArriba, Candidatos(), out var elegido));
            Assert.AreEqual(IdArriba, elegido.Id);
        }

        [Test]
        public void Un_movimiento_debajo_del_umbral_no_cambia_de_objetivo()
        {
            Assert.IsFalse(
                Cambiar(CrearSwitcher(), StickDerecha * (UmbralCambio * 0.5f), Candidatos(), out _)
            );
        }

        [Test]
        public void Mantener_el_stick_solo_cambia_una_vez_hasta_soltarlo()
        {
            var switcher = CrearSwitcher();
            var candidatos = Candidatos();

            Assert.IsTrue(Cambiar(switcher, StickDerecha, candidatos, out _));
            Assert.IsFalse(Cambiar(switcher, StickDerecha, candidatos, out _));

            Cambiar(switcher, StickSuelto, candidatos, out _);
            Assert.IsTrue(Cambiar(switcher, StickDerecha, candidatos, out _));
        }

        [Test]
        public void Sin_objetivos_en_esa_direccion_no_cambia()
        {
            var candidatos = new List<LockOnCandidate>
            {
                new LockOnCandidate(IdActual, PuntoActual),
                new LockOnCandidate(IdIzquierda, new Vector3(-8f, 0f, 20f)),
            };

            Assert.IsFalse(Cambiar(CrearSwitcher(), StickDerecha, candidatos, out _));
        }

        [Test]
        public void Los_objetivos_fuera_de_alcance_no_se_eligen()
        {
            var candidatos = new List<LockOnCandidate>
            {
                new LockOnCandidate(IdActual, PuntoActual),
                new LockOnCandidate(IdLejano, new Vector3(80f, 0f, 80f)),
            };

            Assert.IsFalse(Cambiar(CrearSwitcher(), StickDerecha, candidatos, out _));
        }

        [Test]
        public void Se_elige_el_objetivo_mas_cercano_en_la_direccion_del_stick()
        {
            var candidatos = new List<LockOnCandidate>
            {
                new LockOnCandidate(IdActual, PuntoActual),
                new LockOnCandidate(IdLejano, new Vector3(16f, 0f, 20f)),
                new LockOnCandidate(IdDerecha, new Vector3(4f, 0f, 20f)),
            };

            Assert.IsTrue(Cambiar(CrearSwitcher(), StickDerecha, candidatos, out var elegido));
            Assert.AreEqual(IdDerecha, elegido.Id);
        }
    }
}
