using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tools
{
    [CreateAssetMenu(fileName = "New MaterialPalette", menuName = "UMod SDK/Material Palette", order = 6)]
    [HideInInspector]
    public class MaterialPaletteAsset : ScriptableObject
    {
        [SerializeField]
        public string guid;

        [Header("Material Palette Properties")]
        public List<MaterialEntry> materials = new List<MaterialEntry>();

        private void OnEnable()
        {
            #if UNITY_EDITOR
            if (!EditorApplication.isUpdating && !EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += () => {
                    if (this != null && !EditorApplication.isUpdating && !EditorApplication.isCompiling)
                    {
                        AssignDoNotMasterBundle();
                    }
                };
            }
            #endif
        }

        #if UNITY_EDITOR
        private void AssignDoNotMasterBundle()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                return;

            string assetPath = AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                Debug.Log($"Checking asset path for DONOTMASTERBUNDLE: {assetPath}");
                if (IsInDatGeneratorFolder(assetPath))
                {
                    AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                    if (importer != null)
                    {
                        Debug.Log($"Assigning DONOTMASTERBUNDLE to: {assetPath}");
                        importer.assetBundleName = "DONOTMASTERBUNDLE";
                        EditorUtility.SetDirty(this);
                        EditorApplication.delayCall += AssetDatabase.SaveAssets;
                    }
                    else
                    {
                        Debug.LogWarning($"Could not get AssetImporter for: {assetPath}");
                    }
                }
                else
                {
                    Debug.Log($"Asset not in DatGenerator folder: {assetPath}");
                }
            }
        }
        
        public static bool IsInDatGeneratorFolder(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
                
            string[] validKeywords = new string[] { "DatGenerator", "UMod SDK", "Tools" };
            foreach (string keyword in validKeywords)
            {
                if (assetPath.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        #endif

        private void Awake()
        {
            GenerateGuid();
        }

        #if UNITY_EDITOR
        public void AddNewMaterial()
        {
            if (materials == null)
                materials = new List<MaterialEntry>();
                
            materials.Add(new MaterialEntry());
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void RemoveMaterial(int index)
        {
            if (materials != null && index >= 0 && index < materials.Count)
            {
                materials.RemoveAt(index);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        #endif

        public string GetText()
        {
            #if UNITY_EDITOR
            MaterialPaletteEditor editor = (MaterialPaletteEditor)Editor.CreateEditor(this);
            string text = editor.GetText(this);
            UnityEngine.Object.DestroyImmediate(editor);
            return text;
            #else
            return "";
            #endif
        }

        public string GetEnglishText()
        {
            return "";  
        }

        public string GetFolderName()
        {
            return this.name.Trim();
        }

        public void GenerateGuid(bool regenerate = false) {            
            if (string.IsNullOrEmpty(guid) || regenerate) { 
                guid = Guid.NewGuid().ToString("N");                
            }
            if (regenerate) {
                Debug.Log("Guid Regenerated. Now: " + guid);
            }
        }

        public string GetGuid() {
            GenerateGuid();
            return guid;
        }

        #if UNITY_EDITOR
        public void SaveMaterialPaletteFile(string folderPath)
        {
            string paletteText = GetText();
            string assetPath = AssetDatabase.GetAssetPath(this);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            Debug.Log($"[MatPalette Save] Asset Path: {assetPath}");
            Debug.Log($"[MatPalette Save] Derived Filename (no ext): {fileName}");

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            fileName = fileName.Replace(' ', '_');
            
            string filePath = Path.Combine(folderPath, fileName + ".asset");
            Debug.Log($"[MatPalette Save] Final FilePath: {filePath}");
            File.WriteAllText(filePath, paletteText);
            
            Debug.Log("Material palette file saved to: " + filePath);
            AssetDatabase.Refresh();
        }
        
        public void SaveAllFiles(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.Log($"Creating directory: {folderPath}");
                Directory.CreateDirectory(folderPath);
            }
            
            try
            {
                SaveMaterialPaletteFile(folderPath);
                
                string assetPath = AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string fileName = Path.GetFileName(assetPath);
                    string targetPath = Path.Combine(folderPath, fileName);
                    
                    string fullPath = Path.GetFullPath(assetPath);
                    
                    if (File.Exists(fullPath))
                    {
                        File.Copy(fullPath, targetPath, true);
                        Debug.Log($"Copied asset file from {fullPath} to {targetPath}");
                    }
                    else
                    {
                        string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6); 
                        string fullProjectPath = Path.Combine(projectPath, assetPath);
                        
                        if (File.Exists(fullProjectPath))
                        {
                            File.Copy(fullProjectPath, targetPath, true);
                            Debug.Log($"Copied asset file from {fullProjectPath} to {targetPath}");
                        }
                        else
                        {
                            Debug.LogWarning($"Could not find asset file at {fullPath} or {fullProjectPath}");
                        }
                    }
                }
                
                Debug.Log("All files saved to: " + folderPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving files: {e.Message}\n{e.StackTrace}");
            }
        }
        #endif
    }

    [System.Serializable]
    public class MaterialEntry
    {
        public string bundleName = "core.masterbundle";
        public string materialPath;
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(MaterialPaletteAsset))]
    public class MaterialPaletteAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MaterialPaletteAsset asset = (MaterialPaletteAsset)target;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("guid"), new GUIContent("GUID"));
            if (GUILayout.Button("Regenerate", GUILayout.Width(100)))
            {
                asset.GenerateGuid(true);
                EditorUtility.SetDirty(asset);
            }
            EditorGUILayout.EndHorizontal();

            DrawPropertiesExcluding(serializedObject, "guid");

            serializedObject.ApplyModifiedProperties();
        }
    }

    public class MaterialPaletteAssetPostProcessor : AssetPostprocessor
    {
        private static HashSet<string> processedAssets = new HashSet<string>();
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string importedAsset in importedAssets)
            {
                if (!MaterialPaletteAsset.IsInDatGeneratorFolder(importedAsset))
                    continue;

                if (AssetDatabase.GetMainAssetTypeAtPath(importedAsset) == typeof(MaterialPaletteAsset))
                {
                    MaterialPaletteAsset materialPalette = AssetDatabase.LoadAssetAtPath<MaterialPaletteAsset>(importedAsset);
                    if (materialPalette == null)
                        continue;
                        
                    AssetImporter importer = AssetImporter.GetAtPath(importedAsset);
                    if (importer != null && importer.assetBundleName != "DONOTMASTERBUNDLE")
                    {
                        processedAssets.Add(importedAsset);
                        
                        importer.assetBundleName = "DONOTMASTERBUNDLE";
                        Debug.Log($"Assigned DONOTMASTERBUNDLE to {importedAsset}");
                    }
                }
            }
        }
    }
    #endif
} 