using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    public enum TipoContactoAncla
    {
        Ignorar,
        Superficie,
        OjoValido,
        OjoEquivocado,
    }

    public enum AccionAncla
    {
        Seguir,
        Anclar,
        Retraer,
    }

    public readonly struct ContactoAncla
    {
        public readonly TipoContactoAncla Tipo;
        public readonly float Distancia;

        public ContactoAncla(TipoContactoAncla tipo, float distancia)
        {
            Tipo = tipo;
            Distancia = distancia;
        }

        public bool Inicial => Distancia <= 0f;
    }

    public readonly struct DecisionAncla
    {
        public readonly AccionAncla Accion;
        public readonly int Indice;
        public readonly bool MoverAlPunto;

        public DecisionAncla(AccionAncla accion, int indice, bool moverAlPunto)
        {
            Accion = accion;
            Indice = indice;
            MoverAlPunto = moverAlPunto;
        }

        public static DecisionAncla Seguir => new DecisionAncla(AccionAncla.Seguir, -1, false);
        public static DecisionAncla Retraer => new DecisionAncla(AccionAncla.Retraer, -1, false);
    }

    public static class AnchorContactResolver
    {
        public static DecisionAncla Resolver(IReadOnlyList<ContactoAncla> contactos)
        {
            int masCercano = -1;
            int inicial = -1;
            bool tocandoOjoEquivocado = false;

            for (int i = 0; i < contactos.Count; i++)
            {
                var contacto = contactos[i];
                if (contacto.Tipo == TipoContactoAncla.Ignorar)
                    continue;

                if (contacto.Inicial)
                {
                    RegistrarInicial(contactos, i, ref inicial, ref tocandoOjoEquivocado);
                    continue;
                }

                if (masCercano < 0 || contacto.Distancia < contactos[masCercano].Distancia)
                    masCercano = i;
            }

            if (masCercano >= 0)
                return DecidirImpacto(contactos[masCercano].Tipo, masCercano);

            if (inicial >= 0)
                return new DecisionAncla(AccionAncla.Anclar, inicial, false);

            return tocandoOjoEquivocado ? DecisionAncla.Retraer : DecisionAncla.Seguir;
        }

        private static void RegistrarInicial(
            IReadOnlyList<ContactoAncla> contactos,
            int indice,
            ref int inicial,
            ref bool tocandoOjoEquivocado
        )
        {
            var tipo = contactos[indice].Tipo;
            if (tipo == TipoContactoAncla.OjoEquivocado)
            {
                tocandoOjoEquivocado = true;
                return;
            }

            if (inicial < 0 || tipo == TipoContactoAncla.OjoValido)
                inicial = indice;
        }

        private static DecisionAncla DecidirImpacto(TipoContactoAncla tipo, int indice) =>
            tipo == TipoContactoAncla.OjoEquivocado
                ? DecisionAncla.Retraer
                : new DecisionAncla(AccionAncla.Anclar, indice, true);
    }
}
