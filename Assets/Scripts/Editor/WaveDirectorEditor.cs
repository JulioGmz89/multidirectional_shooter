using UnityEngine;
using UnityEditor;
using ProjectMayhem.Spawning;

namespace ProjectMayhem.Editor
{
    /// <summary>
    /// Custom editor for WaveDirector with runtime visualization.
    /// </summary>
    [CustomEditor(typeof(WaveDirector))]
    public class WaveDirectorEditor : UnityEditor.Editor
    {
        private WaveDirector director;

        // Serialized properties
        private SerializedProperty playerHealthProp;
        private SerializedProperty peakDurationProp;
        private SerializedProperty relaxDurationProp;
        private SerializedProperty minTimeBetweenBreathersProp;
        private SerializedProperty killTrackingWindowProp;
        private SerializedProperty rapidKillThresholdProp;
        private SerializedProperty killDroughtThresholdProp;
        private SerializedProperty lowHealthThresholdProp;
        private SerializedProperty criticalHealthThresholdProp;
        private SerializedProperty highPerformanceMultiplierProp;
        private SerializedProperty lowPerformanceMultiplierProp;
        private SerializedProperty lowHealthPowerUpBonusProp;
        private SerializedProperty criticalHealthPowerUpBonusProp;
        private SerializedProperty expectedWaveDurationProp;
        private SerializedProperty waveTooLongHelpMultiplierProp;
        private SerializedProperty debugModeProp;

        // Foldout states
        private bool showIntensitySettings = true;
        private bool showKillTracking = true;
        private bool showHealthSettings = true;
        private bool showDifficultyModifiers = true;
        private bool showRuntimeStats = true;

        // Colors
        private static readonly Color buildUpColor = new Color(0.3f, 0.5f, 0.8f);
        private static readonly Color peakColor = new Color(0.8f, 0.2f, 0.2f);
        private static readonly Color sustainColor = new Color(0.8f, 0.6f, 0.2f);
        private static readonly Color relaxColor = new Color(0.2f, 0.7f, 0.3f);

        private void OnEnable()
        {
            director = (WaveDirector)target;

            playerHealthProp = serializedObject.FindProperty("playerHealth");
            peakDurationProp = serializedObject.FindProperty("peakDuration");
            relaxDurationProp = serializedObject.FindProperty("relaxDuration");
            minTimeBetweenBreathersProp = serializedObject.FindProperty("minTimeBetweenBreathers");
            killTrackingWindowProp = serializedObject.FindProperty("killTrackingWindow");
            rapidKillThresholdProp = serializedObject.FindProperty("rapidKillThreshold");
            killDroughtThresholdProp = serializedObject.FindProperty("killDroughtThreshold");
            lowHealthThresholdProp = serializedObject.FindProperty("lowHealthThreshold");
            criticalHealthThresholdProp = serializedObject.FindProperty("criticalHealthThreshold");
            highPerformanceMultiplierProp = serializedObject.FindProperty("highPerformanceMultiplier");
            lowPerformanceMultiplierProp = serializedObject.FindProperty("lowPerformanceMultiplier");
            lowHealthPowerUpBonusProp = serializedObject.FindProperty("lowHealthPowerUpBonus");
            criticalHealthPowerUpBonusProp = serializedObject.FindProperty("criticalHealthPowerUpBonus");
            expectedWaveDurationProp = serializedObject.FindProperty("expectedWaveDuration");
            waveTooLongHelpMultiplierProp = serializedObject.FindProperty("waveTooLongHelpMultiplier");
            debugModeProp = serializedObject.FindProperty("debugMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader("Wave Director");

            // Player Health Reference
            EditorGUILayout.PropertyField(playerHealthProp, new GUIContent("Player Health (Auto-found)"));
            EditorGUILayout.Space(5);

            // Runtime Stats (Play Mode)
            showRuntimeStats = EditorGUILayout.Foldout(showRuntimeStats, "Runtime Status", true, EditorStyles.foldoutHeader);
            if (showRuntimeStats)
            {
                DrawRuntimeStats();
            }

            EditorGUILayout.Space(5);

            // Intensity Settings
            showIntensitySettings = EditorGUILayout.Foldout(showIntensitySettings, "Intensity Settings", true, EditorStyles.foldoutHeader);
            if (showIntensitySettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(peakDurationProp, new GUIContent("Peak Duration (s)"));
                EditorGUILayout.PropertyField(relaxDurationProp, new GUIContent("Relax Duration (s)"));
                EditorGUILayout.PropertyField(minTimeBetweenBreathersProp, new GUIContent("Min Breather Interval (s)"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Kill Tracking
            showKillTracking = EditorGUILayout.Foldout(showKillTracking, "Kill Tracking", true, EditorStyles.foldoutHeader);
            if (showKillTracking)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(killTrackingWindowProp, new GUIContent("Tracking Window (s)"));
                EditorGUILayout.PropertyField(rapidKillThresholdProp, new GUIContent("Rapid Kill Threshold"));
                EditorGUILayout.PropertyField(killDroughtThresholdProp, new GUIContent("Kill Drought Time (s)"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Health Settings
            showHealthSettings = EditorGUILayout.Foldout(showHealthSettings, "Health Thresholds", true, EditorStyles.foldoutHeader);
            if (showHealthSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(lowHealthThresholdProp, new GUIContent("Low Health (%)"));
                DrawThresholdBar(lowHealthThresholdProp.floatValue, new Color(0.8f, 0.6f, 0.2f));
                EditorGUILayout.PropertyField(criticalHealthThresholdProp, new GUIContent("Critical Health (%)"));
                DrawThresholdBar(criticalHealthThresholdProp.floatValue, new Color(0.8f, 0.2f, 0.2f));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Difficulty Modifiers
            showDifficultyModifiers = EditorGUILayout.Foldout(showDifficultyModifiers, "Difficulty Modifiers", true, EditorStyles.foldoutHeader);
            if (showDifficultyModifiers)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Difficulty Multipliers", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(highPerformanceMultiplierProp, new GUIContent("High Performance"));
                EditorGUILayout.PropertyField(lowPerformanceMultiplierProp, new GUIContent("Low Performance"));

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Power-Up Bonuses", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(lowHealthPowerUpBonusProp, new GUIContent("Low Health Bonus"));
                EditorGUILayout.PropertyField(criticalHealthPowerUpBonusProp, new GUIContent("Critical Health Bonus"));

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Wave Duration", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(expectedWaveDurationProp, new GUIContent("Expected Duration (s)"));
                EditorGUILayout.PropertyField(waveTooLongHelpMultiplierProp, new GUIContent("Too Long Help Multiplier"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Debug
            EditorGUILayout.PropertyField(debugModeProp);

            // Play Mode Testing
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                DrawTestingButtons();
            }

            serializedObject.ApplyModifiedProperties();

            // Force repaint in play mode for live updates
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawHeader(string title)
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 24);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.3f, 0.6f));
            EditorGUI.LabelField(rect, title, new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = Color.white }
            });
            EditorGUILayout.Space(5);
        }

        private void DrawRuntimeStats()
        {
            EditorGUI.indentLevel++;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime stats.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            // Intensity Phase
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Phase:", GUILayout.Width(100));
            Color phaseColor = director.CurrentPhase switch
            {
                WaveDirector.IntensityPhase.BuildUp => buildUpColor,
                WaveDirector.IntensityPhase.Peak => peakColor,
                WaveDirector.IntensityPhase.Sustain => sustainColor,
                WaveDirector.IntensityPhase.Relax => relaxColor,
                _ => Color.gray
            };
            var phaseRect = EditorGUILayout.GetControlRect(GUILayout.Width(100));
            EditorGUI.DrawRect(phaseRect, phaseColor);
            EditorGUI.LabelField(phaseRect, director.CurrentPhase.ToString(), new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
            EditorGUILayout.EndHorizontal();

            // Intensity Bar
            EditorGUILayout.LabelField($"Intensity: {director.CurrentIntensity:P0}");
            DrawIntensityBar(director.CurrentIntensity);

            EditorGUILayout.Space(5);

            // Player Health
            EditorGUILayout.LabelField($"Player Health: {director.PlayerHealthPercent:P0}");
            DrawHealthBar(director.PlayerHealthPercent, director.IsPlayerLowHealth, director.IsPlayerCriticalHealth);

            // Status indicators
            EditorGUILayout.BeginHorizontal();
            DrawStatusIndicator("Low HP", director.IsPlayerLowHealth, new Color(0.8f, 0.6f, 0.2f));
            DrawStatusIndicator("Critical", director.IsPlayerCriticalHealth, new Color(0.8f, 0.2f, 0.2f));
            DrawStatusIndicator("Wave Long", director.IsWaveTakingTooLong, new Color(0.6f, 0.3f, 0.6f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Kill Stats
            EditorGUILayout.LabelField($"Recent Kills: {director.RecentKillCount}");
            EditorGUILayout.LabelField($"Time Since Kill: {director.TimeSinceLastKill:F1}s");
            EditorGUILayout.LabelField($"Time Since Damage: {director.TimeSinceLastDamage:F1}s");

            EditorGUILayout.Space(5);

            // Computed values
            EditorGUILayout.LabelField("Current Modifiers", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Difficulty Multiplier: {director.GetDifficultyMultiplier():F2}x");
            EditorGUILayout.LabelField($"Power-Up Bonus: +{director.GetPowerUpChanceBonus():P0}");
            EditorGUILayout.LabelField($"Spawn Interval: {director.GetSpawnIntervalMultiplier():F2}x");

            EditorGUI.indentLevel--;
        }

        private void DrawIntensityBar(float value)
        {
            var rect = EditorGUILayout.GetControlRect(false, 12);
            rect.x += EditorGUI.indentLevel * 15;
            rect.width -= EditorGUI.indentLevel * 15;

            // Background
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            // Fill with gradient
            var fillRect = rect;
            fillRect.width *= value;
            Color fillColor = Color.Lerp(relaxColor, peakColor, value);
            EditorGUI.DrawRect(fillRect, fillColor);
        }

        private void DrawHealthBar(float value, bool isLow, bool isCritical)
        {
            var rect = EditorGUILayout.GetControlRect(false, 12);
            rect.x += EditorGUI.indentLevel * 15;
            rect.width -= EditorGUI.indentLevel * 15;

            // Background
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            // Fill
            var fillRect = rect;
            fillRect.width *= value;
            Color fillColor = isCritical ? new Color(0.8f, 0.2f, 0.2f) :
                              isLow ? new Color(0.8f, 0.6f, 0.2f) :
                              new Color(0.2f, 0.7f, 0.2f);
            EditorGUI.DrawRect(fillRect, fillColor);
        }

        private void DrawThresholdBar(float threshold, Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, 8);
            rect.x += EditorGUI.indentLevel * 15;
            rect.width -= EditorGUI.indentLevel * 15;

            // Background
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            // Threshold marker
            var markerRect = rect;
            markerRect.x += rect.width * threshold - 2;
            markerRect.width = 4;
            EditorGUI.DrawRect(markerRect, color);

            // Fill below threshold
            var fillRect = rect;
            fillRect.width *= threshold;
            var dimColor = color;
            dimColor.a = 0.3f;
            EditorGUI.DrawRect(fillRect, dimColor);
        }

        private void DrawStatusIndicator(string label, bool active, Color activeColor)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(80));
            Color bgColor = active ? activeColor : new Color(0.3f, 0.3f, 0.3f);
            EditorGUI.DrawRect(rect, bgColor);
            EditorGUI.LabelField(rect, label, new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = active ? Color.white : Color.gray }
            });
        }

        private void DrawTestingButtons()
        {
            EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Simulate Kill"))
            {
                director.OnEnemyKilled();
            }
            if (GUILayout.Button("Simulate Damage"))
            {
                director.OnPlayerDamaged();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Director"))
            {
                director.Reset();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
