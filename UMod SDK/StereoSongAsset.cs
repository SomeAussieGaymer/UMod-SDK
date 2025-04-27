using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;

namespace Tools
{
    [CreateAssetMenu(fileName = "New Stereo Song", menuName = "UMod SDK/Stereo Song")]
    [HideInInspector]
    public class StereoSongAsset : ScriptableObject
    {
        public string guid;
        public string title;
        public string titleNamespace;
        public string titleToken;
        public string songBundleName;
        public string songAssetPath;
        public string linkURL;
        public bool isLoop;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(guid))
            {
                GenerateGuid();
            }

            if (!EditorApplication.isUpdating && !EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += () => {
                    if (this != null && !EditorApplication.isUpdating && !EditorApplication.isCompiling)
                    {
                        AssignDoNotMasterBundle();
                    }
                };
            }
        }

        private void AssignDoNotMasterBundle()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                return;

            string assetPath = AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                if (importer != null)
                {
                    importer.assetBundleName = "DONOTMASTERBUNDLE";
                    EditorUtility.SetDirty(this);
                    EditorApplication.delayCall += AssetDatabase.SaveAssets;
                }
            }
        }

        private void GenerateGuid()
        {
            if (string.IsNullOrEmpty(guid))
            {
                guid = Guid.NewGuid().ToString("N");
            }
        }

        public string GetExportText()
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine($"GUID {guid}");
            text.AppendLine("Type SDG.Unturned.StereoSongAsset, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            text.AppendLine("ID 0");

            if (!string.IsNullOrEmpty(title))
            {
                text.AppendLine($"Title \"{title}\"");
            }
            else if (!string.IsNullOrEmpty(titleToken))
            {
                text.AppendLine("Title");
                text.AppendLine("{");
                text.AppendLine($"    \"Namespace\" \"{titleNamespace}\"");
                text.AppendLine($"    \"Token\" \"{titleToken}\"");
                text.AppendLine("}");
            }

            if (!string.IsNullOrEmpty(songAssetPath))
            {
                text.AppendLine("Song");
                text.AppendLine("{");
                text.AppendLine($"    \"MasterBundle\" \"{songBundleName}\"");
                text.AppendLine($"    \"AssetPath\" \"{songAssetPath}\"");
                text.AppendLine("}");
            }

            if (!string.IsNullOrEmpty(linkURL))
            {
                text.AppendLine($"Link_URL \"{linkURL}\"");
            }

            if (isLoop)
            {
                text.AppendLine("Is_Loop true");
            }

            return text.ToString();
        }

        public void SaveAllFiles(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("Export folder path is null or empty for StereoSongAsset!");
                return;
            }

            try
            {
                Directory.CreateDirectory(folderPath);

                string fileName = this.name;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                fileName = fileName.Replace(' ', '_');
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "StereoSong_" + this.GetInstanceID();
                    Debug.LogWarning($"Using fallback filename for stereo song: {fileName}");
                }

                string filePath = Path.Combine(folderPath, $"{fileName}.asset");

                string fileContent = GetExportText();

                File.WriteAllText(filePath, fileContent);
                Debug.Log($"Stereo Song exported successfully to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export Stereo Song \'{this.name}\' to {folderPath}: {ex.Message}\\n{ex.StackTrace}");
            }
        }
    }
} 