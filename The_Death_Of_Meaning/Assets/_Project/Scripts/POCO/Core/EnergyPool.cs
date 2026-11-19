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
        private readonly float _regenDelayAfterSpend;
        private float _regenDelayRemaining;

        public EnergyPool(
            float baseMax,
            float regenPorSegundo,
            float drenajeabatidoPorSegundo,
            float regenDelayAfterSpend = 0f
        )
        {
            BaseMax = Math.Max(0f, baseMax);
            _regenPerSecond = Math.Max(0f, regenPorSegundo);
            _downedDrainPerSecond = Math.Max(0f, drenajeabatidoPorSegundo);
            _regenDelayAfterSpend = Math.Max(0f, regenDelayAfterSpend);

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
            _regenDelayRemaining = _regenDelayAfterSpend;
            return true;
        }

        public void Restore(float amount)
        {
            if (amount <= 0f)
                return;
            Current = Math.Min(Current + amount, Max);
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
                if (_regenDelayRemaining > 0f)
                {
                    float pausa = Math.Min(_regenDelayRemaining, dt);
                    _regenDelayRemaining -= pausa;
                    dt -= pausa;
                }
                if (dt > 0f)
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
