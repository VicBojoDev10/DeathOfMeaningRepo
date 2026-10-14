using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    public enum MotivoObjetivo
    {
        Ninguno,
        Unico,
        Agro,
        MasCercano,
    }

    public readonly struct BossTargetCandidate
    {
        public ulong Id { get; }
        public float Distancia { get; }
        public float DanioReciente { get; }

        public BossTargetCandidate(ulong id, float distancia, float danioReciente)
        {
            Id = id;
            Distancia = distancia;
            DanioReciente = danioReciente;
        }
    }

    /// <summary>
    /// Decide a qué jugador ataca AREK, en POCO puro. Solo cuentan los jugadores dentro del radio
    /// de detección y del rango del ataque. Entre ellos: si uno ataca notablemente más (su daño
    /// reciente es al menos factorAgroNotable × el del siguiente), va contra ese; si no, contra
    /// el más cercano. Si nadie está en rango, no hay objetivo (más adelante: minions).
    /// </summary>
    public sealed class BossTargetSelector
    {
        private readonly List<BossTargetCandidate> _enRango = new List<BossTargetCandidate>();

        public float RadioDeteccion { get; set; }
        public float FactorAgroNotable { get; set; }

        public BossTargetSelector(float radioDeteccion, float factorAgroNotable)
        {
            RadioDeteccion = radioDeteccion;
            FactorAgroNotable = factorAgroNotable;
        }

        public MotivoObjetivo Elegir(
            IReadOnlyList<BossTargetCandidate> candidatos,
            float rangoAtaque,
            out ulong objetivoId
        )
        {
            objetivoId = 0;
            float alcance = rangoAtaque < RadioDeteccion ? rangoAtaque : RadioDeteccion;

            _enRango.Clear();
            foreach (var candidato in candidatos)
            {
                if (candidato.Distancia <= alcance)
                    _enRango.Add(candidato);
            }

            if (_enRango.Count == 0)
                return MotivoObjetivo.Ninguno;

            if (_enRango.Count == 1)
            {
                objetivoId = _enRango[0].Id;
                return MotivoObjetivo.Unico;
            }

            if (TryMayorAgro(out ulong agresor))
            {
                objetivoId = agresor;
                return MotivoObjetivo.Agro;
            }

            objetivoId = MasCercano();
            return MotivoObjetivo.MasCercano;
        }

        private bool TryMayorAgro(out ulong agresor)
        {
            agresor = 0;
            float mayor = -1f;
            float segundo = -1f;

            foreach (var candidato in _enRango)
            {
                if (candidato.DanioReciente > mayor)
                {
                    segundo = mayor;
                    mayor = candidato.DanioReciente;
                    agresor = candidato.Id;
                }
                else if (candidato.DanioReciente > segundo)
                {
                    segundo = candidato.DanioReciente;
                }
            }

            return mayor > 0f && mayor >= FactorAgroNotable * segundo;
        }

        private ulong MasCercano()
        {
            var mejor = _enRango[0];
            for (int i = 1; i < _enRango.Count; i++)
            {
                if (_enRango[i].Distancia < mejor.Distancia)
                    mejor = _enRango[i];
            }

            return mejor.Id;
        }
    }
}
