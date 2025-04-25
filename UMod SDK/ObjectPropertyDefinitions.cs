using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Tools
{
    public static class UnturnedObjectProperties
    {
        public static Dictionary<EObjectType, string[]> GetAllObjectProperties()
        {
            Dictionary<EObjectType, string[]> allProperties = new Dictionary<EObjectType, string[]>();
            
            foreach (EObjectType type in Enum.GetValues(typeof(EObjectType)))
            {
                allProperties[type] = GetPropertiesForType(type);
            }
            
            return allProperties;
        }
        
        public static string[] GetPropertiesForType(EObjectType type)
        {
            List<string> properties = new List<string>(GetBaseObjectProperties());
            
            switch (type)
            {
                case EObjectType.Decal:
                    properties.AddRange(GetDecalProperties());
                    break;
                default:
            
                    properties.AddRange(GetInteriorCullingProperties());
                    properties.AddRange(GetInteractiveProperties());
                    properties.AddRange(GetRubbleProperties());
                    break;
            }
            
            return properties.ToArray();
        }
        
        private static string[] GetBaseObjectProperties()
        {
            return new string[] {
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
            };
        }
        
        private static string[] GetDecalProperties()
        {
            return new string[] {
                "Decal_Alpha",
                "Decal_X",
                "Decal_Y",
                "Decal_LOD_Bias"
            };
        }
        
        private static string[] GetInteriorCullingProperties()
        {
            return new string[] {
                "Exclude_From_Culling_Volumes",
                "LOD",
                "LOD_Bias",
                "LOD_Center_X",
                "LOD_Center_Y",
                "LOD_Center_Z",
                "LOD_Size_X",
                "LOD_Size_Y",
                "LOD_Size_Z"
            };
        }
        
        private static string[] GetInteractiveProperties()
        {
            return new string[] {
                "Interactability",
                "Interactability_Animation_Component_Path",
                "Interactability_Blade_ID",
                "Interactability_Delay",
                "Interactability_Dialogue",
                "Interactability_Drops",
                "Interactability_Drop_0",
                "Interactability_Drop_1",
                "Interactability_Drop_2",
                "Interactability_Drop_3",
                "Interactability_Drop_4",
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
            };
        }
        
        private static string[] GetRubbleProperties()
        {
            return new string[] {
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
            };
        }
        
        public static Dictionary<string, string[]> GetPropertyValuesAndDescriptions()
        {
            Dictionary<string, string[]> propertyValues = new Dictionary<string, string[]>();
            
            propertyValues["Chart"] = new string[] {
                "Ignore", "None", "Large", "Medium"
            };
            
            propertyValues["Holiday_Restriction"] = new string[] {
                "None", "Christmas", "Halloween"
            };
            
            propertyValues["Landmark_Quality"] = new string[] {
                "Off", "Low", "Medium", "High", "Ultra"
            };
            
            propertyValues["Interactability"] = new string[] {
                "None", "Binary_State", "Dropper", "Note", "Water", "Fuel", "Rubble", "NPC", "Quest", "Dialogue"
            };
            
            propertyValues["Interactability_Editor"] = new string[] {
                "None", "Toggle"
            };
            
            propertyValues["Interactability_Hint"] = new string[] {
                "Door", "Switch", "Fire", "Generator", "Use", "Custom"
            };
            
            propertyValues["Interactability_Nav"] = new string[] {
                "None", "On", "Off"
            };
            
            propertyValues["Interactability_Power"] = new string[] {
                "None", "Toggle", "Stay"
            };
            
            propertyValues["LOD"] = new string[] {
                "None", "Mesh", "Area"
            };
            
            propertyValues["Rubble"] = new string[] {
                "None", "Destroy"
            };
            
            propertyValues["Rubble_Editor"] = new string[] {
                "Alive", "Dead"
            };
            
            propertyValues["Rubble_Nav_Mode"] = new string[] {
                "Unaffected", "DeactivateIfAllDead"
            };
            
            return propertyValues;
        }
    }
} 