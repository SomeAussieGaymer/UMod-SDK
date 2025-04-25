using UnityEngine;
using UnityEditor;
using System;
using System.IO;

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
    }
} 