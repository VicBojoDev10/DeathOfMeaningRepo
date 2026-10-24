using NUnit.Framework;
using TDOM.Unity.Level;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public sealed class PlataformaCircularTests
    {
        private GameObject _goCenter;
        private GameObject _goPlatform;
        private PlataformaCircular _platform;

        [SetUp]
        public void SetUp()
        {
            _goCenter = new GameObject("CenterArena");
            _goCenter.transform.position = new Vector3(10f, 5f, 20f);

            _goPlatform = new GameObject("Plataforma");
            _goPlatform.transform.position = new Vector3(10f, 5f, 20f);
            _platform = _goPlatform.AddComponent<PlataformaCircular>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_goPlatform);
            Object.DestroyImmediate(_goCenter);
        }

        [Test]
        public void CalcularPosicion_ConFaseCeroYTiempoCero_RetornaRadioEnEjeX()
        {
            // Radio 5, velocidad 30, sentido 1 (antihorario), fase 0
            _platform.Configurar(_goCenter.transform, 5f, 30f, 1f, 0f);

            Vector3 pos = _platform.CalcularPosicion(0d);

            // A fase 0, cos(0)=1, sin(0)=0 -> X = center.x + 5, Z = center.z + 0
            Assert.That(pos.x, Is.EqualTo(15f).Within(1e-4f));
            Assert.That(pos.z, Is.EqualTo(20f).Within(1e-4f));
            Assert.That(pos.y, Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void CalcularPosicion_EsDeterminista_HostYClienteEnMismoTiempoVenMismaPosicion()
        {
            _platform.Configurar(_goCenter.transform, 8f, 25f, 1f, 45f);

            // Simular 5 minutos (300 segundos) de tiempo de red
            double tiempoServidor = 300d;

            Vector3 posHost = _platform.CalcularPosicion(tiempoServidor);
            Vector3 posCliente = _platform.CalcularPosicion(tiempoServidor);

            // La diferencia entre host y cliente debe ser exactamente 0
            float diff = Vector3.Distance(posHost, posCliente);
            Assert.That(diff, Is.LessThan(0.0001f));
        }

        [Test]
        public void CalcularPosicion_SentidoAntihorario_AvanzaEnSentidoTrigonometrico()
        {
            // 90 grados: vel 30 deg/s * 3 seg = 90 deg
            _platform.Configurar(_goCenter.transform, 4f, 30f, 1f, 0f);

            Vector3 pos = _platform.CalcularPosicion(3d);

            // cos(90)=0, sin(90)=1 -> X = center.x, Z = center.z + 4
            Assert.That(pos.x, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(pos.z, Is.EqualTo(24f).Within(1e-4f));
        }

        [Test]
        public void CalcularPosicion_SentidoHorario_AvanzaEnSentidoInverso()
        {
            // Sentido horario (-1), 90 grados: vel 30 deg/s * 3 seg = -90 deg
            _platform.Configurar(_goCenter.transform, 4f, 30f, -1f, 0f);

            Vector3 pos = _platform.CalcularPosicion(3d);

            // cos(-90)=0, sin(-90)=-1 -> X = center.x, Z = center.z - 4
            Assert.That(pos.x, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(pos.z, Is.EqualTo(16f).Within(1e-4f));
        }

        [Test]
        public void CalcularPosicion_ConFaseInicial_IniciaEnAnguloCorrecto()
        {
            // Fase inicial 180 deg
            _platform.Configurar(_goCenter.transform, 6f, 30f, 1f, 180f);

            Vector3 pos = _platform.CalcularPosicion(0d);

            // cos(180)=-1, sin(180)=0 -> X = center.x - 6 = 4, Z = center.z = 20
            Assert.That(pos.x, Is.EqualTo(4f).Within(1e-4f));
            Assert.That(pos.z, Is.EqualTo(20f).Within(1e-4f));
        }

        [Test]
        public void CalcularPosicion_MantieneAlturaOffsetRespectoAlCentro()
        {
            // Si la plataforma está 2 metros por debajo del centro
            _goPlatform.transform.position = new Vector3(10f, 3f, 20f);
            _platform.Configurar(_goCenter.transform, 5f, 30f, 1f, 0f);

            Vector3 pos = _platform.CalcularPosicion(10d);

            Assert.That(pos.y, Is.EqualTo(3f).Within(1e-4f));
        }
    }
}
