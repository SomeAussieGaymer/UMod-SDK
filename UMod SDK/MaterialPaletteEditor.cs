using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

namespace Tools
{
    [CustomEditor(typeof(MaterialPaletteAsset))]
    public class MaterialPaletteEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MaterialPaletteAsset asset = (MaterialPaletteAsset)target;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("guid"), new GUIContent("GUID"));
            if (GUILayout.Button("Regenerate", GUILayout.Width(100)))
            {
                GenerateGuid(asset, true);
                EditorUtility.SetDirty(asset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("materials"), true);

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Add Material"))
            {
                AddNewMaterial(asset);
            }

            if (GUILayout.Button("Save Material Palette"))
            {
                SaveMaterialPaletteFile(asset);
            }
        }

        private void GenerateGuid(MaterialPaletteAsset asset, bool regenerate = false)
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

        private void AddNewMaterial(MaterialPaletteAsset asset)
        {
            if (asset.materials == null)
                asset.materials = new List<MaterialEntry>();

            asset.materials.Add(new MaterialEntry());
            EditorUtility.SetDirty(asset);
        }

        private void SaveMaterialPaletteFile(MaterialPaletteAsset asset)
        {
            string materialText = GetText(asset);
            string fileName = asset.name;

            string path = EditorUtility.SaveFilePanel(
                "Save Material Palette",
                "",
                fileName + ".asset",
                "asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, materialText);
                AssetDatabase.Refresh();
            }
        }

        public string GetText(MaterialPaletteAsset asset)
        {
            string text = "";

            text += "GUID " + asset.guid + "\n";
            text += "Type " + "SDG.Unturned.MaterialPaletteAsset, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" + "\n";
            text += "ID " + "0" + "\n";

            if (asset.materials != null && asset.materials.Count > 0)
            {
                text += "Materials\n[\n";
                foreach (MaterialEntry material in asset.materials)
                {
                    text += "    {\n";
                    text += "        \"Name\" \"" + material.bundleName + "\"\n";
                    text += "        \"Path\" \"" + material.materialPath + "\"\n";
                    text += "    }\n";
                }
                text += "]\n";
            }

            return text;
        }

        private void OnEnable()
        {
            MaterialPaletteAsset asset = (MaterialPaletteAsset)target;
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

        private void AssignDoNotMasterBundle(MaterialPaletteAsset asset)
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