using NUnit.Framework;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class CooldownTimerRecorteTests
    {
        private const float Duracion = 4f;
        private const float FactorMitad = 0.5f;
        private const float Dt = 1f / 60f;

        private static float SimularHastaListo(CooldownTimer cooldown, float? factorAlFallar)
        {
            float tiempo = 0f;
            bool recortado = factorAlFallar == null;
            cooldown.Disparar();
            while (!cooldown.Listo)
            {
                cooldown.Tick(Dt);
                tiempo += Dt;
                if (!recortado)
                {
                    cooldown.Recortar(factorAlFallar.Value);
                    recortado = true;
                }
            }
            return tiempo;
        }

        [Test]
        public void Gancho_que_conecta_tiene_el_cooldown_completo()
        {
            float tiempo = SimularHastaListo(new CooldownTimer(Duracion), null);

            Assert.That(tiempo, Is.EqualTo(Duracion).Within(Dt));
        }

        [Test]
        public void Gancho_que_falla_tiene_la_mitad_del_cooldown()
        {
            float tiempo = SimularHastaListo(new CooldownTimer(Duracion), FactorMitad);

            Assert.That(tiempo, Is.EqualTo(Duracion * FactorMitad).Within(Dt));
        }

        [Test]
        public void Factor_uno_deja_el_cooldown_completo()
        {
            float tiempo = SimularHastaListo(new CooldownTimer(Duracion), 1f);

            Assert.That(tiempo, Is.EqualTo(Duracion).Within(Dt));
        }

        [Test]
        public void Factor_cero_deja_el_gancho_sin_cooldown()
        {
            var cooldown = new CooldownTimer(Duracion);
            cooldown.Disparar();

            cooldown.Recortar(0f);

            Assert.That(cooldown.Listo, Is.True);
        }

        [Test]
        public void Recortar_despues_de_un_tiempo_descuenta_lo_ya_transcurrido()
        {
            var cooldown = new CooldownTimer(Duracion);
            cooldown.Disparar();
            cooldown.Tick(1f);

            cooldown.Recortar(FactorMitad);

            Assert.That(cooldown.Normalizado, Is.EqualTo(1f / Duracion).Within(0.0001f));
        }

        [Test]
        public void Recortar_nunca_aumenta_el_cooldown_restante()
        {
            var cooldown = new CooldownTimer(Duracion);
            cooldown.Disparar();
            cooldown.Tick(3.5f);
            float antes = cooldown.Normalizado;

            cooldown.Recortar(FactorMitad);

            Assert.That(cooldown.Normalizado, Is.LessThanOrEqualTo(antes));
        }

        [Test]
        public void El_recorte_es_determinista_con_dt_fijo()
        {
            float primera = SimularHastaListo(new CooldownTimer(Duracion), FactorMitad);
            float segunda = SimularHastaListo(new CooldownTimer(Duracion), FactorMitad);

            Assert.That(segunda, Is.EqualTo(primera));
        }
    }
}
