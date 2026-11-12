using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    /// <summary>
    /// Une los ojos con las fases del jefe. Cada fase tiene un tramo de la vida total (la
    /// diferencia entre su umbral de entrada y el de salida) que se reparte a partes iguales
    /// entre los ojos: dañar un ojo baja la vida del jefe en proporción a la vida que le quita
    /// a ese ojo, así que cuando los ojos están todos cerrados el jefe llega justo al umbral y
    /// cambia de fase. Los ojos se vuelven a abrir con ReabrirOjos, no al cambiar de fase, para
    /// poder esperar a que termine la transición. Al reabrirlos, si el PlanEscudos da capas para
    /// la fase nueva, cada ojo vuelve con ellas y hay que romperlas de afuera hacia adentro.
    /// </summary>
    public sealed class BossWeakPointDamage
    {
        // Compensa el error de redondeo de float para no quedarse apenas por encima del umbral.
        private const float Margen = 1e-5f;

        private readonly WeakPointStateMachine _ojos;
        private readonly IBossPhaseState _fases;
        private readonly float[] _vidaPorFase;
        private readonly PlanEscudos _escudos;

        private float _consumidoEnFase;
        private bool _esperandoReapertura;

        public BossWeakPointDamage(
            WeakPointStateMachine ojos,
            IBossPhaseState fases,
            IReadOnlyList<float> vidaPorFase,
            PlanEscudos escudos = null
        )
        {
            _ojos = ojos ?? throw new ArgumentNullException(nameof(ojos));
            _fases = fases ?? throw new ArgumentNullException(nameof(fases));

            if (vidaPorFase == null)
                throw new ArgumentNullException(nameof(vidaPorFase));
            if (vidaPorFase.Count == 0)
                throw new ArgumentException("Debe haber al menos una fase.", nameof(vidaPorFase));

            _escudos = escudos;
            _vidaPorFase = new float[vidaPorFase.Count];
            for (int i = 0; i < _vidaPorFase.Length; i++)
            {
                if (vidaPorFase[i] < 0f)
                    throw new ArgumentOutOfRangeException(
                        nameof(vidaPorFase),
                        "La vida de una fase no puede ser negativa."
                    );
                _vidaPorFase[i] = vidaPorFase[i];
            }
        }

        public int TotalOjos => _ojos.TotalOjos;

        /// <summary>True entre el cambio de fase y ReabrirOjos: los ojos siguen cerrados.</summary>
        public bool EsperandoReapertura => _esperandoReapertura;

        private float VidaDeLaFase
        {
            get
            {
                int indice = Math.Min(Math.Max(_fases.FaseActual, 1), _vidaPorFase.Length) - 1;
                return _vidaPorFase[indice];
            }
        }

        public EstadoOjo Estado(int ojo) => _ojos.Estado(ojo);

        public float VidaOjo(int ojo) => _ojos.Vida(ojo);

        public float VidaMaximaOjo => _ojos.VidaMaximaOjo;

        /// <summary>La capa de afuera del ojo (la que toca romper ahora).</summary>
        public ColorEscudo Escudo(int ojo) => _ojos.Escudo(ojo);

        public int CapasEscudo(int ojo) => _ojos.CapasEscudo(ojo);

        public byte EscudosEmpaquetados(int ojo) => _ojos.EscudosEmpaquetados(ojo);

        /// <summary>
        /// Un golpe a un ojo con escudo: si el atacante es del color de la capa de afuera, la rompe (y
        /// ese golpe no hace daño); si no, no pasa nada. Devuelve true si rompió una capa.
        /// </summary>
        public bool IntentarRomperEscudo(int ojo, TipoImpactoOjo tipo, bool atacanteEsAyla)
        {
            if (_esperandoReapertura)
                return false;

            if (tipo == TipoImpactoOjo.Disparo && atacanteEsAyla)
                return false;

            return _ojos.RomperEscudo(ojo, atacanteEsAyla);
        }

        public void AbrirOjoParaDebug(int ojo) => _ojos.AbrirOjoParaDebug(ojo);

        public bool AdmiteImpacto(int ojo, TipoImpactoOjo tipo, bool atacanteEsAyla) =>
            !_esperandoReapertura && _ojos.AdmiteImpacto(ojo, tipo, atacanteEsAyla);

        public bool AdmiteImpacto(int ojo, TipoImpactoOjo tipo) =>
            !_esperandoReapertura && _ojos.AdmiteImpacto(ojo, tipo);

        /// <summary>
        /// Daña un ojo y baja la vida del jefe en consecuencia. Devuelve true si con este golpe
        /// se cerró el último ojo abierto y el jefe pasó de fase. fraccionVidaJefe es la parte de
        /// la vida total del jefe (0 a 1) que quitó el golpe; 0 si no hizo nada.
        /// </summary>
        public bool AplicarDanioOjo(int ojo, float cantidad, out float fraccionVidaJefe)
        {
            fraccionVidaJefe = 0f;

            if (_esperandoReapertura)
                return false;

            float aplicado = _ojos.AplicarDanio(ojo, cantidad);
            if (aplicado <= 0f)
                return false;

            float vidaDeLaFase = VidaDeLaFase;
            float fraccion = aplicado / _ojos.VidaMaximaOjo * (vidaDeLaFase / _ojos.TotalOjos);
            _consumidoEnFase += fraccion;

            if (_ojos.TodosCerrados)
            {
                // Con el último ojo se salda lo que quede del tramo, así el jefe cae justo en
                // el umbral aunque los golpes anteriores hayan perdido algo por redondeo.
                fraccion += Math.Max(0f, vidaDeLaFase - _consumidoEnFase) + Margen;
                _consumidoEnFase = vidaDeLaFase;
            }

            fraccionVidaJefe = fraccion;

            int faseAnterior = _fases.FaseActual;
            _fases.AplicarDaño(fraccion);

            if (_fases.FaseActual == faseAnterior)
                return false;

            _esperandoReapertura = true;
            return true;
        }

        /// <summary>Abre todos los ojos con la vida llena y empieza a contar el tramo de la fase nueva.</summary>
        public void ReabrirOjos()
        {
            _esperandoReapertura = false;
            _consumidoEnFase = 0f;
            _ojos.AbrirTodos();

            if (_escudos == null)
                return;

            for (int ojo = 0; ojo < _ojos.TotalOjos; ojo++)
                _ojos.PonerEscudos(ojo, _escudos.Capas(_fases.FaseActual, ojo, _ojos.TotalOjos));
        }
    }
}
