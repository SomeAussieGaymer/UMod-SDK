using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Tools
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(FoliageResourceAsset))]
    public class FoliageResourceEditor : Editor
    {
        private bool showGeneralSettings = true;
        private bool showAssetDetails = true;
        private bool showPositionSettings = true;
        private bool showRotationSettings = true;
        private bool showScaleSettings = true;
        private bool showResourceSettings = true;
        private bool showExportOptions = true;
        private string exportPath = "";

        private void OnEnable()
        {
            FoliageResourceAsset asset = (FoliageResourceAsset)target;
            if (!EditorApplication.isUpdating && !EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += () => {
                    if (this != null && !EditorApplication.isUpdating && !EditorApplication.isCompiling)
                    {
                        AssignDoNotMasterBundle(asset);
                    }
                };
            }
        }

        private void AssignDoNotMasterBundle(FoliageResourceAsset asset)
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                return;

            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath))
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

        public override void OnInspectorGUI()
        {
            FoliageResourceAsset foliageAsset = (FoliageResourceAsset)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("assetName"));
            EditorGUILayout.Space();
            
            showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General Settings", true);
            if (showGeneralSettings)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("guid"));
                if (GUILayout.Button("Generate", GUILayout.Width(70)))
                {
                    foliageAsset.GenerateGuid(true);
                    serializedObject.FindProperty("guid").stringValue = foliageAsset.guid;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            showAssetDetails = EditorGUILayout.Foldout(showAssetDetails, "Asset Details", true);
            if (showAssetDetails)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("density"));
            }
            
            showPositionSettings = EditorGUILayout.Foldout(showPositionSettings, "Position Settings", true);
            if (showPositionSettings)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minNormalPositionOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxNormalPositionOffset"));
            }
            
            showRotationSettings = EditorGUILayout.Foldout(showRotationSettings, "Rotation Settings", true);
            if (showRotationSettings)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("normalRotationOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("normalRotationAlignment"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minRotation"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxRotation"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAngle"));
            }
            
            showScaleSettings = EditorGUILayout.Foldout(showScaleSettings, "Scale Settings", true);
            if (showScaleSettings)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minWeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxWeight"));
            }

            showResourceSettings = EditorGUILayout.Foldout(showResourceSettings, "Resource Settings", true);
            if (showResourceSettings)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("resourceGuid"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("obstructionRadius"));
            }
            
            showExportOptions = EditorGUILayout.Foldout(showExportOptions, "Export Options", true);
            if (showExportOptions)
            {
                EditorGUILayout.LabelField("File Generation", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                exportPath = EditorGUILayout.TextField("Export Folder", exportPath);
                if (GUILayout.Button("Browse...", GUILayout.Width(70)))
                {
                    string path = EditorUtility.SaveFolderPanel("Select Export Folder", exportPath, "");
                    if (!string.IsNullOrEmpty(path))
                        exportPath = path;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Generate Foliage File"))
                {
                    GenerateFoliageFile(foliageAsset);
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Files will be saved to the selected export folder with .dat extension. If no folder is selected, a file dialog will appear.", MessageType.Info);
            }
            
            if (GUILayout.Button("Test File Generation"))
            {
                Debug.Log(foliageAsset.GetText());
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void GenerateFoliageFile(FoliageResourceAsset foliageAsset)
        {
            string path = GetExportPath(foliageAsset);
            if (!string.IsNullOrEmpty(path))
            {
                foliageAsset.SaveFoliageResourceFile(path);
            }
        }
        
        private string GetExportPath(FoliageResourceAsset foliageAsset)
        {
            string path = exportPath;
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFolderPanel("Select Export Folder", "", foliageAsset.assetName);
                if (!string.IsNullOrEmpty(path))
                {
                    exportPath = path;
                }
            }
            return path;
        }
    }
    #endif
} 