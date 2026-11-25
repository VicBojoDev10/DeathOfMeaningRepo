namespace TDOM.Gameplay.Audio
{
    public static class SonidoBoton
    {
        public static T Elegir<T>(
            TipoSonidoBoton tipo,
            T positivo,
            T negativo,
            T neutral,
            T especial
        )
            where T : class
        {
            switch (tipo)
            {
                case TipoSonidoBoton.Positivo:
                    return positivo;
                case TipoSonidoBoton.Negativo:
                    return negativo;
                case TipoSonidoBoton.Especial:
                    return especial;
                case TipoSonidoBoton.Neutral:
                    return neutral;
                default:
                    return null;
            }
        }
    }
}
