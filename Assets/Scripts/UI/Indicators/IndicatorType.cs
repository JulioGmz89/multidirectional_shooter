namespace ProjectMayhem.UI.Indicators
{
    /// <summary>
    /// Defines the types of off-screen indicators available in the game.
    /// Each type can have different visual settings (color, shape, size) defined in IndicatorConfig.
    /// </summary>
    public enum IndicatorType
    {
        /// <summary>
        /// Chaser enemy indicator - typically red arrow.
        /// </summary>
        ChaserEnemy = 0,

        /// <summary>
        /// Shooter enemy indicator - typically orange arrow.
        /// </summary>
        ShooterEnemy = 1,

        /// <summary>
        /// Rapid fire power-up indicator - typically yellow diamond.
        /// </summary>
        RapidFire = 10,

        /// <summary>
        /// Shield power-up indicator - typically cyan diamond.
        /// </summary>
        Shield = 11,

        /// <summary>
        /// Boss enemy indicator - typically large red skull.
        /// </summary>
        Boss = 100,

        /// <summary>
        /// Objective marker - typically green.
        /// </summary>
        Objective = 200
    }
}
