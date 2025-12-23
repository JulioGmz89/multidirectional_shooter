using UnityEngine;

namespace ProjectMayhem.UI.Indicators
{
    /// <summary>
    /// Interface for objects that can be tracked by the off-screen indicator system.
    /// Implement this on enemies, power-ups, or any object that should show an indicator when off-screen.
    /// </summary>
    public interface ITrackable
    {
        /// <summary>
        /// The transform to track for positioning the indicator.
        /// </summary>
        Transform TrackableTransform { get; }

        /// <summary>
        /// The type of indicator to display for this object.
        /// </summary>
        IndicatorType IndicatorType { get; }

        /// <summary>
        /// Whether tracking is currently enabled for this object.
        /// Return false to temporarily hide the indicator without unregistering.
        /// </summary>
        bool IsTrackingEnabled { get; }

        /// <summary>
        /// Priority for display when too many indicators are on screen.
        /// Higher values are shown first. Power-ups typically have higher priority than enemies.
        /// </summary>
        int TrackingPriority { get; }
    }
}
