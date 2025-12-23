using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectMayhem.UI.Indicators
{
    /// <summary>
    /// ScriptableObject configuration for the off-screen indicator system.
    /// Allows designers to customize indicator appearance and behavior without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "IndicatorConfig", menuName = "ProjectMayhem/UI/Indicator Config")]
    public class IndicatorConfig : ScriptableObject
    {
        [Serializable]
        public class IndicatorSettings
        {
            [Tooltip("The type of target this setting applies to.")]
            public IndicatorType type;

            [Tooltip("The sprite to display for this indicator type. If null, uses prefab's default sprite.")]
            public Sprite sprite;

            [Tooltip("The color tint for this indicator.")]
            public Color color = new Color(1f, 1f, 1f, 1f); // Explicit RGBA to ensure alpha = 1

            [Tooltip("Base scale of the indicator (1 = normal size).")]
            [Range(0.05f, 2f)]
            public float scale = 1f;

            [Tooltip("Distance from screen edge in pixels.")]
            [Range(20f, 100f)]
            public float edgePadding = 50f;

            /// <summary>
            /// Creates a new IndicatorSettings with safe default values.
            /// </summary>
            public IndicatorSettings()
            {
                color = Color.white;
                scale = 1f;
                edgePadding = 50f;
            }
        }

        [Header("Indicator Types")]
        [Tooltip("Settings for each indicator type.")]
        [SerializeField] private List<IndicatorSettings> indicatorSettings = new List<IndicatorSettings>();

        [Header("Global Settings")]
        [Tooltip("Additional buffer from the actual screen edge (in viewport percentage 0-1).")]
        [Range(0f, 0.15f)]
        [SerializeField] private float screenEdgeBuffer = 0.05f;

        [Tooltip("Distance (in world units) at which indicator starts fading as target approaches screen.")]
        [SerializeField] private float fadeStartDistance = 2f;

        [Tooltip("Maximum number of indicators shown at once. Lowest priority indicators hidden first.")]
        [Range(5, 30)]
        [SerializeField] private int maxIndicators = 20;

        [Tooltip("How often to update indicator positions (in seconds). Lower = smoother but more expensive.")]
        [Range(0.016f, 0.1f)]
        [SerializeField] private float updateInterval = 0.033f; // ~30fps

        [Header("Animation Settings")]
        [Tooltip("Duration of fade in/out animations.")]
        [SerializeField] private float fadeDuration = 0.15f;

        [Header("Distance Scaling")]
        [Tooltip("Minimum scale when target is very far (beyond farDistanceThreshold).")]
        [Range(0.5f, 1f)]
        [SerializeField] private float minDistanceScale = 0.7f;

        [Tooltip("Distance (in screen widths) at which indicator reaches minimum scale.")]
        [SerializeField] private float farDistanceThreshold = 4f;

        // Cache for quick lookup
        private Dictionary<IndicatorType, IndicatorSettings> settingsCache;

        /// <summary>
        /// Gets the screen edge buffer value.
        /// </summary>
        public float ScreenEdgeBuffer => screenEdgeBuffer;

        /// <summary>
        /// Gets the distance at which fading begins.
        /// </summary>
        public float FadeStartDistance => fadeStartDistance;

        /// <summary>
        /// Gets the maximum number of indicators.
        /// </summary>
        public int MaxIndicators => maxIndicators;

        /// <summary>
        /// Gets the update interval in seconds.
        /// </summary>
        public float UpdateInterval => updateInterval;

        /// <summary>
        /// Gets the fade duration.
        /// </summary>
        public float FadeDuration => fadeDuration;

        /// <summary>
        /// Gets the minimum distance scale.
        /// </summary>
        public float MinDistanceScale => minDistanceScale;

        /// <summary>
        /// Gets the far distance threshold.
        /// </summary>
        public float FarDistanceThreshold => farDistanceThreshold;

        private void OnEnable()
        {
            BuildCache();
        }

        private void OnValidate()
        {
            BuildCache();
        }

        private void BuildCache()
        {
            settingsCache = new Dictionary<IndicatorType, IndicatorSettings>();
            foreach (var setting in indicatorSettings)
            {
                if (!settingsCache.ContainsKey(setting.type))
                {
                    settingsCache[setting.type] = setting;
                }
            }
        }

        /// <summary>
        /// Gets the settings for a specific indicator type.
        /// </summary>
        /// <param name="type">The indicator type to get settings for.</param>
        /// <returns>The settings for the type, or null if not configured.</returns>
        public IndicatorSettings GetSettings(IndicatorType type)
        {
            // Always ensure cache is built - ScriptableObject OnEnable may not be called reliably at runtime
            if (settingsCache == null || settingsCache.Count == 0)
            {
                BuildCache();
            }

            return settingsCache.TryGetValue(type, out var settings) ? settings : null;
        }

        /// <summary>
        /// Gets all configured indicator settings.
        /// </summary>
        public IReadOnlyList<IndicatorSettings> AllSettings => indicatorSettings;
    }
}
