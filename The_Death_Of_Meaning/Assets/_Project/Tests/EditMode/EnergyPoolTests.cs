using UnityEngine;
using NUnit.Framework;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class EnergyPoolTests
    {
        private const float Tolerance = 0.001f;

        private static EnergyPool CreatePool() => new EnergyPool(100f, 10f, 5f);

        [Test]
        public void Constructor_StartsWithCurrentAndMaxEqualToBaseMax()
        {
            var pool = CreatePool();

            Assert.AreEqual(100f, pool.BaseMax, Tolerance);
            Assert.AreEqual(100f, pool.Max, Tolerance);
            Assert.AreEqual(100f, pool.Current, Tolerance);
        }

        [Test]
        public void TryConsume_WithEnoughEnergy_SubtractsAndReturnsTrue()
        {
            var pool = CreatePool();

            bool result = pool.TryConsume(30f);

            Assert.IsTrue(result);
            Assert.AreEqual(70f, pool.Current, Tolerance);
        }

        [Test]
        public void TryConsume_WithoutEnoughEnergy_ReturnsFalseAndDoesNotChangeCurrent()
        {
            var pool = CreatePool();
            pool.TryConsume(90f);

            bool result = pool.TryConsume(20f);

            Assert.IsFalse(result);
            Assert.AreEqual(10f, pool.Current, Tolerance);
        }

        [TestCase(0f)]
        [TestCase(-5f)]
        public void TryConsume_WithZeroOrNegativeAmount_ReturnsFalseAndDoesNotChangeCurrent(float amount)
        {
            var pool = CreatePool();

            bool result = pool.TryConsume(amount);

            Assert.IsFalse(result);
            Assert.AreEqual(100f, pool.Current, Tolerance);
        }

        [Test]
        public void Tick_NotDowned_RegeneratesButNeverExceedsMax()
        {
            var pool = CreatePool();
            pool.TryConsume(50f);

            pool.Tick(2f, false);
            Assert.AreEqual(70f, pool.Current, Tolerance);

            pool.Tick(100f, false);
            Assert.AreEqual(pool.Max, pool.Current, Tolerance);
            Assert.AreEqual(100f, pool.Current, Tolerance);
        }

        [Test]
        public void Tick_Downed_DrainsCurrentAndReducesMax()
        {
            var pool = CreatePool();

            pool.Tick(4f, true);

            Assert.AreEqual(80f, pool.Current, Tolerance);
            Assert.AreEqual(80f, pool.Max, Tolerance);
        }

        [Test]
        public void Tick_AfterDowned_RegenerationStopsAtReducedMax()
        {
            var pool = CreatePool();
            pool.Tick(4f, true);

            pool.Tick(10f, false);

            Assert.AreEqual(80f, pool.Current, Tolerance);
            Assert.AreEqual(80f, pool.Max, Tolerance);
        }

        [Test]
        public void Tick_Downed_NeverLeavesCurrentNegative()
        {
            var pool = CreatePool();

            pool.Tick(1000f, true);

            Assert.GreaterOrEqual(pool.Current, 0f);
            Assert.AreEqual(0f, pool.Current, Tolerance);
            Assert.AreEqual(0f, pool.Max, Tolerance);
        }
        [Test]
        public void RestoreMaxOnPhaseChange_RestoresMaxButKeepsCurrent()
        {
            var pool = CreatePool();
            pool.Tick(4f, true);

            pool.RestoreMaxOnPhaseChange();

            Assert.AreEqual(100f, pool.Max, Tolerance);
            Assert.AreEqual(80f, pool.Current, Tolerance);
        }

        [Test]
        public void Tick_AfterRestoreMax_RegeneratesUpToBaseMax()
        {
            var pool = CreatePool();
            pool.Tick(4f, true);
            pool.RestoreMaxOnPhaseChange();

            pool.Tick(10f, false);

            Assert.AreEqual(100f, pool.Current, Tolerance);
        }

        [Test]
        public void Tick_Regen_IsFramerateIndependent()
        {
            var at60 = CreatePool();
            var at30 = CreatePool();
            at60.TryConsume(50f);
            at30.TryConsume(50f);

            for (int i = 0; i < 60; i++) at60.Tick(1f / 60f, false);
            for (int i = 0; i < 30; i++) at30.Tick(1f / 30f, false);

            Assert.AreEqual(at60.Current, at30.Current, Tolerance);
            Assert.AreEqual(60f, at60.Current, Tolerance);
        }

        [Test]
        public void Tick_DownedDrain_IsFramerateIndependent()
        {
            var at60 = CreatePool();
            var at30 = CreatePool();

            for (int i = 0; i < 60; i++) at60.Tick(1f / 60f, true);
            for (int i = 0; i < 30; i++) at30.Tick(1f / 30f, true);

            Assert.AreEqual(at60.Current, at30.Current, Tolerance);
            Assert.AreEqual(at60.Max, at30.Max, Tolerance);
            Assert.AreEqual(95f, at60.Current, Tolerance);
        }
    }
}
