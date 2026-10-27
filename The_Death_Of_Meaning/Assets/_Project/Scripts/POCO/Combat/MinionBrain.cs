using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    public enum EstadoMinion
    {
        Perseguir,
        Atacar,
        Recuperar,
    }

    public enum EventoMinion
    {
        Ninguno,
        Aviso,
        Golpe,
    }

    /// <summary>
    /// Un jugador como posible objetivo del minion. Vivo es false si está abatido o muerto.
    /// </summary>
    public readonly struct MinionCandidato
    {
        public ulong Id { get; }
        public float Distancia { get; }
        public bool Vivo { get; }

        public MinionCandidato(ulong id, float distancia, bool vivo)
        {
            Id = id;
            Distancia = distancia;
            Vivo = vivo;
        }
    }

    /// <summary>
    /// IA de un minion de AREK, en POCO puro (sin UnityEngine ni red). Corre solo en el servidor.
    /// Ciclo: Perseguir hasta tener al objetivo en rango → Atacar (combo de golpesPorCombo
    /// golpes, cada uno con Aviso y luego Golpe) → Recuperar → Perseguir.
    /// Una vez empezado, el combo se termina aunque el objetivo se aleje.
    /// </summary>
    public sealed class MinionBrain
    {
        // Mismo margen que BossAttackScheduler para comparar tiempos acumulados con float.
        private const float Tolerancia = 1e-4f;

        private readonly float _rangoAtaque;
        private readonly float _duracionAviso;
        private readonly float _pausaEntreGolpes;
        private readonly float _duracionRecuperar;
        private readonly int _golpesPorCombo;

        private float _tiempo;
        private bool _enAviso;

        public MinionBrain(
            float rangoAtaque,
            float duracionAviso,
            float pausaEntreGolpes,
            float duracionRecuperar,
            int golpesPorCombo = 2
        )
        {
            if (rangoAtaque <= 0f)
                throw new ArgumentOutOfRangeException(nameof(rangoAtaque), "Debe ser mayor que 0.");
            if (duracionAviso <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(duracionAviso),
                    "Debe ser mayor que 0."
                );
            if (pausaEntreGolpes < 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(pausaEntreGolpes),
                    "No puede ser negativo."
                );
            if (duracionRecuperar < 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(duracionRecuperar),
                    "No puede ser negativo."
                );
            if (golpesPorCombo < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(golpesPorCombo),
                    "Debe ser al menos 1."
                );

            _rangoAtaque = rangoAtaque;
            _duracionAviso = duracionAviso;
            _pausaEntreGolpes = pausaEntreGolpes;
            _duracionRecuperar = duracionRecuperar;
            _golpesPorCombo = golpesPorCombo;
        }

        public EstadoMinion Estado { get; private set; } = EstadoMinion.Perseguir;

        /// <summary>Golpes ya dados en el combo actual.</summary>
        public int GolpesDados { get; private set; }

        /// <summary>True solo en Perseguir: el controlador mueve al minion hacia el objetivo.</summary>
        public bool DebeMoverse => Estado == EstadoMinion.Perseguir;

        /// <summary>
        /// Avanza la IA y devuelve a lo mucho un evento por llamada. distanciaAlObjetivo solo
        /// se usa si hayObjetivo es true.
        /// </summary>
        public EventoMinion Tick(float dt, bool hayObjetivo, float distanciaAlObjetivo)
        {
            if (dt > 0f)
                _tiempo += dt;

            switch (Estado)
            {
                case EstadoMinion.Perseguir:
                    if (!hayObjetivo || distanciaAlObjetivo > _rangoAtaque)
                        return EventoMinion.Ninguno;

                    Estado = EstadoMinion.Atacar;
                    GolpesDados = 0;
                    return EmpezarAviso();

                case EstadoMinion.Atacar:
                    return TickAtacar();

                case EstadoMinion.Recuperar:
                    if (_tiempo < _duracionRecuperar - Tolerancia)
                        return EventoMinion.Ninguno;

                    _tiempo = 0f;
                    Estado = EstadoMinion.Perseguir;
                    return EventoMinion.Ninguno;

                default:
                    return EventoMinion.Ninguno;
            }
        }

        private EventoMinion TickAtacar()
        {
            if (!_enAviso)
            {
                if (_tiempo < _pausaEntreGolpes - Tolerancia)
                    return EventoMinion.Ninguno;

                return EmpezarAviso();
            }

            if (_tiempo < _duracionAviso - Tolerancia)
                return EventoMinion.Ninguno;

            _tiempo = Math.Max(0f, _tiempo - _duracionAviso);
            _enAviso = false;
            GolpesDados++;

            if (GolpesDados >= _golpesPorCombo)
            {
                _tiempo = 0f;
                Estado = EstadoMinion.Recuperar;
            }

            return EventoMinion.Golpe;
        }

        private EventoMinion EmpezarAviso()
        {
            _tiempo = 0f;
            _enAviso = true;
            return EventoMinion.Aviso;
        }

        /// <summary>
        /// El jugador más cercano entre los candidatos (solo se usa Id y Distancia).
        /// False si no hay candidatos.
        /// </summary>
        public static bool TryElegirMasCercano(
            IReadOnlyList<BossTargetCandidate> candidatos,
            out ulong objetivoId
        )
        {
            objetivoId = 0;
            if (candidatos == null || candidatos.Count == 0)
                return false;

            float mejor = float.MaxValue;
            foreach (var candidato in candidatos)
            {
                if (candidato.Distancia >= mejor)
                    continue;

                mejor = candidato.Distancia;
                objetivoId = candidato.Id;
            }

            return true;
        }

        /// <summary>
        /// El jugador vivo más cercano; los abatidos y muertos no cuentan, aunque estén más cerca.
        /// False si no hay candidatos o ninguno está vivo (el minion se queda quieto).
        /// </summary>
        public static bool TryElegirObjetivoVivo(
            IReadOnlyList<MinionCandidato> candidatos,
            out ulong objetivoId
        )
        {
            objetivoId = 0;
            if (candidatos == null)
                return false;

            bool hayObjetivo = false;
            float mejor = float.MaxValue;
            foreach (var candidato in candidatos)
            {
                if (!candidato.Vivo || candidato.Distancia >= mejor)
                    continue;

                mejor = candidato.Distancia;
                objetivoId = candidato.Id;
                hayObjetivo = true;
            }

            return hayObjetivo;
        }
    }
}
