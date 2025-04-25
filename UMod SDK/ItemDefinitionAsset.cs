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
    [CreateAssetMenu(fileName = "ItemDatFile", menuName = "UMod SDK/Item Dats", order = 1)]
    public class ItemDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        public string guid; 

        [Header("Item Properties")]
        public ItemType itemType;
        public Rarity rarity = Rarity.Common;
        public UseableType useable = UseableType.None;
        public SlotType slot = SlotType.None;
        
        [Header("Barricade Properties")]
        [Tooltip("Only visible when Item Type is Barricade and Useable is Barricade")]
        public BarricadeBuildType barricadeBuildType = BarricadeBuildType.None;
        
        [Header("ID")]
        public int id;
        
        [Header("Size")]
        public int size_X = 1;
        public int size_Y = 1;
        public float size_Z = -1;
        
        [Header("Localization")]
        public string nameEnglish;
        public string descriptionEnglish;

        [Header("Properties")]
        public List<PropertyValue> properties = new List<PropertyValue>();

        [Header("Custom Key-Value Pairs")]
        public List<KeyValEntry> datFileValues = new List<KeyValEntry>();
        
        [Header("Crafting Recipes")]
        [Tooltip("Add as many crafting recipes as needed for this item")]
        public List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>();
        
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
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                if (IsInDatGeneratorFolder(assetPath))
                {
                    AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                    if (importer != null)
                    {
                        importer.assetBundleName = "DONOTMASTERBUNDLE";
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
        
        public static bool IsInDatGeneratorFolder(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) && assetPath.Contains("DatGenerator");
        }
        #endif

        private void Awake()
        {
            GenerateGuid();
        }

        #if UNITY_EDITOR
        public void AddNewRecipe()
        {
            if (craftingRecipes == null)
                craftingRecipes = new List<CraftingRecipe>();
                
            craftingRecipes.Add(new CraftingRecipe());
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void RemoveRecipe(int index)
        {
            if (craftingRecipes != null && index >= 0 && index < craftingRecipes.Count)
            {
                craftingRecipes.RemoveAt(index);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        
        public void AddIngredient(int recipeIndex)
        {
            if (craftingRecipes != null && recipeIndex >= 0 && recipeIndex < craftingRecipes.Count)
            {
                craftingRecipes[recipeIndex].ingredients.Add(new Supply());
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        
        public void AddOutput(int recipeIndex)
        {
            if (craftingRecipes != null && recipeIndex >= 0 && recipeIndex < craftingRecipes.Count)
            {
                craftingRecipes[recipeIndex].outputs.Add(new Output());
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        
        public void AddDirective(int recipeIndex)
        {
            if (craftingRecipes != null && recipeIndex >= 0 && recipeIndex < craftingRecipes.Count)
            {
                craftingRecipes[recipeIndex].extraDirectives.Add(new KeyValEntry());
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        #endif

        public string CleanText(string text)
        {
            string newText = "";
            string[] lines = text.Trim().Split(newlines, StringSplitOptions.None);

            foreach (string line in lines)
            {
                string cleanedLine = line.Trim();
                string[] cleanedValues = cleanedLine.Split();
                List<string> cleanedValuesList = new List<string>();
                foreach (string cleanedValue in cleanedValues)
                {
                    if (string.IsNullOrEmpty(cleanedValue)) continue;
                    cleanedValuesList.Add(cleanedValue);
                }
                string correctedLine = String.Join(" ", cleanedValuesList.ToArray());
                newText += correctedLine + "\n";
            }
            return newText;
        }

        public void AddFromTextFile(string datFile = null)
        {
            string readyText = "";

            if (datFile == null)
            {
                Debug.LogWarning(".dat file null. Nothing done...");
                return;
            }
            else
            {
                readyText = CleanText(File.ReadAllText(datFile));
            }

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
            if (key == "rarity") return true;
            if (key == "useable") return true;
            if (key == "slot") return true;
            if (key == "size_x") return true;
            if (key == "size_y") return true;
            if (key == "size_z") return true;
            if (key == "name") return true;
            if (key == "description") return true;
            if (key == "build") return true;
            return false;
        }

        public void ProcessSpecialValues(KeyValEntry keyValEntry)
        {
            string key = keyValEntry.key.Trim().ToLower();
            string val = keyValEntry.val.Trim();
            
            if (key == "id")
            {
                int.TryParse(val, out id);
            }
            else if (key == "rarity")
            {
                try {
                    rarity = (Rarity)System.Enum.Parse(typeof(Rarity), val);
                }
                catch {
                    Debug.LogWarning(val + " does not exist in Rarity enum. Using Common.");
                    rarity = Rarity.Common;
                }
            }
            else if (key == "type")
            {
                int typeVal;
                if (int.TryParse(val, out typeVal))
                {
                    if (System.Enum.IsDefined(typeof(ItemType), typeVal))
                    {
                        itemType = (ItemType)typeVal;
                    }
                    else
                    {
                        Debug.LogWarning(typeVal + " does not exist in ItemType enum. Using None.");
                        itemType = ItemType.None;
                    }
                }
                else
                {
                    try {
                        itemType = (ItemType)System.Enum.Parse(typeof(ItemType), val);
                    }
                    catch {
                        Debug.LogWarning(val + " does not exist in ItemType enum. Using None.");
                        itemType = ItemType.None;
                    }
                }
            }
            else if (key == "useable")
            {
                int useableVal;
                if (int.TryParse(val, out useableVal))
                {
                    useable = (UseableType)useableVal;
                }
                else
                {
                    try {
                        useable = (UseableType)System.Enum.Parse(typeof(UseableType), val);
                    }
                    catch {
                        Debug.LogWarning(val + " does not exist in UseableType enum. Using None.");
                        useable = UseableType.None;
                    }
                }
            }
            else if (key == "slot")
            {
                int slotVal;
                if (int.TryParse(val, out slotVal))
                {
                    slot = (SlotType)slotVal;
                }
                else
                {
                    try {
                        slot = (SlotType)System.Enum.Parse(typeof(SlotType), val);
                    }
                    catch {
                        Debug.LogWarning(val + " does not exist in SlotType enum. Using None.");
                        slot = SlotType.None;
                    }
                }
            }
            else if (key == "build" && itemType == ItemType.Barricade && useable == UseableType.Barricade)
            {
                try {
                    barricadeBuildType = (BarricadeBuildType)System.Enum.Parse(typeof(BarricadeBuildType), val, true);
                }
                catch {
                    Debug.LogWarning(val + " does not exist in BarricadeBuildType enum. Using None.");
                    barricadeBuildType = BarricadeBuildType.None;
                }
            }
            else if (itemType == ItemType.Barricade && useable == UseableType.Barricade)
            {
                ParseBarricadeProperty(key, val);
            }
            else if (key == "size_x")
            {
                int.TryParse(val, out size_X);
            }
            else if (key == "size_y")
            {
                int.TryParse(val, out size_Y);
            }
            else if (key == "size_z")
            {
                float.TryParse(val, out size_Z);
            }
        }

        public void ParseBarricadeProperty(string key, string val)
        {
            key = key.Trim();
            val = val.Trim();
            
            if (key.Equals("locked", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("proof_explosion", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("unpickupable", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("unrepairable", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("unsalvageable", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("unsaveable", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("use_water_height_transparent_sort", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("vulnerable", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("exchange_with_target_item", StringComparison.OrdinalIgnoreCase))
            {
                AddOrUpdateProperty(key, 1f);
                return;
            }
            
            if (key.Equals("allow_collision_while_animating", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("allow_placement_inside_clip_volumes", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("allow_placement_on_vehicle", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("bypass_claim", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("bypass_pickup_ownership", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("can_be_damaged", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("canvehiclehookwhileattached", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("can_zombies_target", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("eligible_for_pooling", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("has_clip_prefab", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("enable_participant_scaling", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("contains_bonus_items", StringComparison.OrdinalIgnoreCase))
            {
                float value = val.Equals("true", StringComparison.OrdinalIgnoreCase) ? 1f : 0f;
                AddOrUpdateProperty(key, value);
                return;
            }
            
            if (key.Equals("armor_tier", StringComparison.OrdinalIgnoreCase))
            {
                float value = val.Equals("high", StringComparison.OrdinalIgnoreCase) ? 1f : 0f;
                AddOrUpdateProperty(key, value);
                return;
            }
            
            float floatValue;
            if (float.TryParse(val, out floatValue))
            {
                AddOrUpdateProperty(key, floatValue);
                return;
            }
            
            KeyValEntry entry = new KeyValEntry();
            entry.key = key;
            entry.val = val;
            datFileValues.Add(entry);
        }
        
        private void AddOrUpdateProperty(string name, float value)
        {
            string propertyName = FormatPropertyName(name);
            
            PropertyValue existingProp = properties.Find(p => p.name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            
            if (existingProp != null)
            {
                existingProp.value = value;
            }
            else
            {
                properties.Add(new PropertyValue { name = propertyName, value = value });
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
            itemType = ItemType.None;
            rarity = Rarity.Common;
            nameEnglish = null;
            descriptionEnglish = null;
            useable = UseableType.None;
            slot = SlotType.None;
            size_X = 1;
            size_Y = 1;
            size_Z = -1;
            craftingRecipes.Clear();
        }

        public void Test()
        {  
            Debug.Log(GetText());
        }

        public string GetText()
        {
            string text = "";

            text += "GUID " + guid + "\n";
            text += "Type " + itemType.ToString() + "\n";
            text += "Rarity " + rarity.ToString() + "\n";
            if (useable != UseableType.None)
            {
                text += "Useable " + useable.ToString() + "\n";
            }
            if (slot != SlotType.None) 
            {
                text += "Slot " + slot.ToString() + "\n";
            }
            
            if (itemType == ItemType.Barricade && useable == UseableType.Barricade)
            {
                if (barricadeBuildType != BarricadeBuildType.None)
                {
                    text += "Build " + barricadeBuildType.ToString() + "\n";
                }
            }
            
            text += "ID " + id.ToString() + "\n";
            text += "Size_X " + size_X.ToString() + "\n";
            text += "Size_Y " + size_Y.ToString() + "\n";
            if (size_Z > 0)
            {
                text += "Size_Z " + size_Z.ToString() + "\n";
            }
            
            foreach (KeyValEntry keyValEntry in datFileValues)
            {
                if (keyValEntry.key.ToLower() == "type") continue;
                if (keyValEntry.key.ToLower() == "rarity") continue;
                if (keyValEntry.key.ToLower() == "guid") continue;
                if (keyValEntry.key.ToLower() == "id") continue;
                if (keyValEntry.key.ToLower() == "name") continue;
                if (keyValEntry.key.ToLower() == "description") continue;
                if (keyValEntry.key.ToLower() == "useable") continue;
                if (keyValEntry.key.ToLower() == "slot") continue;
                if (keyValEntry.key.ToLower() == "size_x") continue;
                if (keyValEntry.key.ToLower() == "size_y") continue;
                if (keyValEntry.key.ToLower() == "size_z") continue;
                
                text += keyValEntry.key + " " + keyValEntry.val + "\n";
            }
            
            foreach (PropertyValue prop in properties)
            {
                if (prop.name == "Locked" || prop.name == "Proof_Explosion" || 
                    prop.name == "Unpickupable" || prop.name == "Unrepairable" || 
                    prop.name == "Unsalvageable" || prop.name == "Unsaveable" ||
                    prop.name == "Use_Water_Height_Transparent_Sort" || prop.name == "Vulnerable" ||
                    prop.name == "Exchange_With_Target_Item")
                {
                    if (prop.value > 0)
                        text += prop.name + "\n";
                }
                else if (prop.name == "Armor_Tier")
                {
                    if (prop.value > 0)
                        text += prop.name + " High\n";
                }
                else if (prop.name == "Allow_Collision_While_Animating" || 
                         prop.name == "Allow_Placement_Inside_Clip_Volumes" ||
                         prop.name == "Allow_Placement_On_Vehicle" ||
                         prop.name == "Bypass_Claim" ||
                         prop.name == "Bypass_Pickup_Ownership" ||
                         prop.name == "Can_Be_Damaged" ||
                         prop.name == "CanVehicleHookWhileAttached" ||
                         prop.name == "Can_Zombies_Target" ||
                         prop.name == "Eligible_For_Pooling" ||
                         prop.name == "Has_Clip_Prefab" ||
                         prop.name == "Enable_Participant_Scaling" || 
                         prop.name == "Contains_Bonus_Items")
                {
                    bool defaultValue = true;
                    
                    if (prop.name == "Allow_Collision_While_Animating" || 
                        prop.name == "Allow_Placement_Inside_Clip_Volumes" ||
                        prop.name == "Bypass_Claim" ||
                        prop.name == "Bypass_Pickup_Ownership" ||
                        prop.name == "CanVehicleHookWhileAttached" ||
                        prop.name == "Contains_Bonus_Items")
                    {
                        defaultValue = false;
                    }
                    
                    if ((prop.value > 0) != defaultValue)
                    {
                        text += prop.name + " " + (prop.value > 0 ? "true" : "false") + "\n";
                    }
                }
                else if (prop.value != 0)
                {
                    if (prop.name == "Offset" || prop.name == "Radius" || 
                        prop.name == "Range" || prop.name == "Salvage_Duration_Multiplier")
                    {
                        text += prop.name + " " + prop.value.ToString("0.##") + "\n";
                    }
                    else
                    {
                        text += prop.name + " " + prop.value.ToString() + "\n";
                    }
                }
            }

            if (HasCraftingRecipes()) {
                text += "\n" + GetBlueprintsText();
            }

            return text;
        }

        public bool HasCraftingRecipes() {
            return craftingRecipes != null && craftingRecipes.Count > 0;
        }

        public string GetBlueprintsText() {
            string text = "";
            
            text += "Blueprints " + craftingRecipes.Count + "\n";
            
            for (int recipeIndex = 0; recipeIndex < craftingRecipes.Count; recipeIndex++) {
                CraftingRecipe recipe = craftingRecipes[recipeIndex];
                string blueprintPrefix = "Blueprint_" + recipeIndex;
                
                text += blueprintPrefix + "_Type " + recipe.blueprintType.ToString() + "\n";
                
                if (recipe.tool != null) {
                    text += blueprintPrefix + "_Tool " + recipe.tool.id + "\n";
                }
                
                if (GetSkillLevelInt(recipe.blueprintSkillLevel) > 0 && recipe.blueprintSkill != BluePrintSkill.None) {
                    text += blueprintPrefix + "_Level " + GetSkillLevelInt(recipe.blueprintSkillLevel).ToString() + "\n";
                    text += blueprintPrefix + "_Skill " + recipe.blueprintSkill.ToString() + "\n";
                }
                
                if (recipe.ingredients != null && recipe.ingredients.Count > 0) {
                    text += blueprintPrefix + "_Supplies " + recipe.ingredients.Count + "\n";
                    
                    for (int i = 0; i < recipe.ingredients.Count; i++) {
                        Supply ingredient = recipe.ingredients[i];
                        if (ingredient.item != null) {
                            text += blueprintPrefix + "_Supply_" + i + "_ID " + ingredient.item.id + "\n";
                            text += blueprintPrefix + "_Supply_" + i + "_Amount " + ingredient.amount + "\n";
                        }
                    }
                }
                
                if (recipe.product != null && recipe.product.item != null) {
                    text += blueprintPrefix + "_Product " + recipe.product.item.id + "\n";
                    text += blueprintPrefix + "_Products " + recipe.product.amount + "\n";
                }
                
                if (recipe.outputs != null && recipe.outputs.Count > 0) {
                    text += blueprintPrefix + "_Outputs " + recipe.outputs.Count + "\n";
                    
                    for (int i = 0; i < recipe.outputs.Count; i++) {
                        Output output = recipe.outputs[i];
                        if (output.item != null) {
                            text += blueprintPrefix + "_Output_" + i + "_ID " + output.item.id + "\n";
                            text += blueprintPrefix + "_Output_" + i + "_Amount " + output.amount + "\n";
                        }
                    }
                }
                
                text += blueprintPrefix + "_Build " + recipe.buildAnimation.ToString() + "\n";
                
                if (recipe.explosion != null && !string.IsNullOrEmpty(recipe.explosion.id)) {
                    text += "Explosion " + recipe.explosion.id + "\n";
                }
                
                if (recipe.extraDirectives != null) {
                    foreach (KeyValEntry directive in recipe.extraDirectives) {
                        text += directive.key.Trim() + " " + directive.val.Trim() + "\n";
                    }
                }
                
                text += "\n";
            }
            
            return text;
        }
        
        private int GetSkillLevelInt(BluePrintSkillLevel skillLevel) {
            switch (skillLevel) {
                case BluePrintSkillLevel.None:
                    return 0;
                case BluePrintSkillLevel.SkillLevel_1:
                    return 1;
                case BluePrintSkillLevel.SkillLevel_2:
                    return 2;
                case BluePrintSkillLevel.SkillLevel_3:
                    return 3;
                default:
                    return 0;                   
            }
        }

        public string GetNameEnglish()
        {
            return nameEnglish;
        }

        public string GetDescriptionEnglish()
        {
            return descriptionEnglish;
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

        public string GetEnglishLanguageText()
        {
            if (string.IsNullOrEmpty(nameEnglish) && string.IsNullOrEmpty(descriptionEnglish))
                return null;
                
            string langText = "";
            
            if (!string.IsNullOrEmpty(nameEnglish)) {
                langText += "Name " + nameEnglish + "\n";
            }
            
            if (!string.IsNullOrEmpty(descriptionEnglish)) {
                langText += "Description " + descriptionEnglish + "\n";
            }
            
            return langText;
        }
        
        public void SaveEnglishLanguageFile(string folderPath)
        {
            string englishText = GetEnglishLanguageText();
            if (string.IsNullOrEmpty(englishText))
                return;
                
            string filePath = Path.Combine(folderPath, "English.dat");
            File.WriteAllText(filePath, englishText);
            
            #if UNITY_EDITOR
            Debug.Log("English language file saved to: " + filePath);
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
        
        public void SaveItemDatFile(string folderPath)
        {
            string itemText = GetText();
            string fileName = !string.IsNullOrEmpty(nameEnglish) ? nameEnglish : this.name;

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            string filePath = Path.Combine(folderPath, fileName + ".dat");
            File.WriteAllText(filePath, itemText);
            
            #if UNITY_EDITOR
            Debug.Log("Item DAT file saved to: " + filePath);
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
        
        public void SaveAllFiles(string folderPath)
        {
            if (!Directory.Exists(folderPath)) {
                Debug.Log($"Creating directory: {folderPath}");
                Directory.CreateDirectory(folderPath);
            }
            
            try
            {
    
                SaveItemDatFile(folderPath);
                
      
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
                
                #if UNITY_EDITOR
                Debug.Log("All files saved to: " + folderPath);
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving files: {e.Message}\n{e.StackTrace}");
            }
        }
    }
    
    #if UNITY_EDITOR
    public class UnturnedDatFilePostProcessor : AssetPostprocessor
    {
        private static HashSet<string> processedAssets = new HashSet<string>();
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string importedAsset in importedAssets)
            {
                if (!ItemDefinitionAsset.IsInDatGeneratorFolder(importedAsset))
                    continue;

                if (AssetDatabase.GetMainAssetTypeAtPath(importedAsset) == typeof(ItemDefinitionAsset))
                {
                    ItemDefinitionAsset datFile = AssetDatabase.LoadAssetAtPath<ItemDefinitionAsset>(importedAsset);
                    if (datFile == null)
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