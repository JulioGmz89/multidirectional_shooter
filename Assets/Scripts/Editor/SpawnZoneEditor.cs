using UnityEngine;
using UnityEditor;
using ProjectMayhem.Spawning;

namespace ProjectMayhem.Editor
{
    /// <summary>
    /// Custom editor for SpawnZone that provides enhanced visualization and editing capabilities.
    /// </summary>
    [CustomEditor(typeof(SpawnZone))]
    public class SpawnZoneEditor : UnityEditor.Editor
    {
        // Serialized properties
        private SerializedProperty shapeProp;
        private SerializedProperty zoneTypeProp;
        private SerializedProperty sizeProp;
        private SerializedProperty radiusProp;
        private SerializedProperty minDistanceFromPlayerProp;
        private SerializedProperty maxDistanceFromPlayerProp;
        private SerializedProperty mustBeOffScreenProp;
        private SerializedProperty weightProp;
        private SerializedProperty gizmoColorProp;

        // Preview settings
        private bool showPreviewPoints = false;
        private Vector2[] previewPoints = new Vector2[10];
        private float lastPreviewTime;

        private void OnEnable()
        {
            shapeProp = serializedObject.FindProperty("shape");
            zoneTypeProp = serializedObject.FindProperty("zoneType");
            sizeProp = serializedObject.FindProperty("size");
            radiusProp = serializedObject.FindProperty("radius");
            minDistanceFromPlayerProp = serializedObject.FindProperty("minDistanceFromPlayer");
            maxDistanceFromPlayerProp = serializedObject.FindProperty("maxDistanceFromPlayer");
            mustBeOffScreenProp = serializedObject.FindProperty("mustBeOffScreen");
            weightProp = serializedObject.FindProperty("weight");
            gizmoColorProp = serializedObject.FindProperty("gizmoColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SpawnZone zone = (SpawnZone)target;

            // Zone Configuration Section
            EditorGUILayout.LabelField("Zone Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(shapeProp);
            EditorGUILayout.PropertyField(zoneTypeProp);

            // Show shape-specific properties
            EditorGUI.indentLevel++;
            if (zone.Shape == SpawnZone.ZoneShape.Rectangle)
            {
                EditorGUILayout.PropertyField(sizeProp);
            }
            else
            {
                EditorGUILayout.PropertyField(radiusProp);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // Spawn Rules Section
            EditorGUILayout.LabelField("Spawn Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(minDistanceFromPlayerProp);
            EditorGUILayout.PropertyField(maxDistanceFromPlayerProp);
            EditorGUILayout.PropertyField(mustBeOffScreenProp);
            EditorGUILayout.PropertyField(weightProp);

            EditorGUILayout.Space(10);

            // Gizmo Settings Section
            EditorGUILayout.LabelField("Gizmo Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(gizmoColorProp);

            EditorGUILayout.Space(10);

            // Preview Section
            EditorGUILayout.LabelField("Preview Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            showPreviewPoints = EditorGUILayout.Toggle("Show Preview Points", showPreviewPoints);
            if (GUILayout.Button("Regenerate Points", GUILayout.Width(120)))
            {
                GeneratePreviewPoints(zone);
            }
            EditorGUILayout.EndHorizontal();

            // Info Section
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Zone Info", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Area", zone.GetArea());
            EditorGUI.EndDisabledGroup();

            // Zone type color indicator
            DrawZoneTypeIndicator(zone.Type);

            serializedObject.ApplyModifiedProperties();

            // Auto-regenerate preview points periodically
            if (showPreviewPoints && Time.realtimeSinceStartup - lastPreviewTime > 2f)
            {
                GeneratePreviewPoints(zone);
            }

            // Repaint scene view when inspector changes
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }

        private void DrawZoneTypeIndicator(SpawnZone.ZoneType type)
        {
            Color color = type switch
            {
                SpawnZone.ZoneType.Enemy => new Color(1f, 0.3f, 0.3f),
                SpawnZone.ZoneType.PowerUp => new Color(0.3f, 1f, 0.3f),
                SpawnZone.ZoneType.Both => new Color(1f, 1f, 0.3f),
                _ => Color.white
            };

            string label = type switch
            {
                SpawnZone.ZoneType.Enemy => "🔴 Enemy Spawn Zone",
                SpawnZone.ZoneType.PowerUp => "🟢 Power-Up Spawn Zone",
                SpawnZone.ZoneType.Both => "🟡 Mixed Spawn Zone",
                _ => "Unknown"
            };

            EditorGUILayout.Space(5);
            
            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            style.richText = true;
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;
            
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            EditorGUILayout.LabelField(label, style, GUILayout.Height(25));
            GUI.backgroundColor = originalColor;
        }

        private void GeneratePreviewPoints(SpawnZone zone)
        {
            for (int i = 0; i < previewPoints.Length; i++)
            {
                previewPoints[i] = zone.GetRandomPointInZone();
            }
            lastPreviewTime = Time.realtimeSinceStartup;
            SceneView.RepaintAll();
        }

        private void OnSceneGUI()
        {
            SpawnZone zone = (SpawnZone)target;

            // Draw handles for resizing
            DrawResizeHandles(zone);

            // Draw preview points
            if (showPreviewPoints)
            {
                DrawPreviewPoints(zone);
            }

            // Draw distance indicators
            DrawDistanceIndicators(zone);
        }

        private void DrawResizeHandles(SpawnZone zone)
        {
            Handles.color = zone.GizmoColor;

            if (zone.Shape == SpawnZone.ZoneShape.Rectangle)
            {
                // Corner handles for rectangle
                Vector3 pos = zone.transform.position;
                Vector2 size = zone.Size;

                EditorGUI.BeginChangeCheck();

                // Right handle
                Vector3 rightHandle = Handles.Slider(
                    pos + new Vector3(size.x / 2f, 0, 0),
                    Vector3.right,
                    HandleUtility.GetHandleSize(pos) * 0.1f,
                    Handles.DotHandleCap,
                    0.1f
                );

                // Top handle
                Vector3 topHandle = Handles.Slider(
                    pos + new Vector3(0, size.y / 2f, 0),
                    Vector3.up,
                    HandleUtility.GetHandleSize(pos) * 0.1f,
                    Handles.DotHandleCap,
                    0.1f
                );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(zone, "Resize Spawn Zone");
                    serializedObject.Update();
                    
                    float newWidth = Mathf.Max(0.5f, (rightHandle.x - pos.x) * 2f);
                    float newHeight = Mathf.Max(0.5f, (topHandle.y - pos.y) * 2f);
                    
                    sizeProp.vector2Value = new Vector2(newWidth, newHeight);
                    serializedObject.ApplyModifiedProperties();
                }
            }
            else // Circle
            {
                // Radius handle for circle
                Vector3 pos = zone.transform.position;
                float radius = zone.Radius;

                EditorGUI.BeginChangeCheck();

                float newRadius = Handles.RadiusHandle(
                    Quaternion.identity,
                    pos,
                    radius
                );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(zone, "Resize Spawn Zone");
                    serializedObject.Update();
                    radiusProp.floatValue = Mathf.Max(0.5f, newRadius);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawPreviewPoints(SpawnZone zone)
        {
            for (int i = 0; i < previewPoints.Length; i++)
            {
                Vector2 point = previewPoints[i];
                bool isValid = zone.IsValidSpawnPoint(point);

                Handles.color = isValid ? Color.green : Color.red;
                Handles.DrawSolidDisc(point, Vector3.forward, 0.15f);
                
                // Draw X for invalid points
                if (!isValid)
                {
                    Handles.color = Color.white;
                    float size = 0.1f;
                    Handles.DrawLine(
                        new Vector3(point.x - size, point.y - size, 0),
                        new Vector3(point.x + size, point.y + size, 0)
                    );
                    Handles.DrawLine(
                        new Vector3(point.x + size, point.y - size, 0),
                        new Vector3(point.x - size, point.y + size, 0)
                    );
                }
            }
        }

        private void DrawDistanceIndicators(SpawnZone zone)
        {
            // Find player in scene
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            Vector3 playerPos = player.transform.position;
            SerializedProperty minDistProp = serializedObject.FindProperty("minDistanceFromPlayer");
            SerializedProperty maxDistProp = serializedObject.FindProperty("maxDistanceFromPlayer");

            // Draw min distance circle around player
            if (minDistProp.floatValue > 0)
            {
                Handles.color = new Color(1f, 0f, 0f, 0.3f);
                Handles.DrawWireDisc(playerPos, Vector3.forward, minDistProp.floatValue);
            }

            // Draw max distance circle around player
            if (maxDistProp.floatValue > 0)
            {
                Handles.color = new Color(0f, 1f, 0f, 0.3f);
                Handles.DrawWireDisc(playerPos, Vector3.forward, maxDistProp.floatValue);
            }
        }
    }

    /// <summary>
    /// Custom editor for SpawnZoneManager with zone overview.
    /// </summary>
    [CustomEditor(typeof(SpawnZoneManager))]
    public class SpawnZoneManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SpawnZoneManager manager = (SpawnZoneManager)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Zone Statistics", EditorStyles.boldLabel);

            // Show zone counts (only in play mode or after initialization)
            if (Application.isPlaying)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Total Zones", manager.AllZones.Count);
                EditorGUILayout.IntField("Enemy Zones", manager.EnemyZones.Count);
                EditorGUILayout.IntField("Power-Up Zones", manager.PowerUpZones.Count);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                // Count zones in scene
                SpawnZone[] zones = FindObjectsByType<SpawnZone>(FindObjectsSortMode.None);
                int enemyCount = 0;
                int powerUpCount = 0;

                foreach (var zone in zones)
                {
                    if (zone.CanSpawn(SpawnZone.ZoneType.Enemy)) enemyCount++;
                    if (zone.CanSpawn(SpawnZone.ZoneType.PowerUp)) powerUpCount++;
                }

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Total Zones in Scene", zones.Length);
                EditorGUILayout.IntField("Enemy Zones", enemyCount);
                EditorGUILayout.IntField("Power-Up Zones", powerUpCount);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space(10);

            // Utility buttons
            if (GUILayout.Button("Create New Spawn Zone"))
            {
                CreateNewSpawnZone();
            }

            if (GUILayout.Button("Select All Spawn Zones"))
            {
                SelectAllSpawnZones();
            }

            if (Application.isPlaying && GUILayout.Button("Test Enemy Spawn Point"))
            {
                Vector2 point = manager.GetEnemySpawnPoint();
                Debug.Log($"Test spawn point: {point}");
                
                // Create a temporary visual marker
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "SpawnPointTest";
                marker.transform.position = point;
                marker.transform.localScale = Vector3.one * 0.5f;
                marker.GetComponent<Renderer>().material.color = Color.cyan;
                Object.Destroy(marker, 2f);
            }
        }

        private void CreateNewSpawnZone()
        {
            GameObject zoneObj = new GameObject("SpawnZone");
            zoneObj.AddComponent<SpawnZone>();
            
            // Position in front of scene view camera
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                zoneObj.transform.position = sceneView.pivot;
            }

            Selection.activeGameObject = zoneObj;
            Undo.RegisterCreatedObjectUndo(zoneObj, "Create Spawn Zone");
        }

        private void SelectAllSpawnZones()
        {
            SpawnZone[] zones = FindObjectsByType<SpawnZone>(FindObjectsSortMode.None);
            GameObject[] zoneObjects = new GameObject[zones.Length];
            
            for (int i = 0; i < zones.Length; i++)
            {
                zoneObjects[i] = zones[i].gameObject;
            }

            Selection.objects = zoneObjects;
        }
    }
}
