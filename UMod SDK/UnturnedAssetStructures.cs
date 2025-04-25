using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Tools
{
    [System.Serializable]
    public class UnturnedAssetStructures
    {
        public string id;
        public string name;
        public string description;
    }

    [System.Serializable]
    public class PropertyValue
    {
        public string name;
        public float value;
    }

    [System.Serializable]
    public class KeyValEntry
    {
        public string key;
        public string val;
    }

    [System.Serializable]
    public class Supply
    {
        public UnturnedAssetStructures item;
        public int amount = 1;
    }

    [System.Serializable]
    public class Output
    {
        public UnturnedAssetStructures item;
        public int amount = 1;
    }

    [System.Serializable]
    public class CraftingRecipe
    {
        public string recipeName = "New Recipe";
        public BluePrintType blueprintType = BluePrintType.Repair;
        public BluePrintSkill blueprintSkill = BluePrintSkill.None;
        public BluePrintSkillLevel blueprintSkillLevel = BluePrintSkillLevel.None;
        public UnturnedAssetStructures tool;
        public List<Supply> ingredients = new List<Supply>();
        public List<Output> outputs = new List<Output>();
        public Output product = new Output();
        public int buildAnimation = 27;
        public UnturnedAssetStructures explosion;
        public List<KeyValEntry> extraDirectives = new List<KeyValEntry>();
        public bool foldout = true;
    }
} 