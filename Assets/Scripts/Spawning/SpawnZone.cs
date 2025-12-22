using UnityEngine;

namespace ProjectMayhem.Spawning
{
    /// <summary>
    /// Defines an area where enemies or power-ups can spawn.
    /// Attach this component to empty GameObjects to create spawn zones.
    /// </summary>
    public class SpawnZone : MonoBehaviour
    {
        /// <summary>
        /// The shape of the spawn zone.
        /// </summary>
        public enum ZoneShape
        {
            Rectangle,
            Circle
        }

        /// <summary>
        /// What type of objects can spawn in this zone.
        /// </summary>
        public enum ZoneType
        {
            Enemy,
            PowerUp,
            Both
        }

        [Header("Zone Configuration")]
        [Tooltip("The shape of this spawn zone.")]
        [SerializeField] private ZoneShape shape = ZoneShape.Rectangle;

        [Tooltip("What type of objects can spawn in this zone.")]
        [SerializeField] private ZoneType zoneType = ZoneType.Enemy;

        [Tooltip("Size of the zone (width, height) for Rectangle shape.")]
        [SerializeField] private Vector2 size = new Vector2(5f, 5f);

        [Tooltip("Radius of the zone for Circle shape.")]
        [SerializeField] private float radius = 3f;

        [Header("Spawn Rules")]
        [Tooltip("Minimum distance from the player for a spawn point to be valid. Set to 0 to disable.")]
        [SerializeField] private float minDistanceFromPlayer = 3f;

        [Tooltip("Maximum distance from the player for a spawn point to be valid. Set to 0 to disable.")]
        [SerializeField] private float maxDistanceFromPlayer = 0f;

        [Tooltip("If true, spawn points must be outside the camera's view.")]
        [SerializeField] private bool mustBeOffScreen = true;

        [Tooltip("Selection weight for this zone. Higher values make this zone more likely to be selected.")]
        [SerializeField] private float weight = 1f;

        [Header("Gizmo Settings")]
        [Tooltip("Color used to display this zone in the editor.")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);

        // Cached references
        private Camera mainCamera;
        private Transform playerTransform;

        /// <summary>
        /// Gets the shape of this spawn zone.
        /// </summary>
        public ZoneShape Shape => shape;

        /// <summary>
        /// Gets the type of this spawn zone.
        /// </summary>
        public ZoneType Type => zoneType;

        /// <summary>
        /// Gets the selection weight for this zone.
        /// </summary>
        public float Weight => weight;

        /// <summary>
        /// Gets the size of the zone (for Rectangle shape).
        /// </summary>
        public Vector2 Size => size;

        /// <summary>
        /// Gets the radius of the zone (for Circle shape).
        /// </summary>
        public float Radius => radius;

        /// <summary>
        /// Gets the gizmo color for editor visualization.
        /// </summary>
        public Color GizmoColor => gizmoColor;

        private void Awake()
        {
            mainCamera = Camera.main;
            CachePlayerTransform();
        }

        private void CachePlayerTransform()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        /// <summary>
        /// Gets a random point within this spawn zone.
        /// </summary>
        /// <returns>A world-space position within the zone.</returns>
        public Vector2 GetRandomPointInZone()
        {
            Vector2 localPoint;

            if (shape == ZoneShape.Rectangle)
            {
                localPoint = new Vector2(
                    Random.Range(-size.x / 2f, size.x / 2f),
                    Random.Range(-size.y / 2f, size.y / 2f)
                );
            }
            else // Circle
            {
                // Use square root for uniform distribution within circle
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float r = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
                localPoint = new Vector2(
                    Mathf.Cos(angle) * r,
                    Mathf.Sin(angle) * r
                );
            }

            // Convert to world space
            return (Vector2)transform.position + localPoint;
        }

        /// <summary>
        /// Gets a valid spawn point within this zone, respecting all spawn rules.
        /// </summary>
        /// <param name="maxAttempts">Maximum attempts to find a valid point.</param>
        /// <returns>A valid spawn point, or null if none could be found.</returns>
        public Vector2? GetValidSpawnPoint(int maxAttempts = 10)
        {
            // Refresh player reference if needed
            if (playerTransform == null)
            {
                CachePlayerTransform();
            }

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 point = GetRandomPointInZone();
                if (IsValidSpawnPoint(point))
                {
                    return point;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a given point satisfies all spawn rules for this zone.
        /// </summary>
        /// <param name="point">The world-space point to check.</param>
        /// <returns>True if the point is valid for spawning.</returns>
        public bool IsValidSpawnPoint(Vector2 point)
        {
            // Check player distance constraints
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector2.Distance(point, playerTransform.position);

                // Check minimum distance
                if (minDistanceFromPlayer > 0 && distanceToPlayer < minDistanceFromPlayer)
                {
                    return false;
                }

                // Check maximum distance
                if (maxDistanceFromPlayer > 0 && distanceToPlayer > maxDistanceFromPlayer)
                {
                    return false;
                }
            }

            // Check off-screen constraint
            if (mustBeOffScreen && mainCamera != null)
            {
                if (IsPointOnScreen(point))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if a point is currently visible on screen.
        /// </summary>
        /// <param name="point">The world-space point to check.</param>
        /// <returns>True if the point is visible on screen.</returns>
        private bool IsPointOnScreen(Vector2 point)
        {
            if (mainCamera == null) return false;

            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(point);
            
            // Add a small margin to ensure enemies don't spawn at the very edge
            const float margin = 0.05f;
            
            return viewportPoint.x >= -margin && viewportPoint.x <= 1f + margin &&
                   viewportPoint.y >= -margin && viewportPoint.y <= 1f + margin &&
                   viewportPoint.z > 0; // In front of camera
        }

        /// <summary>
        /// Checks if this zone can spawn the specified type of object.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if this zone can spawn the specified type.</returns>
        public bool CanSpawn(ZoneType type)
        {
            return zoneType == ZoneType.Both || zoneType == type;
        }

        /// <summary>
        /// Checks if a point is within the boundaries of this zone.
        /// </summary>
        /// <param name="worldPoint">The world-space point to check.</param>
        /// <returns>True if the point is inside the zone.</returns>
        public bool ContainsPoint(Vector2 worldPoint)
        {
            Vector2 localPoint = worldPoint - (Vector2)transform.position;

            if (shape == ZoneShape.Rectangle)
            {
                return Mathf.Abs(localPoint.x) <= size.x / 2f &&
                       Mathf.Abs(localPoint.y) <= size.y / 2f;
            }
            else // Circle
            {
                return localPoint.magnitude <= radius;
            }
        }

        /// <summary>
        /// Gets the approximate area of this spawn zone.
        /// </summary>
        /// <returns>The area in square units.</returns>
        public float GetArea()
        {
            if (shape == ZoneShape.Rectangle)
            {
                return size.x * size.y;
            }
            else // Circle
            {
                return Mathf.PI * radius * radius;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawZoneGizmo(0.3f);
        }

        private void OnDrawGizmosSelected()
        {
            DrawZoneGizmo(0.6f);
        }

        private void DrawZoneGizmo(float alphaMultiplier)
        {
            Color color = gizmoColor;
            color.a *= alphaMultiplier;
            Gizmos.color = color;

            if (shape == ZoneShape.Rectangle)
            {
                // Draw filled rectangle
                Gizmos.DrawCube(transform.position, new Vector3(size.x, size.y, 0.1f));
                
                // Draw wire outline
                color.a = 1f;
                Gizmos.color = color;
                Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0.1f));
            }
            else // Circle
            {
                // Draw circle using line segments
                DrawCircleGizmo(transform.position, radius, color);
            }

            // Draw zone type indicator
            DrawZoneTypeIndicator();
        }

        private void DrawCircleGizmo(Vector3 center, float r, Color color)
        {
            const int segments = 32;
            float angleStep = 360f / segments;
            
            Vector3 prevPoint = center + new Vector3(r, 0, 0);
            
            Gizmos.color = color;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }

        private void DrawZoneTypeIndicator()
        {
            // Draw a small icon indicating the zone type
            Vector3 iconPos = transform.position + Vector3.up * (shape == ZoneShape.Rectangle ? size.y / 2f + 0.5f : radius + 0.5f);
            
            Color iconColor = zoneType switch
            {
                ZoneType.Enemy => Color.red,
                ZoneType.PowerUp => Color.green,
                ZoneType.Both => Color.yellow,
                _ => Color.white
            };
            
            Gizmos.color = iconColor;
            Gizmos.DrawSphere(iconPos, 0.2f);
        }
#endif
    }
}
