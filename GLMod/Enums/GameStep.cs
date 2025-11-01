namespace GLMod.Enums
{
    /// <summary>
    /// Represents the different steps in the game flow
    /// </summary>
    public enum GameStep
    {
        /// <summary>
        /// Initial state - game not started
        /// </summary>
        Initial = 0,

        /// <summary>
        /// Players have been added to the game
        /// </summary>
        PlayersAdded = 1,

        /// <summary>
        /// Game data sent to API
        /// </summary>
        GameSent = 2,

        /// <summary>
        /// Game ID synced via RPC
        /// </summary>
        GameIdSynced = 3,

        /// <summary>
        /// Players recorded in the game
        /// </summary>
        PlayersRecorded = 4,

        /// <summary>
        /// Winner team has been set
        /// </summary>
        WinnerSet = 5
    }
}
