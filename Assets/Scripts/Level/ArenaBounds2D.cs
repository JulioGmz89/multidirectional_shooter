using UnityEngine;

/// <summary>
/// Defines the playable arena bounds and (optionally) auto-generates 2D wall colliders.
///
/// Attach to an empty GameObject (e.g., "ArenaBounds") and set the Bounds rect.
/// This creates 4 child GameObjects with BoxCollider2D to physically block Rigidbody2D objects.
/// </summary>
[ExecuteAlways]
public class ArenaBounds2D : MonoBehaviour
{
    [Header("Arena Bounds")]
    [Tooltip("Playable area in world units (x,y = bottom-left, width/height = size).")]
    [SerializeField] private Rect bounds = new Rect(-30f, -20f, 60f, 40f);

    [Header("Wall Colliders")]
    [Tooltip("If enabled, creates/updates 4 BoxCollider2D walls around the bounds.")]
    [SerializeField] private bool autoGenerateWalls = true;

    [Tooltip("Wall thickness in world units.")]
    [Min(0.01f)]
    [SerializeField] private float wallThickness = 1f;

    [Tooltip("If enabled, walls will be triggers instead of solid colliders.")]
    [SerializeField] private bool wallsAreTriggers = false;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.9f, 0.9f);

    public Rect Bounds => bounds;

    private void Reset()
    {
        // Reasonable defaults; matches the proposal doc.
        bounds = new Rect(-30f, -20f, 60f, 40f);
        wallThickness = 1f;
        autoGenerateWalls = true;
        wallsAreTriggers = false;

        RebuildWalls();
    }

    private void OnValidate()
    {
        bounds.width = Mathf.Max(0.1f, bounds.width);
        bounds.height = Mathf.Max(0.1f, bounds.height);
        wallThickness = Mathf.Max(0.01f, wallThickness);

        if (autoGenerateWalls)
        {
            RebuildWalls();
        }
    }

    [ContextMenu("Rebuild Walls")]
    public void RebuildWalls()
    {
        if (!autoGenerateWalls)
        {
            return;
        }

        // Avoid generating when object is inactive in edit-time workflows.
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        float xMin = bounds.xMin;
        float xMax = bounds.xMax;
        float yMin = bounds.yMin;
        float yMax = bounds.yMax;

        float t = wallThickness;

        // Expand wall lengths slightly to overlap at corners.
        float verticalWallHeight = bounds.height + (t * 2f);
        float horizontalWallWidth = bounds.width + (t * 2f);

        // Left
        ConfigureWall(
            childName: "Wall_Left",
            center: new Vector2(xMin - (t * 0.5f), bounds.center.y),
            size: new Vector2(t, verticalWallHeight)
        );

        // Right
        ConfigureWall(
            childName: "Wall_Right",
            center: new Vector2(xMax + (t * 0.5f), bounds.center.y),
            size: new Vector2(t, verticalWallHeight)
        );

        // Bottom
        ConfigureWall(
            childName: "Wall_Bottom",
            center: new Vector2(bounds.center.x, yMin - (t * 0.5f)),
            size: new Vector2(horizontalWallWidth, t)
        );

        // Top
        ConfigureWall(
            childName: "Wall_Top",
            center: new Vector2(bounds.center.x, yMax + (t * 0.5f)),
            size: new Vector2(horizontalWallWidth, t)
        );
    }

    private void ConfigureWall(string childName, Vector2 center, Vector2 size)
    {
        Transform wall = GetOrCreateChild(childName);
        wall.localPosition = center;
        wall.localRotation = Quaternion.identity;
        wall.localScale = Vector3.one;

        BoxCollider2D collider = wall.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = wall.gameObject.AddComponent<BoxCollider2D>();
        }

        collider.offset = Vector2.zero;
        collider.size = size;
        collider.isTrigger = wallsAreTriggers;
    }

    private Transform GetOrCreateChild(string childName)
    {
        Transform existing = transform.Find(childName);
        if (existing != null)
        {
            return existing;
        }

        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, worldPositionStays: false);
        return child.transform;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Vector3 center = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
        Vector3 size = new Vector3(bounds.size.x, bounds.size.y, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
