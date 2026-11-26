using System;

namespace TDOM.Gameplay.Core
{
    public enum EtapaPresentacionArena
    {
        Video,
        Timeline,
        Regreso,
    }

    public readonly struct LineaPresentacionArena
    {
        private readonly double _duracionVideo;
        private readonly double _momentoRevelado;
        private readonly double _inicioRegreso;

        public LineaPresentacionArena(
            double duracionVideo,
            double duracionTimeline,
            double momentoRevelado,
            double duracionRegreso
        )
        {
            _duracionVideo = Math.Max(0d, duracionVideo);
            double timeline = Math.Max(0d, duracionTimeline);
            _momentoRevelado = Math.Min(Math.Max(0d, momentoRevelado), timeline);
            _inicioRegreso = Math.Max(0d, timeline - Math.Max(0d, duracionRegreso));
        }

        public double DuracionVideo => _duracionVideo;

        public double TiempoTimeline(double transcurrido) => transcurrido - _duracionVideo;

        public EtapaPresentacionArena Etapa(double transcurrido)
        {
            double tiempoTimeline = TiempoTimeline(transcurrido);
            if (tiempoTimeline < 0d)
                return EtapaPresentacionArena.Video;
            return tiempoTimeline >= _inicioRegreso
                ? EtapaPresentacionArena.Regreso
                : EtapaPresentacionArena.Timeline;
        }

        public bool DebeRevelarJefe(double transcurrido) =>
            TiempoTimeline(transcurrido) >= _momentoRevelado;
    }
}
