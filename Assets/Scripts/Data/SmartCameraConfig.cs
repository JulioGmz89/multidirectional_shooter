using UnityEngine;

/// <summary>
/// Configuration settings for the Smart Camera system.
/// Defines dead zone, look-ahead behavior, and smoothing parameters.
/// </summary>
[CreateAssetMenu(fileName = "SmartCameraConfig", menuName = "Project Mayhem/Smart Camera Config")]
public class SmartCameraConfig : ScriptableObject
{
    [Header("Dead Zone Settings")]
    [Tooltip("Size of the dead zone around the player where the camera won't move")]
    [SerializeField] private Vector2 deadZoneSize = new Vector2(3f, 2f);
    
    [Tooltip("Visual debug for the dead zone in Scene view")]
    [SerializeField] private bool showDeadZoneGizmo = true;

    [Header("Look-Ahead Settings")]
    [Tooltip("How much the mouse position influences the camera (0 = no influence, 1 = full influence)")]
    [Range(0f, 1f)]
    [SerializeField] private float mouseInfluence = 0.3f;
    
    [Tooltip("Maximum distance the camera can look ahead from the player")]
    [SerializeField] private float maxLookAheadDistance = 8f;
    
    [Tooltip("Minimum distance from player before mouse influence starts")]
    [SerializeField] private float minMouseDistance = 1f;

    [Header("Smoothing Settings")]
    [Tooltip("Speed of camera movement when following the player")]
    [SerializeField] private float followSpeed = 5f;
    
    [Tooltip("Speed of camera movement when adjusting for mouse look-ahead")]
    [SerializeField] private float lookAheadSpeed = 3f;
    
    [Tooltip("Speed of camera movement when returning to player after losing mouse input")]
    [SerializeField] private float returnSpeed = 2f;

    [Header("Boundary Settings")]
    [Tooltip("Optional world boundaries to constrain camera movement")]
    [SerializeField] private bool useBoundaries = false;
    [SerializeField] private Rect worldBoundaries = new Rect(-50f, -50f, 100f, 100f);

    // Public properties for read-only access
    public Vector2 DeadZoneSize => deadZoneSize;
    public bool ShowDeadZoneGizmo => showDeadZoneGizmo;
    public float MouseInfluence => mouseInfluence;
    public float MaxLookAheadDistance => maxLookAheadDistance;
    public float MinMouseDistance => minMouseDistance;
    public float FollowSpeed => followSpeed;
    public float LookAheadSpeed => lookAheadSpeed;
    public float ReturnSpeed => returnSpeed;
    public bool UseBoundaries => useBoundaries;
    public Rect WorldBoundaries => worldBoundaries;

    private void OnValidate()
    {
        // Ensure dead zone size is positive
        deadZoneSize.x = Mathf.Max(0.1f, deadZoneSize.x);
        deadZoneSize.y = Mathf.Max(0.1f, deadZoneSize.y);
        
        // Ensure speeds are positive
        followSpeed = Mathf.Max(0.1f, followSpeed);
        lookAheadSpeed = Mathf.Max(0.1f, lookAheadSpeed);
        returnSpeed = Mathf.Max(0.1f, returnSpeed);
        
        // Ensure distances are positive
        maxLookAheadDistance = Mathf.Max(0.1f, maxLookAheadDistance);
        minMouseDistance = Mathf.Max(0f, minMouseDistance);
    }
}
