using UnityEngine;
using System.Collections;

/// <summary>
/// Handles camera shake effects with configurable intensity and duration.
/// Supports multiple shake types and smooth interpolation.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("The maximum shake intensity multiplier.")]
    [SerializeField] private float maxShakeIntensity = 1f;
    
    [Tooltip("How quickly the shake effect diminishes over time.")]
    [SerializeField] private float shakeDecay = 5f;
    
    [Tooltip("Smoothing factor for shake transitions.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float shakeSmoothness = 0.9f;

    private float currentShakeIntensity;
    private float currentShakeDuration;
    private Coroutine shakeCoroutine;
    private Vector3 currentOffset;

    /// <summary>
    /// Returns true if camera shake is currently active.
    /// </summary>
    public bool IsShaking => shakeCoroutine != null;

    /// <summary>
    /// Current shake offset to apply additively to the camera's base position.
    /// The follow system should add this to its computed camera position.
    /// </summary>
    public Vector3 CurrentOffset => currentOffset;

    private void Awake()
    {
        currentOffset = Vector3.zero;
    }

    /// <summary>
    /// Triggers a camera shake with specified intensity and duration.
    /// </summary>
    /// <param name="intensity">The intensity of the shake (0-1 range recommended)</param>
    /// <param name="duration">How long the shake should last in seconds</param>
    public void Shake(float intensity, float duration)
    {
        // Clamp intensity to prevent excessive shaking
        intensity = Mathf.Clamp01(intensity);
        
        // If a shake is already in progress, blend with the new shake
        if (shakeCoroutine != null)
        {
            // Take the stronger intensity and extend duration if needed
            currentShakeIntensity = Mathf.Max(currentShakeIntensity, intensity);
            currentShakeDuration = Mathf.Max(currentShakeDuration, duration);
        }
        else
        {
            currentShakeIntensity = intensity;
            currentShakeDuration = duration;
            shakeCoroutine = StartCoroutine(ShakeCoroutine());
        }
    }

    /// <summary>
    /// Triggers a predefined shake for enemy death events.
    /// </summary>
    public void ShakeOnEnemyDeath()
    {
        Shake(0.15f, 0.2f);
    }

    /// <summary>
    /// Triggers a predefined shake for player death events.
    /// </summary>
    public void ShakeOnPlayerDeath()
    {
        Shake(0.8f, 1.5f);
    }

    /// <summary>
    /// Triggers a predefined shake for explosion events.
    /// </summary>
    public void ShakeOnExplosion()
    {
        Shake(0.5f, 0.8f);
    }

    /// <summary>
    /// Triggers a predefined shake for player taking damage.
    /// </summary>
    public void ShakeOnPlayerDamage()
    {
        Shake(0.25f, 0.3f);
    }

    /// <summary>
    /// Stops any current shake effect immediately.
    /// </summary>
    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }
        
        currentShakeIntensity = 0f;
        currentShakeDuration = 0f;
        currentOffset = Vector3.zero;
    }

    private IEnumerator ShakeCoroutine()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < currentShakeDuration)
        {
            // Calculate shake intensity with decay over time
            float normalizedTime = elapsedTime / currentShakeDuration;
            float decayFactor = 1f - (normalizedTime * normalizedTime); // Quadratic decay for smooth falloff
            float currentIntensity = currentShakeIntensity * decayFactor * maxShakeIntensity;
            
            // Generate random shake offset (2D)
            Vector3 targetOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ) * currentIntensity;

            // Smooth the offset to avoid harsh jitter
            currentOffset = Vector3.Lerp(currentOffset, targetOffset, shakeSmoothness);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Smoothly return offset to zero
        while (currentOffset.sqrMagnitude > 0.0001f)
        {
            currentOffset = Vector3.Lerp(currentOffset, Vector3.zero, shakeSmoothness);
            yield return null;
        }

        currentOffset = Vector3.zero;
        shakeCoroutine = null;
    }

    /// <summary>
    /// Backwards-compatible no-op.
    /// Shake is now additive; the follow camera owns the base position.
    /// </summary>
    public void UpdateOriginalPosition()
    {
        // Intentionally empty.
    }

    /// <summary>
    /// Backwards-compatible no-op.
    /// Shake is now additive; the follow camera owns the base position.
    /// </summary>
    public void ForceUpdateOriginalPosition(Vector3 newBasePosition)
    {
        // Intentionally empty.
    }

    private void OnValidate()
    {
        // Ensure sensible values in the inspector
        maxShakeIntensity = Mathf.Max(0f, maxShakeIntensity);
        shakeDecay = Mathf.Max(0.1f, shakeDecay);
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool testShake = false;
    [SerializeField] private float testIntensity = 0.5f;
    [SerializeField] private float testDuration = 1f;

    private void Update()
    {
        if (testShake)
        {
            testShake = false;
            Shake(testIntensity, testDuration);
        }
    }
#endif
}
