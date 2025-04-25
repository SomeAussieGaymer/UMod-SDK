using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace Tools
{
    [CustomEditor(typeof(VehicleAsset))]
    public class VehicleEditor : Editor
    {
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Basic", "Health", "Power", "Storage", "Physics", "Paint", "Explosion" };
        private Vector2 scrollPosition;
        private bool showWheelConfigs = true;
        private bool showPaintSections = true;
        private bool showExplosionSettings = true;

        public override void OnInspectorGUI()
        {
            if (target == null) return;

            serializedObject.Update();
            VehicleAsset asset = (VehicleAsset)target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space(5);
            DrawTopSection(asset);
            EditorGUILayout.Space(5);

            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            try
            {
                switch (selectedTab)
                {
                    case 0:
                        DrawBasicTab();
                        break;
                    case 1:
                        DrawHealthTab();
                        break;
                    case 2:
                        DrawFuelAndBatteryTab();
                        break;
                    case 3:
                        DrawStorageTab();
                        break;
                    case 4:
                        DrawPhysicsTab();
                        break;
                    case 5:
                        DrawPaintTab();
                        break;
                    case 6:
                        DrawExplosionTab();
                        break;
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTopSection(VehicleAsset asset)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    SerializedProperty guidProp = serializedObject.FindProperty("guid");
                    if (guidProp != null)
                    {
                        EditorGUILayout.PropertyField(guidProp, new GUIContent("GUID"));
                    }
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(100)))
                {
                    if (GUILayout.Button("Regenerate", GUILayout.Height(20)))
                    {
                        asset.GenerateGuid(true);
                        EditorUtility.SetDirty(asset);
                    }
                }
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export .dat", GUILayout.Height(24)))
                {
                    ExportDatFile(asset);
                }
                
                if (GUILayout.Button("Export english.dat", GUILayout.Height(24)))
                {
                    ExportEnglishDatFile(asset);
                }
                
                if (GUILayout.Button("Export All", GUILayout.Height(24)))
                {
                    string basePath = EditorUtility.SaveFolderPanel("Export All Files", "", "");
                    if (!string.IsNullOrEmpty(basePath))
                    {
                        string fileName = asset.title;
                        ExportDatFile(asset, Path.Combine(basePath, fileName + ".dat"));
                        ExportEnglishDatFile(asset, Path.Combine(basePath, "english.dat"));
                        Debug.Log($"All files exported to: {basePath}");
                    }
                }
            }
        }

        private void DrawBasicTab()
        {
            EditorGUILayout.LabelField("Basic Properties", EditorStyles.boldLabel);
            DrawPropertyIfExists("engine");
            DrawPropertyIfExists("rarity");
            DrawPropertyIfExists("title");
            DrawPropertyIfExists("id");
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Vehicle Features", EditorStyles.boldLabel);
            DrawPropertyIfExists("canBeLocked");
            DrawPropertyIfExists("canRepairWhileSeated");
            DrawPropertyIfExists("exit");
            DrawPropertyIfExists("hasClipPrefab");
            DrawPropertyIfExists("hasHorn");
            DrawPropertyIfExists("isPaintable");
            DrawPropertyIfExists("shouldSpawnSeatCapsules");
            DrawPropertyIfExists("lockMouse");
            DrawPropertyIfExists("reclined");
            DrawPropertyIfExists("camFollowDistance");
        }

        private void DrawHealthTab()
        {
            EditorGUILayout.LabelField("Health Settings", EditorStyles.boldLabel);
            DrawPropertyIfExists("health");
            DrawPropertyIfExists("healthMin");
            DrawPropertyIfExists("healthMax");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Armor Settings", EditorStyles.boldLabel);
            DrawPropertyIfExists("invulnerable");
            DrawPropertyIfExists("environmentInvulnerable");
            DrawPropertyIfExists("explosionsInvulnerable");
            DrawPropertyIfExists("bumperInvulnerable");
            DrawPropertyIfExists("tiresInvulnerable");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Damage Multipliers", EditorStyles.boldLabel);
            DrawPropertyIfExists("bumperMultiplier");
            DrawPropertyIfExists("childExplosionArmorMultiplier");
            DrawPropertyIfExists("passengerExplosionArmor");
        }

        private void DrawFuelAndBatteryTab()
        {
            EditorGUILayout.LabelField("Fuel Settings", EditorStyles.boldLabel);
            DrawPropertyIfExists("fuel");
            DrawPropertyIfExists("fuelMin");
            DrawPropertyIfExists("fuelMax");
            DrawPropertyIfExists("fuelBurnRate");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Battery Settings", EditorStyles.boldLabel);
            DrawPropertyIfExists("batteryPowered");
            DrawPropertyIfExists("batteryBurnRate");
            DrawPropertyIfExists("batteryChargeRate");
            DrawPropertyIfExists("batterySpawnChargeMultiplier");
            DrawPropertyIfExists("canStealBattery");
            DrawPropertyIfExists("cannotSpawnWithBattery");
            DrawPropertyIfExists("defaultBatteryGuid");
        }

        private void DrawStorageTab()
        {
            EditorGUILayout.LabelField("Storage Settings", EditorStyles.boldLabel);
            DrawPropertyIfExists("trunkStorageX");
            DrawPropertyIfExists("trunkStorageY");
        }

        private void DrawPhysicsTab()
        {
            EditorGUILayout.LabelField("Movement Settings", EditorStyles.boldLabel);
            DrawPropertyIfExists("brake");
            DrawPropertyIfExists("lift");
            DrawPropertyIfExists("speedMax");
            DrawPropertyIfExists("speedMin");
            DrawPropertyIfExists("steerMax");
            DrawPropertyIfExists("steerMin");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Air Control", EditorStyles.boldLabel);
            DrawPropertyIfExists("airSteerMax");
            DrawPropertyIfExists("airSteerMin");
            DrawPropertyIfExists("airTurnResponsiveness");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Physics Properties", EditorStyles.boldLabel);
            DrawPropertyIfExists("sleds");
            DrawPropertyIfExists("traction");
            DrawPropertyIfExists("carjackForceMultiplier");
            DrawPropertyIfExists("engineForceMultiplier");
            DrawPropertyIfExists("pitchDrive");
            DrawPropertyIfExists("pitchIdle");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Train Settings", EditorStyles.boldLabel);
            DrawPropertyIfExists("trainTrackOffset");
            DrawPropertyIfExists("trainWheelOffset");
            DrawPropertyIfExists("trainCarLength");
            DrawPropertyIfExists("supportsMobileBuildables");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Center of Mass", EditorStyles.boldLabel);
            DrawPropertyIfExists("overrideCenterOfMass");
            DrawPropertyIfExists("centerOfMassY");

            EditorGUILayout.Space(10);
            showWheelConfigs = EditorGUILayout.Foldout(showWheelConfigs, "Wheel Configurations", true);
            if (showWheelConfigs)
            {
                EditorGUI.indentLevel++;
                SerializedProperty wheelConfigs = serializedObject.FindProperty("wheelConfigurations");
                if (wheelConfigs != null)
                {
                    EditorGUILayout.PropertyField(wheelConfigs, true);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPaintTab()
        {
            EditorGUILayout.LabelField("Paint Settings", EditorStyles.boldLabel);
            DrawPropertyIfExists("defaultPaintColorMode");
            DrawPropertyIfExists("defaultPaintColors");
            DrawPropertyIfExists("defaultPaintColorConfiguration");

            EditorGUILayout.Space(10);
            showPaintSections = EditorGUILayout.Foldout(showPaintSections, "Paintable Sections", true);
            if (showPaintSections)
            {
                EditorGUI.indentLevel++;
                SerializedProperty paintSections = serializedObject.FindProperty("paintableSections");
                if (paintSections != null)
                {
                    EditorGUILayout.PropertyField(paintSections, true);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawExplosionTab()
        {
            EditorGUILayout.LabelField("Explosion Settings", EditorStyles.boldLabel);
            DrawPropertyIfExists("explosionGuid");
            DrawPropertyIfExists("explosion");
            DrawPropertyIfExists("explosionForceMultiplier");
            DrawPropertyIfExists("explosionMaxForce");
            DrawPropertyIfExists("explosionMinForce");
            DrawPropertyIfExists("shouldExplosionBurnMaterials");
            DrawPropertyIfExists("shouldExplosionCauseDamage");

            EditorGUILayout.Space(10);
            showExplosionSettings = EditorGUILayout.Foldout(showExplosionSettings, "Explosion Burn Material Sections", true);
            if (showExplosionSettings)
            {
                EditorGUI.indentLevel++;
                SerializedProperty burnSections = serializedObject.FindProperty("explosionBurnMaterialSections");
                if (burnSections != null)
                {
                    EditorGUILayout.PropertyField(burnSections, true);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPropertyIfExists(string propertyName)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop);
            }
        }

        private void ExportDatFile(VehicleAsset asset, string customPath = null)
        {
            string vehicleText = GetDatContent(asset);
            string fileName = asset.title;

            string path = customPath;
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanel(
                    "Export Vehicle .dat",
                    "",
                    fileName + ".dat",
                    "dat"
                );
            }

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, vehicleText);
                Debug.Log($"Vehicle .dat exported to: {path}");
            }
        }

        private void ExportEnglishDatFile(VehicleAsset asset, string customPath = null)
        {
            string englishText = GetEnglishDatContent(asset);
            string path = customPath;
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanel(
                    "Export english.dat",
                    "",
                    "english.dat",
                    "dat"
                );
            }

            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(englishText))
            {
                File.WriteAllText(path, englishText);
                Debug.Log($"Vehicle english.dat exported to: {path}");
            }
        }

        private string GetDatContent(VehicleAsset asset)
        {
            return asset.GetText();
        }

        private string GetEnglishDatContent(VehicleAsset asset)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(asset.title))
                sb.AppendLine("Name " + asset.title);
            return sb.ToString();
        }

        private void SaveVehicleFile(VehicleAsset asset)
        {
            string vehicleText = asset.GetText();
            string fileName = asset.title;

            string path = EditorUtility.SaveFilePanel(
                "Save Vehicle",
                "",
                fileName + ".dat",
                "dat"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, vehicleText);
                Debug.Log($"Vehicle saved to: {path}");
            }
        }

        private void OnEnable()
        {
            VehicleAsset asset = (VehicleAsset)target;
            if (asset != null && string.IsNullOrEmpty(asset.guid))
            {
                asset.GenerateGuid();
            }

            if (!EditorApplication.isUpdating && !EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += () => {
                    if (this != null && target != null && !EditorApplication.isUpdating && !EditorApplication.isCompiling)
                    {
                        AssignDoNotMasterBundle(asset);
                    }
                };
            }
        }

        private void AssignDoNotMasterBundle(VehicleAsset asset)
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling || asset == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath))
            {
                if (assetPath.Contains("UMod SDK"))
                {
                    AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                    if (importer != null)
                    {
                        importer.assetBundleName = "DONOTMASTERBUNDLE";
                        EditorUtility.SetDirty(asset);
                        EditorApplication.delayCall += AssetDatabase.SaveAssets;
                    }
                }
            }
        }
    }
} 