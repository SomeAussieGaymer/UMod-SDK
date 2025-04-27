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
    [CreateAssetMenu(fileName = "New FoliageResource", menuName = "UMod SDK/Foliage Resource", order = 7)]
    public class FoliageResourceAsset : ScriptableObject
    {
        [Header("Asset Details")]
        public string assetName;
        public string guid;
        public ushort id;
        
        [Header("Resource Properties")]
        public string resourceGuid;
        public float obstructionRadius = 1f;
        
        [Header("Position Properties")]
        public float minNormalPositionOffset;
        public float maxNormalPositionOffset;
        
        [Header("Rotation Properties")]
        public float normalRotationOffset;
        public float normalRotationAlignment = 1f;
        public Vector3 minRotation;
        public Vector3 maxRotation;
        public float minAngle;
        public float maxAngle;
        
        [Header("Scale Properties")]
        public Vector3 minScale = Vector3.one;
        public Vector3 maxScale = Vector3.one;
        public float minWeight = 1f;
        public float maxWeight = 1f;
        
        [Header("Spawn Properties")]
        public float density = 1f;

        private void OnEnable()
        {
            #if UNITY_EDITOR
            AssignDoNotMasterBundle();
            #endif
        }

        #if UNITY_EDITOR
        private void AssignDoNotMasterBundle()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                if (importer != null)
                {
                    importer.assetBundleName = "DONOTMASTERBUNDLE";
                    EditorUtility.SetDirty(this);
                }
            }
        }
        #endif

        private void Awake()
        {
            GenerateGuid();
        }

        public void GenerateGuid(bool regenerate = false)
        {
            if (string.IsNullOrEmpty(guid) || regenerate)
            {
                guid = Guid.NewGuid().ToString("N");
            }
            if (regenerate)
            {
                Debug.Log("Guid Regenerated. Now: " + guid);
            }
        }

        public string GetText()
        {
            string text = "";
            
            text += "GUID " + guid + "\n";
            text += "Type " + "SDG.Unturned.FoliageResourceAsset, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" + "\n";
            text += "ID " + id.ToString() + "\n";
            
            if (!string.IsNullOrEmpty(resourceGuid))
            {
                text += "Resource_GUID " + resourceGuid + "\n";
            }
            
            text += "Obstruction_Radius " + obstructionRadius.ToString("0.####") + "\n";
            
            if (minNormalPositionOffset != 0 || maxNormalPositionOffset != 0)
            {
                text += "Min_Normal_Position_Offset " + minNormalPositionOffset.ToString("0.####") + "\n";
                text += "Max_Normal_Position_Offset " + maxNormalPositionOffset.ToString("0.####") + "\n";
            }
            
            if (normalRotationOffset != 0)
            {
                text += "Normal_Rotation_Offset " + normalRotationOffset.ToString("0.####") + "\n";
            }
            
            if (normalRotationAlignment != 1)
            {
                text += "Normal_Rotation_Alignment " + normalRotationAlignment.ToString("0.####") + "\n";
            }
            
            if (minRotation != Vector3.zero || maxRotation != Vector3.zero)
            {
                text += "Min_Rotation_X " + minRotation.x.ToString("0.####") + "\n";
                text += "Min_Rotation_Y " + minRotation.y.ToString("0.####") + "\n";
                text += "Min_Rotation_Z " + minRotation.z.ToString("0.####") + "\n";
                
                text += "Max_Rotation_X " + maxRotation.x.ToString("0.####") + "\n";
                text += "Max_Rotation_Y " + maxRotation.y.ToString("0.####") + "\n";
                text += "Max_Rotation_Z " + maxRotation.z.ToString("0.####") + "\n";
            }
            
            if (minAngle != 0 || maxAngle != 0)
            {
                text += "Min_Angle " + minAngle.ToString("0.####") + "\n";
                text += "Max_Angle " + maxAngle.ToString("0.####") + "\n";
            }
            
            if (minScale != Vector3.one || maxScale != Vector3.one)
            {
                text += "Min_Scale_X " + minScale.x.ToString("0.####") + "\n";
                text += "Min_Scale_Y " + minScale.y.ToString("0.####") + "\n";
                text += "Min_Scale_Z " + minScale.z.ToString("0.####") + "\n";
                
                text += "Max_Scale_X " + maxScale.x.ToString("0.####") + "\n";
                text += "Max_Scale_Y " + maxScale.y.ToString("0.####") + "\n";
                text += "Max_Scale_Z " + maxScale.z.ToString("0.####") + "\n";
            }
            
            if (minWeight != 1 || maxWeight != 1)
            {
                text += "Min_Weight " + minWeight.ToString("0.####") + "\n";
                text += "Max_Weight " + maxWeight.ToString("0.####") + "\n";
            }
            
            if (density != 1)
            {
                text += "Density " + density.ToString("0.####") + "\n";
            }
            
            return text;
        }
        
        public string GetEnglishText()
        {
            return "";
        }

        #if UNITY_EDITOR
        public void SaveFoliageResourceFile(string folderPath)
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(folderPath))
            {
                if (!string.IsNullOrEmpty(assetPath))
                {
                    folderPath = Path.GetDirectoryName(assetPath);
                }
            }
            
            string resourceText = GetText();
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            Debug.Log($"[Foliage Save (.asset)] Asset Path: {assetPath}");
            Debug.Log($"[Foliage Save (.asset)] Derived Filename (no ext): {fileName}");

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            fileName = fileName.Replace(' ', '_');
            
            string absoluteFolderPath = folderPath;
            if (folderPath.StartsWith("Assets"))
            {
                string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
                absoluteFolderPath = Path.Combine(projectPath, folderPath);
            }
            
            string filePath = Path.Combine(absoluteFolderPath, fileName + ".asset");
            Debug.Log($"[Foliage Save (.asset)] Final FilePath: {filePath}");
            File.WriteAllText(filePath, resourceText);
            
            Debug.Log("Foliage resource file saved to: " + filePath);
            AssetDatabase.Refresh();
        }
        
        public void SaveAllFiles(string folderPath)
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(folderPath))
            {
                if (!string.IsNullOrEmpty(assetPath))
                {
                    folderPath = Path.GetDirectoryName(assetPath);
                }
            }
            
            string absoluteFolderPath = folderPath;
            if (folderPath.StartsWith("Assets"))
            {
                string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
                absoluteFolderPath = Path.Combine(projectPath, folderPath);
            }
            
            if (!Directory.Exists(absoluteFolderPath))
            {
                Debug.Log($"Creating directory: {absoluteFolderPath}");
                Directory.CreateDirectory(absoluteFolderPath);
            }
            
            try
            {
                string foliageData = GetText();
                string fileName = Path.GetFileNameWithoutExtension(assetPath);

                Debug.Log($"[Foliage SaveAll (.dat)] Asset Path: {assetPath}");
                Debug.Log($"[Foliage SaveAll (.dat)] Derived Filename (no ext): {fileName}");
                
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                fileName = fileName.Replace(' ', '_');
                
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "FoliageResource_" + GetInstanceID();
                    Debug.LogWarning($"Using fallback filename for foliage resource: {fileName}");
                }
                
                string filePath = Path.Combine(absoluteFolderPath, fileName + ".dat");
                Debug.Log($"[Foliage SaveAll (.dat)] Final FilePath: {filePath}");
                File.WriteAllText(filePath, foliageData);
                Debug.Log($"Foliage Resource data saved to: {filePath}");
                
                if (!string.IsNullOrEmpty(assetPath) && !string.IsNullOrEmpty(absoluteFolderPath))
                {
                    string assetFileName = Path.GetFileName(assetPath);
                    string targetPath = Path.Combine(absoluteFolderPath, assetFileName);
                    
                    string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
                    string fullProjectPath = Path.Combine(projectPath, assetPath);
                    
                    if (File.Exists(fullProjectPath))
                    {
                        File.Copy(fullProjectPath, targetPath, true);
                        Debug.Log($"Copied asset file to: {targetPath}");
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find asset file at {fullProjectPath}");
                    }
                }
                
                Debug.Log("All files saved to: " + absoluteFolderPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving files: {e.Message}\n{e.StackTrace}");
            }
        }
        #endif
    }
} 