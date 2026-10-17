using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    public sealed class BossAggroTracker
    {
        private readonly struct Golpe
        {
            public ulong JugadorId { get; }
            public float Danio { get; }
            public float Tiempo { get; }

            public Golpe(ulong jugadorId, float danio, float tiempo)
            {
                JugadorId = jugadorId;
                Danio = danio;
                Tiempo = tiempo;
            }
        }

        private readonly List<Golpe> _golpes = new List<Golpe>();

        public float Ventana { get; set; }

        public BossAggroTracker(float ventana)
        {
            Ventana = ventana;
        }

        public void Registrar(ulong jugadorId, float danio, float tiempo)
        {
            if (danio <= 0f)
                return;

            _golpes.Add(new Golpe(jugadorId, danio, tiempo));
        }

        public float DanioReciente(ulong jugadorId, float ahora)
        {
            Purgar(ahora);

            float total = 0f;
            foreach (var golpe in _golpes)
            {
                if (golpe.JugadorId == jugadorId)
                    total += golpe.Danio;
            }

            return total;
        }

        public void Limpiar()
        {
            _golpes.Clear();
        }
        private void Purgar(float ahora)
        {
            float limite = ahora - Ventana;
            _golpes.RemoveAll(golpe => golpe.Tiempo < limite);
        }
    }
}
