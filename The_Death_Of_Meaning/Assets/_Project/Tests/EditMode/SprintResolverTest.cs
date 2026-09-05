using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay;
using UnityEngine;

namespace TDOM.Tests
{
    public sealed class SprintResolverTest
    {
        [Test]
        public void Correr_se_apaga_al_soltar_el_stick()
        {
            var resolver = new SprintResolver();

            var input = new InputSnapshot(false, false) { SprintPressed = true, Move = Vector2.up };

            resolver.Tick(input);
            Assert.IsTrue(resolver.Corriendo);

            input.SprintPressed = false;
            resolver.Tick(input);
            Assert.IsTrue(resolver.Corriendo);

            input.Move = Vector2.zero;
            resolver.Tick(input);

            Assert.IsFalse(resolver.Corriendo);
        }
    }
}
