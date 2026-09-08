using NUnit.Framework;
using TDOM.Gameplay.Camera;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class LookResolverTests
    {
        [Test]
        public void Pitch_NuncaSuperaElClamp()
        {
            var resolver = new LookResolver(sensibilidad: 100f, pitchMin: -85f, pitchMax: 85f);

            // Empujamos el pitch muy fuerte hacia arriba durante muchos frames
            for (int i = 0; i < 100; i++)
                resolver.Tick(new Vector2(0f, 10f), dt: 1f);

            Assert.LessOrEqual(resolver.Pitch, 85f);
            Assert.GreaterOrEqual(resolver.Pitch, -85f);
        }

        [Test]
        public void Yaw_EnvuelveCorrectamente()
        {
            var resolver = new LookResolver(sensibilidad: 1f);

            // Empujamos el yaw bastante más allá de 360°
            resolver.Tick(new Vector2(500f, 0f), dt: 1f);

            Assert.GreaterOrEqual(resolver.Yaw, 0f);
            Assert.Less(resolver.Yaw, 360f);
        }

        [Test]
        public void Sensibilidad_Escala()
        {
            var lento = new LookResolver(sensibilidad: 1f);
            var rapido = new LookResolver(sensibilidad: 5f);

            lento.Tick(new Vector2(1f, 0f), dt: 1f);
            rapido.Tick(new Vector2(1f, 0f), dt: 1f);

            Assert.Greater(rapido.Yaw, lento.Yaw);
        }
    }
}
