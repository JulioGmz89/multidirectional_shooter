using UnityEngine;
using UnityEditor;
using ProjectMayhem.Spawning;
using ProjectMayhem.Data;

namespace ProjectMayhem.Editor
{
    /// <summary>
    /// Custom editor for InfiniteWaveGenerator with preview and testing tools.
    /// </summary>
    [CustomEditor(typeof(InfiniteWaveGenerator))]
    public class InfiniteWaveGeneratorEditor : UnityEditor.Editor
    {
        private SerializedProperty configProp;
        private SerializedProperty debugLoggingProp;
        private SerializedProperty testWaveNumberProp;

        private bool showDifficultyCurve = false;
        private bool showWavePreview = false;
        private int previewWaveNumber = 1;

        private void OnEnable()
        {
            configProp = serializedObject.FindProperty("config");
            debugLoggingProp = serializedObject.FindProperty("debugLogging");
            testWaveNumberProp = serializedObject.FindProperty("testWaveNumber");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InfiniteWaveGenerator generator = (InfiniteWaveGenerator)target;

            // Configuration Section
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(configProp);

            if (configProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign an InfiniteModeConfig_SO to enable wave generation.", MessageType.Warning);
                
                if (GUILayout.Button("Create New Config Asset"))
                {
                    CreateConfigAsset();
                }
            }

            EditorGUILayout.Space(10);

            // Debug Section
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLoggingProp);

            EditorGUILayout.Space(10);

            // Runtime Info (Play Mode only)
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Current Wave", generator.CurrentWaveIndex);
                EditorGUILayout.IntField("Seed", generator.GetSeed());
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(5);

                if (GUILayout.Button("Reset Generator"))
                {
                    generator.Reset();
                }
            }

            EditorGUILayout.Space(10);

            // Preview Tools Section
            if (configProp.objectReferenceValue != null)
            {
                DrawPreviewTools(configProp.objectReferenceValue as InfiniteModeConfig_SO);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPreviewTools(InfiniteModeConfig_SO config)
        {
            EditorGUILayout.LabelField("Preview Tools", EditorStyles.boldLabel);

            // Difficulty Curve Foldout
            showDifficultyCurve = EditorGUILayout.Foldout(showDifficultyCurve, "Difficulty Curve", true);
            if (showDifficultyCurve)
            {
                EditorGUI.indentLevel++;
                DrawDifficultyCurve(config);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Wave Preview Foldout
            showWavePreview = EditorGUILayout.Foldout(showWavePreview, "Wave Preview", true);
            if (showWavePreview)
            {
                EditorGUI.indentLevel++;
                DrawWavePreview(config);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawDifficultyCurve(InfiniteModeConfig_SO config)
        {
            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wave", EditorStyles.miniLabel, GUILayout.Width(40));
            EditorGUILayout.LabelField("Budget", EditorStyles.miniLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("Interval", EditorStyles.miniLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("Special", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Draw curve for waves 1-15
            for (int i = 1; i <= 15; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Wave number
                EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(40));
                
                // Budget with visual bar
                int budget = config.CalculateBudget(i);
                float budgetRatio = (float)budget / config.MaxBudget;
                DrawProgressBar(budget.ToString(), budgetRatio, 50);
                
                // Spawn interval
                float interval = config.CalculateSpawnInterval(i);
                EditorGUILayout.LabelField($"{interval:F2}s", GUILayout.Width(50));
                
                // Special wave indicator
                string special = "";
                if (config.IsBossWave(i)) special = "🔴 BOSS";
                else if (config.IsSwarmWave(i)) special = "🟡 SWARM";
                EditorGUILayout.LabelField(special);
                
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.HelpBox($"Budget scales from {config.StartingBudget} to max {config.MaxBudget}", MessageType.Info);
        }

        private void DrawProgressBar(string label, float value, float width)
        {
            Rect rect = GUILayoutUtility.GetRect(width, 16);
            EditorGUI.ProgressBar(rect, value, label);
        }

        private void DrawWavePreview(InfiniteModeConfig_SO config)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wave Number:", GUILayout.Width(100));
            previewWaveNumber = EditorGUILayout.IntSlider(previewWaveNumber, 1, 100);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Wave stats
            int budget = config.CalculateBudget(previewWaveNumber);
            float interval = config.CalculateSpawnInterval(previewWaveNumber);
            float timeBetween = config.CalculateTimeBetweenWaves(previewWaveNumber);
            float powerUpChance = config.CalculatePowerUpChance(previewWaveNumber);

            EditorGUILayout.LabelField($"Budget: {budget}");
            EditorGUILayout.LabelField($"Spawn Interval: {interval:F2}s");
            EditorGUILayout.LabelField($"Time to Next Wave: {timeBetween:F1}s");
            EditorGUILayout.LabelField($"Power-Up Chance: {powerUpChance:P1}");

            // Special wave indicator
            if (config.IsBossWave(previewWaveNumber))
            {
                EditorGUILayout.HelpBox("This is a BOSS WAVE!", MessageType.Warning);
            }
            else if (config.IsSwarmWave(previewWaveNumber))
            {
                EditorGUILayout.HelpBox("This is a SWARM WAVE!", MessageType.Info);
            }

            // Available enemies
            var available = config.GetAvailableEnemiesForWave(previewWaveNumber);
            EditorGUILayout.LabelField($"Available Enemy Types: {available.Count}");

            EditorGUI.indentLevel++;
            foreach (var enemy in available)
            {
                if (enemy != null)
                {
                    string bossTag = enemy.IsBoss ? " [BOSS]" : "";
                    EditorGUILayout.LabelField($"• {enemy.DisplayName} (Cost: {enemy.DifficultyCost}){bossTag}");
                }
            }
            EditorGUI.indentLevel--;
        }

        private void CreateConfigAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Infinite Mode Config",
                "InfiniteModeConfig",
                "asset",
                "Choose where to save the config asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                InfiniteModeConfig_SO newConfig = ScriptableObject.CreateInstance<InfiniteModeConfig_SO>();
                AssetDatabase.CreateAsset(newConfig, path);
                AssetDatabase.SaveAssets();
                
                configProp.objectReferenceValue = newConfig;
                serializedObject.ApplyModifiedProperties();
                
                Selection.activeObject = newConfig;
            }
        }
    }

    /// <summary>
    /// Custom editor for EnemyConfig_SO with helper tools.
    /// </summary>
    [CustomEditor(typeof(EnemyConfig_SO))]
    public class EnemyConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EnemyConfig_SO config = (EnemyConfig_SO)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

            // Drag prefab to set pool tag
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Set Pool Tag from Prefab:");
            GameObject prefab = (GameObject)EditorGUILayout.ObjectField(null, typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();

            if (prefab != null)
            {
                Undo.RecordObject(config, "Set Pool Tag");
                config.SetPoolTagFromPrefab(prefab);
                EditorUtility.SetDirty(config);
            }

            // Preview info
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Effective Pool Tag", config.PoolTag);
            EditorGUILayout.TextField("Display Name", config.DisplayName);
            EditorGUI.EndDisabledGroup();
        }
    }

    /// <summary>
    /// Custom editor for InfiniteModeConfig_SO with validation and preview.
    /// </summary>
    [CustomEditor(typeof(InfiniteModeConfig_SO))]
    public class InfiniteModeConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            InfiniteModeConfig_SO config = (InfiniteModeConfig_SO)target;

            EditorGUILayout.Space(10);

            // Validation
            if (config.AvailableEnemies.Count == 0)
            {
                EditorGUILayout.HelpBox("No enemies configured! Add EnemyConfig_SO assets to the Available Enemies list.", MessageType.Error);
            }
            else
            {
                int nullCount = 0;
                int bossCount = 0;
                
                foreach (var enemy in config.AvailableEnemies)
                {
                    if (enemy == null) nullCount++;
                    else if (enemy.IsBoss) bossCount++;
                }

                if (nullCount > 0)
                {
                    EditorGUILayout.HelpBox($"{nullCount} null enemy config(s) in the list!", MessageType.Warning);
                }

                EditorGUILayout.LabelField($"Total Enemy Types: {config.AvailableEnemies.Count - nullCount}");
                EditorGUILayout.LabelField($"Boss Types: {bossCount}");
            }

            EditorGUILayout.Space(10);

            // Quick preview buttons
            if (GUILayout.Button("Log Wave 1 Stats"))
            {
                Debug.Log($"Wave 1: Budget={config.CalculateBudget(1)}, Interval={config.CalculateSpawnInterval(1):F2}s");
            }

            if (GUILayout.Button("Log Wave 10 Stats"))
            {
                Debug.Log($"Wave 10: Budget={config.CalculateBudget(10)}, Interval={config.CalculateSpawnInterval(10):F2}s, IsBoss={config.IsBossWave(10)}");
            }

            if (GUILayout.Button("Log Wave 50 Stats"))
            {
                Debug.Log($"Wave 50: Budget={config.CalculateBudget(50)}, Interval={config.CalculateSpawnInterval(50):F2}s");
            }
        }
    }
}
