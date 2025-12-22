using UnityEngine;

namespace ProjectMayhem.Audio
{
    public enum AudioEvent
    {
        // Player
        PlayerShoot,
        PlayerDash,
        PlayerHit,
        PlayerDeath,
        PlayerSpawn,

        // Enemies
        EnemySpawn,
        EnemyShoot,
        EnemyHit,
        EnemyDeath,

        // World / FX
        ExplosionSmall,
        ExplosionBig,
        PickupSpawn,
        PickupCollect,
        PowerUpActivate,
        WaveStart,
        WaveComplete,
        GameOver,

        // UI
        UI_Click,
        UI_Hover,
        UI_PauseOpen,
        UI_PauseClose
    }
}
