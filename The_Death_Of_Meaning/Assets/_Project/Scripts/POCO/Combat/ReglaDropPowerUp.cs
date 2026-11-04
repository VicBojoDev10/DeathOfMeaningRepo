using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    /// <summary>
    /// Decide en POCO puro si una muerte suelta un power-up y cuál (TW-276). Los azares entran
    /// como parámetro para poder probarla con valores fijos; en juego se pasa Random.value.
    /// Lleva la cuenta de muertes seguidas sin drop para la garantía.
    /// </summary>
    public sealed class ReglaDropPowerUp
    {
        private readonly float _probabilidad;
        private readonly int _muertesParaGarantizado;
        private readonly int _maxVivos;
        private readonly float[] _pesos;
        private readonly float _pesoTotal;

        /// <summary>Muertes seguidas que no soltaron nada.</summary>
        public int MuertesSinDrop { get; private set; }

        /// <param name="probabilidad">0 a 1: probabilidad de soltar en cada muerte.</param>
        /// <param name="muertesParaGarantizado">Muertes seguidas sin drop tras las que la siguiente suelta seguro (mínimo 1).</param>
        /// <param name="maxVivos">Pickups vivos a la vez; con ese tope no suelta.</param>
        /// <param name="pesos">Peso de cada entrada de la tabla; el índice devuelto es el de esta lista.</param>
        public ReglaDropPowerUp(
            float probabilidad,
            int muertesParaGarantizado,
            int maxVivos,
            IReadOnlyList<float> pesos
        )
        {
            _probabilidad = Math.Clamp(probabilidad, 0f, 1f);
            _muertesParaGarantizado = Math.Max(1, muertesParaGarantizado);
            _maxVivos = Math.Max(0, maxVivos);

            int cantidad = pesos != null ? pesos.Count : 0;
            _pesos = new float[cantidad];
            for (int i = 0; i < cantidad; i++)
            {
                _pesos[i] = Math.Max(0f, pesos[i]);
                _pesoTotal += _pesos[i];
            }
        }

        /// <summary>
        /// Índice de la entrada elegida, o null si esta muerte no suelta nada. Cuenta como una
        /// muerte: si no suelta suma a la garantía y si suelta la reinicia.
        /// </summary>
        /// <param name="azarDrop">0 a 1: se compara con la probabilidad.</param>
        /// <param name="azarPeso">0 a 1: elige la entrada según su peso.</param>
        /// <param name="vivos">Pickups vivos ahora mismo.</param>
        public int? Elegir(float azarDrop, float azarPeso, int vivos)
        {
            int? elegido = null;

            if (vivos < _maxVivos && _pesoTotal > 0f && DebeSoltar(azarDrop))
                elegido = ElegirPorPeso(azarPeso);

            MuertesSinDrop = elegido.HasValue ? 0 : MuertesSinDrop + 1;
            return elegido;
        }

        private bool DebeSoltar(float azarDrop) =>
            _probabilidad >= 1f
            || azarDrop < _probabilidad
            || MuertesSinDrop + 1 >= _muertesParaGarantizado;

        private int ElegirPorPeso(float azarPeso)
        {
            float objetivo = Math.Clamp(azarPeso, 0f, 1f) * _pesoTotal;
            float acumulado = 0f;
            int ultimoConPeso = 0;

            for (int i = 0; i < _pesos.Length; i++)
            {
                if (_pesos[i] <= 0f)
                    continue;

                ultimoConPeso = i;
                acumulado += _pesos[i];
                if (objetivo < acumulado)
                    return i;
            }

            return ultimoConPeso;
        }
    }
}
