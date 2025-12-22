# Project Mayhem - Audio System Setup

This document explains how to set up and use the new Audio System for SFX.

## Components

- AudioEvent (enum): Canonical list of sound events.
- AudioLibrary (ScriptableObject): Maps AudioEvent -> Clips + settings.
- AudioManager (MonoBehaviour): Central SFX player with AudioSource pooling and pause integration.
- SFX (static): Convenience wrapper methods.
- UISFX (MonoBehaviour): Drop-in UI sounds for hover/click.

## Create the Audio Library

1) In Project window: Create > Project Mayhem > Audio > Audio Library
2) Assign per-event settings:
   - Clips: One or more AudioClips to randomize.
   - Volume / Volume Variance
   - Pitch / Pitch Variance
   - Spatial Blend (0 = 2D, 1 = 3D), Min/Max Distance, Rolloff
   - Mixer Group (optional)
   - Loop (for continuous SFX; rarely needed for SFX)

Recommended initial mappings:
- PlayerShoot
- PlayerHit
- PlayerDeath
- PlayerSpawn
- EnemySpawn
- EnemyShoot
- EnemyHit
- EnemyDeath
- ExplosionSmall
- ExplosionBig (optional, for bigger effects)
- PickupSpawn
- PickupCollect
- PowerUpActivate
- WaveStart
- WaveComplete
- GameOver
- UI_Click
- UI_Hover
- UI_PauseOpen
- UI_PauseClose

## Add the AudioManager to the Scene

- Create an empty GameObject: "AudioManager"
- Add the AudioManager component
- Assign the AudioLibrary ScriptableObject
- Configure:
  - Prewarm Sources: 10
  - Max Sources: 32
  - Steal Oldest When Pool Exhausted: true

AudioManager is DontDestroyOnLoad and will persist across scenes.

## UI Sounds

- On any Selectable (e.g., Button), add UISFX component.
- Configure Click/Hover events if you want to override defaults.

## Using SFX in Code

- Play 2D events:
  SFX.Play(AudioEvent.PlayerShoot);

- Play 3D events at a world position:
  SFX.Play(AudioEvent.EnemyDeath, transform.position);

- Play a loop following a target (e.g., engine hum):
  var id = SFX.PlayLoop(AudioEvent.SomeLoop, transform);
  SFX.StopLoop(id);

## GameState Integration

Already wired:
- Pause: All audio sources Pause
- Resume Gameplay: UnPause
- Pause Open/Close UI sounds
- Game Over: Plays GameOver

## What’s already integrated

- Player
  - Spawn, Shoot, Hit, Death
  - PowerUpActivate on RapidFire and Shield (via Health.ActivateShield)
- Enemies
  - Spawn, Shoot (ShooterEnemy), Hit, Death
- Projectiles
  - Impact (ExplosionSmall)
- PowerUps
  - Spawn, Collect
- Waves
  - Start, Complete

## Best practices

- Route SFX to a Unity AudioMixer for global control (SFX bus)
- Keep clips short and normalized (e.g., -12 dBFS)
- Use pitch/volume variance for natural feel
- Prefer 2D SFX unless positional info is helpful
- Consider a rate limiter for extremely spammy SFX (future enhancement)

## Troubleshooting

- No sound: Ensure AudioLibrary assigned to AudioManager, entries have clips
- Too many sounds: Reduce MaxSources or enable Steal Oldest
- No pause behavior: Ensure GameStateManager drives pause states (already integrated)
