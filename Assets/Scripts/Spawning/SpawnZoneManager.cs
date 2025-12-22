using System.Collections.Generic;
using UnityEngine;

namespace ProjectMayhem.Spawning
{
    /// <summary>
    /// Manages all spawn zones in the scene and provides spawn points on request.
    /// Singleton pattern for easy access from other systems.
    /// </summary>
    public class SpawnZoneManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static SpawnZoneManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("If true, automatically finds all SpawnZone components in the scene on Awake.")]
        [SerializeField] private bool autoDiscoverZones = true;

        [Tooltip("Manually assigned spawn zones (used if autoDiscoverZones is false or to add specific zones).")]
        [SerializeField] private List<SpawnZone> manualZones = new List<SpawnZone>();

        [Header("Fallback Settings")]
        [Tooltip("If no valid spawn point is found, use a random point within this radius from world origin.")]
        [SerializeField] private float fallbackRadius = 15f;

        [Tooltip("Maximum attempts to find a valid spawn point before using fallback.")]
        [SerializeField] private int maxSpawnAttempts = 20;

        // Cached zone lists by type
        private List<SpawnZone> allZones = new List<SpawnZone>();
        private List<SpawnZone> enemyZones = new List<SpawnZone>();
        private List<SpawnZone> powerUpZones = new List<SpawnZone>();

        // Cached weight totals for weighted random selection
        private float enemyZoneTotalWeight;
        private float powerUpZoneTotalWeight;

        /// <summary>
        /// Gets all registered spawn zones.
        /// </summary>
        public IReadOnlyList<SpawnZone> AllZones => allZones;

        /// <summary>
        /// Gets all zones that can spawn enemies.
        /// </summary>
        public IReadOnlyList<SpawnZone> EnemyZones => enemyZones;

        /// <summary>
        /// Gets all zones that can spawn power-ups.
        /// </summary>
        public IReadOnlyList<SpawnZone> PowerUpZones => powerUpZones;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple SpawnZoneManagers detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeZones();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Initializes and categorizes all spawn zones.
        /// </summary>
        private void InitializeZones()
        {
            allZones.Clear();
            enemyZones.Clear();
            powerUpZones.Clear();

            // Auto-discover zones if enabled
            if (autoDiscoverZones)
            {
                SpawnZone[] foundZones = FindObjectsByType<SpawnZone>(FindObjectsSortMode.None);
                allZones.AddRange(foundZones);
            }

            // Add manual zones
            foreach (var zone in manualZones)
            {
                if (zone != null && !allZones.Contains(zone))
                {
                    allZones.Add(zone);
                }
            }

            // Categorize zones by type
            foreach (var zone in allZones)
            {
                if (zone.CanSpawn(SpawnZone.ZoneType.Enemy))
                {
                    enemyZones.Add(zone);
                }
                if (zone.CanSpawn(SpawnZone.ZoneType.PowerUp))
                {
                    powerUpZones.Add(zone);
                }
            }

            // Calculate total weights
            RecalculateWeights();

            Debug.Log($"SpawnZoneManager initialized with {allZones.Count} zones " +
                      $"({enemyZones.Count} enemy, {powerUpZones.Count} power-up).");
        }

        /// <summary>
        /// Recalculates the total weights for weighted random selection.
        /// </summary>
        private void RecalculateWeights()
        {
            enemyZoneTotalWeight = 0f;
            foreach (var zone in enemyZones)
            {
                enemyZoneTotalWeight += zone.Weight;
            }

            powerUpZoneTotalWeight = 0f;
            foreach (var zone in powerUpZones)
            {
                powerUpZoneTotalWeight += zone.Weight;
            }
        }

        /// <summary>
        /// Registers a new spawn zone at runtime.
        /// </summary>
        /// <param name="zone">The zone to register.</param>
        public void RegisterZone(SpawnZone zone)
        {
            if (zone == null || allZones.Contains(zone)) return;

            allZones.Add(zone);

            if (zone.CanSpawn(SpawnZone.ZoneType.Enemy))
            {
                enemyZones.Add(zone);
            }
            if (zone.CanSpawn(SpawnZone.ZoneType.PowerUp))
            {
                powerUpZones.Add(zone);
            }

            RecalculateWeights();
        }

        /// <summary>
        /// Unregisters a spawn zone at runtime.
        /// </summary>
        /// <param name="zone">The zone to unregister.</param>
        public void UnregisterZone(SpawnZone zone)
        {
            if (zone == null) return;

            allZones.Remove(zone);
            enemyZones.Remove(zone);
            powerUpZones.Remove(zone);

            RecalculateWeights();
        }

        /// <summary>
        /// Gets a valid spawn point for an enemy.
        /// </summary>
        /// <returns>A world-space position for spawning.</returns>
        public Vector2 GetEnemySpawnPoint()
        {
            return GetSpawnPoint(enemyZones, enemyZoneTotalWeight);
        }

        /// <summary>
        /// Gets a valid spawn point for a power-up.
        /// </summary>
        /// <returns>A world-space position for spawning.</returns>
        public Vector2 GetPowerUpSpawnPoint()
        {
            return GetSpawnPoint(powerUpZones, powerUpZoneTotalWeight);
        }

        /// <summary>
        /// Gets a spawn point from the specified zone list using weighted random selection.
        /// </summary>
        private Vector2 GetSpawnPoint(List<SpawnZone> zones, float totalWeight)
        {
            if (zones.Count == 0)
            {
                Debug.LogWarning("No spawn zones available. Using fallback position.");
                return GetFallbackSpawnPoint();
            }

            // Try multiple times to find a valid point
            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                SpawnZone selectedZone = SelectWeightedRandomZone(zones, totalWeight);
                if (selectedZone == null) continue;

                Vector2? point = selectedZone.GetValidSpawnPoint();
                if (point.HasValue)
                {
                    return point.Value;
                }
            }

            // Fallback: just get any point from a random zone
            Debug.LogWarning("Could not find valid spawn point after max attempts. Using zone point without validation.");
            SpawnZone fallbackZone = zones[Random.Range(0, zones.Count)];
            return fallbackZone.GetRandomPointInZone();
        }

        /// <summary>
        /// Selects a random zone using weighted probability.
        /// </summary>
        private SpawnZone SelectWeightedRandomZone(List<SpawnZone> zones, float totalWeight)
        {
            if (zones.Count == 0) return null;
            if (zones.Count == 1) return zones[0];

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var zone in zones)
            {
                currentWeight += zone.Weight;
                if (randomValue <= currentWeight)
                {
                    return zone;
                }
            }

            // Fallback to last zone (shouldn't happen normally)
            return zones[zones.Count - 1];
        }

        /// <summary>
        /// Gets multiple spawn points with minimum spacing between them.
        /// </summary>
        /// <param name="count">Number of spawn points to generate.</param>
        /// <param name="minSpacing">Minimum distance between spawn points.</param>
        /// <param name="forEnemies">True for enemy spawn points, false for power-ups.</param>
        /// <returns>Array of spawn points.</returns>
        public Vector2[] GetSpawnPoints(int count, float minSpacing, bool forEnemies = true)
        {
            List<Vector2> points = new List<Vector2>();
            List<SpawnZone> zones = forEnemies ? enemyZones : powerUpZones;
            float totalWeight = forEnemies ? enemyZoneTotalWeight : powerUpZoneTotalWeight;

            int maxTotalAttempts = count * maxSpawnAttempts;
            int totalAttempts = 0;

            while (points.Count < count && totalAttempts < maxTotalAttempts)
            {
                totalAttempts++;

                Vector2 candidate = GetSpawnPoint(zones, totalWeight);

                // Check spacing against existing points
                bool validSpacing = true;
                foreach (var existingPoint in points)
                {
                    if (Vector2.Distance(candidate, existingPoint) < minSpacing)
                    {
                        validSpacing = false;
                        break;
                    }
                }

                if (validSpacing)
                {
                    points.Add(candidate);
                }
            }

            // If we couldn't get enough points with spacing, fill the rest without spacing check
            while (points.Count < count)
            {
                points.Add(GetSpawnPoint(zones, totalWeight));
            }

            return points.ToArray();
        }

        /// <summary>
        /// Gets a spawn point near a specific location.
        /// </summary>
        /// <param name="center">The center point to spawn near.</param>
        /// <param name="maxDistance">Maximum distance from the center.</param>
        /// <param name="forEnemies">True for enemy spawn points, false for power-ups.</param>
        /// <returns>A spawn point near the center, or the center if no valid zone found.</returns>
        public Vector2 GetSpawnPointNear(Vector2 center, float maxDistance, bool forEnemies = true)
        {
            List<SpawnZone> zones = forEnemies ? enemyZones : powerUpZones;
            List<SpawnZone> nearbyZones = new List<SpawnZone>();

            // Find zones within range
            foreach (var zone in zones)
            {
                float distanceToZone = Vector2.Distance(center, zone.transform.position);
                float zoneRadius = zone.Shape == SpawnZone.ZoneShape.Circle ? zone.Radius : 
                                   Mathf.Max(zone.Size.x, zone.Size.y) / 2f;
                
                if (distanceToZone - zoneRadius <= maxDistance)
                {
                    nearbyZones.Add(zone);
                }
            }

            if (nearbyZones.Count == 0)
            {
                // No nearby zones, return point near center
                Vector2 offset = Random.insideUnitCircle * maxDistance;
                return center + offset;
            }

            // Get spawn point from nearby zone
            SpawnZone selectedZone = nearbyZones[Random.Range(0, nearbyZones.Count)];
            Vector2? point = selectedZone.GetValidSpawnPoint();
            
            return point ?? center;
        }

        /// <summary>
        /// Gets a fallback spawn point when no zones are available.
        /// </summary>
        private Vector2 GetFallbackSpawnPoint()
        {
            return Random.insideUnitCircle * fallbackRadius;
        }

        /// <summary>
        /// Refreshes the zone lists. Call this if zones are added/removed at runtime.
        /// </summary>
        public void RefreshZones()
        {
            InitializeZones();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw fallback radius
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            DrawCircleGizmo(Vector3.zero, fallbackRadius);
        }

        private void DrawCircleGizmo(Vector3 center, float radius)
        {
            const int segments = 32;
            float angleStep = 360f / segments;
            
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
#endif
    }
}
