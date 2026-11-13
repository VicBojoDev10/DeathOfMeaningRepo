using System;

namespace TDOM.Gameplay.Core
{
    public class EnergyPool
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public float BaseMax { get; }
        private readonly float _regenPerSecond;
        private readonly float _downedDrainPerSecond;

        public EnergyPool(float baseMax, float regenPorSegundo, float drenajeabatidoPorSegundo)
        {
            BaseMax = Math.Max(0f, baseMax);
            _regenPerSecond = Math.Max(0f, regenPorSegundo);
            _downedDrainPerSecond = Math.Max(0f, drenajeabatidoPorSegundo);

            Max = BaseMax;
            Current = Max;
        }

        public bool TryConsume(float amount)
        {
            if (amount <= 0f)
                return false;
            if (amount > Current)
                return false;

            Current -= amount;
            return true;
        }

        public void Tick(float dt, bool isDowned, bool allowRegeneration = true)
        {
            if (dt <= 0f)
                return;
            if (isDowned)
            {
                Current = Math.Max(Current - _downedDrainPerSecond * dt, 0f);
                Max = Math.Min(Max, Current);
            }
            else if (allowRegeneration)
            {
                Current = Math.Min(Current + _regenPerSecond * dt, Max);
            }
        }

        public void RestoreMaxOnPhaseChange()
        {
            Max = BaseMax;
            Current = Max;
        }

        public void RestoreMaxPreservingCurrent()
        {
            Max = BaseMax;
            Current = Math.Min(Current, Max);
        }
    }
}
