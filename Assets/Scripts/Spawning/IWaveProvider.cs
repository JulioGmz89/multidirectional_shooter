namespace ProjectMayhem.Spawning
{
    /// <summary>
    /// Interface for wave providers. Implementations can provide waves from
    /// ScriptableObjects (campaign mode) or generate them procedurally (infinite mode).
    /// </summary>
    public interface IWaveProvider
    {
        /// <summary>
        /// Gets the current wave index (0-based).
        /// </summary>
        int CurrentWaveIndex { get; }

        /// <summary>
        /// Gets the total number of waves available.
        /// Returns -1 for infinite mode (unlimited waves).
        /// </summary>
        int TotalWaves { get; }

        /// <summary>
        /// Gets the next wave data and advances the internal counter.
        /// </summary>
        /// <returns>The RuntimeWaveData for the next wave, or null if no more waves.</returns>
        RuntimeWaveData GetNextWave();

        /// <summary>
        /// Peeks at the next wave without advancing the counter.
        /// </summary>
        /// <returns>The RuntimeWaveData for the next wave, or null if no more waves.</returns>
        RuntimeWaveData PeekNextWave();

        /// <summary>
        /// Checks if there are more waves available.
        /// </summary>
        /// <returns>True if more waves are available, false otherwise.</returns>
        bool HasMoreWaves();

        /// <summary>
        /// Resets the provider to the beginning (wave 0).
        /// </summary>
        void Reset();

        /// <summary>
        /// Gets whether this provider has a finite number of waves.
        /// </summary>
        bool IsFinite { get; }
    }
}
