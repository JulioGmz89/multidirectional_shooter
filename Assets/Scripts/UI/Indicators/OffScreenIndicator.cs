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

            // Start with default scale from prefab
            baseScale = Vector3.one;

            if (currentSettings != null)
            {
                // Apply visual settings
                if (indicatorImage != null)
                {
                    // Only change sprite if one is assigned in settings
                    if (currentSettings.sprite != null)
                    {
                        indicatorImage.sprite = currentSettings.sprite;
                    }
                    
                    // Apply color, ensuring alpha is visible
                    Color settingsColor = currentSettings.color;
                    if (settingsColor.a < 0.01f)
                    {
                        settingsColor.a = 1f; // Force visible alpha if accidentally set to 0
                    }
                    indicatorImage.color = settingsColor;
                }

                // Apply scale, ensuring it's not zero
                float scale = currentSettings.scale;
                if (scale < 0.1f)
                {
                    scale = 1f; // Fallback to default if scale is too small
                }
                baseScale = Vector3.one * scale;
            }
            else if (indicatorImage != null)
            {
                // No settings found - use a default visible color
                indicatorImage.color = Color.white;
            }

            // Reset state
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

            rectTransform.localScale = baseScale * distanceScale;
        }

        /// <summary>
        /// Sets whether the indicator should be visible.
        /// Triggers fade animation.
        /// </summary>
        /// <param name="visible">Whether the indicator should be visible.</param>
        public void SetVisible(bool visible)
        {
            // If becoming visible and GameObject is inactive, reactivate it
            if (visible && !gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
                fadeAlpha = 0f; // Start from invisible for fade-in
                isVisible = false; // Reset so the state change is recognized
            }

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
