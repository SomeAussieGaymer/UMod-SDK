using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Tools
{

    public static class UnturnedItemProperties
    {

        public static Dictionary<ItemType, string[]> GetAllItemProperties()
        {
            Dictionary<ItemType, string[]> allProperties = new Dictionary<ItemType, string[]>();
            
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                if (type != ItemType.None)
                {
                    allProperties[type] = GetPropertiesForType(type);
                }
            }
            
            return allProperties;
        }
        
        public static string[] GetPropertiesForType(ItemType type)
        {
            switch (type)
            {
                case ItemType.Hat:
                    return GetHatProperties();
                case ItemType.Pants:
                    return GetPantsProperties();
                case ItemType.Shirt:
                    return GetShirtProperties();
                case ItemType.Mask:
                    return GetMaskProperties();
                case ItemType.Backpack:
                    return GetBackpackProperties();
                case ItemType.Vest:
                    return GetVestProperties();
                case ItemType.Glasses:
                    return GetGlassesProperties();
                case ItemType.Gun:
                    return GetGunProperties();
                case ItemType.Sight:
                    return GetSightProperties();
                case ItemType.Tactical:
                    return GetTacticalProperties();
                case ItemType.Grip:
                    return GetGripProperties();
                case ItemType.Barrel:
                    return GetBarrelProperties();
                case ItemType.Magazine:
                    return GetMagazineProperties();
                case ItemType.Food:
                    return GetFoodProperties();
                case ItemType.Water:
                    return GetWaterProperties();
                case ItemType.Medical:
                    return GetMedicalProperties();
                case ItemType.Melee:
                    return GetMeleeProperties();
                case ItemType.Fuel:
                    return GetFuelProperties();
                case ItemType.Barricade:
                    return GetBarricadeProperties();
                default:
                    return new string[0];
            }
        }
        
        private static string[] GetClothingProperties()
        {
            return new string[] {
                "Armor",
                "Armor_Explosion",
                "Proof_Water",
                "Proof_Fire",
                "Proof_Radiation",
                "Movement_Speed_Multiplier",
                "Hair_Visible",
                "Beard_Visible"
            };
        }
        
        private static string[] GetGearProperties()
        {
            return new string[] {
                "Hair",
                "Beard",
                "Hair_Override"
            };
        }
        
        private static string[] GetBagProperties()
        {
            return new string[] {
                "Storage_X",
                "Storage_Y"
            };
        }
        
        private static string[] GetWeaponProperties()
        {
            return new string[] {
                "Range",
                "Player_Damage",
                "Player_Leg_Multiplier",
                "Player_Arm_Multiplier",
                "Player_Spine_Multiplier",
                "Player_Skull_Multiplier",
                "Player_Damage_Bleeding",
                "Player_Damage_Bones",
                "Player_Damage_Food",
                "Player_Damage_Water",
                "Player_Damage_Virus",
                "Player_Damage_Hallucination",
                "Zombie_Damage",
                "Zombie_Leg_Multiplier",
                "Zombie_Arm_Multiplier",
                "Zombie_Spine_Multiplier",
                "Zombie_Skull_Multiplier",
                "Animal_Damage",
                "Animal_Leg_Multiplier",
                "Animal_Spine_Multiplier",
                "Animal_Skull_Multiplier",
                "Barricade_Damage",
                "Structure_Damage",
                "Vehicle_Damage",
                "Resource_Damage",
                "Object_Damage",
                "Durability",
                "Wear",
                "Allow_Flesh_Fx",
                "Invulnerable"
            };
        }
        private static string[] GetConsumableProperties()
        {
            return new string[] {
                "Health",
                "Food",
                "Water",
                "Virus",
                "Disinfectant",
                "Energy",
                "Vision",
                "Oxygen",
                "Warmth",
                "Experience",
                "Bleeding_Modifier",
                "Bones_Modifier",
                "Aid",
                "Should_Delete_After_Use",
                "ConsumeAudioClip",
                "Explosion",
                "Item_Reward_Spawn_ID",
                "Min_Item_Rewards",
                "Max_Item_Rewards"
            };
        }
        
        private static string[] GetCaliberProperties()
        {
            return new string[] {
                "Calibers",
                "Recoil_X",
                "Recoil_Y",
                "Aiming_Recoil_Multiplier",
                "Spread",
                "Sway",
                "Shake",
                "Damage",
                "Firerate",
                "Ballistic_Damage_Multiplier"
            };
        }

        private static string[] GetHatProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetClothingProperties());
            properties.AddRange(GetGearProperties());
            properties.AddRange(new string[] {
                "Protection",
                "Durability",
                "Weight",
                "Armor"
            });
            return properties.ToArray();
        }
        
        private static string[] GetPantsProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetClothingProperties());
            properties.AddRange(GetBagProperties());
            properties.AddRange(new string[] {
                "Protection",
                "Durability",
                "Weight"
            });
            return properties.ToArray();
        }
        
        private static string[] GetShirtProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetClothingProperties());
            properties.AddRange(GetBagProperties());
            properties.AddRange(new string[] {
                "Protection",
                "Durability",
                "Weight",
                "Has_1P_Character_Mesh_Override",
                "Ignore_Hand"
            });
            return properties.ToArray();
        }
        
        private static string[] GetMaskProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetClothingProperties());
            properties.AddRange(GetGearProperties());
            properties.AddRange(new string[] {
                "Protection",
                "Durability",
                "Weight",
                "Filter",
                "Earpiece"
            });
            return properties.ToArray();
        }
        
        private static string[] GetBackpackProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetClothingProperties());
            properties.AddRange(GetBagProperties());
            properties.AddRange(new string[] {
                "Protection",
                "Durability",
                "Weight"
            });
            return properties.ToArray();
        }
        
        private static string[] GetVestProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetClothingProperties());
            properties.AddRange(GetBagProperties());
            properties.AddRange(new string[] {
                "Protection",
                "Durability",
                "Weight"
            });
            return properties.ToArray();
        }
        
        private static string[] GetGlassesProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetClothingProperties());
            properties.AddRange(GetGearProperties());
            properties.AddRange(new string[] {
                "Protection",
                "Durability",
                "Weight",
                "Vision",
                "Nightvision_Color_R",
                "Nightvision_Color_G",
                "Nightvision_Color_B",
                "Nightvision_Fog_Intensity",
                "Blindfold"
            });
            return properties.ToArray();
        }
        private static string[] GetGunProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetWeaponProperties());
            properties.AddRange(new string[] {
                "Ammo_Min",
                "Ammo_Max",
                "Sight",
                "Tactical",
                "Grip",
                "Barrel",
                "Magazine",
                "Unplace",
                "Replace",
                "Hook_Sight",
                "Hook_Tactical",
                "Hook_Grip",
                "Hook_Barrel",
                "Magazine_Calibers",
                "Attachment_Calibers",
                "Caliber",
                "Firerate",
                "Action",
                "Should_Delete_Empty_Magazines",
                "Bursts",
                "Safety",
                "Semi",
                "Auto",
                "Spread_Aim",
                "Spread_Hip",
                "Recoil_Aim",
                "Recoil_Min_X",
                "Recoil_Min_Y",
                "Recoil_Max_X",
                "Recoil_Max_Y",
                "Aiming_Recoil_Multiplier",
                "Recover_X",
                "Recover_Y",
                "Ballistic_Drop",
                "Ballistic_Force",
                "Reload_Time",
                "Muzzle",
                "Explosion",
                "Shell",
                "Alert_Radius",
                "Range_Rangefinder",
                "Instakill_Headshots",
                "Infinite_Ammo",
                "Ammo_Per_Shot",
                "Fire_Delay_Seconds",
                "Allow_Magazine_Change",
                "Can_Aim_During_Sprint",
                "Jam_Quality_Threshold",
                "Jam_Max_Chance"
            });
            return properties.ToArray();
        }
        
        private static string[] GetSightProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetCaliberProperties());
            properties.AddRange(new string[] {
                "Vision",
                "Nightvision_Color_R",
                "Nightvision_Color_G",
                "Nightvision_Color_B",
                "Nightvision_Fog_Intensity",
                "Zoom",
                "Holographic"
            });
            return properties.ToArray();
        }
        

        private static string[] GetTacticalProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetCaliberProperties());
            properties.AddRange(new string[] {
                "Laser",
                "Light",
                "Rangefinder",
                "Melee",
                "SpotLight_Range",
                "SpotLight_Angle",
                "SpotLight_Intensity",
                "Spotlight_Color_R",
                "Spotlight_Color_G",
                "Spotlight_Color_B"
            });
            return properties.ToArray();
        }
        
        private static string[] GetGripProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetCaliberProperties());
            return properties.ToArray();
        }
        
        private static string[] GetBarrelProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetCaliberProperties());
            properties.AddRange(new string[] {
                "Braked",
                "Silenced",
                "Volume",
                "Durability",
                "Ballistic_Drop",
                "Gunshot_Rolloff_Distance_Multiplier"
            });
            return properties.ToArray();
        }
        
        private static string[] GetMagazineProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetCaliberProperties());
            properties.AddRange(new string[] {
                "Pellets",
                "Stuck",
                "Projectile_Damage_Multiplier",
                "Projectile_Blast_Radius_Multiplier",
                "Projectile_Launch_Force_Multiplier",
                "Range",
                "Player_Damage",
                "Zombie_Damage",
                "Animal_Damage",
                "Barricade_Damage",
                "Structure_Damage",
                "Vehicle_Damage",
                "Resource_Damage",
                "Object_Damage",
                "Explosion_Launch_Speed",
                "Explosion",
                "Tracer",
                "Impact",
                "Speed",
                "Explosive",
                "Delete_Empty",
                "Should_Fill_After_Detach"
            });
            return properties.ToArray();
        }
        
        private static string[] GetFoodProperties()
        {
            return GetConsumableProperties();
        }
        
        private static string[] GetWaterProperties()
        {
            return GetConsumableProperties();
        }
        
        private static string[] GetMedicalProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(new string[] {
                "Range",
                "Durability"
            });
            properties.AddRange(GetConsumableProperties());
            properties.AddRange(new string[] {
                "Bleeding",
                "Broken"
            });
            return properties.ToArray();
        }
        
        private static string[] GetMeleeProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetWeaponProperties());
            properties.AddRange(new string[] {
                "Strength",
                "Weak",
                "Strong",
                "Stamina",
                "Repair",
                "Repeated",
                "Light",
                "SpotLight_Range",
                "SpotLight_Angle",
                "SpotLight_Intensity",
                "Alert_Radius"
            });
            return properties.ToArray();
        }
        
        private static string[] GetFuelProperties()
        {
            List<string> properties = new List<string>();
            properties.AddRange(GetConsumableProperties());
            properties.AddRange(new string[] {
                "Fill_Amount",
                "Has_Emissions",
                "Emissions_Key"
            });
            return properties.ToArray();
        }
        
        private static string[] GetBarricadeProperties()
        {
            return new string[] {
                "Allow_Collision_While_Animating",
                "Allow_Placement_Inside_Clip_Volumes",
                "Allow_Placement_On_Vehicle",
                "Armor_Tier",
                "Bypass_Claim",
                "Bypass_Pickup_Ownership",
                "Can_Be_Damaged",
                "CanVehicleHookWhileAttached",
                "Can_Zombies_Target",
                "Eligible_For_Pooling",
                "Explosion",
                "Has_Clip_Prefab",
                "Health",
                "Locked",
                "Offset",
                "PlacementAudioClip",
                "PlacementPreviewPrefab",
                "Proof_Explosion",
                "Radius",
                "Range",
                "Salvage_Duration_Multiplier",
                "Unpickupable",
                "Unrepairable",
                "Unsalvageable",
                "Unsaveable",
                "Use_Water_Height_Transparent_Sort",
                "Vulnerable",
                "Wave",
                "Rewards",
                "Reward_ID",
                "Enable_Participant_Scaling",
                "Generate",
                "Destroy",
                "Drops",
                "Item_Origin",
                "Probability_Model",
                "Contains_Bonus_Items",
                "Exchange_With_Target_Item",
                "Capacity",
                "Tax"
            };
        }
    }
} 