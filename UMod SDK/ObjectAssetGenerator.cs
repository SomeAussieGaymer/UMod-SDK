using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;
using System.IO;

public class Resource : MonoBehaviour
{
    public int healthPoints;
    public int maxHealthPoints;
    public bool dropable;
    public int dropObjectCount;
}

public class FoliageEffect : MonoBehaviour
{
    public Renderer[] renderers;
}

public class ObjectAssetGenerator : EditorWindow
{
    private enum ObjectType { Small, Medium, Large, Resource, Trees, Bushes }

    private static readonly Dictionary<string, int> LayerCache = new Dictionary<string, int>();
    private static readonly Dictionary<ObjectType, System.Action<ObjectAssetGenerator, string>> PrefabGenerators = 
        new Dictionary<ObjectType, System.Action<ObjectAssetGenerator, string>>();
    private delegate void ComponentSetup(GameObject obj);
    private static readonly Dictionary<string, ComponentSetup> ComponentSetups = new Dictionary<string, ComponentSetup>();

    private const string BASE_FOLDER = "Assets/Objects";
    private bool useCustomExportPath = false;
    private string customExportPath = BASE_FOLDER;
    
    private static readonly Dictionary<ObjectType, string> TypeFolders = new Dictionary<ObjectType, string>()
    {
        { ObjectType.Small, "Small" },
        { ObjectType.Medium, "Medium" },
        { ObjectType.Large, "Large" },
        { ObjectType.Resource, "Resources" },
        { ObjectType.Trees, "Trees" },
        { ObjectType.Bushes, "Bushes" }
    };

    private static readonly Dictionary<ObjectType, float[]> LODPresets = new Dictionary<ObjectType, float[]>
    {
        { ObjectType.Small, new float[] { 1f } },
        { ObjectType.Medium, new float[] { 1f } },
        { ObjectType.Large, new float[] { 1f } }
    };

    private static readonly Dictionary<ObjectType, float> DefaultCullingDistances = new Dictionary<ObjectType, float>
    {
        { ObjectType.Small, 50f },
        { ObjectType.Medium, 100f },
        { ObjectType.Large, 200f }
    };

    private static readonly Dictionary<ObjectType, float> DefaultShadowDistances = new Dictionary<ObjectType, float>
    {
        { ObjectType.Small, 25f },
        { ObjectType.Medium, 50f },
        { ObjectType.Large, 100f }
    };

    private ObjectType selectedType;
    private string objectName = "";
    private Mesh mainMesh;
    private Mesh metalMesh;
    private Mesh stumpMesh;
    private Material mainMaterial;
    private PhysicMaterial colliderMaterial;
    private List<Mesh> childMeshes = new List<Mesh>();
    
    private bool createSkybox;
    private Mesh skyboxMesh;
    
    private bool configureLOD = false;
    private List<float> lodTransitions = new List<float> { 10f };
    private bool useLODPreset = true;
    private float lodBias = 1.0f;
    private bool useCustomLODSettings = false;
    private float cullingDistance = 100f;
    private float shadowDistance = 50f;
    
    private SerializedObject serializedObject;
    private Vector2 scrollPosition;

    private Mesh trunkMesh;
    private Mesh leavesMesh;
    private Mesh treeStumpMesh;
    private Texture2D trunkTexture;
    private Texture2D leavesTexture;
    private PhysicMaterial treePhysicsMaterial;
    private Shader foliageShader;
    private Material trunkMaterial;
    private Material leavesMaterial;
    
    private List<Mesh> additionalTrunkMeshes = new List<Mesh>();
    private List<Mesh> additionalLeavesMeshes = new List<Mesh>();
    private List<Mesh> additionalStumpMeshes = new List<Mesh>();

    private List<Mesh> bushModels = new List<Mesh>() { null };
    private Texture2D bushTexture;
    private bool hasBushForage = false;
    private Mesh bushForageMesh;
    private Material bushMaterial;
    private PhysicMaterial bushPhysicsMaterial;

    static ObjectAssetGenerator()
    {
        InitializePrefabGenerators();
        InitializeComponentSetups();
    }

    private static void InitializePrefabGenerators()
    {
        PrefabGenerators[ObjectType.Small] = (generator, path) => generator.GenerateObjectPrefabs(path);
        PrefabGenerators[ObjectType.Medium] = (generator, path) => generator.GenerateObjectPrefabs(path);
        PrefabGenerators[ObjectType.Large] = (generator, path) => generator.GenerateObjectPrefabs(path);
        PrefabGenerators[ObjectType.Resource] = (generator, path) => generator.GenerateResourcePrefabs(path);
        PrefabGenerators[ObjectType.Trees] = (generator, path) => generator.GenerateTreePrefabs(path);
        PrefabGenerators[ObjectType.Bushes] = (generator, path) => generator.GenerateBushPrefabs(path);
    }

    private static void InitializeComponentSetups()
    {
        ComponentSetups["NavMesh"] = (obj) =>
        {
            obj.tag = "Navmesh";
            obj.layer = GetOrCreateLayer("Navmesh");
            var collider = obj.AddComponent<MeshCollider>();
            collider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | 
                                    MeshColliderCookingOptions.EnableMeshCleaning | 
                                    MeshColliderCookingOptions.WeldColocatedVertices | 
                                    MeshColliderCookingOptions.UseFastMidphase;
            collider.sharedMesh = null;
        };

        ComponentSetups["Object"] = (obj) =>
        {
            var collider = obj.AddComponent<MeshCollider>();
            collider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | 
                                    MeshColliderCookingOptions.EnableMeshCleaning | 
                                    MeshColliderCookingOptions.WeldColocatedVertices | 
                                    MeshColliderCookingOptions.UseFastMidphase;
        };
    }

    private static int GetOrCreateLayer(string layerName)
    {
        if (LayerCache.TryGetValue(layerName, out int layerIndex))
        {
            return layerIndex;
        }
        
        layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex != -1)
        {
            LayerCache[layerName] = layerIndex;
            return layerIndex;
        }
        
        
        Debug.LogWarning($"Layer '{layerName}' not found. Using default layer.");
        return 0;
    }

    [MenuItem("UMod SDK/Object & Resource Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<ObjectAssetGenerator>("Object & Resource Generator");
        window.minSize = new Vector2(400, 600);
    }

    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
    }

    private void EnsureFolderExists(string parentPath, string folderName)
    {
        string fullPath = Path.Combine(parentPath, folderName).Replace("\\", "/");
        
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentPath, folderName);
            AssetDatabase.Refresh();
        }
    }

    private void GeneratePrefabsAsync()
    {
        string basePath = useCustomExportPath ? customExportPath : BASE_FOLDER;
        string targetFolder = basePath;

        try
        {
            string parentFolder = Path.GetDirectoryName(basePath);
            string folderName = Path.GetFileName(basePath);
            
            if (!string.IsNullOrEmpty(parentFolder) && !string.IsNullOrEmpty(folderName))
            {
                EnsureFolderExists(parentFolder, folderName);
            }

            if (!string.IsNullOrEmpty(objectName))
            {
                targetFolder = Path.Combine(basePath, objectName).Replace("\\", "/");
                EnsureFolderExists(basePath, objectName);
            }

            if (selectedType == ObjectType.Trees)
            {
                EnsureFolderExists(targetFolder, "Materials");
                EnsureFolderExists(targetFolder, "Textures");
                
                ProcessTreeTextures(targetFolder);
            }

            PrefabGenerators[selectedType](this, targetFolder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Prefabs generated successfully in {targetFolder}!", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating prefabs: {e.Message}\nPath: {targetFolder}\nStack: {e.StackTrace}");
            EditorUtility.DisplayDialog("Error", $"Failed to generate prefabs: {e.Message}", "OK");
        }
    }

    private void OnGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space(10);
        
        DrawTypeSelection();
        DrawExportPathSettings();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.Space(10);
        
        switch (selectedType)
        {
            case ObjectType.Trees:
                DrawTreeSettings();
                break;
            case ObjectType.Bushes:
                DrawBushSettings();
                break;
            case ObjectType.Resource:
                DrawResourceSettings();
                break;
            default:
                DrawStandardObjectSettings();
                break;
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        DrawGenerateButton();
        DrawHelpInfo();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawTypeSelection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Type");
        selectedType = (ObjectType)EditorGUILayout.EnumPopup(selectedType);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Name");
        objectName = EditorGUILayout.TextField(objectName);
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawExportPathSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        useCustomExportPath = EditorGUILayout.Toggle("Use Custom Export Path", useCustomExportPath);
        
        if (useCustomExportPath)
        {
            EditorGUILayout.BeginHorizontal();
            customExportPath = EditorGUILayout.TextField("Export Path", customExportPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Export Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.Contains(Application.dataPath))
                    {
                        customExportPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside the Assets directory.", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }
    
    private void DrawTreeSettings()
    {
        DrawSectionHeader("Tree Settings");
        DrawTreeBaseModels();
        
        EditorGUILayout.Space(5);
        
        DrawSectionHeader("Additional Tree Models");
        DrawAdditionalTreeModels();
        
        EditorGUILayout.Space(5);
        
        DrawSectionHeader("Stump Settings");
        DrawTreeStumpModels();
        
        EditorGUILayout.Space(5);
        
        DrawSectionHeader("Materials");
        DrawTreeMaterials();
    }
    
    private void DrawTreeBaseModels()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Base Model", EditorStyles.boldLabel);
        trunkMesh = (Mesh)EditorGUILayout.ObjectField("Trunk Mesh (Model_0)", trunkMesh, typeof(Mesh), false);
        leavesMesh = (Mesh)EditorGUILayout.ObjectField("Leaves Mesh (Foliage_0)", leavesMesh, typeof(Mesh), false);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawAdditionalTreeModels()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        for (int i = 0; i < additionalTrunkMeshes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Model_{i + 1}", GUILayout.Width(60));
            
            EditorGUILayout.BeginVertical();
            additionalTrunkMeshes[i] = (Mesh)EditorGUILayout.ObjectField("Trunk", additionalTrunkMeshes[i], typeof(Mesh), false);
            additionalLeavesMeshes[i] = (Mesh)EditorGUILayout.ObjectField("Leaves", additionalLeavesMeshes[i], typeof(Mesh), false);
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(40)))
            {
                additionalTrunkMeshes.RemoveAt(i);
                additionalLeavesMeshes.RemoveAt(i);
                i--;
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (i < additionalTrunkMeshes.Count - 1)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space(2);
            }
        }
        
        EditorGUILayout.Space(2);
        if (GUILayout.Button("Add Tree Model"))
        {
            additionalTrunkMeshes.Add(null);
            additionalLeavesMeshes.Add(null);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawTreeStumpModels()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        treeStumpMesh = (Mesh)EditorGUILayout.ObjectField("Stump Mesh (Model_0)", treeStumpMesh, typeof(Mesh), false);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Additional Stump Models", EditorStyles.boldLabel);
        
        for (int i = 0; i < additionalStumpMeshes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Model_{i + 1}", GUILayout.Width(60));
            additionalStumpMeshes[i] = (Mesh)EditorGUILayout.ObjectField("Mesh", additionalStumpMeshes[i], typeof(Mesh), false);
            
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                additionalStumpMeshes.RemoveAt(i);
                i--;
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (i < additionalStumpMeshes.Count - 1)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space(2);
            }
        }
        
        EditorGUILayout.Space(2);
        if (GUILayout.Button("Add Stump Model"))
        {
            additionalStumpMeshes.Add(null);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawTreeMaterials()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        trunkTexture = (Texture2D)EditorGUILayout.ObjectField("Trunk Texture", trunkTexture, typeof(Texture2D), false);
        leavesTexture = (Texture2D)EditorGUILayout.ObjectField("Leaves Texture", leavesTexture, typeof(Texture2D), false);
        treePhysicsMaterial = (PhysicMaterial)EditorGUILayout.ObjectField("Physics Material", treePhysicsMaterial, typeof(PhysicMaterial), false);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawResourceSettings()
    {
        DrawSectionHeader("Model Settings");
            
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        mainMesh = (Mesh)EditorGUILayout.ObjectField("Main Mesh", mainMesh, typeof(Mesh), false);
        metalMesh = (Mesh)EditorGUILayout.ObjectField("Metal Mesh", metalMesh, typeof(Mesh), false);
        stumpMesh = (Mesh)EditorGUILayout.ObjectField("Stump Mesh", stumpMesh, typeof(Mesh), false);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        DrawSectionHeader("Materials");
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        mainMaterial = (Material)EditorGUILayout.ObjectField("Material", mainMaterial, typeof(Material), false);
        colliderMaterial = (PhysicMaterial)EditorGUILayout.ObjectField("Physics Material", colliderMaterial, typeof(PhysicMaterial), false);
        EditorGUILayout.EndVertical();
    }
    
    private void DrawStandardObjectSettings()
    {
        DrawSectionHeader("Model Settings");
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        mainMesh = (Mesh)EditorGUILayout.ObjectField("Main Mesh", mainMesh, typeof(Mesh), false);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        DrawSectionHeader("Materials");
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        mainMaterial = (Material)EditorGUILayout.ObjectField("Material", mainMaterial, typeof(Material), false);
        colliderMaterial = (PhysicMaterial)EditorGUILayout.ObjectField("Physics Material", colliderMaterial, typeof(PhysicMaterial), false);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        DrawSectionHeader("Child Models");
        DrawChildMeshes();
        
        EditorGUILayout.Space(5);
        
        DrawSectionHeader("Skybox");
        DrawSkyboxSettings();
        
        EditorGUILayout.Space(5);
        
        DrawSectionHeader("LOD Settings");
        DrawLODSettings();
    }
    
    private void DrawChildMeshes()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
        for (int i = 0; i < childMeshes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            childMeshes[i] = (Mesh)EditorGUILayout.ObjectField($"Model_{i + 1}", childMeshes[i], typeof(Mesh), false);
            
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                childMeshes.RemoveAt(i);
                i--;
            }
            
            EditorGUILayout.EndHorizontal();
                    
            if (i < childMeshes.Count - 1)
            {
                EditorGUILayout.Space(2);
            }
        }

        EditorGUILayout.Space(2);
        if (GUILayout.Button("Add Model")) childMeshes.Add(null);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawSectionHeader(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
    
    private void DrawSkyboxSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        createSkybox = EditorGUILayout.Toggle("Create Skybox", createSkybox);
        
        if (createSkybox)
        {
            EditorGUI.indentLevel++;
            skyboxMesh = (Mesh)EditorGUILayout.ObjectField("Skybox Mesh", skyboxMesh, typeof(Mesh), false);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawLODSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        configureLOD = EditorGUILayout.Toggle("Configure LOD", configureLOD);
        
        if (configureLOD)
        {
            EditorGUI.indentLevel++;
            
            useLODPreset = EditorGUILayout.Toggle("Use LOD Preset", useLODPreset);
            
            if (useLODPreset)
            {
                DrawLODPresetSettings();
            }
            else
            {
                DrawCustomLODSettings();
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawLODPresetSettings()
    {
        EditorGUILayout.HelpBox($"Using default preset: LOD_0 (Model_0) with 1% transition", MessageType.Info);
        lodBias = EditorGUILayout.Slider("LOD Bias", lodBias, 0.1f, 2f);
        
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.FloatField("LOD_0 Transition (%)", 1f * lodBias);
        EditorGUI.EndDisabledGroup();
    }
    
    private void DrawCustomLODSettings()
    {
        useCustomLODSettings = EditorGUILayout.Toggle("Custom LOD Settings", useCustomLODSettings);
        
        if (useCustomLODSettings)
        {
            cullingDistance = EditorGUILayout.FloatField("Culling Distance", cullingDistance);
            shadowDistance = EditorGUILayout.FloatField("Shadow Distance", shadowDistance);
        }
        else
        {
            cullingDistance = DefaultCullingDistances[selectedType];
            shadowDistance = DefaultShadowDistances[selectedType];
        }

        while (lodTransitions.Count < 1)
        {
            lodTransitions.Add(1f);
        }

        lodTransitions[0] = EditorGUILayout.FloatField("LOD_0 Transition (%)", lodTransitions[0]);

        for (int i = 1; i < lodTransitions.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            lodTransitions[i] = EditorGUILayout.FloatField($"LOD_{i} Transition (%)", lodTransitions[i]);
            
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                lodTransitions.RemoveAt(i);
                i--;
            }
            
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add LOD Level"))
        {
            float lastTransition = lodTransitions.Count > 0 ? lodTransitions[lodTransitions.Count - 1] : 1f;
            lodTransitions.Add(Mathf.Max(lastTransition / 2, 0.01f));
        }
    }

    private void DrawGenerateButton()
    {
        EditorGUI.BeginDisabledGroup(!IsValid());
        if (GUILayout.Button("Generate"))
        {
            GeneratePrefabsAsync();
        }
        EditorGUI.EndDisabledGroup();
    }
    
    private void DrawHelpInfo()
    {
        EditorGUILayout.Space(5);
        
        string exportPath = useCustomExportPath ? customExportPath : BASE_FOLDER;
        string finalPath = !string.IsNullOrEmpty(objectName) ? $"{exportPath}/{objectName}" : exportPath;
        EditorGUILayout.HelpBox($"Objects will be saved to: {finalPath}", MessageType.Info);
        
        if (selectedType == ObjectType.Small || selectedType == ObjectType.Medium || selectedType == ObjectType.Large)
        {
            EditorGUILayout.HelpBox("Due to limitations, slots for doors, windows, and hatches cannot be generated for you", MessageType.Warning);
        }
    }

    private bool IsValid()
    {
        if (selectedType == ObjectType.Trees)
        {
            bool baseModelsValid = trunkMesh != null && leavesMesh != null && treeStumpMesh != null && 
                                 trunkTexture != null && leavesTexture != null && treePhysicsMaterial != null;
            
            bool additionalModelsValid = true;
            for (int i = 0; i < additionalTrunkMeshes.Count; i++)
            {
                bool modelPairValid = (additionalTrunkMeshes[i] != null && additionalLeavesMeshes[i] != null) || 
                                     (additionalTrunkMeshes[i] == null && additionalLeavesMeshes[i] == null);
                
                if (!modelPairValid)
                {
                    additionalModelsValid = false;
                    break;
                }
            }
            
            return baseModelsValid && additionalModelsValid;
        }
        
        if (selectedType == ObjectType.Bushes)
        {
            bool baseValid = bushModels.Count > 0 && bushModels[0] != null && bushTexture != null && bushPhysicsMaterial != null;
            
            bool forageValid = !hasBushForage || (hasBushForage && bushForageMesh != null);
            
            return baseValid && forageValid;
        }
        
        if (mainMesh == null || mainMaterial == null) return false;
        if (selectedType == ObjectType.Resource && (metalMesh == null || stumpMesh == null)) return false;
        return true;
    }

    private void ProcessTreeTextures(string targetFolder)
    {
        try
        {
            string materialsFolder = Path.Combine(targetFolder, "Materials").Replace("\\", "/");
            EnsureFolderExists(Path.GetDirectoryName(materialsFolder), Path.GetFileName(materialsFolder));
            
            if (trunkTexture == null)
            {
                Debug.LogError("Trunk texture is null.");
                trunkMaterial = CreateFallbackMaterial("Standard", new Color(0.545f, 0.27f, 0.075f));
            }
            
            if (leavesTexture == null)
            {
                Debug.LogError("Leaves texture is null.");
                leavesMaterial = CreateFallbackMaterial("Standard", new Color(0.0f, 0.5f, 0.0f));
            }
            
            if (trunkTexture != null && leavesTexture != null)
            {
                Debug.Log($"Using trunk texture directly: {AssetDatabase.GetAssetPath(trunkTexture)}");
                Debug.Log($"Using leaves texture directly: {AssetDatabase.GetAssetPath(leavesTexture)}");
                
                trunkMaterial = new Material(Shader.Find("Standard"));
                trunkMaterial.mainTexture = trunkTexture;
                trunkMaterial.SetFloat("_Mode", 0);
                trunkMaterial.SetColor("_SpecColor", new Color(0, 0, 0));
                trunkMaterial.SetFloat("_SpecSource", 1);
                trunkMaterial.SetFloat("_Glossiness", 0);
                
                string trunkMaterialPath = Path.Combine(materialsFolder, "TreeTrunk.mat").Replace("\\", "/");
                AssetDatabase.CreateAsset(trunkMaterial, trunkMaterialPath);
                
                Shader foliageShader = FindOrCreateFoliageShader();
                if (foliageShader == null)
                {
                    Debug.LogWarning("Foliage shader not found, using Standard shader for leaves.");
                    foliageShader = Shader.Find("Standard");
                }
                
                leavesMaterial = new Material(foliageShader);
                leavesMaterial.mainTexture = leavesTexture;
                
                if (foliageShader.name.Contains("Foliage"))
                {
                    leavesMaterial.SetVector("_WaveAndDistance", new Vector4(12f, 3.6f, 0.5f, 1f));
                    leavesMaterial.SetFloat("_Cutoff", 0.2f);
                }
                leavesMaterial.renderQueue = 2450;
                
                string leavesMaterialPath = Path.Combine(materialsFolder, "TreeLeaves.mat").Replace("\\", "/");
                AssetDatabase.CreateAsset(leavesMaterial, leavesMaterialPath);
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ProcessTreeTextures: {e.Message}\n{e.StackTrace}");
            
            if (trunkMaterial == null) trunkMaterial = CreateFallbackMaterial("Standard", new Color(0.545f, 0.27f, 0.075f));
            if (leavesMaterial == null) leavesMaterial = CreateFallbackMaterial("Standard", new Color(0.0f, 0.5f, 0.0f));
        }
    }
    
    private Material CreateFallbackMaterial(string shaderName, Color color)
    {
        try
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Standard");
            
            Material material = new Material(shader);
            material.color = color;
            return material;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating fallback material: {e.Message}");
            return null;
        }
    }

    private void GenerateObjectPrefabs(string folderPath)
    {
        var navPrefab = new GameObject("Nav");
        navPrefab.tag = "Navmesh";
        navPrefab.layer = LayerMask.NameToLayer("Navmesh");
        var navCollider = navPrefab.AddComponent<MeshCollider>();
        navCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | 
                                    MeshColliderCookingOptions.EnableMeshCleaning | 
                                    MeshColliderCookingOptions.WeldColocatedVertices | 
                                    MeshColliderCookingOptions.UseFastMidphase;
        navCollider.sharedMesh = mainMesh;
        SavePrefab(navPrefab, folderPath, "Nav");

        var objectPrefab = new GameObject("Object");
        objectPrefab.tag = selectedType.ToString();
        objectPrefab.layer = LayerMask.NameToLayer(selectedType.ToString());
        
        var objCollider = objectPrefab.AddComponent<MeshCollider>();
        objCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | 
                                    MeshColliderCookingOptions.EnableMeshCleaning | 
                                    MeshColliderCookingOptions.WeldColocatedVertices | 
                                    MeshColliderCookingOptions.UseFastMidphase;
        objCollider.sharedMesh = mainMesh;
        objCollider.sharedMaterial = colliderMaterial;

        SetupLODGroup(objectPrefab);

        SavePrefab(objectPrefab, folderPath, "Object");

        if (createSkybox)
        {
            var skyboxPrefab = new GameObject("Skybox");
            skyboxPrefab.tag = selectedType.ToString();
            skyboxPrefab.layer = LayerMask.NameToLayer(selectedType.ToString());

            var model = new GameObject("Model_0");
            model.transform.SetParent(skyboxPrefab.transform);
            model.tag = skyboxPrefab.tag;
            model.layer = skyboxPrefab.layer;
            
            var meshFilter = model.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = skyboxMesh != null ? skyboxMesh : mainMesh;
            
            var meshRenderer = model.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = mainMaterial;

            SavePrefab(skyboxPrefab, folderPath, "Skybox");
        }
    }

    private void GenerateResourcePrefabs(string folderPath)
    {
        var navPrefab = new GameObject("Nav");
        navPrefab.tag = "Navmesh";
        navPrefab.layer = LayerMask.NameToLayer("Navmesh");
        
        var navCollider = navPrefab.AddComponent<MeshCollider>();
        navCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | 
                                    MeshColliderCookingOptions.EnableMeshCleaning | 
                                    MeshColliderCookingOptions.WeldColocatedVertices | 
                                    MeshColliderCookingOptions.UseFastMidphase;
        navCollider.sharedMesh = mainMesh;
        SavePrefab(navPrefab, folderPath, "Nav");
        
        var objectPrefab = new GameObject("Object");
        objectPrefab.tag = selectedType.ToString();
        objectPrefab.layer = LayerMask.NameToLayer(selectedType.ToString());
        
        var model = new GameObject("Model_0");
        model.transform.SetParent(objectPrefab.transform);
        model.tag = objectPrefab.tag;
        model.layer = objectPrefab.layer;

        var meshFilter = model.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mainMesh;
        
        var meshRenderer = model.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mainMaterial;
        
        var objCollider = model.AddComponent<MeshCollider>();
        objCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | 
                                     MeshColliderCookingOptions.EnableMeshCleaning | 
                                     MeshColliderCookingOptions.WeldColocatedVertices | 
                                     MeshColliderCookingOptions.UseFastMidphase;
        objCollider.sharedMesh = mainMesh;
        objCollider.sharedMaterial = colliderMaterial;
        
        var modelMetal = new GameObject("Model_Metal");
        modelMetal.transform.SetParent(objectPrefab.transform);
        modelMetal.tag = objectPrefab.tag;
        modelMetal.layer = objectPrefab.layer;
        
        var metalMeshFilter = modelMetal.AddComponent<MeshFilter>();
        metalMeshFilter.sharedMesh = metalMesh;
        
        var metalMeshRenderer = modelMetal.AddComponent<MeshRenderer>();
        metalMeshRenderer.sharedMaterial = mainMaterial;
        
        var modelStump = new GameObject("Model_Stump");
        modelStump.transform.SetParent(objectPrefab.transform);
        modelStump.tag = objectPrefab.tag;
        modelStump.layer = objectPrefab.layer;
        
        var stumpMeshFilter = modelStump.AddComponent<MeshFilter>();
        stumpMeshFilter.sharedMesh = stumpMesh;
        
        var stumpMeshRenderer = modelStump.AddComponent<MeshRenderer>();
        stumpMeshRenderer.sharedMaterial = mainMaterial;
        
        var resourceComponent = objectPrefab.AddComponent<Resource>();
        resourceComponent.healthPoints = 100;
        resourceComponent.maxHealthPoints = 100;
        resourceComponent.dropable = true;
        resourceComponent.dropObjectCount = 5;
        
        SavePrefab(objectPrefab, folderPath, "Object");
        
        if (createSkybox)
        {
            var skyboxPrefab = new GameObject("Skybox");
            skyboxPrefab.tag = selectedType.ToString();
            skyboxPrefab.layer = LayerMask.NameToLayer(selectedType.ToString());
            
            var skyboxModel = new GameObject("Model_0");
            skyboxModel.transform.SetParent(skyboxPrefab.transform);
            skyboxModel.tag = skyboxPrefab.tag;
            skyboxModel.layer = skyboxPrefab.layer;
            
            var skyboxMeshFilter = skyboxModel.AddComponent<MeshFilter>();
            skyboxMeshFilter.sharedMesh = skyboxMesh != null ? skyboxMesh : mainMesh;
            
            var skyboxMeshRenderer = skyboxModel.AddComponent<MeshRenderer>();
            skyboxMeshRenderer.sharedMaterial = mainMaterial;
            
            SavePrefab(skyboxPrefab, folderPath, "Skybox");
        }
    }
    
    private void GenerateTreePrefabs(string folderPath)
    {
        ProcessTreeTextures(folderPath);
        
        var navPrefab = new GameObject("Nav");
        navPrefab.tag = "Navmesh";
        navPrefab.layer = LayerMask.NameToLayer("Navmesh");
        var navCollider = navPrefab.AddComponent<MeshCollider>();
        navCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | 
                                    MeshColliderCookingOptions.EnableMeshCleaning | 
                                    MeshColliderCookingOptions.WeldColocatedVertices | 
                                    MeshColliderCookingOptions.UseFastMidphase;
        navCollider.sharedMesh = trunkMesh;
        
        SavePrefab(navPrefab, folderPath, "Nav");
        
        var objectPrefab = new GameObject("Object");
        objectPrefab.tag = selectedType.ToString();
        objectPrefab.layer = LayerMask.NameToLayer(selectedType.ToString());
        
        var trunkModel = new GameObject("Model_Trunk");
        trunkModel.transform.SetParent(objectPrefab.transform);
        trunkModel.tag = objectPrefab.tag;
        trunkModel.layer = objectPrefab.layer;
        
        var trunkMeshFilter = trunkModel.AddComponent<MeshFilter>();
        trunkMeshFilter.sharedMesh = trunkMesh;
        
        var trunkMeshRenderer = trunkModel.AddComponent<MeshRenderer>();
        trunkMeshRenderer.sharedMaterial = trunkMaterial;
        
        var objCollider = trunkModel.AddComponent<MeshCollider>();
        objCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | 
                                    MeshColliderCookingOptions.EnableMeshCleaning | 
                                    MeshColliderCookingOptions.WeldColocatedVertices | 
                                    MeshColliderCookingOptions.UseFastMidphase;
        objCollider.sharedMesh = trunkMesh;
        objCollider.sharedMaterial = treePhysicsMaterial;
        
        var leavesModel = new GameObject("Model_Leaves");
        leavesModel.transform.SetParent(objectPrefab.transform);
        leavesModel.tag = objectPrefab.tag;
        leavesModel.layer = objectPrefab.layer;
        
        var leavesMeshFilter = leavesModel.AddComponent<MeshFilter>();
        leavesMeshFilter.sharedMesh = leavesMesh;
        
        var leavesMeshRenderer = leavesModel.AddComponent<MeshRenderer>();
        leavesMeshRenderer.sharedMaterial = leavesMaterial;
        
        var stumpModel = new GameObject("Model_Stump");
        stumpModel.transform.SetParent(objectPrefab.transform);
        stumpModel.tag = objectPrefab.tag;
        stumpModel.layer = objectPrefab.layer;
        
        var stumpMeshFilter = stumpModel.AddComponent<MeshFilter>();
        stumpMeshFilter.sharedMesh = treeStumpMesh;
        
        var stumpMeshRenderer = stumpModel.AddComponent<MeshRenderer>();
        stumpMeshRenderer.sharedMaterial = trunkMaterial;
        
        for (int i = 0; i < Mathf.Min(additionalTrunkMeshes.Count, additionalLeavesMeshes.Count); i++)
        {
            if (additionalTrunkMeshes[i] != null && additionalLeavesMeshes[i] != null)
            {
                var additionalTrunkModel = new GameObject($"Model_AdditionalTrunk_{i + 1}");
                additionalTrunkModel.transform.SetParent(objectPrefab.transform);
                additionalTrunkModel.tag = objectPrefab.tag;
                additionalTrunkModel.layer = objectPrefab.layer;
                
                var addTrunkMeshFilter = additionalTrunkModel.AddComponent<MeshFilter>();
                addTrunkMeshFilter.sharedMesh = additionalTrunkMeshes[i];
                
                var addTrunkMeshRenderer = additionalTrunkModel.AddComponent<MeshRenderer>();
                addTrunkMeshRenderer.sharedMaterial = trunkMaterial;
                
                var additionalLeavesModel = new GameObject($"Model_AdditionalLeaves_{i + 1}");
                additionalLeavesModel.transform.SetParent(objectPrefab.transform);
                additionalLeavesModel.tag = objectPrefab.tag;
                additionalLeavesModel.layer = objectPrefab.layer;
                
                var addLeavesMeshFilter = additionalLeavesModel.AddComponent<MeshFilter>();
                addLeavesMeshFilter.sharedMesh = additionalLeavesMeshes[i];
                
                var addLeavesMeshRenderer = additionalLeavesModel.AddComponent<MeshRenderer>();
                addLeavesMeshRenderer.sharedMaterial = leavesMaterial;
            }
        }
        
        var resourceComponent = objectPrefab.AddComponent<Resource>();
        resourceComponent.healthPoints = 100;
        resourceComponent.maxHealthPoints = 100;
        resourceComponent.dropable = true;
        resourceComponent.dropObjectCount = 5;
        
        var foliageComponent = objectPrefab.AddComponent<FoliageEffect>();
        foliageComponent.renderers = objectPrefab.GetComponentsInChildren<MeshRenderer>()
            .Where(mr => mr.gameObject.name.Contains("Leaves")).ToArray();
        
        SavePrefab(objectPrefab, folderPath, "Object");
    }
    
    private void GenerateBushPrefabs(string folderPath)
    {
        string materialsFolder = Path.Combine(folderPath, "Materials").Replace("\\", "/");
        EnsureFolderExists(Path.GetDirectoryName(materialsFolder), Path.GetFileName(materialsFolder));
        
        if (bushTexture == null)
        {
            Debug.LogError("Bush texture is null.");
            bushMaterial = CreateFallbackMaterial("Standard", new Color(0.0f, 0.5f, 0.0f));
        }
        else
        {
            Shader foliageShader = FindOrCreateFoliageShader();
            if (foliageShader == null)
            {
                Debug.LogWarning("Foliage shader not found, using Standard shader for bush.");
                foliageShader = Shader.Find("Standard");
            }
            
            bushMaterial = new Material(foliageShader);
            bushMaterial.mainTexture = bushTexture;
            
            if (foliageShader.name.Contains("Foliage"))
            {
                bushMaterial.SetVector("_WaveAndDistance", new Vector4(12f, 3.6f, 0.5f, 1f));
                bushMaterial.SetFloat("_Cutoff", 0.2f);
            }
            bushMaterial.renderQueue = 2450;
            
            string bushMaterialPath = Path.Combine(materialsFolder, "BushMaterial.mat").Replace("\\", "/");
            AssetDatabase.CreateAsset(bushMaterial, bushMaterialPath);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        var navPrefab = new GameObject("Nav");
        navPrefab.tag = "Navmesh";
        navPrefab.layer = LayerMask.NameToLayer("Navmesh");
        
        var navCollider = navPrefab.AddComponent<MeshCollider>();
        navCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | 
                                    MeshColliderCookingOptions.EnableMeshCleaning | 
                                    MeshColliderCookingOptions.WeldColocatedVertices | 
                                    MeshColliderCookingOptions.UseFastMidphase;
        navCollider.sharedMesh = bushModels[0];
        
        SavePrefab(navPrefab, folderPath, "Nav");
        
        var objectPrefab = new GameObject("Object");
        objectPrefab.tag = selectedType.ToString();
        objectPrefab.layer = LayerMask.NameToLayer(selectedType.ToString());
        
        List<MeshRenderer> foliageRenderers = new List<MeshRenderer>();
        
        for (int i = 0; i < bushModels.Count; i++)
        {
            if (bushModels[i] != null)
            {
                var model = new GameObject($"Model_{i}");
                model.transform.SetParent(objectPrefab.transform);
                model.tag = objectPrefab.tag;
                model.layer = objectPrefab.layer;
                
                var meshFilter = model.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = bushModels[i];
                
                var meshRenderer = model.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = bushMaterial;
                
                foliageRenderers.Add(meshRenderer);
                
                if (i == 0)
                {
                    var objCollider = model.AddComponent<MeshCollider>();
                    objCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | 
                                                MeshColliderCookingOptions.EnableMeshCleaning | 
                                                MeshColliderCookingOptions.WeldColocatedVertices | 
                                                MeshColliderCookingOptions.UseFastMidphase;
                    objCollider.sharedMesh = bushModels[i];
                    objCollider.sharedMaterial = bushPhysicsMaterial;
                }
            }
        }
        
        if (hasBushForage && bushForageMesh != null)
        {
            var forageModel = new GameObject("Model_Forage");
            forageModel.transform.SetParent(objectPrefab.transform);
            forageModel.tag = "Forage";
            forageModel.layer = objectPrefab.layer;
            
            var forageMeshFilter = forageModel.AddComponent<MeshFilter>();
            forageMeshFilter.sharedMesh = bushForageMesh;
            
            var forageMeshRenderer = forageModel.AddComponent<MeshRenderer>();
            forageMeshRenderer.sharedMaterial = bushMaterial;
            
            foliageRenderers.Add(forageMeshRenderer);
        }
        
        var resourceComponent = objectPrefab.AddComponent<Resource>();
        resourceComponent.healthPoints = 50;
        resourceComponent.maxHealthPoints = 50;
        resourceComponent.dropable = true;
        resourceComponent.dropObjectCount = 3;
        
        var foliageComponent = objectPrefab.AddComponent<FoliageEffect>();
        foliageComponent.renderers = foliageRenderers.ToArray();
        
        SavePrefab(objectPrefab, folderPath, "Object");
    }
    
    private Shader FindOrCreateFoliageShader()
    {
        Shader existingShader = Shader.Find("Custom/Foliage");
        if (existingShader != null) return existingShader;
        
        Shader standardShader = Shader.Find("Standard");
        return standardShader;
    }
    
    private void SetupLODGroup(GameObject prefab)
    {
        if (!configureLOD) return;
        
        var lodGroup = prefab.AddComponent<LODGroup>();
        List<LOD> lods = new List<LOD>();
        
        if (useLODPreset)
        {
            GameObject model = new GameObject("Model_0");
            model.transform.SetParent(prefab.transform);
            model.tag = prefab.tag;
            model.layer = prefab.layer;
            
            var meshFilter = model.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mainMesh;
            
            var meshRenderer = model.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = mainMaterial;
            
            for (int i = 0; i < childMeshes.Count; i++)
            {
                if (childMeshes[i] != null)
                {
                    GameObject childModel = new GameObject($"Model_{i + 1}");
                    childModel.transform.SetParent(prefab.transform);
                    childModel.tag = prefab.tag;
                    childModel.layer = prefab.layer;
                    
                    var childMeshFilter = childModel.AddComponent<MeshFilter>();
                    childMeshFilter.sharedMesh = childMeshes[i];
                    
                    var childMeshRenderer = childModel.AddComponent<MeshRenderer>();
                    childMeshRenderer.sharedMaterial = mainMaterial;
                }
            }
            
            Renderer[] allRenderers = prefab.GetComponentsInChildren<Renderer>();
            lods.Add(new LOD(lodBias * 0.01f, allRenderers));
        }
        else
        {
            for (int i = 0; i < lodTransitions.Count; i++)
            {
                GameObject model = new GameObject($"Model_{i}");
                model.transform.SetParent(prefab.transform);
                model.tag = prefab.tag;
                model.layer = prefab.layer;
                
                var meshFilter = model.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = i == 0 ? mainMesh : (i <= childMeshes.Count ? childMeshes[i - 1] : mainMesh);
                
                var meshRenderer = model.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = mainMaterial;
                
                Renderer[] renderers = { meshRenderer };
                lods.Add(new LOD(lodTransitions[i] * 0.01f, renderers));
            }
        }
        
        lodGroup.SetLODs(lods.ToArray());
        lodGroup.RecalculateBounds();
        
        if (useCustomLODSettings)
        {
            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.animateCrossFading = true;
        }
    }
    
    private void SavePrefab(GameObject prefab, string folderPath, string suffix)
    {
        string prefabName = suffix;
        string prefabPath = Path.Combine(folderPath, $"{prefabName}.prefab").Replace("\\", "/");
        
        try
        {
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            Debug.Log($"Created prefab at {prefabPath}");
            
            GameObject.DestroyImmediate(prefab);
            
            return;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create prefab: {e.Message}");
            GameObject.DestroyImmediate(prefab);
        }
    }

    private void DrawBushSettings()
    {
        DrawSectionHeader("Bush Settings");
        DrawBushBaseModels();
        
        EditorGUILayout.Space(5);
        
        DrawSectionHeader("Materials");
        DrawBushMaterials();
        
        EditorGUILayout.Space(5);
        
        DrawSectionHeader("Forage Settings");
        DrawBushForageSettings();
    }
    
    private void DrawBushBaseModels()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("Base Models", EditorStyles.boldLabel);
        
        for (int i = 0; i < bushModels.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            bushModels[i] = (Mesh)EditorGUILayout.ObjectField($"Model_{i}", bushModels[i], typeof(Mesh), false);
            
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                bushModels.RemoveAt(i);
                i--;
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (i < bushModels.Count - 1)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space(2);
            }
        }
        
        EditorGUILayout.Space(2);
        if (GUILayout.Button("Add Bush Model"))
        {
            bushModels.Add(null);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawBushMaterials()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        bushTexture = (Texture2D)EditorGUILayout.ObjectField("Bush Texture", bushTexture, typeof(Texture2D), false);
        bushPhysicsMaterial = (PhysicMaterial)EditorGUILayout.ObjectField("Physics Material", bushPhysicsMaterial, typeof(PhysicMaterial), false);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawBushForageSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        hasBushForage = EditorGUILayout.Toggle("Has Forage", hasBushForage);
        
        if (hasBushForage)
        {
            EditorGUI.indentLevel++;
            bushForageMesh = (Mesh)EditorGUILayout.ObjectField("Forage Mesh", bushForageMesh, typeof(Mesh), false);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }
}