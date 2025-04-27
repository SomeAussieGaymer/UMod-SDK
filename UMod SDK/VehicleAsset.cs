using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using System.IO;

namespace Tools
{
    [CreateAssetMenu(fileName = "New Vehicle", menuName = "UMod SDK/Vehicle Dats", order = 2)]
    public class VehicleAsset : ScriptableObject
    {
        [SerializeField]
        public string guid;

        [Header("Basic Properties")]
        public EEngine engine = EEngine.Car;
        public ERarity rarity = ERarity.Common;
        public string title;
        public ushort id;
        public bool lockMouse;
        public bool reclined;
        public float camFollowDistance;
        [TextArea(3, 5)]
        public string description;

        [Header("Health and Armor")]
        public ushort health;
        public ushort healthMin;
        public ushort healthMax;
        public bool invulnerable;
        public bool environmentInvulnerable;
        public bool explosionsInvulnerable;
        public bool bumperInvulnerable;
        public bool tiresInvulnerable;
        public float bumperMultiplier = 1.0f;
        public float childExplosionArmorMultiplier = 0.2f;
        public float passengerExplosionArmor = 1.0f;

        [Header("Fuel")]
        public ushort fuel;
        public ushort fuelMin;
        public ushort fuelMax;
        public float fuelBurnRate;

        [Header("Battery")]
        public bool batteryPowered;
        public float batteryBurnRate = 20f;
        public float batteryChargeRate = 20f;
        public float batterySpawnChargeMultiplier = 1f;
        public bool canStealBattery = true;
        public bool cannotSpawnWithBattery;
        public string defaultBatteryGuid = "098b13be34a7411db7736b7f866ada69";

        [Header("Storage")]
        public byte trunkStorageX;
        public byte trunkStorageY;

        [Header("Vehicle Properties")]
        public bool canBeLocked = true;
        public bool canRepairWhileSeated;
        public float exit = 2f;
        public bool hasClipPrefab = true;
        public bool hasHorn = true;
        public bool isPaintable;
        public bool shouldSpawnSeatCapsules;
        public string ignitionAudioClip;
        public string hornAudioClip;

        [Header("Physics")]
        public float brake;
        public float lift;
        public float speedMax;
        public float speedMin;
        public float steerMax;
        public float steerMin;
        public float airSteerMax;
        public float airSteerMin;
        public float airTurnResponsiveness = 2f;
        public bool sleds;
        public bool traction;
        public float carjackForceMultiplier = 1.0f;
        public float engineForceMultiplier = 1.0f;
        public float pitchDrive;

        [Header("Train Properties")]
        public float trainTrackOffset;
        public float trainWheelOffset;
        public float trainCarLength;
        public bool supportsMobileBuildables;
        public float pitchIdle;

        [Header("Center of Mass")]
        public bool overrideCenterOfMass;
        public float centerOfMassY;

        [Header("Engine Settings")]
        public float reverseGearRatio;
        public List<float> forwardGearRatios = new List<float>();
        public float gearShiftUpThresholdRPM;
        public float gearShiftDuration;
        public float gearShiftInterval;
        public float engineIdleRPM;
        public float engineMaxRPM;
        public float engineMaxTorque;

        [Header("Engine Sound")]
        public EEngineSoundType engineSoundType;
        public EngineSoundSettings engineSound = new EngineSoundSettings();

        [Header("Wheel Configuration")]
        public List<VehicleWheelConfiguration> wheelConfigurations = new List<VehicleWheelConfiguration>();

        [Header("Paint")]
        public List<PaintableVehicleSection> paintableSections = new List<PaintableVehicleSection>();
        public EVehicleDefaultPaintColorMode defaultPaintColorMode;
        public List<Color> defaultPaintColors = new List<Color>();
        public VehicleRandomPaintColorConfiguration defaultPaintColorConfiguration = new VehicleRandomPaintColorConfiguration();

        [Header("Explosion")]
        public string explosionGuid;
        public float explosionForceMultiplier = 1.0f;
        public Vector3 explosionMaxForce = new Vector3(0, 1024, 0);
        public Vector3 explosionMinForce = new Vector3(0, 1024, 0);
        public bool shouldExplosionBurnMaterials = true;
        public bool shouldExplosionCauseDamage = true;
        public ushort explosion;
        public List<PaintableVehicleSection> explosionBurnMaterialSections = new List<PaintableVehicleSection>();

        [Header("Turrets")]
        public byte turretCount;
        public List<VehicleTurret> turrets = new List<VehicleTurret>();

        [Header("Localized Texts")]
        [Tooltip("Text displayed when interacting with the vehicle")]
        public string interactText;
        [Tooltip("Text displayed when exiting the vehicle")]
        public string exitText;
        [Tooltip("Text displayed when using the horn")]
        public string hornText;
        [Tooltip("Text displayed when locking the vehicle")]
        public string lockText;
        [Tooltip("Text displayed when unlocking the vehicle")]
        public string unlockText;
        [Tooltip("Text displayed when starting the vehicle")]
        public string startText;
        [Tooltip("Text displayed when stopping the vehicle")]
        public string stopText;
        [Tooltip("Text displayed when repairing the vehicle")]
        public string repairText;
        [Tooltip("Text displayed when refueling the vehicle")]
        public string refuelText;
        [Tooltip("Text displayed when recharging the vehicle's battery")]
        public string rechargeText;
        [Tooltip("Text displayed when stealing the vehicle's battery")]
        public string stealBatteryText;
        [Tooltip("Text displayed when inserting a battery into the vehicle")]
        public string insertBatteryText;

        private void OnEnable()
        {
            GenerateGuid();
        }

        public void GenerateGuid(bool regenerate = false)
        {
            if (string.IsNullOrEmpty(guid) || regenerate)
            {
                guid = Guid.NewGuid().ToString("N");
            }
            if (regenerate)
            {
                Debug.Log("Guid Regenerated. Now: " + guid);
            }
        }

        public string GetText()
        {
            StringBuilder sb = new StringBuilder();

            AppendLine(sb, "GUID", guid);
            AppendLine(sb, "Type", "Vehicle");
            if (rarity != ERarity.Common) AppendLine(sb, "Rarity", rarity.ToString());
            if (id > 0) AppendLine(sb, "ID", id);

            if (engine != EEngine.Car) AppendLine(sb, "Engine", engine.ToString());

            if (lockMouse) AppendLine(sb, "LockMouse");
            if (reclined) AppendLine(sb, "Reclined");
            if (camFollowDistance > 0) AppendLine(sb, "Cam_Follow_Distance", camFollowDistance);

            if (health > 0) AppendLine(sb, "Health", health);
            if (healthMin > 0) AppendLine(sb, "Health_Min", healthMin);
            if (healthMax > 0) AppendLine(sb, "Health_Max", healthMax);

            if (invulnerable) AppendLine(sb, "Invulnerable");
            if (environmentInvulnerable) AppendLine(sb, "Environment_Invulnerable");
            if (explosionsInvulnerable) AppendLine(sb, "Explosions_Invulnerable");
            if (bumperInvulnerable) AppendLine(sb, "Bumper_Invulnerable");
            if (tiresInvulnerable) AppendLine(sb, "Tires_Invulnerable");

            if (bumperMultiplier != 1.0f) AppendLine(sb, "Bumper_Multiplier", bumperMultiplier);
            if (childExplosionArmorMultiplier != 0.2f) AppendLine(sb, "Child_Explosion_Armor_Multiplier", childExplosionArmorMultiplier);
            if (passengerExplosionArmor != 1.0f) AppendLine(sb, "Passenger_Explosion_Armor", passengerExplosionArmor);

            if (fuel > 0) AppendLine(sb, "Fuel", fuel);
            if (fuelMin > 0) AppendLine(sb, "Fuel_Min", fuelMin);
            if (fuelMax > 0) AppendLine(sb, "Fuel_Max", fuelMax);
            if (fuelBurnRate > 0) AppendLine(sb, "Fuel_Burn_Rate", fuelBurnRate);

            if (batteryPowered) AppendLine(sb, "Battery_Powered");
            if (batteryBurnRate != 20f) AppendLine(sb, "Battery_Burn_Rate", batteryBurnRate);
            if (batteryChargeRate != 20f) AppendLine(sb, "Battery_Charge_Rate", batteryChargeRate);
            if (batterySpawnChargeMultiplier != 1f) AppendLine(sb, "Battery_Spawn_Charge_Multiplier", batterySpawnChargeMultiplier);
            if (!canStealBattery) AppendLine(sb, "Can_Steal_Battery", "false");
            if (cannotSpawnWithBattery) AppendLine(sb, "Cannot_Spawn_With_Battery");
            if (defaultBatteryGuid != "098b13be34a7411db7736b7f866ada69") AppendLine(sb, "Default_Battery", defaultBatteryGuid);

            if (trunkStorageX > 0) AppendLine(sb, "Trunk_Storage_X", trunkStorageX);
            if (trunkStorageY > 0) AppendLine(sb, "Trunk_Storage_Y", trunkStorageY);

            if (!canBeLocked) AppendLine(sb, "Can_Be_Locked", "false");
            if (canRepairWhileSeated) AppendLine(sb, "Can_Repair_While_Seated", "true");
            if (exit != 2f) AppendLine(sb, "Exit", exit);
            if (!hasClipPrefab) AppendLine(sb, "Has_Clip_Prefab", "false");
            if (!hasHorn) AppendLine(sb, "Has_Horn", "false");
            if (isPaintable) AppendLine(sb, "IsPaintable", "true");
            if (shouldSpawnSeatCapsules) AppendLine(sb, "Should_Spawn_Seat_Capsules", "true");
            if (!string.IsNullOrEmpty(ignitionAudioClip)) AppendLine(sb, "IgnitionAudioClip", ignitionAudioClip);
            if (!string.IsNullOrEmpty(hornAudioClip)) AppendLine(sb, "HornAudioClip", hornAudioClip);

            if (brake > 0) AppendLine(sb, "Brake", brake);
            if (lift > 0) AppendLine(sb, "Lift", lift);
            if (speedMax != 0) AppendLine(sb, "Speed_Max", speedMax);
            if (speedMin != 0) AppendLine(sb, "Speed_Min", speedMin);
            if (steerMax > 0) AppendLine(sb, "Steer_Max", steerMax);
            if (steerMin > 0) AppendLine(sb, "Steer_Min", steerMin);
            if (airSteerMax > 0) AppendLine(sb, "Air_Steer_Max", airSteerMax);
            if (airSteerMin > 0) AppendLine(sb, "Air_Steer_Min", airSteerMin);
            if (airTurnResponsiveness != 2f) AppendLine(sb, "Air_Turn_Responsiveness", airTurnResponsiveness);
            if (sleds) AppendLine(sb, "Sleds");
            if (traction) AppendLine(sb, "Traction");
            if (carjackForceMultiplier != 1.0f) AppendLine(sb, "Carjack_Force_Multiplier", carjackForceMultiplier);
            if (engineForceMultiplier != 1.0f) AppendLine(sb, "Engine_Force_Multiplier", engineForceMultiplier);
            if (pitchDrive > 0) AppendLine(sb, "Pitch_Drive", pitchDrive);

            if (trainTrackOffset > 0) AppendLine(sb, "Train_Track_Offset", trainTrackOffset);
            if (trainWheelOffset > 0) AppendLine(sb, "Train_Wheel_Offset", trainWheelOffset);
            if (trainCarLength > 0) AppendLine(sb, "Train_Car_Length", trainCarLength);
            if (supportsMobileBuildables) AppendLine(sb, "Supports_Mobile_Buildables");
            if (pitchIdle > 0) AppendLine(sb, "Pitch_Idle", pitchIdle);

            if (overrideCenterOfMass)
            {
                AppendLine(sb, "Override_Center_Of_Mass", "True");
                AppendLine(sb, "Center_Of_Mass_Y", centerOfMassY);
            }

            if (reverseGearRatio > 0) AppendLine(sb, "ReverseGearRatio", reverseGearRatio);
            if (forwardGearRatios.Count > 0)
            {
                AppendLine(sb, "ForwardGearRatios");
                AppendLine(sb, "[");
                foreach (float ratio in forwardGearRatios)
                {
                    AppendLine(sb, "\t" + ratio);
                }
                AppendLine(sb, "]");
            }
            if (gearShiftUpThresholdRPM > 0) AppendLine(sb, "GearShift_UpThresholdRPM", gearShiftUpThresholdRPM);
            if (gearShiftDuration > 0) AppendLine(sb, "GearShift_Duration", gearShiftDuration);
            if (gearShiftInterval > 0) AppendLine(sb, "GearShift_Interval", gearShiftInterval);
            if (engineIdleRPM > 0) AppendLine(sb, "EngineIdleRPM", engineIdleRPM);
            if (engineMaxRPM > 0) AppendLine(sb, "EngineMaxRPM", engineMaxRPM);
            if (engineMaxTorque > 0) AppendLine(sb, "EngineMaxTorque", engineMaxTorque);

            if (engineSoundType != EEngineSoundType.None)
            {
                AppendLine(sb, "EngineSound_Type", engineSoundType.ToString());
                AppendLine(sb, "EngineSound");
                AppendLine(sb, "{");
                AppendLine(sb, "\tIdlePitch", engineSound.idlePitch);
                AppendLine(sb, "\tIdleVolume", engineSound.idleVolume);
                AppendLine(sb, "\tMaxPitch", engineSound.maxPitch);
                AppendLine(sb, "\tMaxVolume", engineSound.maxVolume);
                AppendLine(sb, "}");
            }

            if (wheelConfigurations.Count > 0)
            {
                AppendLine(sb, "WheelConfigurations");
                AppendLine(sb, "[");
                foreach (var wheel in wheelConfigurations)
                {
                    AppendLine(sb, "\t{");
                    if (!string.IsNullOrEmpty(wheel.wheelColliderPath))
                        AppendLine(sb, "\t\tWheelColliderPath", wheel.wheelColliderPath);
                    if (!string.IsNullOrEmpty(wheel.modelPath))
                        AppendLine(sb, "\t\tModelPath", wheel.modelPath);
                    if (wheel.isColliderPowered)
                        AppendLine(sb, "\t\tIsColliderPowered", "true");
                    if (wheel.isColliderSteered)
                        AppendLine(sb, "\t\tIsColliderSteered", "true");
                    if (wheel.modelUseColliderPose)
                        AppendLine(sb, "\t\tModelUseColliderPose", "true");
                    AppendLine(sb, "\t}");
                }
                AppendLine(sb, "]");
            }

            if (paintableSections.Count > 0)
            {
                AppendLine(sb, "PaintableSections");
                AppendLine(sb, "[");
                foreach (var section in paintableSections)
                {
                    AppendLine(sb, "    {");
                    AppendLine(sb, "        Path", section.path);
                    AppendLine(sb, "        MaterialIndex", section.materialIndex);
                    if (section.allMaterials)
                        AppendLine(sb, "        AllMaterials", "true");
                    AppendLine(sb, "    }");
                }
                AppendLine(sb, "]");
            }

            if (defaultPaintColors.Count > 0)
            {
                AppendLine(sb, "DefaultPaintColors");
                AppendLine(sb, "[");
                foreach (Color color in defaultPaintColors)
                {
                    string colorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
                    switch (colorHex.ToLowerInvariant())
                    {
                        case "#475e83":
                            AppendLine(sb, "    " + colorHex + " // Coalition");
                            break;
                        case "#a69884":
                            AppendLine(sb, "    " + colorHex + " // Desert");
                            break;
                        case "#437c44":
                            AppendLine(sb, "    " + colorHex + " // Forest");
                            break;
                        case "#495631":
                            AppendLine(sb, "    " + colorHex + " // Russia");
                            break;
                        default:
                            AppendLine(sb, "    " + colorHex);
                            break;
                    }
                }
                AppendLine(sb, "]");
            }

            if (turretCount > 0)
            {
                AppendLine(sb, "Turrets", turretCount);
                for (int i = 0; i < turrets.Count; i++)
                {
                    var turret = turrets[i];
                    AppendLine(sb, "Turret_" + i + "_Seat_Index", turret.seatIndex);
                    AppendLine(sb, "Turret_" + i + "_Item_ID", turret.itemId);
                    AppendLine(sb, "Turret_" + i + "_Yaw_Min", turret.yawMin);
                    AppendLine(sb, "Turret_" + i + "_Yaw_Max", turret.yawMax);
                    AppendLine(sb, "Turret_" + i + "_Pitch_Min", turret.pitchMin);
                    AppendLine(sb, "Turret_" + i + "_Pitch_Max", turret.pitchMax);
                    if (turret.ignoreAimCamera) AppendLine(sb, "Turret_" + i + "_Ignore_Aim_Camera");
                }
            }

            if (explosion > 0) AppendLine(sb, "Explosion", explosion);
            if (!string.IsNullOrEmpty(explosionGuid))
            {
                AppendLine(sb, "Explosion", explosionGuid);
                if (explosionForceMultiplier != 1.0f)
                    AppendLine(sb, "Explosion_Force_Multiplier", explosionForceMultiplier);
                if (explosionMaxForce != new Vector3(0, 1024, 0))
                    AppendLine(sb, "Explosion_Max_Force", explosionMaxForce.ToString("F1"));
                if (explosionMinForce != new Vector3(0, 1024, 0))
                    AppendLine(sb, "Explosion_Min_Force", explosionMinForce.ToString("F1"));
                if (!shouldExplosionBurnMaterials)
                    AppendLine(sb, "ShouldExplosionBurnMaterials", "false");
                if (!shouldExplosionCauseDamage)
                    AppendLine(sb, "ShouldExplosionCauseDamage", "false");
            }

            return sb.ToString();
        }

        private void AppendLine(StringBuilder sb, string key)
        {
            sb.AppendLine(key);
        }

        private void AppendLine(StringBuilder sb, string key, object value)
        {
            sb.AppendLine($"{key} {value}");
        }

        public string GetEnglishText()
        {
            StringBuilder sb = new StringBuilder();
            
            if (!string.IsNullOrEmpty(title))
                sb.AppendLine("Name " + title);
                
            if (!string.IsNullOrEmpty(description))
                sb.AppendLine("Description " + description);
                
            return sb.ToString();
        }

        public void SaveVehicleDatFile(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            string vehicleText = GetText();
            string assetPath = AssetDatabase.GetAssetPath(this);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            
            Debug.Log($"[Vehicle Save] Asset Path: {assetPath}");
            Debug.Log($"[Vehicle Save] Derived Filename (no ext): {fileName}");

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            fileName = fileName.Replace(' ', '_');
            
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "Vehicle_" + GetInstanceID();
                Debug.LogWarning($"Using fallback filename for vehicle: {fileName}");
            }
            
            string filePath = Path.Combine(folderPath, fileName + ".dat");
            Debug.Log($"[Vehicle Save] Final FilePath: {filePath}");
            File.WriteAllText(filePath, vehicleText);
            
            Debug.Log("Vehicle DAT file saved to: " + filePath);
        }

        public void SaveEnglishLanguageFile(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            string englishText = GetEnglishText();
            if (string.IsNullOrEmpty(englishText))
                return;
                
            string filePath = Path.Combine(folderPath, "English.dat");
            File.WriteAllText(filePath, englishText);
            
            Debug.Log("English language file saved to: " + filePath);
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
                SaveVehicleDatFile(folderPath);
                
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
                        Debug.Log($"Copied vehicle asset file from {fullPath} to {targetPath}");
                    }
                    else
                    {
                        string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6); 
                        string fullProjectPath = Path.Combine(projectPath, assetPath);
                        
                        if (File.Exists(fullProjectPath))
                        {
                            File.Copy(fullProjectPath, targetPath, true);
                            Debug.Log($"Copied vehicle asset file from {fullProjectPath} to {targetPath}");
                        }
                        else
                        {
                            Debug.LogWarning($"Could not find vehicle asset file at {fullPath} or {fullProjectPath}");
                        }
                    }
                }
                
                Debug.Log($"All vehicle files saved to: {folderPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving vehicle files: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    [System.Serializable]
    public class VehicleWheelConfiguration
    {
        public string wheelColliderPath;
        public string modelPath;
        public bool isColliderPowered;
        public bool isColliderSteered;
        public bool modelUseColliderPose;
    }

    [System.Serializable]
    public class PaintableVehicleSection
    {
        public string path;
        public int materialIndex;
        public bool allMaterials;
    }

    [System.Serializable]
    public class VehicleRandomPaintColorConfiguration
    {
        public float minSaturation;
        public float maxSaturation;
        public float minValue;
        public float maxValue;
        public float grayscaleChance;
    }

    [System.Serializable]
    public class EngineSoundSettings
    {
        public float idlePitch = 1.0f;
        public float idleVolume = 0.75f;
        public float maxPitch = 2.0f;
        public float maxVolume = 1.0f;
    }

    [System.Serializable]
    public class VehicleTurret
    {
        public byte seatIndex;
        public ushort itemId;
        public float yawMin;
        public float yawMax;
        public float pitchMin;
        public float pitchMax;
        public bool ignoreAimCamera;
    }

    public enum EEngine
    {
        Car,
        Plane,
        Blimp,
        Boat,
        Train,
        Helicopter
    }

    public enum ERarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythical
    }

    public enum EVehicleDefaultPaintColorMode
    {
        None,
        List,
        RandomHueOrGrayscale
    }

    public enum EEngineSoundType
    {
        None,
        EngineRPMSimple
    }
} 