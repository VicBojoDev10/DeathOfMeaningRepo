using System;

namespace TDOM.Gameplay.Combat
{
    public enum ColorEscudo
    {
        Ninguno,
        Azul,
        Rojo,
    }

    public static class EscudoOjo
    {
        public const int MaxCapas = 3;

        /// <summary>
        /// Guarda las capas de un ojo en un byte (2 bits por capa, la de afuera en los bits bajos).
        /// Así un ojo entero se sincroniza por red como un solo valor y romper la capa de afuera es
        /// desplazar los bits.
        /// </summary>
        public static byte Empaquetar(ColorEscudo[] capas)
        {
            if (capas == null)
                throw new ArgumentNullException(nameof(capas));
            if (capas.Length > MaxCapas)
                throw new ArgumentException($"Máximo {MaxCapas} capas.", nameof(capas));

            int empaquetado = 0;
            for (int i = 0; i < capas.Length; i++)
            {
                if (capas[i] == ColorEscudo.Ninguno)
                    throw new ArgumentException("Una capa no puede ser Ninguno.", nameof(capas));

                empaquetado |= (int)capas[i] << (2 * i);
            }
            return (byte)empaquetado;
        }

        public static ColorEscudo Capa(byte empaquetado, int capa) =>
            capa < 0 || capa >= MaxCapas
                ? ColorEscudo.Ninguno
                : (ColorEscudo)((empaquetado >> (2 * capa)) & 3);

        public static int NumCapas(byte empaquetado)
        {
            int n = 0;
            while (n < MaxCapas && Capa(empaquetado, n) != ColorEscudo.Ninguno)
                n++;
            return n;
        }

        /// <summary>La capa de afuera (la que hay que romper primero).</summary>
        public static ColorEscudo Exterior(byte empaquetado) => Capa(empaquetado, 0);

        public static byte SinExterior(byte empaquetado) => (byte)(empaquetado >> 2);

        /// <summary>El escudo azul lo rompe Zendre y el rojo, Ayla.</summary>
        public static bool Rompe(ColorEscudo escudo, bool atacanteEsAyla)
        {
            switch (escudo)
            {
                case ColorEscudo.Azul:
                    return !atacanteEsAyla;
                case ColorEscudo.Rojo:
                    return atacanteEsAyla;
                default:
                    return false;
            }
        }

        public static ColorEscudo Opuesto(ColorEscudo escudo)
        {
            switch (escudo)
            {
                case ColorEscudo.Azul:
                    return ColorEscudo.Rojo;
                case ColorEscudo.Rojo:
                    return ColorEscudo.Azul;
                default:
                    return ColorEscudo.Ninguno;
            }
        }
    }

    /// <summary>
    /// Qué escudos llevan los ojos en cada fase, de afuera hacia adentro (la primera capa es la
    /// que está al frente y la que hay que romper primero):
    /// fase 2: una capa del color sorteado; fase 3: una capa del otro color;
    /// fase 4: una capa, la mitad de los ojos azul y la otra mitad roja, repartidos al azar;
    /// fase 5: tres capas, la mitad de los ojos rojo-azul-rojo y la otra mitad azul-rojo-azul.
    /// Las demás fases no llevan escudo. El azar sale de la semilla, así que el mismo plan siempre
    /// reparte igual.
    /// </summary>
    public sealed class PlanEscudos
    {
        public const int FaseUnColor1 = 2;
        public const int FaseUnColor2 = 3;
        public const int FaseMezcla = 4;
        public const int FaseTresCapas = 5;

        private readonly ColorEscudo _primero;
        private readonly int _semilla;

        public PlanEscudos(int semilla)
        {
            _semilla = semilla;
            _primero = new Random(semilla).Next(2) == 0 ? ColorEscudo.Azul : ColorEscudo.Rojo;
        }

        public PlanEscudos(ColorEscudo primero, int semilla = 0)
        {
            if (primero == ColorEscudo.Ninguno)
                throw new ArgumentException("El primer escudo debe ser azul o rojo.", nameof(primero));

            _primero = primero;
            _semilla = semilla;
        }

        /// <summary>Capas del ojo en la fase, de afuera hacia adentro (vacío si no lleva escudo).</summary>
        public ColorEscudo[] Capas(int fase, int ojo, int totalOjos)
        {
            switch (fase)
            {
                case FaseUnColor1:
                    return new[] { _primero };
                case FaseUnColor2:
                    return new[] { EscudoOjo.Opuesto(_primero) };
                case FaseMezcla:
                    return EnPrimeraMitad(fase, ojo, totalOjos)
                        ? new[] { ColorEscudo.Azul }
                        : new[] { ColorEscudo.Rojo };
                case FaseTresCapas:
                    return EnPrimeraMitad(fase, ojo, totalOjos)
                        ? new[] { ColorEscudo.Rojo, ColorEscudo.Azul, ColorEscudo.Rojo }
                        : new[] { ColorEscudo.Azul, ColorEscudo.Rojo, ColorEscudo.Azul };
                default:
                    return new ColorEscudo[0];
            }
        }

        // Baraja los ojos (siempre igual para la misma semilla y fase) y la primera mitad forma el
        // grupo "A". Con un número impar de ojos el grupo A queda con uno menos.
        private bool EnPrimeraMitad(int fase, int ojo, int totalOjos)
        {
            if (ojo < 0 || ojo >= totalOjos)
                throw new ArgumentOutOfRangeException(nameof(ojo));

            var orden = new int[totalOjos];
            for (int i = 0; i < totalOjos; i++)
                orden[i] = i;

            var azar = new Random(unchecked(_semilla * 31 + fase * 7919));
            for (int i = totalOjos - 1; i > 0; i--)
            {
                int j = azar.Next(i + 1);
                int tmp = orden[i];
                orden[i] = orden[j];
                orden[j] = tmp;
            }

            return Array.IndexOf(orden, ojo) < totalOjos / 2;
        }
    }
}
