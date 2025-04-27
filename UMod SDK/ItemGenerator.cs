using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class ItemAsset : EditorWindow
{
    private enum ItemType { Item, Barricade }
    private enum ItemSubType 
    { 
        Supply,
        Arrest_Start,
        Arrest_End,
        Filter,
        Detenator,
        Maps,
        Medical,
        Melee,
        Tools,
        Water,
        Fuels,
        Clouds,
        Blueprints
    }
    private enum ViewportMode { Single, MultiView }
    private enum ViewAngle { 
        Front, Back,
        Left, Right,
        Top, Bottom,
        Orbit 
    }

    private ItemType itemType = ItemType.Item;
    private ItemSubType itemSubType = ItemSubType.Supply;
    private bool useCustomIconPlacement = false;
    private bool addTrapCollider = false;
    private Vector3 customIconOffset = Vector3.zero;
    private Vector3 customIconRotation = Vector3.zero;
    private string singleTexturePath = "";
    private string exportPath = "Assets/Items";
    private Mesh selectedMesh;
    private AnimationClip equipAnimation;
    private AnimationClip useAnimation;
    private AudioClip useSound;
    private PreviewRenderUtility previewRenderUtility;
    private GameObject previewInstance;
    private Material previewMaterial;
    private Vector2 previewEulerAngles = new Vector2(30f, 0f);
    private float previewZoom = 3f;
    private Vector3 previewPanOffset = Vector3.zero;
    private bool isDragging = false;
    private bool isPanning = false;
    private Vector2 lastMousePosition;
    private Vector3 lastCameraPosition;
    private Vector3 lastCameraForward;
    private const string PREFS_PREFIX = "ItemGen_";
    private const string BASE_FOLDER_PATH = "Assets/Items/Barricades";
    private static readonly int MATERIAL_MODE = Shader.PropertyToID("_Mode");
    private static readonly int MATERIAL_CUTOFF = Shader.PropertyToID("_Cutoff");
    private static readonly int MATERIAL_SRC_BLEND = Shader.PropertyToID("_SrcBlend");
    private static readonly int MATERIAL_DST_BLEND = Shader.PropertyToID("_DstBlend");
    private static readonly int MATERIAL_ZWRITE = Shader.PropertyToID("_ZWrite");
    private bool isProcessing = false;
    private string currentFileName = "";
    private readonly Dictionary<ItemType, ItemTypeInfo> itemTypeInfoMap;
    private readonly HashSet<ItemType> specialTypesSet;

    private Material highlightMaterial;
    private Color highlightColor = new Color(1f, 0.5f, 0f, 0.8f);

    private string lastPreviewedTexturePath;
    private Texture2D tempPreviewTexture;

    private ViewportMode viewportMode = ViewportMode.Single;
    private ViewAngle currentView = ViewAngle.Orbit;
    private Color backgroundColor = Color.gray;
    private bool useCustomBackground = false;
    private Texture2D backgroundTexture;
    private Vector2 previewScrollPosition;
    private Vector2 scrollPosition;

    private Dictionary<ViewAngle, Vector3> viewRotations = new Dictionary<ViewAngle, Vector3>()
    {
        { ViewAngle.Front, new Vector3(0, 180, 0) },
        { ViewAngle.Back, new Vector3(0, 0, 0) },
        { ViewAngle.Left, new Vector3(0, 90, 0) },
        { ViewAngle.Right, new Vector3(0, 270, 0) },
        { ViewAngle.Top, new Vector3(90, 180, 0) },
        { ViewAngle.Bottom, new Vector3(-90, 180, 0) },
        { ViewAngle.Orbit, new Vector3(30, 225, 0) }
    };

    private Dictionary<ViewAngle, Vector3> viewPositions = new Dictionary<ViewAngle, Vector3>()
    {
        { ViewAngle.Front, new Vector3(0, 0, -2) },
        { ViewAngle.Back, new Vector3(0, 0, 2) },
        { ViewAngle.Left, new Vector3(-2, 0, 0) },
        { ViewAngle.Right, new Vector3(2, 0, 0) },
        { ViewAngle.Top, new Vector3(0, 2, 0) },
        { ViewAngle.Bottom, new Vector3(0, -2, 0) },
        { ViewAngle.Orbit, new Vector3(1.4f, 1.4f, -1.4f) }
    };

    private Dictionary<ViewAngle, Vector3> customCameraPositions = new Dictionary<ViewAngle, Vector3>()
    {
        { ViewAngle.Orbit, new Vector3(0, 0, -3f) }
    };

    private Dictionary<ViewAngle, Vector3> customCameraRotations = new Dictionary<ViewAngle, Vector3>()
    {
        { ViewAngle.Orbit, new Vector3(30f, 0f, 0f) }
    };

    private Dictionary<ViewAngle, bool> useCustomPositions = new Dictionary<ViewAngle, bool>()
    {
        { ViewAngle.Front, false },
        { ViewAngle.Back, false },
        { ViewAngle.Left, false },
        { ViewAngle.Right, false },
        { ViewAngle.Top, false },
        { ViewAngle.Bottom, false },
        { ViewAngle.Orbit, false }
    };

    private bool useCustomOrbitPosition = false;

    private const float MOVEMENT_SPEED = 0.1f;
    private bool isFocused = false;

    private List<Mesh> additionalMeshes = new List<Mesh>();
    private List<Material> additionalMaterials = new List<Material>();
    private HashSet<ItemSubType> multiModelTypes = new HashSet<ItemSubType>
    {
        ItemSubType.Arrest_Start,
        ItemSubType.Arrest_End,
        ItemSubType.Filter,
        ItemSubType.Detenator,
        ItemSubType.Maps,
        ItemSubType.Medical,
        ItemSubType.Melee,
        ItemSubType.Tools
    };

    private string customFolderName = "";
    private bool useCustomFolderName = false;
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>()
    {
        { "itemSettings", true },
        { "textureSettings", true },
        { "previewSettings", true },
        { "modelSettings", true },
        { "animationSettings", true },
        { "audioSettings", true },
        { "exportSettings", true }
    };

    [MenuItem("UMod SDK/Item Generator")]
    public static void ShowWindow()
    {
        GetWindow<ItemAsset>("Item Generator");
    }

    public ItemAsset()
    {
        itemTypeInfoMap = new Dictionary<ItemType, ItemTypeInfo>
        {
            { ItemType.Item, new ItemTypeInfo("Items", "item.png") },
            { ItemType.Barricade, new ItemTypeInfo("Barricades", "barricade.png") }
        };

        specialTypesSet = new HashSet<ItemType>
        {
            ItemType.Barricade
        };
    }

    private void OnEnable()
    {
        LoadPreferences();
        InitializePreviewRenderUtility();
        InitializeHighlightMaterial();
        EditorApplication.update += Repaint;
    }

    private void LoadPreferences()
    {
        singleTexturePath = EditorPrefs.GetString($"{PREFS_PREFIX}SingleTexture", "");
        exportPath = EditorPrefs.GetString($"{PREFS_PREFIX}ExportPath", "Assets/Items");
        itemType = (ItemType)EditorPrefs.GetInt($"{PREFS_PREFIX}Type", 0);
        itemSubType = (ItemSubType)EditorPrefs.GetInt($"{PREFS_PREFIX}SubType", 0);
        selectedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(EditorPrefs.GetString($"{PREFS_PREFIX}MeshPath", ""));
        equipAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(EditorPrefs.GetString($"{PREFS_PREFIX}EquipAnimPath", ""));
        useAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(EditorPrefs.GetString($"{PREFS_PREFIX}UseAnimPath", ""));
        useSound = AssetDatabase.LoadAssetAtPath<AudioClip>(EditorPrefs.GetString($"{PREFS_PREFIX}UseSoundPath", ""));
        customFolderName = EditorPrefs.GetString($"{PREFS_PREFIX}CustomFolderName", "");
        useCustomFolderName = EditorPrefs.GetBool($"{PREFS_PREFIX}UseCustomFolderName", false);
        additionalMeshes.Clear();
        additionalMaterials.Clear();
    }

    private void InitializePreviewRenderUtility()
    {
        if (previewRenderUtility == null)
        {
            previewRenderUtility = new PreviewRenderUtility();
            previewRenderUtility.cameraFieldOfView = 60f;
        }
    }

    private void InitializeHighlightMaterial()
    {
        if (highlightMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            highlightMaterial = new Material(shader);
            highlightMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    private void OnDisable()
    {
        SavePreferences();
        CleanupResources();
        EditorApplication.update -= Repaint;
        if (tempPreviewTexture != null)
        {
            DestroyImmediate(tempPreviewTexture);
            tempPreviewTexture = null;
        }
    }

    private void SavePreferences()
    {
        EditorPrefs.SetString($"{PREFS_PREFIX}SingleTexture", singleTexturePath);
        EditorPrefs.SetString($"{PREFS_PREFIX}ExportPath", exportPath);
        EditorPrefs.SetInt($"{PREFS_PREFIX}Type", (int)itemType);
        EditorPrefs.SetInt($"{PREFS_PREFIX}SubType", (int)itemSubType);
        EditorPrefs.SetString($"{PREFS_PREFIX}MeshPath", AssetDatabase.GetAssetPath(selectedMesh));
        EditorPrefs.SetString($"{PREFS_PREFIX}EquipAnimPath", AssetDatabase.GetAssetPath(equipAnimation));
        EditorPrefs.SetString($"{PREFS_PREFIX}UseAnimPath", AssetDatabase.GetAssetPath(useAnimation));
        EditorPrefs.SetString($"{PREFS_PREFIX}UseSoundPath", AssetDatabase.GetAssetPath(useSound));
        EditorPrefs.SetString($"{PREFS_PREFIX}CustomFolderName", customFolderName);
        EditorPrefs.SetBool($"{PREFS_PREFIX}UseCustomFolderName", useCustomFolderName);
    }

    private void CleanupResources()
    {
        if (previewRenderUtility != null)
        {
            previewRenderUtility.Cleanup();
            previewRenderUtility = null;
        }

        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        if (previewMaterial != null)
        {
            DestroyImmediate(previewMaterial);
            previewMaterial = null;
        }

        if (highlightMaterial != null)
        {
            DestroyImmediate(highlightMaterial);
            highlightMaterial = null;
        }

        CancelProcessing();
    }

    private void CancelProcessing()
    {
        isProcessing = false;
    }

    private void OnGUI()
    {
        GUILayout.Label("Item Generator Tool", EditorStyles.boldLabel);

        if (isProcessing)
        {
            DisplayProgressBar();
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        using (new EditorGUILayout.VerticalScope())
        {
            DrawMainControls();

            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Generate Item", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    GenerateItem();
                }
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(20);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawMainControls()
    {
        foldoutStates["itemSettings"] = EditorGUILayout.Foldout(foldoutStates["itemSettings"], "Item Settings", true);
        if (foldoutStates["itemSettings"])
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(5);
                itemType = (ItemType)EditorGUILayout.EnumPopup("Type", itemType);

                if (itemType == ItemType.Item)
                {
                    EditorGUI.indentLevel++;
                    itemSubType = (ItemSubType)EditorGUILayout.EnumPopup("Item Category", itemSubType);
                    EditorGUI.indentLevel--;
                }

                useCustomIconPlacement = EditorGUILayout.Toggle("Use Camera Position", useCustomIconPlacement);
                
                if (itemType == ItemType.Barricade)
                {
                    addTrapCollider = EditorGUILayout.Toggle("Add Trap Collider", addTrapCollider);
                }
            }
        }

        EditorGUILayout.Space(10);

        foldoutStates["textureSettings"] = EditorGUILayout.Foldout(foldoutStates["textureSettings"], "Texture Settings", true);
        if (foldoutStates["textureSettings"])
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(5);
                DisplaySingleModeControls();
            }
        }

        EditorGUILayout.Space(10);

        foldoutStates["previewSettings"] = EditorGUILayout.Foldout(foldoutStates["previewSettings"], "Preview", true);
        if (foldoutStates["previewSettings"])
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(5);
                DrawSingleModePreview();
            }
        }

        EditorGUILayout.Space(10);

        foldoutStates["modelSettings"] = EditorGUILayout.Foldout(foldoutStates["modelSettings"], "Models", true);
        if (foldoutStates["modelSettings"])
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(5);
                selectedMesh = (Mesh)EditorGUILayout.ObjectField("Model_0", selectedMesh, typeof(Mesh), false);

                if (itemType == ItemType.Item && multiModelTypes.Contains(itemSubType))
                {
                    EditorGUI.indentLevel++;
                    
                    for (int i = 0; i < additionalMeshes.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        additionalMeshes[i] = (Mesh)EditorGUILayout.ObjectField($"Model_{i + 1}", additionalMeshes[i], typeof(Mesh), false);
                        
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            additionalMeshes.RemoveAt(i);
                            if (i < additionalMaterials.Count)
                                additionalMaterials.RemoveAt(i);
                            i--;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space(5);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Add Model", GUILayout.Width(100)))
                        {
                            additionalMeshes.Add(null);
                            additionalMaterials.Add(null);
                        }
                        GUILayout.FlexibleSpace();
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        EditorGUILayout.Space(10);

        foldoutStates["animationSettings"] = EditorGUILayout.Foldout(foldoutStates["animationSettings"], "Animations", true);
        if (foldoutStates["animationSettings"])
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(5);
                equipAnimation = (AnimationClip)EditorGUILayout.ObjectField("Equip Animation", equipAnimation, typeof(AnimationClip), false);
                useAnimation = (AnimationClip)EditorGUILayout.ObjectField("Use Animation", useAnimation, typeof(AnimationClip), false);
            }
        }

        EditorGUILayout.Space(10);

        foldoutStates["audioSettings"] = EditorGUILayout.Foldout(foldoutStates["audioSettings"], "Audio", true);
        if (foldoutStates["audioSettings"])
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(5);
                useSound = (AudioClip)EditorGUILayout.ObjectField("Use Sound", useSound, typeof(AudioClip), false);
            }
        }

        EditorGUILayout.Space(10);

        foldoutStates["exportSettings"] = EditorGUILayout.Foldout(foldoutStates["exportSettings"], "Export Settings", true);
        if (foldoutStates["exportSettings"])
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Export Location");
                
                EditorGUILayout.LabelField(exportPath, EditorStyles.textField);
                
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel("Select Export Location", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        if (selectedPath.StartsWith(Application.dataPath))
                        {
                            exportPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside the Assets directory.", "OK");
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                useCustomFolderName = EditorGUILayout.Toggle("Use Custom Folder Name", useCustomFolderName);
                if (useCustomFolderName)
                {
                    EditorGUI.indentLevel++;
                    customFolderName = EditorGUILayout.TextField("Folder Name", customFolderName);
                    EditorGUI.indentLevel--;
                }
            }
        }
    }

    private void GenerateItem()
    {
        if (string.IsNullOrEmpty(singleTexturePath)) return;

        isProcessing = true;
        try
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(singleTexturePath);
            if (tex != null)
            {
                GenerateItem(tex);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("✅ Generated barricade successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating barricade: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
        }
    }

    private void DisplayProgressBar()
    {
        EditorGUILayout.HelpBox($"Processing {currentFileName}", MessageType.Info);
        if (GUILayout.Button("Cancel"))
        {
            isProcessing = false;
        }
    }

    private void DisplaySingleModeControls()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel("Texture");
            Texture2D selectedTexture = EditorGUILayout.ObjectField("Texture", AssetDatabase.LoadAssetAtPath<Texture2D>(singleTexturePath), typeof(Texture2D), false) as Texture2D;
            if (selectedTexture != null)
            {
                singleTexturePath = AssetDatabase.GetAssetPath(selectedTexture);
            }
        }

        if (!string.IsNullOrEmpty(singleTexturePath))
        {
            EditorGUILayout.LabelField(Path.GetFileName(singleTexturePath));
        }
    }

    private void HandlePreviewInput(Rect rect)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            isFocused = true;
            EditorGUI.FocusTextInControl(null);
        }
        else if (e.type == EventType.MouseDown && !rect.Contains(e.mousePosition))
        {
            isFocused = false;
        }

        if (!rect.Contains(e.mousePosition) && e.type != EventType.KeyDown && e.type != EventType.KeyUp) return;

        Vector2 delta = lastMousePosition != Vector2.zero ? e.mousePosition - lastMousePosition : Vector2.zero;
        lastMousePosition = e.mousePosition;

        if (currentView == ViewAngle.Orbit)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0) isDragging = true;
                    else if (e.button == 2) isPanning = true;
                    e.Use();
                    break;

                case EventType.MouseUp:
                    isDragging = false;
                    isPanning = false;
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (isDragging)
                    {
                        previewEulerAngles += delta * 0.5f;
                        if (useCustomOrbitPosition)
                        {
                            customCameraRotations[ViewAngle.Orbit] = new Vector3(previewEulerAngles.y, previewEulerAngles.x, 0);
                        }
                    }
                    else if (isPanning)
                    {
                        previewPanOffset += new Vector3(delta.x, -delta.y, 0) * 0.01f;
                        UpdateCustomPosition();
                    }
                    e.Use();
                    break;

                case EventType.ScrollWheel:
                    previewZoom = Mathf.Clamp(previewZoom + e.delta.y * 0.1f, 1f, 10f);
                    UpdateCustomPosition();
                    e.Use();
                    break;
            }

            if (isFocused && e.type == EventType.KeyDown)
            {
                Vector3 moveDirection = Vector3.zero;
                bool moved = false;

                switch (e.keyCode)
                {
                    case KeyCode.W:
                        moveDirection += Vector3.forward;
                        moved = true;
                        break;
                    case KeyCode.S:
                        moveDirection += Vector3.back;
                        moved = true;
                        break;
                    case KeyCode.A:
                        moveDirection += Vector3.left;
                        moved = true;
                        break;
                    case KeyCode.D:
                        moveDirection += Vector3.right;
                        moved = true;
                        break;
                    case KeyCode.Q:
                        moveDirection += Vector3.down;
                        moved = true;
                        break;
                    case KeyCode.E:
                        moveDirection += Vector3.up;
                        moved = true;
                        break;
                }

                if (moved)
                {
                    Quaternion rotation = Quaternion.Euler(previewEulerAngles.y, previewEulerAngles.x, 0);
                    Vector3 rotatedMovement = rotation * moveDirection * MOVEMENT_SPEED;

                    if (useCustomOrbitPosition)
                    {
                        Vector3 newPos = customCameraPositions[ViewAngle.Orbit] + rotatedMovement;
                        customCameraPositions[ViewAngle.Orbit] = newPos;
                    }
                    else
                    {
                        previewPanOffset += rotatedMovement;
                        UpdateCustomPosition();
                    }

                    e.Use();
                    Repaint();
                }
            }
        }

        if (e.type != EventType.Layout && e.type != EventType.Repaint)
        {
            Repaint();
        }
    }

    private void UpdateCustomPosition()
    {
        if (useCustomOrbitPosition)
        {
            Vector3 cameraPosition = new Vector3(0, 0, -previewZoom);
            cameraPosition = Quaternion.Euler(previewEulerAngles.y, previewEulerAngles.x, 0) * cameraPosition;
            cameraPosition += previewPanOffset;
            customCameraPositions[ViewAngle.Orbit] = cameraPosition;
        }
    }

    private void DrawSingleModePreview()
    {
        GUILayout.Space(10);
        
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Preview Settings", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(200)))
                {
                    viewportMode = (ViewportMode)EditorGUILayout.EnumPopup("View Mode", viewportMode);
                    currentView = (ViewAngle)EditorGUILayout.EnumPopup("Camera Angle", currentView);

                    if (currentView == ViewAngle.Orbit)
                    {
                        EditorGUILayout.Space(5);
                        useCustomOrbitPosition = EditorGUILayout.ToggleLeft("Use Custom Position", useCustomOrbitPosition);
                        
                        if (useCustomOrbitPosition)
                        {
                            EditorGUI.indentLevel++;
                            Vector3 customPos = customCameraPositions[ViewAngle.Orbit];
                            Vector3 customRot = customCameraRotations[ViewAngle.Orbit];
                            
                            EditorGUI.BeginChangeCheck();
                            customPos = EditorGUILayout.Vector3Field("Position", customPos);
                            customRot = EditorGUILayout.Vector3Field("Rotation", customRot);
                            if (EditorGUI.EndChangeCheck())
                            {
                                customCameraPositions[ViewAngle.Orbit] = customPos;
                                customCameraRotations[ViewAngle.Orbit] = customRot;
                                
                                previewEulerAngles = new Vector2(customRot.y, customRot.x);
                                previewPanOffset = customPos;
                                
                                Repaint();
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }

                GUILayout.Space(20);

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(200)))
                {
                    useCustomBackground = EditorGUILayout.Toggle("Custom Background", useCustomBackground);
                    if (useCustomBackground)
                    {
                        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
                        backgroundTexture = (Texture2D)EditorGUILayout.ObjectField("Background Image", backgroundTexture, typeof(Texture2D), false);
                    }
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (currentView == ViewAngle.Orbit)
                {
                    if (GUILayout.Button("Reset Camera", GUILayout.Width(100)))
                    {
                        ResetOrbitView();
                    }
                }
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Export Preview", GUILayout.Width(100)))
                {
                    ExportPreview();
                }
            }
        }

        GUILayout.Space(5);
        EditorGUILayout.HelpBox("Preview shows actual material settings as they will appear in-game", MessageType.Info);

        Rect previewRect = GUILayoutUtility.GetRect(300, 300, GUILayout.ExpandWidth(true));
        HandlePreviewInput(previewRect);
        DrawPreview(previewRect);
    }

    private void SetupPreviewLighting()
    {
        if (previewRenderUtility.lights == null || previewRenderUtility.lights.Length == 0) return;

        previewRenderUtility.lights[0].intensity = 0.7f;
        previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        previewRenderUtility.lights[0].enabled = true;
        previewRenderUtility.lights[0].shadows = LightShadows.None;

        if (previewRenderUtility.lights.Length > 1)
        {
            previewRenderUtility.lights[1].intensity = 0.7f;
            previewRenderUtility.lights[1].transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            previewRenderUtility.lights[1].enabled = true;
            previewRenderUtility.lights[1].shadows = LightShadows.None;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.7f);
    }

    private void DrawPreview(Rect rect)
    {
        if (Event.current.type != EventType.Repaint) return;

        Texture2D previewTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(singleTexturePath);
        if (previewTexture == null) return;

        UpdatePreviewMaterial(previewTexture);

        if (viewportMode == ViewportMode.MultiView)
        {
            DrawMultiViewPreview(rect);
        }
        else
        {
            DrawPreviewWithAngle(rect, currentView);
        }

        DrawPreviewControls(rect);
    }

    private void DrawPreviewWithAngle(Rect rect, ViewAngle angle)
    {
        float toolbarHeight = 24;
        Rect toolbarRect = new Rect(rect.x, rect.y, rect.width, toolbarHeight);
        Rect previewRect = new Rect(rect.x, rect.y + toolbarHeight, rect.width, rect.height - toolbarHeight);

        EditorGUI.DrawRect(toolbarRect, new Color(0.2f, 0.2f, 0.2f, 1));

        if (angle == ViewAngle.Orbit)
        {
            Rect labelRect = new Rect(toolbarRect.x + 5, toolbarRect.y + 4, 60, 16);
            EditorGUI.LabelField(labelRect, "Orbit", EditorStyles.whiteMiniLabel);

            if (!useCustomOrbitPosition)
            {
                Rect controlsRect = new Rect(labelRect.xMax + 5, toolbarRect.y + 4, 300, 16);
                EditorGUI.LabelField(controlsRect, "Left: Rotate | Middle: Pan | Scroll: Zoom | WASD/QE: Move", EditorStyles.whiteMiniLabel);
            }
        }
        else
        {
            Rect labelRect = new Rect(toolbarRect.x + 5, toolbarRect.y + 4, 100, 16);
            EditorGUI.LabelField(labelRect, angle.ToString(), EditorStyles.whiteMiniLabel);
        }

        if (Event.current.type != EventType.Repaint) return;

        RenderTexture tempRT = RenderTexture.GetTemporary(
            (int)previewRect.width,
            (int)previewRect.height,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);

        previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);

        GL.Clear(true, true, Color.clear);

        float aspect = previewRect.width / previewRect.height;
        Matrix4x4 projection = Matrix4x4.Perspective(60f, aspect, 0.01f, 100f);
        previewRenderUtility.camera.projectionMatrix = projection;
        previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
        previewRenderUtility.camera.backgroundColor = Color.clear;

        if (angle == ViewAngle.Orbit)
        {
            if (useCustomOrbitPosition)
            {
                Vector3 customPos = customCameraPositions[ViewAngle.Orbit];
                Vector3 customRot = customCameraRotations[ViewAngle.Orbit];
                
                previewRenderUtility.camera.transform.position = customPos;
                previewRenderUtility.camera.transform.rotation = Quaternion.Euler(customRot);
            }
            else
            {
                Vector3 cameraPosition = new Vector3(0, 0, -previewZoom);
                cameraPosition = Quaternion.Euler(previewEulerAngles.y, previewEulerAngles.x, 0) * cameraPosition;
                cameraPosition += previewPanOffset;

                previewRenderUtility.camera.transform.position = cameraPosition;
                previewRenderUtility.camera.transform.LookAt(previewPanOffset, Vector3.up);
            }
        }
        else
        {
            Vector3 position = viewPositions[angle];
            Vector3 rotation = viewRotations[angle];
            
            previewRenderUtility.camera.transform.position = position;
            previewRenderUtility.camera.transform.rotation = Quaternion.Euler(rotation);
            previewRenderUtility.camera.transform.LookAt(Vector3.zero, Vector3.up);
        }

        SetupPreviewLighting();

        Mesh previewMesh = selectedMesh != null ? selectedMesh : Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        if (previewMesh != null)
        {
            Matrix4x4 objectMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            previewRenderUtility.DrawMesh(previewMesh, objectMatrix, previewMaterial, 0);
        }

        previewRenderUtility.camera.Render();
        
        Texture previewTexture = previewRenderUtility.EndPreview();

        if (useCustomBackground)
        {
            if (backgroundTexture != null)
            {
                GUI.DrawTexture(previewRect, backgroundTexture, ScaleMode.StretchToFill);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, backgroundColor);
            }
        }
        else
        {
            EditorGUI.DrawRect(previewRect, new Color(0.3f, 0.3f, 0.3f, 1));
        }

        GUI.DrawTexture(previewRect, previewTexture, ScaleMode.StretchToFill);

        RenderTexture.ReleaseTemporary(tempRT);
    }

    private void ExportPreview()
    {
        string path = EditorUtility.SaveFilePanel(
            "Save Preview Image",
            "",
            "preview.png",
            "png");

        if (string.IsNullOrEmpty(path)) return;

        var originalRT = RenderTexture.active;
        var originalTargetTexture = previewRenderUtility.camera.targetTexture;
        var originalClearFlags = previewRenderUtility.camera.clearFlags;
        var originalBackgroundColor = previewRenderUtility.camera.backgroundColor;

        RenderTexture rt = RenderTexture.GetTemporary(1024, 1024, 24, RenderTextureFormat.ARGB32);
        
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        
        try
        {
            previewRenderUtility.camera.targetTexture = rt;
            previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            previewRenderUtility.camera.backgroundColor = useCustomBackground ? backgroundColor : new Color(0.3f, 0.3f, 0.3f, 1);

            float aspect = 1.0f;
            Matrix4x4 projection = Matrix4x4.Perspective(60f, aspect, 0.01f, 100f);
            previewRenderUtility.camera.projectionMatrix = projection;

            if (currentView == ViewAngle.Orbit)
            {
                Vector3 cameraPosition = new Vector3(0, 0, -previewZoom);
                cameraPosition = Quaternion.Euler(previewEulerAngles.y, previewEulerAngles.x, 0) * cameraPosition;
                cameraPosition += previewPanOffset;

                previewRenderUtility.camera.transform.position = cameraPosition;
                previewRenderUtility.camera.transform.LookAt(previewPanOffset, Vector3.up);
            }
            else
            {
                previewRenderUtility.camera.transform.position = viewPositions[currentView];
                previewRenderUtility.camera.transform.rotation = Quaternion.Euler(viewRotations[currentView]);
                previewRenderUtility.camera.transform.LookAt(Vector3.zero, Vector3.up);
            }

            SetupPreviewLighting();

            RenderTexture.active = rt;
            GL.Clear(true, true, useCustomBackground ? backgroundColor : new Color(0.3f, 0.3f, 0.3f, 1));

            if (useCustomBackground && backgroundTexture != null)
            {
                Graphics.Blit(backgroundTexture, rt);
            }

            if (selectedMesh != null)
            {
                Matrix4x4 objectMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                previewRenderUtility.DrawMesh(selectedMesh, objectMatrix, previewMaterial, 0);
            }

            previewRenderUtility.camera.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);

            Debug.Log($"Preview saved to: {path}");
        }
        finally
        {
            previewRenderUtility.camera.targetTexture = originalTargetTexture;
            previewRenderUtility.camera.clearFlags = originalClearFlags;
            previewRenderUtility.camera.backgroundColor = originalBackgroundColor;
            RenderTexture.active = originalRT;

            RenderTexture.ReleaseTemporary(rt);
            DestroyImmediate(tex);
            Repaint();
        }

        AssetDatabase.Refresh();
    }

    private void DrawPreviewContent()
    {
        float aspect = previewRenderUtility.camera.pixelRect.width / previewRenderUtility.camera.pixelRect.height;
        Matrix4x4 projection = Matrix4x4.Perspective(60f, aspect, 0.01f, 100f);
        previewRenderUtility.camera.projectionMatrix = projection;

        SetupPreviewLighting();

        Matrix4x4 objectMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
        previewRenderUtility.DrawMesh(
            selectedMesh != null ? selectedMesh : Resources.GetBuiltinResource<Mesh>("Quad.fbx"),
            objectMatrix,
            previewMaterial,
            0);
    }
    
    private struct ItemTypeInfo
    {
        public string FolderName;
        public string ImageName;

        public ItemTypeInfo(string folderName, string imageName)
        {
            FolderName = folderName;
            ImageName = imageName;
        }
    }

    private void GenerateItem(Texture2D selectedTexture)
    {
        if (selectedTexture == null)
            throw new ArgumentNullException(nameof(selectedTexture));

        string originalPath = AssetDatabase.GetAssetPath(selectedTexture);
        string fileName = Path.GetFileNameWithoutExtension(originalPath);
        
        string baseFolder;
        if (itemType == ItemType.Item)
        {
            baseFolder = $"{exportPath}/{itemSubType}/{(useCustomFolderName ? customFolderName : fileName)}";
        }
        else
        {
            baseFolder = $"{exportPath}/Barricades/{(useCustomFolderName ? customFolderName : fileName)}";
        }

        CreateFolderStructure(fileName);
        AssetDatabase.Refresh();

        EnsureTagExists("Item");
        EnsureLayerExists("Item");
        EnsureTagExists("Logic");
        EnsureLayerExists("Logic");

        if (itemType == ItemType.Barricade)
        {
            EnsureTagExists("Barricade");
            EnsureLayerExists("Barricade");
            EnsureTagExists("Trap");
            EnsureLayerExists("Trap");
        }

        string newImagePath = $"{baseFolder}/{(itemType == ItemType.Item ? "item.png" : "barricade.png")}";
        CopyAndConfigureTexture(originalPath, newImagePath);

        if (useSound != null)
        {
            string originalSoundPath = AssetDatabase.GetAssetPath(useSound);
            string newSoundPath = $"{baseFolder}/Use{Path.GetExtension(originalSoundPath)}";
            File.Copy(originalSoundPath, newSoundPath, true);
            AssetDatabase.ImportAsset(newSoundPath);
        }

        Texture2D copiedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newImagePath);
        if (copiedTexture == null)
            throw new Exception($"Failed to load copied texture at path: {newImagePath}");

        Material mat = CreateMaterial(copiedTexture, $"{baseFolder}/{(useCustomFolderName ? customFolderName : fileName)}_Mat.mat");

        GameObject itemObj = CreateItemObject(fileName, LayerMask.NameToLayer("Item"), mat);
        string itemPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/Item.prefab");
        PrefabUtility.SaveAsPrefabAsset(itemObj, itemPrefabPath);
        DestroyImmediate(itemObj);

        if (equipAnimation != null || useAnimation != null)
        {
            CreateAnimationPrefab(baseFolder, LayerMask.NameToLayer("Logic"));
        }

        if (itemType == ItemType.Barricade)
        {
            string specialPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/Barricade.prefab");
            CreateSpecialTypeItem(itemType, fileName, LayerMask.NameToLayer("Barricade"), mat, baseFolder, specialPrefabPath);
        }

        string typeDescription = itemType == ItemType.Item ? $"{itemType} ({itemSubType})" : itemType.ToString();
        Debug.Log($"✅ Generated {typeDescription} successfully.");
    }

    private void CreateFolderStructure(string fileName)
    {
        string[] pathParts = exportPath.Split('/');
        string currentPath = pathParts[0];
        for (int i = 1; i < pathParts.Length; i++)
        {
            string nextPath = $"{currentPath}/{pathParts[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                string parentFolder = currentPath;
                string newFolderName = pathParts[i];
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
            }
            currentPath = nextPath;
        }

        string categoryPath;
        if (itemType == ItemType.Item)
        {
            categoryPath = $"{exportPath}/{itemSubType}";
            if (!AssetDatabase.IsValidFolder(categoryPath))
            {
                AssetDatabase.CreateFolder(exportPath, itemSubType.ToString());
            }
        }
        else
        {
            categoryPath = $"{exportPath}/Barricades";
            if (!AssetDatabase.IsValidFolder(categoryPath))
            {
                AssetDatabase.CreateFolder(exportPath, "Barricades");
            }
        }

        string folderName = useCustomFolderName && !string.IsNullOrEmpty(customFolderName) ? customFolderName : fileName;
        string baseFolder = $"{categoryPath}/{folderName}";

        if (!AssetDatabase.IsValidFolder(baseFolder))
        {
            AssetDatabase.CreateFolder(categoryPath, folderName);
        }

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", baseFolder));
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        AssetDatabase.Refresh();
    }

    private void CopyAndConfigureTexture(string originalPath, string newPath)
    {
        File.Copy(originalPath, newPath, true);
        AssetDatabase.Refresh();

        TextureImporter textureImporter = AssetImporter.GetAtPath(newPath) as TextureImporter;
        if (textureImporter != null)
        {
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            AssetDatabase.ImportAsset(newPath);
        }
    }

    private Material CreateMaterial(Texture2D texture, string assetPath)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.mainTexture = texture;
        mat.SetFloat(MATERIAL_MODE, 1);
        mat.SetOverrideTag("RenderType", "TransparentCutout");
        mat.SetInt(MATERIAL_SRC_BLEND, (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt(MATERIAL_DST_BLEND, (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt(MATERIAL_ZWRITE, 1);
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        mat.SetFloat(MATERIAL_CUTOFF, 0.5f);

        AssetDatabase.CreateAsset(mat, assetPath);
        return mat;
    }

    private GameObject CreateItemObject(string name, int layer, Material material)
    {
        GameObject obj = new GameObject(name);
        obj.layer = layer;
        obj.tag = "Item";

        if (itemType == ItemType.Item && multiModelTypes.Contains(itemSubType))
        {
            GameObject model0 = new GameObject("Model_0");
            model0.transform.SetParent(obj.transform);
            model0.layer = layer;
            model0.tag = "Item";

            if (selectedMesh != null)
            {
                MeshFilter mf = model0.AddComponent<MeshFilter>();
                MeshRenderer mr = model0.AddComponent<MeshRenderer>();
                mf.sharedMesh = selectedMesh;
                mr.sharedMaterial = material;
                model0.AddComponent<BoxCollider>();
            }

            for (int i = 0; i < additionalMeshes.Count; i++)
            {
                if (additionalMeshes[i] != null)
                {
                    GameObject additionalModel = new GameObject($"Model_{i + 1}");
                    additionalModel.transform.SetParent(obj.transform);
                    additionalModel.layer = layer;
                    additionalModel.tag = "Item";

                    MeshFilter mf = additionalModel.AddComponent<MeshFilter>();
                    MeshRenderer mr = additionalModel.AddComponent<MeshRenderer>();
                    mf.sharedMesh = additionalMeshes[i];
                    mr.sharedMaterial = material;
                    additionalModel.AddComponent<BoxCollider>();
                }
            }
        }
        else
        {
            if (selectedMesh != null)
            {
                MeshFilter mf = obj.AddComponent<MeshFilter>();
                MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                mf.sharedMesh = selectedMesh;
                mr.sharedMaterial = material;
                obj.AddComponent<BoxCollider>();
            }
            else
            {
                GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
                primitive.transform.SetParent(obj.transform);
                primitive.transform.localPosition = Vector3.zero;
                primitive.transform.localRotation = Quaternion.identity;
                primitive.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(obj.transform);
        icon.layer = layer;
        icon.tag = "Item";

        if (useCustomOrbitPosition)
        {
            icon.transform.localPosition = customCameraPositions[ViewAngle.Orbit];
            icon.transform.localRotation = Quaternion.Euler(customCameraRotations[ViewAngle.Orbit]);
        }
        else
        {
            Vector3 cameraPosition = new Vector3(0, 0, -previewZoom);
            cameraPosition = Quaternion.Euler(previewEulerAngles.y, previewEulerAngles.x, 0) * cameraPosition;
            cameraPosition += previewPanOffset;
            
            icon.transform.localPosition = cameraPosition;
            icon.transform.LookAt(obj.transform);
        }

        return obj;
    }

    private void CreateAnimationPrefab(string baseFolder, int logicLayer)
    {
        GameObject animationObject = new GameObject("Animations");
        animationObject.layer = logicLayer;
        animationObject.tag = "Logic";

        Animation anim = animationObject.AddComponent<Animation>();

        if (equipAnimation != null)
        {
            anim.AddClip(equipAnimation, "Equip");
            anim.Play("Equip");
        }

        if (useAnimation != null)
        {
            anim.AddClip(useAnimation, "Use");
        }

        string animPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/Animations.prefab");
        PrefabUtility.SaveAsPrefabAsset(animationObject, animPrefabPath);
        DestroyImmediate(animationObject);
    }

    private void CreateSpecialTypeItem(ItemType type, string fileName, int barricadeLayer, Material material, string baseFolder, string prefabPath)
    {
        GameObject specialObj = new GameObject(type.ToString());
        specialObj.tag = "Barricade";
        specialObj.layer = barricadeLayer;

        BoxCollider boxCollider = specialObj.AddComponent<BoxCollider>();
        if (selectedMesh != null)
        {
            boxCollider.center = selectedMesh.bounds.center;
            boxCollider.size = selectedMesh.bounds.size;
        }

        GameObject modelChild = new GameObject("Model_0");
        modelChild.transform.SetParent(specialObj.transform);
        modelChild.tag = "Barricade";
        modelChild.layer = barricadeLayer;

        MeshFilter mf = modelChild.AddComponent<MeshFilter>();
        MeshRenderer mr = modelChild.AddComponent<MeshRenderer>();

        if (selectedMesh != null)
        {
            mf.sharedMesh = selectedMesh;
            mr.sharedMaterial = material;
        }

        if (addTrapCollider)
        {
            GameObject trapChild = new GameObject("Trap");
            trapChild.transform.SetParent(specialObj.transform);
            trapChild.tag = "Trap";
            trapChild.layer = LayerMask.NameToLayer("Trap");

            BoxCollider trapCollider = trapChild.AddComponent<BoxCollider>();
            if (selectedMesh != null)
            {
                trapCollider.center = selectedMesh.bounds.center;
                trapCollider.size = selectedMesh.bounds.size * 1.2f;
            }
            else
            {
                trapCollider.size = new Vector3(2f, 2f, 2f);
            }

            trapCollider.isTrigger = true;
        }

        PrefabUtility.SaveAsPrefabAsset(specialObj, prefabPath);
        DestroyImmediate(specialObj);
    }

    private static readonly object tagLock = new object();
    private void EnsureTagExists(string tag)
    {
        lock (tagLock)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return;
                }
            }

            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }

    private static readonly object layerLock = new object();
    private void EnsureLayerExists(string layerName)
    {
        lock (layerLock)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                if (sp.stringValue == layerName)
                {
                    return;
                }
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }
        }
    }

    private void ResetOrbitView()
    {
        previewEulerAngles = new Vector2(0f, 30f);
        previewZoom = 3f;
        previewPanOffset = Vector3.zero;
        
        Vector3 defaultPos = new Vector3(0, 0, -previewZoom);
        defaultPos = Quaternion.Euler(previewEulerAngles.y, previewEulerAngles.x, 0) * defaultPos;
        
        customCameraPositions[ViewAngle.Orbit] = defaultPos;
        customCameraRotations[ViewAngle.Orbit] = new Vector3(previewEulerAngles.y, previewEulerAngles.x, 0);
    }

    private void UpdatePreviewMaterial(Texture2D texture)
    {
        if (previewMaterial == null)
        {
            previewMaterial = new Material(Shader.Find("Standard"));
            previewMaterial.SetFloat(MATERIAL_MODE, 1);
            previewMaterial.SetOverrideTag("RenderType", "TransparentCutout");
            previewMaterial.SetInt(MATERIAL_SRC_BLEND, (int)UnityEngine.Rendering.BlendMode.One);
            previewMaterial.SetInt(MATERIAL_DST_BLEND, (int)UnityEngine.Rendering.BlendMode.Zero);
            previewMaterial.SetInt(MATERIAL_ZWRITE, 1);
            previewMaterial.DisableKeyword("_ALPHABLEND_ON");
            previewMaterial.EnableKeyword("_ALPHATEST_ON");
            previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            previewMaterial.SetFloat(MATERIAL_CUTOFF, 0.5f);
        }

        previewMaterial.mainTexture = texture;
    }

    private void DrawPreviewControls(Rect rect)
    {
    }

    private void DrawMultiViewPreview(Rect totalRect)
    {
        float padding = 2f;
        float thirdWidth = (totalRect.width - padding * 2) / 3f;
        float thirdHeight = (totalRect.height - padding * 2) / 3f;

        Rect frontRect = new Rect(totalRect.x + thirdWidth + padding, totalRect.y, thirdWidth, thirdHeight);
        DrawPreviewWithAngle(frontRect, ViewAngle.Front);

        Rect backRect = new Rect(totalRect.x + thirdWidth + padding, totalRect.y + 2 * (thirdHeight + padding), thirdWidth, thirdHeight);
        DrawPreviewWithAngle(backRect, ViewAngle.Back);

        Rect leftRect = new Rect(totalRect.x, totalRect.y + thirdHeight + padding, thirdWidth, thirdHeight);
        DrawPreviewWithAngle(leftRect, ViewAngle.Left);

        Rect rightRect = new Rect(totalRect.x + 2 * (thirdWidth + padding), totalRect.y + thirdHeight + padding, thirdWidth, thirdHeight);
        DrawPreviewWithAngle(rightRect, ViewAngle.Right);

        Rect topRect = new Rect(totalRect.x + thirdWidth + padding, totalRect.y, thirdWidth, thirdHeight);
        DrawPreviewWithAngle(topRect, ViewAngle.Top);

        Rect bottomRect = new Rect(totalRect.x + thirdWidth + padding, totalRect.y + thirdHeight + padding, thirdWidth, thirdHeight);
        DrawPreviewWithAngle(bottomRect, ViewAngle.Bottom);

        Rect orbitRect = new Rect(totalRect.x + 2 * (thirdWidth + padding), totalRect.y + 2 * (thirdHeight + padding), thirdWidth, thirdHeight);
        
        if (Event.current.type != EventType.Repaint)
        {
            HandlePreviewInput(orbitRect);
        }
        
        DrawPreviewWithAngle(orbitRect, ViewAngle.Orbit);
    }
}