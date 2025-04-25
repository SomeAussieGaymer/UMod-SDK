using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Tools
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(ItemDefinitionAsset))]
    public class UnturnedAssetEditor : Editor
    {
        private bool showGeneralSettings = true;
        private bool showItemProperties = true;
        private bool showLocalization = true;
        private bool showExportOptions = true;
        private string exportPath = "";
        private string selectedProperty = "";
        
        private bool propPopupHasFocus = false;
        private int propSelectedIndex = -1;
        private int propControlID = -1;
        
        private bool itemTypePopupHasFocus = false;
        private int itemTypeControlID = -1;
        
        private bool rarityPopupHasFocus = false;
        private int rarityControlID = -1;
        
        private bool useablePopupHasFocus = false; 
        private int useableControlID = -1;
        
        private bool slotPopupHasFocus = false;
        private int slotControlID = -1;
        
        private bool barricadeTypePopupHasFocus = false;
        private int barricadeTypeControlID = -1;
        
        private Dictionary<ItemType, string[]> availableProperties = new Dictionary<ItemType, string[]>();

        private void OnEnable()
        {
            availableProperties = UnturnedItemProperties.GetAllItemProperties();
        }

        private bool ShouldShowField(ItemDefinitionAsset datFile, string fieldName)
        {
            if (fieldName == "guid" || fieldName == "id" || fieldName == "nameEnglish" || 
                fieldName == "descriptionEnglish" || fieldName == "datFileValues" || 
                fieldName == "craftingRecipes")
                return true;

            if (availableProperties.ContainsKey(datFile.itemType))
            {
                string[] props = availableProperties[datFile.itemType];
                if (Array.IndexOf(props, fieldName) >= 0)
                    return true;
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            ItemDefinitionAsset datFile = (ItemDefinitionAsset)target;
            
            showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General Settings", true);
            if (showGeneralSettings)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("guid"));
                if (GUILayout.Button("Generate", GUILayout.Width(70)))
                {
                    datFile.GenerateGuid(true);
                    serializedObject.FindProperty("guid").stringValue = datFile.guid;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), new GUIContent("Item ID"));
            }
            
            showItemProperties = EditorGUILayout.Foldout(showItemProperties, "Item Properties", true);
            if (showItemProperties)
            {
                SerializedProperty itemTypeProp = serializedObject.FindProperty("itemType");
                HandleEnumFieldWithScroll(itemTypeProp, "Item Type", typeof(ItemType), 
                                         ref itemTypePopupHasFocus, ref itemTypeControlID);
                
                SerializedProperty rarityProp = serializedObject.FindProperty("rarity");
                HandleEnumFieldWithScroll(rarityProp, "Rarity", typeof(Rarity),
                                         ref rarityPopupHasFocus, ref rarityControlID);
                
                SerializedProperty useableProp = serializedObject.FindProperty("useable");
                HandleEnumFieldWithScroll(useableProp, "Useable", typeof(UseableType),
                                         ref useablePopupHasFocus, ref useableControlID);
                
                SerializedProperty slotProp = serializedObject.FindProperty("slot");
                HandleEnumFieldWithScroll(slotProp, "Slot", typeof(SlotType),
                                         ref slotPopupHasFocus, ref slotControlID);
                
                if (datFile.itemType == ItemType.Barricade && datFile.useable == UseableType.Barricade)
                {
                    SerializedProperty barricadeTypeProp = serializedObject.FindProperty("barricadeBuildType");
                    HandleEnumFieldWithScroll(barricadeTypeProp, "Barricade Build Type", typeof(BarricadeBuildType),
                                             ref barricadeTypePopupHasFocus, ref barricadeTypeControlID);
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("size_X"), new GUIContent("Size X"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("size_Y"), new GUIContent("Size Y"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("size_Z"), new GUIContent("Size Z"));
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
                
                for (int i = 0; i < datFile.properties.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(datFile.properties[i].name);
                    datFile.properties[i].value = EditorGUILayout.FloatField(datFile.properties[i].value);
                    
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        datFile.properties.RemoveAt(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (availableProperties.ContainsKey(datFile.itemType))
                {
                    List<string> existingPropertyNames = datFile.properties.Select(p => p.name).ToList();
                    string[] allProps = availableProperties[datFile.itemType];
                    List<string> availableProps = allProps.Where(p => !existingPropertyNames.Contains(p)).ToList();
                    
                    if (availableProps.Count > 0)
                    {
                        string[] displayProps = availableProps.ToArray();
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        if (!string.IsNullOrEmpty(selectedProperty) && !availableProps.Contains(selectedProperty))
                        {
                            selectedProperty = "";
                            propSelectedIndex = -1;
                        }
                        
                        EditorGUILayout.PrefixLabel("Add Property");
                        
                        Rect controlPosition = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
                        
                        int prevID = propControlID;
                        
                        if (propSelectedIndex < 0 || propSelectedIndex >= displayProps.Length)
                            propSelectedIndex = -1;
                        else
                            propSelectedIndex = Array.IndexOf(displayProps, selectedProperty);
                        
                        EditorGUIUtility.AddCursorRect(controlPosition, MouseCursor.Link);
                        
                        propSelectedIndex = EditorGUI.Popup(controlPosition, propSelectedIndex, displayProps);
                        
                        propControlID = GUIUtility.GetControlID(FocusType.Passive);
                        
                        bool isHovering = controlPosition.Contains(Event.current.mousePosition);
                        propPopupHasFocus = (GUIUtility.keyboardControl == propControlID || (prevID == propControlID && propPopupHasFocus) || isHovering);
                        
                        if ((propPopupHasFocus || isHovering) && Event.current.type == EventType.ScrollWheel && displayProps.Length > 0)
                        {
                            int direction = Event.current.delta.y > 0 ? 1 : -1;
                            propSelectedIndex = (propSelectedIndex + direction) % displayProps.Length;
                            if (propSelectedIndex < 0) propSelectedIndex = displayProps.Length - 1;
                            Event.current.Use();
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                        
                        if (propSelectedIndex >= 0 && propSelectedIndex < displayProps.Length)
                        {
                            selectedProperty = displayProps[propSelectedIndex];
                            if (GUILayout.Button("Add", GUILayout.Width(60)))
                            {
                                datFile.properties.Add(new PropertyValue { name = selectedProperty, value = 0 });
                                selectedProperty = "";
                                propSelectedIndex = -1;
                                GUIUtility.keyboardControl = 0;
                                propPopupHasFocus = false;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("All available properties for this item type have been added.", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No specific properties available for this item type.", MessageType.Info);
                }
            }
            
            showLocalization = EditorGUILayout.Foldout(showLocalization, "Localization", true);
            if (showLocalization)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nameEnglish"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("descriptionEnglish"));
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
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate Item.dat File"))
                {
                    GenerateItemFile(datFile);
                }
                if (GUILayout.Button("Generate English.dat File"))
                {
                    GenerateEnglishFile(datFile);
                }
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Generate All Files"))
                {
                    GenerateAllFiles(datFile);
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Files will be saved to the selected export folder. If no folder is selected, a file dialog will appear.", MessageType.Info);
            }
            
            if (GUILayout.Button("Test DAT Generation"))
            {
                datFile.Test();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void GenerateItemFile(ItemDefinitionAsset datFile)
        {
            string path = GetExportPath(datFile);
            if (!string.IsNullOrEmpty(path))
            {
                datFile.SaveItemDatFile(path);
            }
        }
        
        private void GenerateEnglishFile(ItemDefinitionAsset datFile)
        {
            string path = GetExportPath(datFile);
            if (!string.IsNullOrEmpty(path))
            {
                datFile.SaveEnglishLanguageFile(path);
            }
        }
        
        private void GenerateAllFiles(ItemDefinitionAsset datFile)
        {
            string path = GetExportPath(datFile);
            if (!string.IsNullOrEmpty(path))
            {
                datFile.SaveAllFiles(path);
            }
        }
        
        private string GetExportPath(ItemDefinitionAsset datFile)
        {
            string path = exportPath;
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFolderPanel("Select Export Folder", "", datFile.name);
                if (!string.IsNullOrEmpty(path))
                {
                    exportPath = path;
                }
            }
            return path;
        }

        private void HandleEnumFieldWithScroll(SerializedProperty enumProperty, string label, System.Type enumType, 
                                              ref bool hasFocus, ref int controlID)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            
            Rect controlPosition = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
            
            int prevID = controlID;
            
            string[] enumNames = System.Enum.GetNames(enumType);
            
            int currentValue = enumProperty.enumValueIndex;
            
            EditorGUIUtility.AddCursorRect(controlPosition, MouseCursor.Link);
            
            int newValue = EditorGUI.Popup(controlPosition, currentValue, enumNames);
            
            if (newValue != currentValue)
            {
                enumProperty.enumValueIndex = newValue;
            }
            
            controlID = GUIUtility.GetControlID(FocusType.Passive);
            
            bool isHovering = controlPosition.Contains(Event.current.mousePosition);
            hasFocus = (GUIUtility.keyboardControl == controlID || (prevID == controlID && hasFocus) || isHovering);
            
            if ((hasFocus || isHovering) && Event.current.type == EventType.ScrollWheel && enumNames.Length > 0)
            {
                int direction = Event.current.delta.y > 0 ? 1 : -1;
                newValue = (currentValue + direction) % enumNames.Length;
                if (newValue < 0) newValue = enumNames.Length - 1;
                
                enumProperty.enumValueIndex = newValue;
                Event.current.Use();
                Repaint();
                GUIUtility.ExitGUI();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    #endif
}