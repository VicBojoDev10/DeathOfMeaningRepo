using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    public enum EstadoOjo
    {
        Cerrado,
        Brillando,
        Abierto,
        Enfriando,
    }

    public enum ResultadoGolpe
    {
        Ignorado,
        Abrio,
        Rebote,
    }

    public sealed class WeakPointStateMachine
    {
        private const float Tolerancia = 1e-4f;

        private static readonly int[] OjosPorFase = { 2, 2, 2, 3, 3 };

        private readonly EstadoOjo[] _estados;
        private readonly float[] _timers;
        private readonly float _duracionAbierto;
        private readonly float _cooldown;
        private readonly Random _random;
        private readonly List<int> _candidatos = new List<int>();

        private int _ojosActivosObjetivo;

        public WeakPointStateMachine(
            int totalOjos = 12,
            float duracionAbierto = 10f,
            float cooldown = 5f,
            int semilla = 0
        )
        {
            if (totalOjos <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(totalOjos),
                    "Debe haber al menos 1 ojo."
                );
            if (duracionAbierto <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(duracionAbierto),
                    "Debe ser mayor que 0."
                );
            if (cooldown < 0f)
                throw new ArgumentOutOfRangeException(nameof(cooldown), "No puede ser negativo.");

            _estados = new EstadoOjo[totalOjos];
            _timers = new float[totalOjos];
            _duracionAbierto = duracionAbierto;
            _cooldown = cooldown;
            _random = new Random(semilla);
        }

        public int TotalOjos => _estados.Length;

        public int FaseActual { get; private set; }

        public EstadoOjo Estado(int ojo)
        {
            ValidarOjo(ojo);
            return _estados[ojo];
        }

        public void ActivarFase(int fase)
        {
            if (fase < 1 || fase > OjosPorFase.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(fase),
                    "La fase debe estar entre 1 y 5."
                );

            FaseActual = fase;
            _ojosActivosObjetivo = Math.Min(OjosPorFase[fase - 1], TotalOjos);

            ApagarBrillantesSobrantes();
            Rellenar();
        }

        public ResultadoGolpe GolpeMelee(int ojo)
        {
            ValidarOjo(ojo);

            switch (_estados[ojo])
            {
                case EstadoOjo.Brillando:
                    _estados[ojo] = EstadoOjo.Abierto;
                    _timers[ojo] = 0f;
                    return ResultadoGolpe.Abrio;
                case EstadoOjo.Abierto:
                    return ResultadoGolpe.Rebote;
                default:
                    return ResultadoGolpe.Ignorado;
            }
        }

        public bool Disparo(int ojo)
        {
            ValidarOjo(ojo);
            return _estados[ojo] == EstadoOjo.Abierto;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f)
                return;

            for (int ojo = 0; ojo < _estados.Length; ojo++)
                AvanzarOjo(ojo, dt);

            Rellenar();
        }

        private void AvanzarOjo(int ojo, float dt)
        {
            if (_estados[ojo] == EstadoOjo.Abierto)
            {
                _timers[ojo] += dt;
                if (_timers[ojo] < _duracionAbierto - Tolerancia)
                    return;

                dt = Math.Max(0f, _timers[ojo] - _duracionAbierto);
                _estados[ojo] = EstadoOjo.Enfriando;
                _timers[ojo] = 0f;
            }

            if (_estados[ojo] == EstadoOjo.Enfriando)
            {
                _timers[ojo] += dt;
                if (_timers[ojo] < _cooldown - Tolerancia)
                    return;

                _estados[ojo] = EstadoOjo.Cerrado;
                _timers[ojo] = 0f;
            }
        }

        private void Rellenar()
        {
            int activos = ContarActivos();

            while (activos < _ojosActivosObjetivo)
            {
                ReunirCandidatos(EstadoOjo.Cerrado);
                if (_candidatos.Count == 0)
                    return;

                int elegido = _candidatos[_random.Next(_candidatos.Count)];
                _estados[elegido] = EstadoOjo.Brillando;
                _timers[elegido] = 0f;
                activos++;
            }
        }

        private void ApagarBrillantesSobrantes()
        {
            int activos = ContarActivos();

            while (activos > _ojosActivosObjetivo)
            {
                ReunirCandidatos(EstadoOjo.Brillando);
                if (_candidatos.Count == 0)
                    return;

                int elegido = _candidatos[_random.Next(_candidatos.Count)];
                _estados[elegido] = EstadoOjo.Cerrado;
                _timers[elegido] = 0f;
                activos--;
            }
        }

        private int ContarActivos()
        {
            int activos = 0;
            foreach (var estado in _estados)
            {
                if (estado == EstadoOjo.Brillando || estado == EstadoOjo.Abierto)
                    activos++;
            }
            return activos;
        }

        private void ReunirCandidatos(EstadoOjo estado)
        {
            _candidatos.Clear();
            for (int ojo = 0; ojo < _estados.Length; ojo++)
            {
                if (_estados[ojo] == estado)
                    _candidatos.Add(ojo);
            }
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
