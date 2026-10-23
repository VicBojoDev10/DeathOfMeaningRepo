using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Core
{
    public enum PlayerLifeState : byte
    {
        Vivo = 0,
        Abatido = 1,
        Muerto = 2,
    }

    public class HealthPool
    {
        private const float DuracionMinimaAbatido = 0.1f;
        public float Max { get; private set; }
        public float Current { get; private set; }
        public float BaseMax { get; }
        public PlayerLifeState State { get; private set; }
        public float DownedDuration { get; }
        public float DownedRemainingTime { get; private set; }
        public float ReviveProgress { get; private set; }

        private float _reviveTimeElapsed;

        public float DownedNormalizedTime =>
            DownedDuration > 0f ? Math.Clamp(DownedRemainingTime / DownedDuration, 0f, 1f) : 0f;

        public HealthPool(float baseMax, float downedDuration)
        {
            BaseMax = Math.Max(0f, baseMax);
            Max = BaseMax;
            Current = Max;
            DownedDuration = Math.Max(DuracionMinimaAbatido, downedDuration);
            DownedRemainingTime = DownedDuration;
            State = PlayerLifeState.Vivo;
            ReviveProgress = 0f;
            _reviveTimeElapsed = 0f;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || State != PlayerLifeState.Vivo)
                return;

            Current = Math.Max(0f, Current - amount);
            if (Current <= 0f)
            {
                Current = 0f;
                State = PlayerLifeState.Abatido;
                DownedRemainingTime = DownedDuration;
                ReviveProgress = 0f;
                _reviveTimeElapsed = 0f;
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || State != PlayerLifeState.Vivo)
                return;

            Current = Math.Min(Max, Current + amount);
        }

        public bool Revive(float health)
        {
            if (State != PlayerLifeState.Abatido || health <= 0f || float.IsNaN(health))
                return false;

            State = PlayerLifeState.Vivo;
            Current = Math.Clamp(health, 0f, Max);
            DownedRemainingTime = DownedDuration;
            ReviveProgress = 0f;
            _reviveTimeElapsed = 0f;
            return true;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f)
                return;

            if (State == PlayerLifeState.Abatido)
            {
                DownedRemainingTime = Math.Max(0f, DownedRemainingTime - dt);
                if (DownedRemainingTime <= 0f)
                {
                    DownedRemainingTime = 0f;
                    State = PlayerLifeState.Muerto;
                }
            }
        }

        public bool ProgressRevive(float dt, float requiredDuration, float reviveHealth)
        {
            if (
                State != PlayerLifeState.Abatido
                || dt <= 0f
                || requiredDuration <= 0f
                || reviveHealth <= 0f
            )
                return false;

            _reviveTimeElapsed += dt;
            ReviveProgress = Math.Clamp(_reviveTimeElapsed / requiredDuration, 0f, 1f);

            if (_reviveTimeElapsed >= requiredDuration)
            {
                return Revive(reviveHealth);
            }

            return false;
        }

        public void CancelRevive()
        {
            _reviveTimeElapsed = 0f;
            ReviveProgress = 0f;
        }

        public void TickDowned(
            float dt,
            bool companionInRange,
            float reviveDuration,
            float reviveHealth
        )
        {
            if (State != PlayerLifeState.Abatido || dt <= 0f)
                return;

            if (companionInRange && reviveDuration > 0f && reviveHealth > 0f)
            {
                ProgressRevive(dt, reviveDuration, reviveHealth);
                return;
            }

            CancelRevive();
            Tick(dt);
        }

        public static bool CheckGameOver(HealthPool player1, HealthPool player2)
        {
            if (player1 == null || player2 == null)
                return false;

            if (player1.State == PlayerLifeState.Muerto || player2.State == PlayerLifeState.Muerto)
                return true;

            if (
                player1.State == PlayerLifeState.Abatido
                && player2.State == PlayerLifeState.Abatido
            )
                return true;

            return false;
        }

        public static bool CheckGameOver(IReadOnlyList<HealthPool> players)
        {
            if (players == null || players.Count == 0)
                return false;

            int downedOrDeadCount = 0;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null)
                    continue;

                if (p.State == PlayerLifeState.Muerto)
                    return true;

                if (p.State == PlayerLifeState.Abatido)
                    downedOrDeadCount++;
            }

            return downedOrDeadCount >= players.Count;
        }
    }
}
