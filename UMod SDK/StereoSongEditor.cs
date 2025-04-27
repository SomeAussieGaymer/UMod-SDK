using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace Tools
{
    [CustomEditor(typeof(StereoSongAsset))]
    public class StereoSongEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            StereoSongAsset asset = (StereoSongAsset)target;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("guid"), new GUIContent("GUID"));
            if (GUILayout.Button("Regenerate", GUILayout.Width(100)))
            {
                GenerateGuid(asset, true);
                EditorUtility.SetDirty(asset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), new GUIContent("Title"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("titleNamespace"), new GUIContent("Title Namespace"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("titleToken"), new GUIContent("Title Token"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("linkURL"), new GUIContent("Link URL"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isLoop"), new GUIContent("Is Loop"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Song Reference", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("songBundleName"), new GUIContent("Bundle Name"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("songAssetPath"), new GUIContent("Asset Path"));

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Save Stereo Song"))
            {
                SaveSongFile(asset);
            }
        }

        private void GenerateGuid(StereoSongAsset asset, bool regenerate = false)
        {
            if (string.IsNullOrEmpty(asset.guid) || regenerate)
            {
                asset.guid = Guid.NewGuid().ToString("N");
                if (regenerate)
                {
                    Debug.Log("Guid Regenerated. Now: " + asset.guid);
                }
            }
        }

        private void SaveSongFile(StereoSongAsset asset)
        {
            string songText = asset.GetExportText();
            string fileName = asset.name;

            string path = EditorUtility.SaveFilePanel(
                "Save Stereo Song",
                "",
                fileName + ".asset",
                "asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, songText);
                AssetDatabase.Refresh();
            }
        }

        private void OnEnable()
        {
            StereoSongAsset asset = (StereoSongAsset)target;
            if (string.IsNullOrEmpty(asset.guid))
            {
                GenerateGuid(asset);
            }

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

        private void AssignDoNotMasterBundle(StereoSongAsset asset)
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
    }
} 