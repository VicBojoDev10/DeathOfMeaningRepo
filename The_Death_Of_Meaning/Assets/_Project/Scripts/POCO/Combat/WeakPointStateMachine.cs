using System;

namespace TDOM.Gameplay.Combat
{
    public enum EstadoOjo
    {
        Cerrado,
        Abierto,
    }

    public enum TipoImpactoOjo
    {
        Melee,
        Disparo,
    }

    /// <summary>
    /// Ojos del jefe. Todos los ojos arrancan abiertos y se pueden golpear; cada uno tiene su
    /// propia vida y, al llegar a 0, se cierra y ya no recibe daño. Los vuelve a abrir (con la
    /// vida llena) AbrirTodos, que se llama al empezar una fase nueva. Un ojo con escudo no recibe
    /// daño hasta que lo rompe el atacante del color que toca (ver EscudoOjo.Rompe).
    /// </summary>
    public sealed class WeakPointStateMachine
    {
        private readonly EstadoOjo[] _estados;
        private readonly float[] _vida;
        private readonly byte[] _escudos; // capas empaquetadas (ver EscudoOjo.Empaquetar)
        private readonly float _vidaMaximaOjo;

        public WeakPointStateMachine(int totalOjos = 8, float vidaOjo = 200f)
        {
            if (totalOjos <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(totalOjos),
                    "Debe haber al menos 1 ojo."
                );
            if (vidaOjo <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(vidaOjo),
                    "La vida de cada ojo debe ser mayor que 0."
                );

            _estados = new EstadoOjo[totalOjos];
            _vida = new float[totalOjos];
            _escudos = new byte[totalOjos];
            _vidaMaximaOjo = vidaOjo;
            AbrirTodos();
        }

        public int TotalOjos => _estados.Length;

        public float VidaMaximaOjo => _vidaMaximaOjo;

        public int OjosAbiertos
        {
            get
            {
                int abiertos = 0;
                foreach (var estado in _estados)
                {
                    if (estado == EstadoOjo.Abierto)
                        abiertos++;
                }
                return abiertos;
            }
        }

        public bool TodosCerrados => OjosAbiertos == 0;

        public EstadoOjo Estado(int ojo)
        {
            ValidarOjo(ojo);
            return _estados[ojo];
        }

        public float Vida(int ojo)
        {
            ValidarOjo(ojo);
            return _vida[ojo];
        }

        /// <summary>La capa de afuera del ojo, la que toca romper ahora (Ninguno si no tiene).</summary>
        public ColorEscudo Escudo(int ojo)
        {
            ValidarOjo(ojo);
            return EscudoOjo.Exterior(_escudos[ojo]);
        }

        /// <summary>Capas que le quedan al ojo.</summary>
        public int CapasEscudo(int ojo)
        {
            ValidarOjo(ojo);
            return EscudoOjo.NumCapas(_escudos[ojo]);
        }

        /// <summary>Todas las capas del ojo empaquetadas (para sincronizarlas por red).</summary>
        public byte EscudosEmpaquetados(int ojo)
        {
            ValidarOjo(ojo);
            return _escudos[ojo];
        }

        /// <summary>Pone el mismo escudo de una capa en todos los ojos abiertos (Ninguno los quita).</summary>
        public void PonerEscudos(ColorEscudo escudo)
        {
            var capas = escudo == ColorEscudo.Ninguno ? new ColorEscudo[0] : new[] { escudo };
            for (int ojo = 0; ojo < _escudos.Length; ojo++)
                PonerEscudos(ojo, capas);
        }

        /// <summary>
        /// Pone las capas de un ojo abierto, de afuera hacia adentro (vacío lo deja sin escudo).
        /// </summary>
        public void PonerEscudos(int ojo, ColorEscudo[] capas)
        {
            ValidarOjo(ojo);
            _escudos[ojo] =
                _estados[ojo] == EstadoOjo.Abierto ? EscudoOjo.Empaquetar(capas) : (byte)0;
        }

        /// <summary>
        /// Rompe la capa de afuera del ojo si el atacante es del color que toca. Devuelve true si
        /// la rompió; si había más capas, queda la siguiente.
        /// </summary>
        public bool RomperEscudo(int ojo, bool atacanteEsAyla)
        {
            ValidarOjo(ojo);

            if (
                _estados[ojo] != EstadoOjo.Abierto
                || !EscudoOjo.Rompe(EscudoOjo.Exterior(_escudos[ojo]), atacanteEsAyla)
            )
                return false;

            _escudos[ojo] = EscudoOjo.SinExterior(_escudos[ojo]);
            return true;
        }

        /// <summary>Abre todos los ojos con la vida llena (inicio de cada fase).</summary>
        public void AbrirTodos()
        {
            for (int ojo = 0; ojo < _estados.Length; ojo++)
                Abrir(ojo);
        }

        public void AbrirOjoParaDebug(int ojo)
        {
            ValidarOjo(ojo);
            Abrir(ojo);
        }

        /// <summary>
        /// Resta vida al ojo, sin pasarse de la que le queda, y lo cierra si llega a 0.
        /// Devuelve el daño que realmente absorbió (0 si el ojo ya estaba cerrado).
        /// </summary>
        public float AplicarDanio(int ojo, float cantidad)
        {
            ValidarOjo(ojo);

            if (_estados[ojo] != EstadoOjo.Abierto || _escudos[ojo] != 0 || cantidad <= 0f)
                return 0f;

            float aplicado = Math.Min(cantidad, _vida[ojo]);
            _vida[ojo] -= aplicado;

            if (_vida[ojo] <= 0f)
            {
                _vida[ojo] = 0f;
                _estados[ojo] = EstadoOjo.Cerrado;
            }

            return aplicado;
        }

        public static bool AdmiteAncla(EstadoOjo estado) => estado == EstadoOjo.Abierto;

        public bool AdmiteImpacto(int ojo, TipoImpactoOjo tipo, bool atacanteEsAyla)
        {
            if (tipo == TipoImpactoOjo.Disparo && atacanteEsAyla)
                return false;

            return AdmiteImpacto(ojo, tipo);
        }

        public bool AdmiteImpacto(int ojo, TipoImpactoOjo tipo)
        {
            ValidarOjo(ojo);
            return _estados[ojo] == EstadoOjo.Abierto && _escudos[ojo] == 0;
        }

        private void Abrir(int ojo)
        {
            _escudos[ojo] = 0;
            _estados[ojo] = EstadoOjo.Abierto;
            _vida[ojo] = _vidaMaximaOjo;
        }

        private void ValidarOjo(int ojo)
        {
            if (ojo < 0 || ojo >= _estados.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(ojo),
                    $"El ojo debe estar entre 0 y {_estados.Length - 1}."
                );
        }
    }
}
