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
        [Test]
        public void Testing_Tick()
        {
            var gravity = new GravityModel(-9.81f, -20f, 1f);
            var jump = new JumpResolver(1, 8f, 0.1f, 0.1f);
            var ground = new GroundControlResolver(4.5f, 6.5f, 25f, 30f, 0.3f);
            var run = new SprintResolver();

            var dashProfile = ScriptableObject.CreateInstance<DashProfile>();
            dashProfile.Distance = 6f;
            dashProfile.Duration = 0.2f;
            dashProfile.Cooldown = 1f;
            dashProfile.MaxTurnRate = 0f;
            dashProfile.Easing = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            var dash = new DashResolver(dashProfile);

            var input = new InputSnapshot(
                Vector2.zero,
                Vector2.zero,
                false,
                false,
                false,
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
            var yaw = Quaternion.identity;
            const float dt = 1f / 60f;

            var playerLocomotion = new PlayerLocomotion(gravity, jump, ground, run, dash);
            var intent = playerLocomotion.Tick(input, yaw, dt);

            Assert.That(playerLocomotion.State, Is.Not.Null);
            Assert.That(intent.IgnoreGravity, Is.False);
        }
    }
}
