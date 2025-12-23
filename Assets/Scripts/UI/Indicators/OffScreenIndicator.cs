using UnityEngine;
using UnityEngine.UI;

namespace ProjectMayhem.UI.Indicators
{
    /// <summary>
    /// Component that controls an individual off-screen indicator.
    /// Handles positioning, rotation, animation, and fading for a single target.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class OffScreenIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image indicatorImage;
        [SerializeField] private RectTransform rotationPivot;

        // Runtime state
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private ITrackable currentTarget;
        private IndicatorConfig config;
        private IndicatorConfig.IndicatorSettings currentSettings;

        // Animation state
        private float pulseTimer;
        private float fadeAlpha = 1f;
        private float targetAlpha = 1f;
        private Vector3 baseScale;
        private bool isVisible;

        /// <summary>
        /// The target this indicator is tracking.
        /// </summary>
        public ITrackable Target => currentTarget;

        /// <summary>
        /// Whether this indicator is currently active and tracking a target.
        /// </summary>
        public bool IsActive => currentTarget != null && isVisible;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (indicatorImage == null)
            {
                indicatorImage = GetComponentInChildren<Image>();
            }

            if (rotationPivot == null)
            {
                rotationPivot = rectTransform;
            }

            baseScale = rectTransform.localScale;
        }

        /// <summary>
        /// Initializes the indicator with configuration and assigns a target.
        /// </summary>
        /// <param name="indicatorConfig">The configuration to use.</param>
        /// <param name="target">The target to track.</param>
        public void Initialize(IndicatorConfig indicatorConfig, ITrackable target)
        {
            config = indicatorConfig;
            currentTarget = target;
            currentSettings = config?.GetSettings(target.IndicatorType);

            if (currentSettings != null)
            {
                // Apply visual settings
                if (indicatorImage != null)
                {
                    if (currentSettings.sprite != null)
                    {
                        indicatorImage.sprite = currentSettings.sprite;
                    }
                    indicatorImage.color = currentSettings.color;
                }

                baseScale = Vector3.one * currentSettings.scale;
            }

            // Reset state
            pulseTimer = 0f;
            fadeAlpha = 0f;
            targetAlpha = 1f;
            isVisible = true;

            gameObject.SetActive(true);
            UpdateAlpha();
        }

        /// <summary>
        /// Clears the current target and prepares for pooling.
        /// </summary>
        public void Clear()
        {
            currentTarget = null;
            currentSettings = null;
            isVisible = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates the indicator position and rotation to point toward the target.
        /// </summary>
        /// <param name="screenPosition">The position on screen where the indicator should appear.</param>
        /// <param name="directionAngle">The angle (in degrees) the indicator should point.</param>
        /// <param name="distanceFactor">Normalized distance factor (0 = close, 1 = far) for scaling.</param>
        public void UpdatePosition(Vector2 screenPosition, float directionAngle, float distanceFactor)
        {
            if (!isVisible) return;

            // Set position
            rectTransform.position = screenPosition;

            // Set rotation to point toward target
            rotationPivot.rotation = Quaternion.Euler(0f, 0f, directionAngle - 90f); // -90 because arrow points up by default

            // Apply distance-based scaling
            float distanceScale = config != null
                ? Mathf.Lerp(1f, config.MinDistanceScale, distanceFactor)
                : 1f;

            // Apply pulse animation if enabled
            float pulseScale = 1f;
            if (currentSettings != null && currentSettings.pulseAnimation && config != null)
            {
                pulseTimer += Time.deltaTime;
                float pulseProgress = (Mathf.Sin(pulseTimer * Mathf.PI * 2f / config.PulseDuration) + 1f) / 2f;
                pulseScale = Mathf.Lerp(config.PulseScaleRange.x, config.PulseScaleRange.y, pulseProgress);
            }

            rectTransform.localScale = baseScale * distanceScale * pulseScale;
        }

        /// <summary>
        /// Sets whether the indicator should be visible.
        /// Triggers fade animation.
        /// </summary>
        /// <param name="visible">Whether the indicator should be visible.</param>
        public void SetVisible(bool visible)
        {
            if (visible == isVisible) return;

            isVisible = visible;
            targetAlpha = visible ? 1f : 0f;
        }

        /// <summary>
        /// Updates fade animation. Call this every frame.
        /// </summary>
        public void UpdateFade()
        {
            if (config == null)
            {
                fadeAlpha = targetAlpha;
            }
            else
            {
                float fadeSpeed = 1f / config.FadeDuration;
                fadeAlpha = Mathf.MoveTowards(fadeAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            }

            UpdateAlpha();

            // Deactivate when fully faded out
            if (fadeAlpha <= 0f && !isVisible)
            {
                gameObject.SetActive(false);
            }
        }

        private void UpdateAlpha()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = fadeAlpha;
            }
        }

        /// <summary>
        /// Gets the edge padding for this indicator type.
        /// </summary>
        public float GetEdgePadding()
        {
            return currentSettings?.edgePadding ?? 50f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (indicatorImage == null)
            {
                indicatorImage = GetComponentInChildren<Image>();
            }
        }
#endif
    }
}
