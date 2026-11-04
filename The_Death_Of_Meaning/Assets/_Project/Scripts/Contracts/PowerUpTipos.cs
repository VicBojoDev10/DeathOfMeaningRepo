namespace TDOM.Contracts
{
    /// <summary>Si el power-up ayuda a los jugadores o los afecta (hoy solo define el color).</summary>
    public enum NaturalezaPowerUp
    {
        Potenciador,
        Afectacion,
    }

    /// <summary>A quién se aplica el power-up cuando alguien lo toca.</summary>
    public enum ModoAlcancePowerUp
    {
        QuienLoToca,
        AmbosJugadores,
    }

    /// <summary>
    /// Qué hace el power-up. Los siguientes tickets agregan casos (velocidad, daño, etc.) aquí
    /// y en el switch de PlayerRoot.AplicarPowerUp.
    /// </summary>
    public enum TipoEfectoPowerUp
    {
        Curar,
    }
}
