using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tools
{
    [Serializable]
    public class ObjectPropertyValue
    {
        public string name;
        public string value;
    }

    [CreateAssetMenu(fileName = "ObjectDatFile", menuName = "UMod SDK/Object Dats", order = 3)]
    public class ObjectDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        public string guid;

        [Header("Object Properties")]
        public EObjectType objectType;
        
        [Header("ID")]
        public int id;
        
        [Header("Localization")]
        public string nameEnglish;
        public string interactEnglish;

        [Header("Object Properties")]
        public List<ObjectPropertyValue> properties = new List<ObjectPropertyValue>();

        [Header("Custom Key-Value Pairs")]
        public List<KeyValEntry> datFileValues = new List<KeyValEntry>();
        
        private string[] newlines = new string[] { "\r\n", "\r", "\n" };

        private void OnEnable()
        {
            #if UNITY_EDITOR
            AssignDoNotMasterBundle();
            #endif
        }

        #if UNITY_EDITOR
        private void AssignDoNotMasterBundle()
        {
            AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(this));
            if (importer != null)
            {
                importer.assetBundleName = "DONOTMASTERBUNDLE";
            }
        }

        public void GenerateGuid(bool force = false)
        {
            if (string.IsNullOrEmpty(guid) || force)
            {
                System.Random random = new System.Random();
                byte[] buffer = new byte[16];
                random.NextBytes(buffer);
                guid = BitConverter.ToString(buffer).Replace("-", "").ToLower();
            }
        }
        #endif

        public void LoadFromDatFile(string datFile)
        {
            if (datFile == null)
            {
                Debug.LogWarning(".dat file null. Nothing done...");
                return;
            }
            
            string readyText = "";
            
            readyText = CleanText(File.ReadAllText(datFile));

            string[] lines = readyText.Split(newlines, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                string[] parts = line.Split();

                KeyValEntry keyValEntry = new KeyValEntry();
                if (parts.Length > 0)
                {
                    keyValEntry.key = parts[0];
                }
                else
                {
                    continue;
                }
                if (parts.Length > 1)
                {
                    keyValEntry.val = parts[1];
                }
                ProcessSpecialValues(keyValEntry);
                if (!SkipKey(keyValEntry.key))
                {
                    datFileValues.Add(keyValEntry);
                }
            }            
        }
        
        public bool SkipKey(string key) {
            if (key == null) return false;
            key = key.ToLower();
            if (key == "id") return true;
            if (key == "guid") return true;
            if (key == "type") return true;
            if (key == "name") return true;
            if (key == "interact") return true;
            return false;
        }

        public void ProcessSpecialValues(KeyValEntry keyValEntry)
        {
            string key = keyValEntry.key.Trim().ToLower();
            string val = keyValEntry.val.Trim();
            
            if (key == "guid")
            {
                guid = val;
            }
            else if (key == "type")
            {
                try {
                    objectType = (EObjectType)System.Enum.Parse(typeof(EObjectType), val, true);
                }
                catch {
                    Debug.LogWarning(val + " does not exist in EObjectType enum. Using Small.");
                    objectType = EObjectType.Small;
                }
            }
            else if (key == "id")
            {
                int.TryParse(val, out id);
            }
            else if (key == "name")
            {
                nameEnglish = val;
            }
            else if (key == "interact")
            {
                interactEnglish = val;
            }
            else 
            {
                SetProperty(FormatPropertyName(key), val);
            }
        }
        
        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            string[] lines = text.Split(newlines, StringSplitOptions.None);
            List<string> cleanedLines = new List<string>();
            
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                    continue;
                    
                
            int commentIndex = trimmedLine.IndexOf("//");
                if (commentIndex > 0)
                {
                    trimmedLine = trimmedLine.Substring(0, commentIndex).Trim();
                }
                
                cleanedLines.Add(trimmedLine);
            }
            
            return string.Join("\n", cleanedLines);
        }
        
        public ObjectPropertyValue FindProperty(string propertyName)
        {
            return properties.FirstOrDefault(prop => prop.name == propertyName);
        }
        
        public bool HasProperty(string propertyName)
        {
            return FindProperty(propertyName) != null;
        }
        
        public bool GetBoolProperty(string propertyName, bool defaultValue = false)
        {
            ObjectPropertyValue prop = FindProperty(propertyName);
            
            if (prop != null)
            {
                bool result;
                if (bool.TryParse(prop.value, out result))
                {
                    return result;
                }
            }
            
            return defaultValue;
        }
        
        public int GetIntProperty(string propertyName, int defaultValue = 0)
        {
            ObjectPropertyValue prop = FindProperty(propertyName);
            
            if (prop != null)
            {
                int result;
                if (int.TryParse(prop.value, out result))
                {
                    return result;
                }
            }
            
            return defaultValue;
        }
        
        public float GetFloatProperty(string propertyName, float defaultValue = 0f)
        {
            ObjectPropertyValue prop = FindProperty(propertyName);
            
            if (prop != null)
            {
                float result;
                if (float.TryParse(prop.value, out result))
                {
                    return result;
                }
            }
            
            return defaultValue;
        }
        
        public string GetStringProperty(string propertyName, string defaultValue = "")
        {
            ObjectPropertyValue prop = FindProperty(propertyName);
            
            if (prop != null)
            {
                return prop.value;
            }
            
            return defaultValue;
        }
        
        public void SetProperty(string propertyName, string value)
        {
            ObjectPropertyValue existingProp = FindProperty(propertyName);
            
            if (existingProp != null)
            {
                existingProp.value = value;
            }
            else
            {
                properties.Add(new ObjectPropertyValue { name = propertyName, value = value });
            }
        }
        
        private string FormatPropertyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
                
            string[] parts = name.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1).ToLower() : "");
                }
            }
            
            return string.Join("_", parts);
        }

        public void Clear()
        {
            datFileValues.Clear();
            id = 0;
            objectType = EObjectType.Small;
            nameEnglish = null;
            interactEnglish = null;
            properties.Clear();
        }

        public void Test()
        {  
            Debug.Log(GetText());
        }

        public string GetText()
        {
            string text = "";

            text += "GUID " + guid + "\n";
            text += "Type " + objectType.ToString() + "\n";
            text += "ID " + id.ToString() + "\n";
            
            foreach (ObjectPropertyValue prop in properties)
            {
                text += prop.name + " " + prop.value + "\n";
            }
            
            foreach (KeyValEntry keyValEntry in datFileValues)
            {
                if (keyValEntry.key.ToLower() == "type") continue;
                if (keyValEntry.key.ToLower() == "guid") continue;
                if (keyValEntry.key.ToLower() == "id") continue;
                
                text += keyValEntry.key + " " + keyValEntry.val + "\n";
            }
            
            return text;
        }
        
        public string GetEnglishText()
        {
            string text = "";
            
            if (!string.IsNullOrEmpty(nameEnglish))
            {
                text += "Name " + nameEnglish + "\n";
            }
            
            if (!string.IsNullOrEmpty(interactEnglish))
            {
                text += "Interact " + interactEnglish + "\n";
            }
            
            for (int i = 0; i < properties.Count; i++)
            {
                ObjectPropertyValue prop = properties[i];
                if (prop.name.StartsWith("Interactability_Text_Line_"))
                {
                    string lineNumber = prop.name.Substring("Interactability_Text_Line_".Length);
                    text += "Line_" + lineNumber + " " + prop.value + "\n";
                }
            }
            
            return text;
        }

        #if UNITY_EDITOR
        public void SaveObjectDatFile(string folderPath)
        {
            string objectText = GetText();
            string assetPath = AssetDatabase.GetAssetPath(this);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            Debug.Log($"[ObjectDef Save] Asset Path: {assetPath}");
            Debug.Log($"[ObjectDef Save] Derived Filename (no ext): {fileName}");

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            string filePath = Path.Combine(folderPath, fileName + ".dat");
            Debug.Log($"[ObjectDef Save] Final FilePath: {filePath}");
            File.WriteAllText(filePath, objectText);
            
            Debug.Log("Object DAT file saved to: " + filePath);
            AssetDatabase.Refresh();
        }
        
        public void SaveEnglishLanguageFile(string folderPath)
        {
            string englishText = GetEnglishText();
            
            if (string.IsNullOrEmpty(englishText))
            {
                Debug.LogWarning("No English text to save.");
                return;
            }
                
            string filePath = Path.Combine(folderPath, "English.dat");
            File.WriteAllText(filePath, englishText);
            
            Debug.Log("English language file saved to: " + filePath);
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
                SaveObjectDatFile(folderPath);
                
                SaveEnglishLanguageFile(folderPath);
                
                string assetPath = AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string fileName = Path.GetFileName(assetPath);
                    string targetPath = Path.Combine(folderPath, fileName);
                    
                    string fullPath = Path.GetFullPath(assetPath);
                    
                    if (File.Exists(fullPath))
                    {
                        File.Copy(fullPath, targetPath, true);
                        Debug.Log($"Copied object asset file from {fullPath} to {targetPath}");
                    }
                    else
                    {
                        string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6); 
                        string fullProjectPath = Path.Combine(projectPath, assetPath);
                        
                        if (File.Exists(fullProjectPath))
                        {
                            File.Copy(fullProjectPath, targetPath, true);
                            Debug.Log($"Copied object asset file from {fullProjectPath} to {targetPath}");
                        }
                        else
                        {
                            Debug.LogWarning($"Could not find object asset file at {fullPath} or {fullProjectPath}");
                        }
                    }
                }
                
                Debug.Log($"All object files saved to: {folderPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving object files: {e.Message}\n{e.StackTrace}");
            }
        }
        #endif
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(ObjectDefinitionAsset))]
    public class ObjectAssetEditor : Editor
    {
        private bool showGeneralSettings = true;
        private bool showObjectProperties = true;
        private bool showLocalization = true;
        private bool showExportOptions = true;
        private string exportPath = "";
        private string selectedProperty = "";
        
        private bool propPopupHasFocus = false;
        private int propSelectedIndex = -1;
        private int propControlID = -1;
        
        private Dictionary<string, string[]> availableProperties = new Dictionary<string, string[]>()
        {
            {"Basic", new string[] {
                "Add_Kill_Triggers", 
                "Add_Night_Light_Script", 
                "Allow_Structures", 
                "Causes_Fall_Damage", 
                "Chart", 
                "Christmas_Redirect", 
                "Collision_Important", 
                "Exclude_From_Level_Batching", 
                "Foliage", 
                "Fuel", 
                "Halloween_Redirect", 
                "Has_Clip_Prefab", 
                "Holiday_Restriction", 
                "Is_Gore", 
                "Landmark_Quality", 
                "Load_Nav_On_Server", 
                "Load_Nav_In_Editor", 
                "Material_Palette", 
                "Refill", 
                "Snowshoe", 
                "Soft", 
                "Use_Water_Height_Transparent_Sort", 
                "Exclude_From_Satellite_Capture"
            }},
            {"Decals", new string[] {
                "Decal_Alpha", 
                "Decal_X", 
                "Decal_Y", 
                "Decal_LOD_Bias"
            }},
            {"Interior Culling", new string[] {
                "Exclude_From_Culling_Volumes", 
                "LOD", 
                "LOD_Bias", 
                "LOD_Center_X", 
                "LOD_Center_Y", 
                "LOD_Center_Z", 
                "LOD_Size_X", 
                "LOD_Size_Y", 
                "LOD_Size_Z"
            }},
            {"Interactable", new string[] {
                "Interactability", 
                "Interactability_Animation_Component_Path", 
                "Interactability_Blade_ID", 
                "Interactability_Delay", 
                "Interactability_Dialogue", 
                "Interactability_Drops", 
                "Interactability_Editor", 
                "Interactability_Effect", 
                "Interactability_Finale", 
                "Interactability_Health", 
                "Interactability_Hint", 
                "Interactability_Invulnerable", 
                "Interactability_Nav", 
                "Interactability_Power", 
                "Interactability_Proof_Explosion", 
                "Interactability_Remote", 
                "Interactability_Reset", 
                "Interactability_Resource", 
                "Interactability_Reward_ID", 
                "Interactability_Rewards_Min", 
                "Interactability_Rewards_Max", 
                "Interactability_Reward_Probability", 
                "Interactability_Reward_XP", 
                "Interactability_Text_Lines"
            }},
            {"Rubble", new string[] {
                "Rubble", 
                "Rubble_All_Sections_Destroyed_Alert_Radius", 
                "Rubble_Blade_ID", 
                "Rubble_Can_Zombies_Damage", 
                "Rubble_Editor", 
                "Rubble_Effect", 
                "Rubble_Finale", 
                "Rubble_Health", 
                "Rubble_Invulnerable", 
                "Rubble_Nav_Mode", 
                "Rubble_Proof_Explosion", 
                "Rubble_Reset", 
                "Rubble_Reward_ID", 
                "Rubble_Rewards_Min", 
                "Rubble_Rewards_Max", 
                "Rubble_Reward_Probability", 
                "Rubble_Reward_XP", 
                "Rubble_Section_Destroyed_Alert_Radius", 
                "Rubble_Zombie_Damage_Multiplier"
            }},
            {"Conditions", new string[] {
                "Conditions", 
                "Interactability_Conditions", 
                "Interactability_Reward"
            }}
        };

        public override void OnInspectorGUI()
        {
            ObjectDefinitionAsset objectDat = (ObjectDefinitionAsset)target;
            
            showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General Settings", true);
            if (showGeneralSettings)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("guid"));
                if (GUILayout.Button("Generate", GUILayout.Width(70)))
                {
                    objectDat.GenerateGuid(true);
                    serializedObject.FindProperty("guid").stringValue = objectDat.guid;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), new GUIContent("Object ID"));
            }
            
            showObjectProperties = EditorGUILayout.Foldout(showObjectProperties, "Object Properties", true);
            if (showObjectProperties)
            {
                EditorGUI.BeginChangeCheck();
                EObjectType selectedObjectType = (EObjectType)EditorGUILayout.EnumPopup("Object Type", objectDat.objectType);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(objectDat, "Change Object Type");
                    objectDat.objectType = selectedObjectType;
                    EditorUtility.SetDirty(objectDat);
                }
                
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Current Properties", EditorStyles.boldLabel);
                
                for (int i = 0; i < objectDat.properties.Count; i++)
                {
                    ObjectPropertyValue prop = objectDat.properties[i];
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(prop.name, GUILayout.Width(200));
                    
                    string newValue = EditorGUILayout.TextField(prop.value);
                    if (newValue != prop.value)
                    {
                        prop.value = newValue;
                    }
                    
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        objectDat.properties.RemoveAt(i);
                        i--;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space();
                
                Dictionary<string, List<string>> categorizedProps = new Dictionary<string, List<string>>();
                foreach (var category in availableProperties)
                {
                    List<string> availableCategoryProps = new List<string>();
                    foreach (var prop in category.Value)
                    {
                        if (!objectDat.HasProperty(prop))
                        {
                            availableCategoryProps.Add(prop);
                        }
                    }
                    
                    if (availableCategoryProps.Count > 0)
                    {
                        categorizedProps[category.Key] = availableCategoryProps;
                    }
                }
                
                ObjectPropertyValue interactabilityProp = objectDat.FindProperty("Interactability");
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Interactability Settings", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Interactability", GUILayout.Width(150));
                
                string[] interactabilityOptions = new string[] {
                    "None", "Binary_State", "Dropper", "Note", "Water", "Fuel", 
                    "Rubble", "NPC", "Quest", "Dialogue"
                };
                
                string currentInteractability = interactabilityProp != null ? interactabilityProp.value : "None";
                int currentIndex = Array.IndexOf(interactabilityOptions, currentInteractability);
                if (currentIndex < 0) currentIndex = 0;
                
                int newIndex = EditorGUILayout.Popup(currentIndex, interactabilityOptions);
                if (newIndex != currentIndex)
                {
                    if (newIndex == 0 && interactabilityProp != null)
                    {
                        int index = objectDat.properties.FindIndex(p => p.name == "Interactability");
                        if (index >= 0)
                        {
                            objectDat.properties.RemoveAt(index);
                        }
                        
                        for (int i = objectDat.properties.Count - 1; i >= 0; i--)
                        {
                            if (objectDat.properties[i].name.StartsWith("Interactability_"))
                            {
                                objectDat.properties.RemoveAt(i);
                            }
                        }
                        
                        EditorUtility.SetDirty(objectDat);
                    }
                    else
                    {
                        objectDat.SetProperty("Interactability", interactabilityOptions[newIndex]);
                        EditorUtility.SetDirty(objectDat);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                ObjectPropertyValue textLinesProperty = objectDat.FindProperty("Interactability_Text_Lines");
                if (textLinesProperty != null)
                {
                    int textLines;
                    if (int.TryParse(textLinesProperty.value, out textLines))
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Note Text Lines", EditorStyles.boldLabel);
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Number of Lines", GUILayout.Width(150));
                        int newLineCount = EditorGUILayout.IntField(textLines);
                        if (newLineCount != textLines && newLineCount >= 0)
                        {
                            textLinesProperty.value = newLineCount.ToString();
                            if (newLineCount > textLines)
                            {
                                for (int i = textLines; i < newLineCount; i++)
                                {
                                    string propName = "Interactability_Text_Line_" + i;
                                    if (!objectDat.HasProperty(propName))
                                    {
                                        objectDat.SetProperty(propName, "");
                                    }
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        for (int i = 0; i < textLines; i++)
                        {
                            string propName = "Interactability_Text_Line_" + i;
                            ObjectPropertyValue prop = objectDat.FindProperty(propName);
                            
                            string value = prop != null ? prop.value : "";
                            
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Line " + i, GUILayout.Width(150));
                            string newValue = EditorGUILayout.TextField(value);
                            if (newValue != value)
                            {
                                objectDat.SetProperty(propName, newValue);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                
                if (true)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Add Property", EditorStyles.boldLabel);
                    
                    Dictionary<string, List<string>> filteredProps = new Dictionary<string, List<string>>();
                    
                    string interactabilityType = interactabilityProp != null ? interactabilityProp.value : "None";
                    
                    if (categorizedProps.ContainsKey("Basic"))
                    {
                        List<string> relevantBasicProps = new List<string>();
                        
                        foreach (var prop in categorizedProps["Basic"])
                        {
                            bool isRelevant = true;
                            
                            if (interactabilityType != "None")
                            {
                                if (interactabilityType == "Dropper" || 
                                    interactabilityType == "Water" || 
                                    interactabilityType == "Fuel")
                                {
                                    if (prop == "Snowshoe" || 
                                        prop == "Add_Kill_Triggers" || 
                                        prop == "Causes_Fall_Damage" ||
                                        prop == "Foliage")
                                    {
                                        isRelevant = false;
                                    }
                                }
                                
                                if (interactabilityType == "Binary_State" || 
                                    interactabilityType == "Note" || 
                                    interactabilityType == "NPC" || 
                                    interactabilityType == "Dialogue")
                                {
                                    if (prop == "Fuel" || prop == "Refill")
                                    {
                                        isRelevant = false;
                                    }
                                }
                            }
                            
                            if (isRelevant)
                            {
                                relevantBasicProps.Add(prop);
                            }
                        }
                        
                        if (relevantBasicProps.Count > 0)
                        {
                            filteredProps["Basic"] = relevantBasicProps;
                        }
                    }
                    
                    switch (objectDat.objectType)
                    {
                        case EObjectType.Small:
                        case EObjectType.Medium:
                        case EObjectType.Large:
                            if (interactabilityType == "None" || 
                                interactabilityType == "Binary_State" || 
                                interactabilityType == "Rubble")
                            {
                                if (categorizedProps.ContainsKey("Interior Culling"))
                                    filteredProps["Interior Culling"] = new List<string>(categorizedProps["Interior Culling"]);
                            }
                            
                            if (interactabilityType == "None" || 
                                interactabilityType == "Binary_State")
                            {
                                if (categorizedProps.ContainsKey("Decals"))
                                    filteredProps["Decals"] = new List<string>(categorizedProps["Decals"]);
                            }
                            
                            if (interactabilityType == "None" || interactabilityType == "Rubble")
                            {
                                if (categorizedProps.ContainsKey("Rubble"))
                                    filteredProps["Rubble"] = new List<string>(categorizedProps["Rubble"]);
                            }
                            
                            if (categorizedProps.ContainsKey("Conditions") && interactabilityType != "None")
                                filteredProps["Conditions"] = new List<string>(categorizedProps["Conditions"]);
                            break;
                            
                        case EObjectType.Decal:
                            if (categorizedProps.ContainsKey("Decals"))
                                filteredProps["Decals"] = new List<string>(categorizedProps["Decals"]);
                            break;
                            
                        case EObjectType.NPC:
                            if (categorizedProps.ContainsKey("Conditions"))
                                filteredProps["Conditions"] = new List<string>(categorizedProps["Conditions"]);
                            break;
                    }
                    
                    if (interactabilityProp != null && interactabilityProp.value != "None")
                    {
                        string interactType = interactabilityProp.value;
                        List<string> interactabilityProps = new List<string>();
                        
                        if (!objectDat.HasProperty("Interactability_Delay"))
                            interactabilityProps.Add("Interactability_Delay");
                        if (!objectDat.HasProperty("Interactability_Effect"))
                            interactabilityProps.Add("Interactability_Effect");
                        if (!objectDat.HasProperty("Interactability_Hint"))
                            interactabilityProps.Add("Interactability_Hint");
                        if (!objectDat.HasProperty("Interactability_Remote"))
                            interactabilityProps.Add("Interactability_Remote");
                        
                        switch (interactType)
                        {
                            case "Binary_State":
                                if (!objectDat.HasProperty("Interactability_Animation_Component_Path"))
                                    interactabilityProps.Add("Interactability_Animation_Component_Path");
                                if (!objectDat.HasProperty("Interactability_Nav"))
                                    interactabilityProps.Add("Interactability_Nav");
                                if (!objectDat.HasProperty("Interactability_Power"))
                                    interactabilityProps.Add("Interactability_Power");
                                break;
                            case "Dropper":
                                if (!objectDat.HasProperty("Interactability_Drops"))
                                    interactabilityProps.Add("Interactability_Drops");
                                if (!objectDat.HasProperty("Interactability_Drop_0"))
                                    interactabilityProps.Add("Interactability_Drop_0");
                                if (!objectDat.HasProperty("Interactability_Drop_1"))
                                    interactabilityProps.Add("Interactability_Drop_1");
                                if (!objectDat.HasProperty("Interactability_Drop_2"))
                                    interactabilityProps.Add("Interactability_Drop_2");
                                if (!objectDat.HasProperty("Interactability_Reset"))
                                    interactabilityProps.Add("Interactability_Reset");
                                break;
                            case "Note":
                                if (!objectDat.HasProperty("Interactability_Text_Lines"))
                                    interactabilityProps.Add("Interactability_Text_Lines");
                                break;
                            case "Water":
                            case "Fuel":
                                if (!objectDat.HasProperty("Interactability_Resource"))
                                    interactabilityProps.Add("Interactability_Resource");
                                break;
                            case "Rubble":
                                if (!objectDat.HasProperty("Interactability_Blade_ID"))
                                    interactabilityProps.Add("Interactability_Blade_ID");
                                if (!objectDat.HasProperty("Interactability_Health"))
                                    interactabilityProps.Add("Interactability_Health");
                                if (!objectDat.HasProperty("Interactability_Invulnerable"))
                                    interactabilityProps.Add("Interactability_Invulnerable");
                                if (!objectDat.HasProperty("Interactability_Proof_Explosion"))
                                    interactabilityProps.Add("Interactability_Proof_Explosion");
                                if (!objectDat.HasProperty("Interactability_Reset"))
                                    interactabilityProps.Add("Interactability_Reset");
                                if (!objectDat.HasProperty("Interactability_Reward_ID"))
                                    interactabilityProps.Add("Interactability_Reward_ID");
                                if (!objectDat.HasProperty("Interactability_Rewards_Min"))
                                    interactabilityProps.Add("Interactability_Rewards_Min");
                                if (!objectDat.HasProperty("Interactability_Rewards_Max"))
                                    interactabilityProps.Add("Interactability_Rewards_Max");
                                if (!objectDat.HasProperty("Interactability_Reward_Probability"))
                                    interactabilityProps.Add("Interactability_Reward_Probability");
                                if (!objectDat.HasProperty("Interactability_Reward_XP"))
                                    interactabilityProps.Add("Interactability_Reward_XP");
                                break;
                            case "NPC":
                            case "Dialogue":
                                if (!objectDat.HasProperty("Interactability_Dialogue"))
                                    interactabilityProps.Add("Interactability_Dialogue");
                                break;
                        }
                        
                        if (interactabilityProps.Count > 0)
                        {
                            filteredProps[$"{interactType} Properties"] = interactabilityProps;
                        }
                    }
                    
                    List<string> displayProps = new List<string>();
                    foreach (var category in filteredProps)
                    {
                        if (category.Value.Count > 0)
                        {
                            displayProps.Add($"--- {category.Key} ---");
                            
                            foreach (var prop in category.Value)
                            {
                                displayProps.Add(prop);
                            }
                        }
                    }
                    
                    if (displayProps.Count > 0)
                    {
                        string[] displayPropsArray = displayProps.ToArray();
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        if (!string.IsNullOrEmpty(selectedProperty) && !displayProps.Contains(selectedProperty))
                        {
                            selectedProperty = "";
                            propSelectedIndex = -1;
                        }
                        
                        EditorGUILayout.PrefixLabel("Select Property");
                        
                        Rect controlPosition = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
                        
                        int prevID = propControlID;
                        
                        if (propSelectedIndex < 0 || propSelectedIndex >= displayPropsArray.Length)
                            propSelectedIndex = -1;
                        else
                            propSelectedIndex = Array.IndexOf(displayPropsArray, selectedProperty);
                        
                        EditorGUIUtility.AddCursorRect(controlPosition, MouseCursor.Link);
                        
                        propSelectedIndex = EditorGUI.Popup(controlPosition, propSelectedIndex, displayPropsArray);
                        
                        propControlID = GUIUtility.GetControlID(FocusType.Passive);
                        
                        bool isHovering = controlPosition.Contains(Event.current.mousePosition);
                        propPopupHasFocus = (GUIUtility.keyboardControl == propControlID || (prevID == propControlID && propPopupHasFocus) || isHovering);
                        
                        bool isSelectedItemCategory = propSelectedIndex >= 0 && 
                                                   propSelectedIndex < displayPropsArray.Length && 
                                                   displayPropsArray[propSelectedIndex].StartsWith("--- ");
                        
                        if (Event.current.type == EventType.ScrollWheel && controlPosition.Contains(Event.current.mousePosition) && displayPropsArray.Length > 0)
                        {
                            int direction = Event.current.delta.y > 0 ? 1 : -1;
                            
                            if (propSelectedIndex < 0)
                                propSelectedIndex = direction > 0 ? 0 : displayPropsArray.Length - 1;
                            
                            do {
                                propSelectedIndex = (propSelectedIndex + direction) % displayPropsArray.Length;
                                if (propSelectedIndex < 0) propSelectedIndex = displayPropsArray.Length - 1;
                            } while (displayPropsArray[propSelectedIndex].StartsWith("--- "));
                            
                            selectedProperty = displayPropsArray[propSelectedIndex];
                            
                            propPopupHasFocus = true;
                            GUIUtility.keyboardControl = propControlID;
                            
                            Event.current.Use();
                            Repaint();
                        }
                        
                        if (propSelectedIndex >= 0 && propSelectedIndex < displayPropsArray.Length && !isSelectedItemCategory)
                        {
                            selectedProperty = displayPropsArray[propSelectedIndex];
                            if (GUILayout.Button("Add", GUILayout.Width(60)))
                            {
                                objectDat.SetProperty(selectedProperty, "");
                                selectedProperty = "";
                                propSelectedIndex = -1;
                                GUIUtility.keyboardControl = 0;
                                propPopupHasFocus = false;
                                
                                EditorUtility.SetDirty(objectDat);
                                serializedObject.Update();
                                Repaint();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("All available properties for the current configuration have been added.", MessageType.Info);
                    }
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Custom Property", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                selectedProperty = EditorGUILayout.TextField("Property Name", selectedProperty);
                
                if (GUILayout.Button("Add Property", GUILayout.Width(100)))
                {
                    if (!string.IsNullOrEmpty(selectedProperty) && !objectDat.HasProperty(selectedProperty))
                    {
                        objectDat.SetProperty(selectedProperty, "");
                        selectedProperty = "";
                        
                        EditorUtility.SetDirty(objectDat);
                        serializedObject.Update();
                        Repaint();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            showLocalization = EditorGUILayout.Foldout(showLocalization, "Localization", true);
            if (showLocalization)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nameEnglish"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("interactEnglish"));
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
                if (GUILayout.Button("Generate Object.dat File"))
                {
                    GenerateObjectFile(objectDat);
                }
                if (GUILayout.Button("Generate English.dat File"))
                {
                    GenerateEnglishFile(objectDat);
                }
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Generate All Files"))
                {
                    GenerateAllFiles(objectDat);
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Files will be saved to the selected export folder. If no folder is selected, a file dialog will appear.", MessageType.Info);
            }
            
            if (GUILayout.Button("Test DAT Generation"))
            {
                objectDat.Test();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void GenerateObjectFile(ObjectDefinitionAsset objectDat)
        {
            string path = GetExportPath(objectDat);
            if (!string.IsNullOrEmpty(path))
            {
                objectDat.SaveObjectDatFile(path);
            }
        }
        
        private void GenerateEnglishFile(ObjectDefinitionAsset objectDat)
        {
            string path = GetExportPath(objectDat);
            if (!string.IsNullOrEmpty(path))
            {
                objectDat.SaveEnglishLanguageFile(path);
            }
        }
        
        private void GenerateAllFiles(ObjectDefinitionAsset objectDat)
        {
            string path = GetExportPath(objectDat);
            if (!string.IsNullOrEmpty(path))
            {
                objectDat.SaveAllFiles(path);
            }
        }
        
        private string GetExportPath(ObjectDefinitionAsset objectDat)
        {
            string path = exportPath;
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFolderPanel("Select Export Folder", "", objectDat.name);
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
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent(label), enumProperty);
            
            rect = EditorGUI.PrefixLabel(rect, new GUIContent(label));
            
            int id = GUIUtility.GetControlID(FocusType.Passive);
            
            if (controlID != id)
            {
                hasFocus = false;
                controlID = id;
            }
            
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                hasFocus = true;
                GUIUtility.keyboardControl = controlID;
                Event.current.Use();
            }
            
            string[] names = System.Enum.GetNames(enumType);
            Array values = System.Enum.GetValues(enumType);
            
            int currentIndex = -1;
            for (int i = 0; i < values.Length; i++)
            {
                if ((int)values.GetValue(i) == enumProperty.intValue)
                {
                    currentIndex = i;
                    break;
                }
            }
            
            string currentName = currentIndex >= 0 ? names[currentIndex] : "";
            
            if (EditorGUI.DropdownButton(rect, new GUIContent(currentName), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                
                for (int i = 0; i < names.Length; i++)
                {
                    int index = i;
                    bool isSelected = index == currentIndex;
                    
                    menu.AddItem(new GUIContent(names[index]), isSelected, () => {
                        enumProperty.intValue = (int)values.GetValue(index);
                        
                        EditorUtility.SetDirty(enumProperty.serializedObject.targetObject);
                        enumProperty.serializedObject.ApplyModifiedProperties();
                        GUI.changed = true;
                    });
                }
                
                menu.DropDown(rect);
                hasFocus = false;
            }
            
            if (hasFocus && Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    if (currentIndex > 0)
                    {
                        enumProperty.intValue = (int)values.GetValue(currentIndex - 1);
                        
                        EditorUtility.SetDirty(enumProperty.serializedObject.targetObject);
                        enumProperty.serializedObject.ApplyModifiedProperties();
                        GUI.changed = true;
                    }
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    if (currentIndex < names.Length - 1)
                    {
                        enumProperty.intValue = (int)values.GetValue(currentIndex + 1);
                        
                        EditorUtility.SetDirty(enumProperty.serializedObject.targetObject);
                        enumProperty.serializedObject.ApplyModifiedProperties();
                        GUI.changed = true;
                    }
                    Event.current.Use();
                }
            }
            
            EditorGUI.EndProperty();
        }
    }
    #endif
} 