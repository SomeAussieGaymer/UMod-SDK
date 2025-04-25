using UnityEngine;

namespace Tools
{
    public enum EObjectType
    {
        Small,
        Medium,
        Large,
        NPC,
        Decal
    }
    

    public enum EObjectChart
    {
        Ignore,
        None,
        Large,
        Medium
    }
    

    public enum ENPCHoliday
    {
        None,
        Christmas,
        Halloween
    }
    

    public enum LandmarkQuality
    {
        Off,
        Low,
        Medium,
        High,
        Ultra
    }
    
    public enum ObjectLOD
    {
        None,
        Mesh,
        Area
    }
    

    public enum ObjectInteractability
    {
        None,
        Binary_State,
        Dropper,
        Note,
        Water,
        Fuel,
        Rubble,
        NPC,
        Quest,
        Dialogue
    }

    public enum InteractabilityEditor
    {
        None,
        Toggle
    }
    

    public enum InteractabilityHint
    {
        Door,
        Switch,
        Fire,
        Generator,
        Use,
        Custom
    }
    

    public enum InteractabilityNav
    {
        None,
        On,
        Off
    }
    

    public enum InteractabilityPower
    {
        None,
        Toggle,
        Stay
    }
    
    
    public enum RubbleMode
    {
        None,
        Destroy
    }
    
    public enum RubbleEditor
    {
        Alive,
        Dead
    }
    
    public enum RubbleNavMode
    {
        Unaffected,
        DeactivateIfAllDead
    }
} 