using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    /// <summary>
    /// Lleva la cuenta de cuándo puede volver a golpear el cuerpo del jefe a cada jugador. El
    /// contacto no daña cada frame: tras un golpe, ese jugador queda en pausa durante el
    /// intervalo, aunque salga y vuelva a entrar al cuerpo. Cada jugador lleva su propia pausa.
    /// </summary>
    public sealed class RegistroContactoJefe
    {
        private readonly Dictionary<ulong, float> _proximoGolpe = new Dictionary<ulong, float>();
        private readonly float _intervalo;

        public RegistroContactoJefe(float intervalo)
        {
            if (intervalo < 0f)
                throw new ArgumentOutOfRangeException(nameof(intervalo), "No puede ser negativo.");

            _intervalo = intervalo;
        }

        /// <summary>
        /// True si el jugador puede recibir el golpe en <paramref name="ahora"/>; en ese caso
        /// queda en pausa hasta <paramref name="ahora"/> más el intervalo.
        /// </summary>
        public bool TryGolpear(ulong jugadorId, float ahora)
        {
            if (_proximoGolpe.TryGetValue(jugadorId, out float proximo) && ahora < proximo)
                return false;

            _proximoGolpe[jugadorId] = ahora + _intervalo;
            return true;
        }

        public void Olvidar(ulong jugadorId) => _proximoGolpe.Remove(jugadorId);

        public void Limpiar() => _proximoGolpe.Clear();
    }
}
