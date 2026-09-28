using UnityEngine;

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
            BaseMax = Mathf.Max(0f, baseMax);
            _regenPerSecond = Mathf.Max(0f, regenPorSegundo);
            _downedDrainPerSecond = Mathf.Max(0f, drenajeabatidoPorSegundo);

            Max = baseMax;
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

        public void Tick(float dt, bool isDowned)
        {
            if (dt <= 0f)
                return;
            if (isDowned)
            {
                Current = Mathf.Max(Current - _downedDrainPerSecond * dt, 0f);
                Max = Mathf.Min(Max, Current);
            }
            else
            {
                Current = Mathf.Min(Current + _regenPerSecond * dt, Max);
            }
        }

        public void RestoreMaxOnPhaseChange()
        {
            Max = BaseMax;
        }
    }
}
