using NUnit.Framework;

namespace TDOM.Tests.EditMode
{
    /// <summary>
    /// Test de humo: solo comprueba que el pipeline de tests corre en el CI y
    /// en el Test Runner. Es la plantilla mínima para escribir un test POCO.
    ///
    /// Cuando existan los resolvers reales (bloque C, D, E), agrega su asmdef
    /// a "references" en TDOM.Tests.EditMode.asmdef (por ejemplo TDOM.Gameplay)
    /// y reemplaza este archivo por los tests de verdad. Ver CLAUDE.md.
    /// </summary>
    public sealed class SmokeTest
    {
        [Test]
        public void ElPipelineDeTestsCorre()
        {
            Assert.AreEqual(4, 2 + 2);
        }
    }
}
