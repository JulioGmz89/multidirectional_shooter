using System.Collections.Generic;
using UnityEngine;

namespace ProjectMayhem.UI.Indicators
{
    /// <summary>
    /// Manages all off-screen indicators in the game.
    /// Tracks registered targets and updates indicator positions each frame.
    /// </summary>
    public class OffScreenIndicatorManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static OffScreenIndicatorManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Configuration asset for indicator settings.")]
        [SerializeField] private IndicatorConfig config;

        [Header("References")]
        [Tooltip("The camera to use for screen calculations. If null, uses Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Parent transform for indicator UI elements.")]
        [SerializeField] private RectTransform indicatorContainer;

        [Header("Prefabs")]
        [Tooltip("Default indicator prefab to instantiate.")]
        [SerializeField] private GameObject indicatorPrefab;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo;

        // Registered targets
        private HashSet<ITrackable> registeredTargets = new HashSet<ITrackable>();
        private List<ITrackable> activeTargets = new List<ITrackable>();

        // Indicator pool
        private List<OffScreenIndicator> indicatorPool = new List<OffScreenIndicator>();
        private Dictionary<ITrackable, OffScreenIndicator> activeIndicators = new Dictionary<ITrackable, OffScreenIndicator>();

        // Update timing
        private float updateTimer;

        // Cached calculations
        private Vector2 screenCenter;
        private Vector2 screenSize;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple OffScreenIndicatorManagers detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Find camera if not assigned
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    Debug.LogError("OffScreenIndicatorManager: No camera found! System will not function.", this);
                    enabled = false;
                    return;
                }
            }

            // Validate config
            if (config == null)
            {
                Debug.LogError("OffScreenIndicatorManager: No configuration assigned!", this);
                enabled = false;
                return;
            }

            // Cache screen dimensions
            UpdateScreenDimensions();

            // Pre-warm the indicator pool
            PreWarmPool(config.MaxIndicators / 2);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            // Only update during gameplay
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Gameplay)
            {
                return;
            }

            // Throttle updates based on config
            updateTimer += Time.deltaTime;
            if (config != null && updateTimer < config.UpdateInterval)
            {
                // Still update fade animations every frame
                UpdateFadeAnimations();
                return;
            }
            updateTimer = 0f;

            // Update screen dimensions in case of resize
            UpdateScreenDimensions();

            // Clean up invalid targets
            CleanupInvalidTargets();

            // Update active targets list sorted by priority
            UpdateActiveTargets();

            // Update indicators for each active target
            UpdateIndicators();

            // Update fade animations
            UpdateFadeAnimations();
        }

        /// <summary>
        /// Registers a trackable target with the indicator system.
        /// </summary>
        /// <param name="target">The target to track.</param>
        public void RegisterTarget(ITrackable target)
        {
            if (target == null) return;

            if (registeredTargets.Add(target))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"OffScreenIndicatorManager: Registered target {target.TrackableTransform?.name}", this);
                }
            }
        }

        /// <summary>
        /// Unregisters a trackable target from the indicator system.
        /// </summary>
        /// <param name="target">The target to stop tracking.</param>
        public void UnregisterTarget(ITrackable target)
        {
            if (target == null) return;

            if (registeredTargets.Remove(target))
            {
                // Return indicator to pool
                if (activeIndicators.TryGetValue(target, out var indicator))
                {
                    indicator.Clear();
                    activeIndicators.Remove(target);
                }

                if (showDebugInfo)
                {
                    Debug.Log($"OffScreenIndicatorManager: Unregistered target", this);
                }
            }
        }

        /// <summary>
        /// Gets the number of currently registered targets.
        /// </summary>
        public int RegisteredTargetCount => registeredTargets.Count;

        /// <summary>
        /// Gets the number of currently visible indicators.
        /// </summary>
        public int ActiveIndicatorCount => activeIndicators.Count;

        private void UpdateScreenDimensions()
        {
            screenSize = new Vector2(Screen.width, Screen.height);
            screenCenter = screenSize / 2f;
        }

        private void CleanupInvalidTargets()
        {
            // Remove targets that are no longer valid
            registeredTargets.RemoveWhere(t =>
                t == null ||
                t.TrackableTransform == null ||
                !t.TrackableTransform.gameObject.activeInHierarchy);

            // Clean up indicators for removed targets
            var indicatorsToRemove = new List<ITrackable>();
            foreach (var kvp in activeIndicators)
            {
                if (!registeredTargets.Contains(kvp.Key))
                {
                    kvp.Value.Clear();
                    indicatorsToRemove.Add(kvp.Key);
                }
            }

            foreach (var target in indicatorsToRemove)
            {
                activeIndicators.Remove(target);
            }
        }

        private void UpdateActiveTargets()
        {
            activeTargets.Clear();

            foreach (var target in registeredTargets)
            {
                if (target.IsTrackingEnabled)
                {
                    activeTargets.Add(target);
                }
            }

            // Sort by priority (higher priority first)
            activeTargets.Sort((a, b) => b.TrackingPriority.CompareTo(a.TrackingPriority));

            // Limit to max indicators
            if (config != null && activeTargets.Count > config.MaxIndicators)
            {
                activeTargets.RemoveRange(config.MaxIndicators, activeTargets.Count - config.MaxIndicators);
            }
        }

        private void UpdateIndicators()
        {
            if (targetCamera == null) return;

            // Track which indicators are still in use
            var usedIndicators = new HashSet<ITrackable>();

            foreach (var target in activeTargets)
            {
                Vector3 targetWorldPos = target.TrackableTransform.position;
                Vector3 targetScreenPos = targetCamera.WorldToScreenPoint(targetWorldPos);

                // Check if target is behind camera
                bool isBehindCamera = targetScreenPos.z < 0;
                if (isBehindCamera)
                {
                    // Flip position for targets behind camera
                    targetScreenPos.x = screenSize.x - targetScreenPos.x;
                    targetScreenPos.y = screenSize.y - targetScreenPos.y;
                }

                // Check if target is on screen
                bool isOnScreen = IsOnScreen(targetScreenPos);

                if (isOnScreen && !isBehindCamera)
                {
                    // Target is visible - hide indicator
                    if (activeIndicators.TryGetValue(target, out var indicator))
                    {
                        indicator.SetVisible(false);
                    }
                }
                else
                {
                    // Target is off-screen - show indicator
                    usedIndicators.Add(target);

                    // Get or create indicator
                    if (!activeIndicators.TryGetValue(target, out var indicator))
                    {
                        indicator = GetIndicatorFromPool();
                        if (indicator != null)
                        {
                            indicator.Initialize(config, target);
                            activeIndicators[target] = indicator;
                        }
                    }

                    if (indicator != null)
                    {
                        indicator.SetVisible(true);
                        UpdateIndicatorPosition(indicator, targetScreenPos, targetWorldPos);
                    }
                }
            }

            // Hide indicators for targets no longer in active list
            var indicatorsToHide = new List<ITrackable>();
            foreach (var kvp in activeIndicators)
            {
                if (!usedIndicators.Contains(kvp.Key) && !activeTargets.Contains(kvp.Key))
                {
                    kvp.Value.SetVisible(false);
                    indicatorsToHide.Add(kvp.Key);
                }
            }

            // Actually remove the hidden indicators from activeIndicators
            foreach (var target in indicatorsToHide)
            {
                activeIndicators.Remove(target);
            }
        }

        private bool IsOnScreen(Vector3 screenPos)
        {
            float buffer = config != null ? config.ScreenEdgeBuffer * Mathf.Min(screenSize.x, screenSize.y) : 0f;
            return screenPos.x > buffer &&
                   screenPos.x < screenSize.x - buffer &&
                   screenPos.y > buffer &&
                   screenPos.y < screenSize.y - buffer &&
                   screenPos.z > 0;
        }

        private void UpdateIndicatorPosition(OffScreenIndicator indicator, Vector3 targetScreenPos, Vector3 targetWorldPos)
        {
            // Calculate direction from screen center to target
            Vector2 direction = ((Vector2)targetScreenPos - screenCenter).normalized;

            // Calculate edge position
            float edgePadding = indicator.GetEdgePadding();
            Vector2 edgePosition = FindScreenEdgeIntersection(direction, edgePadding);

            // Calculate rotation angle
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Calculate distance factor for scaling (0 = close, 1 = far)
            float worldDistance = Vector3.Distance(targetCamera.transform.position, targetWorldPos);
            float screenWidthInWorld = targetCamera.orthographicSize * 2f * targetCamera.aspect;
            float distanceFactor = config != null
                ? Mathf.Clamp01((worldDistance / screenWidthInWorld) / config.FarDistanceThreshold)
                : 0f;

            indicator.UpdatePosition(edgePosition, angle, distanceFactor);
        }

        private Vector2 FindScreenEdgeIntersection(Vector2 direction, float padding)
        {
            // Calculate bounds with padding
            float halfWidth = (screenSize.x / 2f) - padding;
            float halfHeight = (screenSize.y / 2f) - padding;

            // Find intersection with screen edge using ray-box intersection
            float tX = direction.x != 0 ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue;
            float tY = direction.y != 0 ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue;
            float t = Mathf.Min(tX, tY);

            return screenCenter + direction * t;
        }

        private void UpdateFadeAnimations()
        {
            foreach (var kvp in activeIndicators)
            {
                kvp.Value.UpdateFade();
            }
        }

        private void PreWarmPool(int count)
        {
            if (indicatorPrefab == null)
            {
                Debug.LogWarning("OffScreenIndicatorManager: No indicator prefab assigned. Cannot pre-warm pool.", this);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                CreatePooledIndicator();
            }
        }

        private OffScreenIndicator GetIndicatorFromPool()
        {
            // Find an inactive indicator in the pool
            foreach (var indicator in indicatorPool)
            {
                if (!indicator.gameObject.activeInHierarchy)
                {
                    // Remove this indicator from any existing target mapping
                    // (it may have been assigned to a target that went on-screen)
                    ITrackable existingTarget = null;
                    foreach (var kvp in activeIndicators)
                    {
                        if (kvp.Value == indicator)
                        {
                            existingTarget = kvp.Key;
                            break;
                        }
                    }
                    if (existingTarget != null)
                    {
                        activeIndicators.Remove(existingTarget);
                    }

                    return indicator;
                }
            }

            // Create a new one if pool is exhausted
            return CreatePooledIndicator();
        }

        private OffScreenIndicator CreatePooledIndicator()
        {
            if (indicatorPrefab == null) return null;

            GameObject indicatorObj = Instantiate(indicatorPrefab, indicatorContainer);
            indicatorObj.SetActive(false);

            var indicator = indicatorObj.GetComponent<OffScreenIndicator>();
            if (indicator == null)
            {
                indicator = indicatorObj.AddComponent<OffScreenIndicator>();
            }

            indicatorPool.Add(indicator);
            return indicator;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"Registered Targets: {registeredTargets.Count}");
            GUILayout.Label($"Active Targets: {activeTargets.Count}");
            GUILayout.Label($"Active Indicators: {activeIndicators.Count}");
            GUILayout.Label($"Pool Size: {indicatorPool.Count}");
            GUILayout.EndArea();
        }
#endif
    }
}
