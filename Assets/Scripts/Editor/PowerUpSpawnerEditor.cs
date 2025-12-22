using UnityEngine;
using UnityEditor;
using ProjectMayhem.Spawning;

namespace ProjectMayhem.Editor
{
    /// <summary>
    /// Custom editor for PowerUpSpawner with preview and testing tools.
    /// </summary>
    [CustomEditor(typeof(PowerUpSpawner))]
    public class PowerUpSpawnerEditor : UnityEditor.Editor
    {
        private PowerUpSpawner spawner;
        private SerializedProperty powerUpPoolProp;
        private SerializedProperty baseChanceOnKillProp;
        private SerializedProperty spawnOnWaveCompleteProp;
        private SerializedProperty waveCompleteChanceProp;
        private SerializedProperty maxActivePowerUpsProp;
        private SerializedProperty spawnCooldownProp;
        private SerializedProperty debugModeProp;

        private bool showPoolFoldout = true;
        private bool showStatsFoldout = true;
        private bool showTestingFoldout = true;
        private int previewWaveNumber = 1;

        private static readonly Color headerColor = new Color(0.2f, 0.6f, 0.2f);
        private static readonly Color validColor = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color invalidColor = new Color(0.8f, 0.3f, 0.3f);

        private void OnEnable()
        {
            spawner = (PowerUpSpawner)target;

            powerUpPoolProp = serializedObject.FindProperty("powerUpPool");
            baseChanceOnKillProp = serializedObject.FindProperty("baseChanceOnKill");
            spawnOnWaveCompleteProp = serializedObject.FindProperty("spawnOnWaveComplete");
            waveCompleteChanceProp = serializedObject.FindProperty("waveCompleteChance");
            maxActivePowerUpsProp = serializedObject.FindProperty("maxActivePowerUps");
            spawnCooldownProp = serializedObject.FindProperty("spawnCooldown");
            debugModeProp = serializedObject.FindProperty("debugMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader("Power-Up Spawner");

            // Power-Up Pool
            showPoolFoldout = EditorGUILayout.Foldout(showPoolFoldout, "Power-Up Pool", true, EditorStyles.foldoutHeader);
            if (showPoolFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPowerUpPool();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Spawn Settings
            EditorGUILayout.LabelField("Spawn Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(baseChanceOnKillProp, new GUIContent("Chance On Enemy Kill"));
            DrawChanceBar(baseChanceOnKillProp.floatValue, "Kill");
            
            EditorGUILayout.Space(3);
            EditorGUILayout.PropertyField(spawnOnWaveCompleteProp, new GUIContent("Spawn On Wave Complete"));
            if (spawnOnWaveCompleteProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(waveCompleteChanceProp, new GUIContent("Wave Complete Chance"));
                DrawChanceBar(waveCompleteChanceProp.floatValue, "Wave");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Limits
            EditorGUILayout.LabelField("Spawn Limits", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxActivePowerUpsProp, new GUIContent("Max Active Power-Ups", "0 = unlimited"));
            EditorGUILayout.PropertyField(spawnCooldownProp, new GUIContent("Cooldown (seconds)"));

            EditorGUILayout.Space(5);

            // Stats (Play Mode)
            showStatsFoldout = EditorGUILayout.Foldout(showStatsFoldout, "Runtime Stats", true, EditorStyles.foldoutHeader);
            if (showStatsFoldout)
            {
                DrawRuntimeStats();
            }

            EditorGUILayout.Space(5);

            // Testing Tools
            showTestingFoldout = EditorGUILayout.Foldout(showTestingFoldout, "Testing & Preview", true, EditorStyles.foldoutHeader);
            if (showTestingFoldout)
            {
                DrawTestingTools();
            }

            EditorGUILayout.Space(5);

            // Debug
            EditorGUILayout.PropertyField(debugModeProp);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(string title)
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 24);
            EditorGUI.DrawRect(rect, headerColor);
            EditorGUI.LabelField(rect, title, new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = Color.white }
            });
            EditorGUILayout.Space(5);
        }

        private void DrawPowerUpPool()
        {
            if (powerUpPoolProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No power-ups configured. Add power-up prefab pool tags.", MessageType.Warning);
            }

            for (int i = 0; i < powerUpPoolProp.arraySize; i++)
            {
                var element = powerUpPoolProp.GetArrayElementAtIndex(i);
                var poolTagProp = element.FindPropertyRelative("poolTag");
                var weightProp = element.FindPropertyRelative("weight");
                var minWaveProp = element.FindPropertyRelative("minWave");
                var enabledProp = element.FindPropertyRelative("enabled");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Header row with enable toggle and remove button
                EditorGUILayout.BeginHorizontal();
                enabledProp.boolValue = EditorGUILayout.Toggle(enabledProp.boolValue, GUILayout.Width(20));
                
                string displayName = string.IsNullOrEmpty(poolTagProp.stringValue) ? $"Power-Up {i + 1}" : poolTagProp.stringValue;
                EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
                
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    powerUpPoolProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (!enabledProp.boolValue)
                {
                    EditorGUI.BeginDisabledGroup(true);
                }

                EditorGUILayout.PropertyField(poolTagProp, new GUIContent("Pool Tag"));
                EditorGUILayout.PropertyField(weightProp, new GUIContent("Weight"));
                EditorGUILayout.PropertyField(minWaveProp, new GUIContent("Min Wave"));

                // Show weight percentage
                float totalWeight = CalculateTotalWeight(previewWaveNumber);
                if (totalWeight > 0 && enabledProp.boolValue && minWaveProp.intValue <= previewWaveNumber)
                {
                    float percentage = (weightProp.floatValue / totalWeight) * 100f;
                    EditorGUILayout.LabelField($"Spawn Chance: {percentage:F1}%", EditorStyles.miniLabel);
                }

                if (!enabledProp.boolValue)
                {
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            // Add button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Power-Up", GUILayout.Width(120)))
            {
                powerUpPoolProp.InsertArrayElementAtIndex(powerUpPoolProp.arraySize);
                var newElement = powerUpPoolProp.GetArrayElementAtIndex(powerUpPoolProp.arraySize - 1);
                newElement.FindPropertyRelative("poolTag").stringValue = "";
                newElement.FindPropertyRelative("weight").floatValue = 1f;
                newElement.FindPropertyRelative("minWave").intValue = 1;
                newElement.FindPropertyRelative("enabled").boolValue = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawChanceBar(float chance, string label)
        {
            var rect = EditorGUILayout.GetControlRect(false, 8);
            rect.x += EditorGUI.indentLevel * 15;
            rect.width -= EditorGUI.indentLevel * 15;

            // Background
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            // Fill
            var fillRect = rect;
            fillRect.width *= chance;
            Color fillColor = Color.Lerp(invalidColor, validColor, chance);
            EditorGUI.DrawRect(fillRect, fillColor);
        }

        private void DrawRuntimeStats()
        {
            EditorGUI.indentLevel++;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime stats.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Current Wave", spawner.CurrentWaveNumber.ToString());
                EditorGUILayout.LabelField("Active Power-Ups", spawner.ActivePowerUpCount.ToString());
                EditorGUILayout.LabelField("Can Spawn", spawner.CanSpawn ? "Yes" : "No (cooldown/limit)");
            }

            EditorGUI.indentLevel--;
        }

        private void DrawTestingTools()
        {
            EditorGUI.indentLevel++;

            // Wave preview selector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preview Wave", GUILayout.Width(100));
            previewWaveNumber = EditorGUILayout.IntSlider(previewWaveNumber, 1, 20);
            EditorGUILayout.EndHorizontal();

            // Show available power-ups for wave
            int availableCount = CountAvailablePowerUps(previewWaveNumber);
            float totalWeight = CalculateTotalWeight(previewWaveNumber);
            EditorGUILayout.LabelField($"Available at Wave {previewWaveNumber}: {availableCount} power-ups");

            // Weight distribution chart
            if (availableCount > 0)
            {
                DrawWeightDistribution(previewWaveNumber);
            }

            EditorGUILayout.Space(5);

            // Play mode testing buttons
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Play Mode Testing", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Spawn Random"))
                {
                    spawner.SpawnRandomPowerUp();
                }
                if (GUILayout.Button("Try Kill Spawn"))
                {
                    spawner.OnEnemyKilled();
                }
                if (GUILayout.Button("Wave Complete"))
                {
                    spawner.OnWaveComplete(true);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Set Wave"))
                {
                    spawner.CurrentWaveNumber = previewWaveNumber;
                }
                if (GUILayout.Button("Reset Spawner"))
                {
                    spawner.Reset();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawWeightDistribution(int waveNumber)
        {
            var rect = EditorGUILayout.GetControlRect(false, 24);
            rect.x += EditorGUI.indentLevel * 15;
            rect.width -= EditorGUI.indentLevel * 15;

            float totalWeight = CalculateTotalWeight(waveNumber);
            if (totalWeight <= 0) return;

            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            // Draw segments
            float x = rect.x;
            Color[] colors = new Color[] 
            { 
                new Color(0.2f, 0.6f, 0.2f),
                new Color(0.2f, 0.4f, 0.8f),
                new Color(0.8f, 0.6f, 0.2f),
                new Color(0.6f, 0.2f, 0.6f),
                new Color(0.2f, 0.7f, 0.7f)
            };

            int colorIndex = 0;
            for (int i = 0; i < powerUpPoolProp.arraySize; i++)
            {
                var element = powerUpPoolProp.GetArrayElementAtIndex(i);
                var enabledProp = element.FindPropertyRelative("enabled");
                var minWaveProp = element.FindPropertyRelative("minWave");
                var weightProp = element.FindPropertyRelative("weight");
                var poolTagProp = element.FindPropertyRelative("poolTag");

                if (!enabledProp.boolValue || minWaveProp.intValue > waveNumber)
                    continue;

                float percentage = weightProp.floatValue / totalWeight;
                float segmentWidth = rect.width * percentage;

                var segmentRect = new Rect(x, rect.y, segmentWidth, rect.height);
                EditorGUI.DrawRect(segmentRect, colors[colorIndex % colors.Length]);

                // Label if wide enough
                if (segmentWidth > 40)
                {
                    var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    string label = string.IsNullOrEmpty(poolTagProp.stringValue) 
                        ? $"{percentage * 100:F0}%" 
                        : $"{poolTagProp.stringValue.Replace("PowerUp_", "")}";
                    EditorGUI.LabelField(segmentRect, label, labelStyle);
                }

                x += segmentWidth;
                colorIndex++;
            }
        }

        private int CountAvailablePowerUps(int waveNumber)
        {
            int count = 0;
            for (int i = 0; i < powerUpPoolProp.arraySize; i++)
            {
                var element = powerUpPoolProp.GetArrayElementAtIndex(i);
                var enabledProp = element.FindPropertyRelative("enabled");
                var minWaveProp = element.FindPropertyRelative("minWave");

                if (enabledProp.boolValue && minWaveProp.intValue <= waveNumber)
                    count++;
            }
            return count;
        }

        private float CalculateTotalWeight(int waveNumber)
        {
            float total = 0f;
            for (int i = 0; i < powerUpPoolProp.arraySize; i++)
            {
                var element = powerUpPoolProp.GetArrayElementAtIndex(i);
                var enabledProp = element.FindPropertyRelative("enabled");
                var minWaveProp = element.FindPropertyRelative("minWave");
                var weightProp = element.FindPropertyRelative("weight");

                if (enabledProp.boolValue && minWaveProp.intValue <= waveNumber)
                    total += weightProp.floatValue;
            }
            return total;
        }
    }
}
