using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Tools
{
    
    public enum ItemType
    {
        None = -1, 
        Hat,
        Pants,
        Shirt,
        Mask,
        Backpack,
        Vest,
        Glasses,
        Gun,
        Sight,
        Tactical,
        Grip,
        Barrel,
        Magazine,
        Food,
        Water,
        Medical,
        Melee,
        Fuel,
        Tool,
        Barricade,
        Storage,
        Beacon,
        Farm,
        Trap,
        Structure,
        Supply,
        Throwable,
        Grower,
        Optic,
        Refill,
        Fisher,
        Cloud,
        Map,
        Key,
        Box,
        Arrest_Start,
        Arrest_End,
        Tank,
        Generator,
        Detonator,
        Charge,
        Library,
        Filter,
        Sentry,
        Vehicle_Repair_Tool,
        Tire,
        Compass,
        Oil_Pump,
        Vehicle_Paint_Tool
    }
    
    
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythical
    }
    
    
    public enum UseableType
    {
        None,
        Clothing,
        Gun,
        Consumeable,
        Melee,
        Fuel,
        Carjack,
        Barricade,
        Structure,
        Throwable,
        Grower,
        Optic,
        Refill,
        Fisher,
        Cloud,
        Arrest_Start,
        Arrest_End,
        Detonator,
        Filter,
        Carlockpick
    }
    
    
    public enum SlotType
    {
        None,
        Primary,
        Secondary,
        Tertiary,
        Any
    }
    
    
    public enum BluePrintType
    {
        Repair,
        Tool,
        Apparel,
        Supply,
        Gear,
        Ammo,
        Barricade,
        Structure,
        Utilities,
        Furniture,
    }

    
    public enum BluePrintSkill
    {
        None,
        Craft,
        Tool,
        Repair,
    }
    
    
    public enum BluePrintSkillLevel
    {
        None,
        SkillLevel_1,
        SkillLevel_2,
        SkillLevel_3,
    }
    
    
    public enum BarricadeBuildType
    {
        None,
        Barrel_Rain,
        Barricade,
        Barricade_Wall,
        Beacon,
        Bed,
        Cage,
        Campfire,
        Charge,
        Claim,
        Clock,
        Door,
        Farm,
        Fortification,
        Freeform,
        Gate,
        Generator,
        Glass,
        Hatch,
        Ladder,
        Library,
        Mannequin,
        Note,
        Oil,
        Oven,
        Oxygenator,
        Safezone,
        Sentry_Freeform,
        Sentry,
        Shutter,
        Sign_Wall,
        Sign,
        Spike,
        Spot,
        Stereo,
        Storage_Wall,
        Storage,
        Tank,
        Torch,
        Vehicle,
        Wire
    }
    
    
    public enum ArmorTier
    {
        Low,
        High
    }
} 
