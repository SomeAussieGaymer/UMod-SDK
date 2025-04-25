    using UnityEngine;
    using UnityEditor;
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class ClothingAssetGenerator : EditorWindow
    {
        private enum ClothingType { Shirt, Pants, Vest, Hat, Glasses, Mask, Backpack }
        private enum SelectionMode { Single, Multiple }
        private enum ViewportMode { Single, MultiView }
        private enum ViewAngle { 
            Front, Back,
            Left, Right,
            Top, Bottom,
            Orbit 
        }

        private ClothingType clothingType = ClothingType.Shirt;
        private SelectionMode selectionMode = SelectionMode.Multiple;
        private bool useCustomIconPlacement = false;
        private Vector3 customIconOffset = Vector3.zero;
        private Vector3 customIconRotation = Vector3.zero;
        private string textureFolderPath = "Assets";
        private string singleTexturePath = "";
        private string[] availableTexturePaths;
        private int selectedTextureIndex = -1;
        private System.Random random = new System.Random();
        private Mesh selectedMesh;
        private AnimationClip equipAnimation;
        private AnimationClip useAnimation;
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
        private const string PREFS_PREFIX = "ClothingGen_";
        private const string BASE_FOLDER_PATH = "Assets/Clothing";
        private static readonly int MATERIAL_MODE = Shader.PropertyToID("_Mode");
        private static readonly int MATERIAL_CUTOFF = Shader.PropertyToID("_Cutoff");
        private static readonly int MATERIAL_SRC_BLEND = Shader.PropertyToID("_SrcBlend");
        private static readonly int MATERIAL_DST_BLEND = Shader.PropertyToID("_DstBlend");
        private static readonly int MATERIAL_ZWRITE = Shader.PropertyToID("_ZWrite");
        private bool isProcessing = false;
        private float progress = 0f;
        private int totalFiles = 0;
        private int processedFiles = 0;
        private string currentFileName = "";
        private CancellationTokenSource cancellationTokenSource;
        private readonly Dictionary<ClothingType, ClothingTypeInfo> clothingTypeInfoMap;
        private readonly HashSet<ClothingType> specialTypesSet;

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

        public ClothingAssetGenerator()
        {
            clothingTypeInfoMap = new Dictionary<ClothingType, ClothingTypeInfo>
            {
                { ClothingType.Shirt, new ClothingTypeInfo("Shirts", "shirt.png") },
                { ClothingType.Pants, new ClothingTypeInfo("Pants", "pants.png") },
                { ClothingType.Vest, new ClothingTypeInfo("Vests", "vest.png") },
                { ClothingType.Hat, new ClothingTypeInfo("Hats", "Hat.png") },
                { ClothingType.Glasses, new ClothingTypeInfo("Glasses", "Glasses.png") },
                { ClothingType.Mask, new ClothingTypeInfo("Masks", "Mask.png") },
                { ClothingType.Backpack, new ClothingTypeInfo("Backpacks", "Backpack.png") }
            };

            specialTypesSet = new HashSet<ClothingType>
            {
                ClothingType.Vest,
                ClothingType.Hat,
                ClothingType.Glasses,
                ClothingType.Mask,
                ClothingType.Backpack
            };
        }

        [MenuItem("UMod SDK/Clothing Generator")]
        public static void ShowWindow()
        {
            GetWindow<ClothingAssetGenerator>("Clothing Generator");
        }

        private void OnEnable()
        {
            LoadPreferences();
            InitializePreviewRenderUtility();
            InitializeHighlightMaterial();
            EditorApplication.update += Repaint;
            RefreshAvailableTextures();
        }

        private void LoadPreferences()
        {
            textureFolderPath = EditorPrefs.GetString($"{PREFS_PREFIX}TextureFolder", "Assets");
            singleTexturePath = EditorPrefs.GetString($"{PREFS_PREFIX}SingleTexture", "");
            clothingType = (ClothingType)EditorPrefs.GetInt($"{PREFS_PREFIX}Type", 0);
            selectedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(EditorPrefs.GetString($"{PREFS_PREFIX}MeshPath", ""));
            equipAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(EditorPrefs.GetString($"{PREFS_PREFIX}EquipAnimPath", ""));
            useAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(EditorPrefs.GetString($"{PREFS_PREFIX}UseAnimPath", ""));
            selectionMode = (SelectionMode)EditorPrefs.GetInt($"{PREFS_PREFIX}SelectionMode", 1);
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
            EditorPrefs.SetString($"{PREFS_PREFIX}TextureFolder", textureFolderPath);
            EditorPrefs.SetString($"{PREFS_PREFIX}SingleTexture", singleTexturePath);
            EditorPrefs.SetInt($"{PREFS_PREFIX}Type", (int)clothingType);
            EditorPrefs.SetString($"{PREFS_PREFIX}MeshPath", AssetDatabase.GetAssetPath(selectedMesh));
            EditorPrefs.SetString($"{PREFS_PREFIX}EquipAnimPath", AssetDatabase.GetAssetPath(equipAnimation));
            EditorPrefs.SetString($"{PREFS_PREFIX}UseAnimPath", AssetDatabase.GetAssetPath(useAnimation));
            EditorPrefs.SetInt($"{PREFS_PREFIX}SelectionMode", (int)selectionMode);
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
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }

            isProcessing = false;
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label("Clothing Generator Tool", EditorStyles.boldLabel);

                if (isProcessing)
                {
                    DisplayProgressBar();
                    return;
                }

                DrawMainControls();

                if (GUILayout.Button("Generate Item"))
                {
                    GenerateItems();
                }
            }
        }

        private void DrawMainControls()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                clothingType = (ClothingType)EditorGUILayout.EnumPopup("Item Type", clothingType);
                selectionMode = (SelectionMode)EditorGUILayout.EnumPopup("Selection Mode", selectionMode);
                useCustomIconPlacement = EditorGUILayout.Toggle("Use Camera Position", useCustomIconPlacement);

                if (selectionMode == SelectionMode.Multiple)
                {
                    DisplayMultipleModeControls();
                    if (availableTexturePaths != null && availableTexturePaths.Length > 0)
                    {
                        DrawMultipleModePreview();
                    }
                }
                else
                {
                    DisplaySingleModeControls();
                    DrawSingleModePreview();
                }

                selectedMesh = (Mesh)EditorGUILayout.ObjectField("Item", selectedMesh, typeof(Mesh), false);
                equipAnimation = (AnimationClip)EditorGUILayout.ObjectField("Equip Animation", equipAnimation, typeof(AnimationClip), false);
                useAnimation = (AnimationClip)EditorGUILayout.ObjectField("Use Animation", useAnimation, typeof(AnimationClip), false);
            }
        }

        private async void GenerateItems()
        {
            if (selectionMode == SelectionMode.Multiple)
            {
                await GenerateMultipleItems();
            }
            else
            {
                await GenerateSingleItem();
            }
        }

        private async Task GenerateMultipleItems()
        {
            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                isProcessing = true;
                string[] pngPaths = await Task.Run(() => Directory.GetFiles(textureFolderPath, "*.png", SearchOption.AllDirectories));

                if (pngPaths.Length == 0)
                {
                    Debug.LogWarning("No PNG files found in selected folder.");
                    return;
                }

                totalFiles = pngPaths.Length;
                processedFiles = 0;
                progress = 0f;

                int maxConcurrent = Math.Max(1, Environment.ProcessorCount - 1);
                using (var semaphore = new SemaphoreSlim(maxConcurrent))
                {
                    var tasks = pngPaths.Select(async path =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            await ProcessSingleTexture(path);
                            processedFiles++;
                            progress = (float)processedFiles / totalFiles;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"✅ Generated {processedFiles} clothing item(s).");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Operation was canceled.");
            }
            finally
            {
                isProcessing = false;
            }
        }

        private async Task GenerateSingleItem()
        {
            if (string.IsNullOrEmpty(singleTexturePath)) return;

            isProcessing = true;
            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await ProcessSingleTexture(singleTexturePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                isProcessing = false;
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }
            }
        }

        private async Task ProcessSingleTexture(string path)
        {
            if (cancellationTokenSource == null || cancellationTokenSource.Token.IsCancellationRequested) return;

            currentFileName = Path.GetFileNameWithoutExtension(path);
            await Task.Run(() =>
            {
                if (cancellationTokenSource.Token.IsCancellationRequested) return;

                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (tex != null)
                        {
                            GenerateClothing(tex);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing {path}: {ex.Message}");
                    }
                };
            });
        }

        private void DisplayProgressBar()
        {
            EditorGUILayout.HelpBox($"Processing {currentFileName}", MessageType.Info);
            Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, progress, $"Processing {processedFiles}/{totalFiles}");

            if (GUILayout.Button("Cancel"))
            {
                CancelProcessing();
            }
        }

        private void DisplayMultipleModeControls()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUI.BeginChangeCheck();
                textureFolderPath = EditorGUILayout.TextField("Texture Folder", textureFolderPath);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Browse Folder"))
                    {
                        string selected = EditorUtility.OpenFolderPanel("Select PNG Folder", Application.dataPath, "");
                        if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
                        {
                            textureFolderPath = "Assets" + selected.Substring(Application.dataPath.Length);
                            RefreshAvailableTextures();
                        }
                    }

                    if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                    {
                        RefreshAvailableTextures();
                    }

                    if (GUILayout.Button("Random", GUILayout.Width(60)) && availableTexturePaths != null && availableTexturePaths.Length > 0)
                    {
                        selectedTextureIndex = random.Next(0, availableTexturePaths.Length);
                        Repaint();
                    }
                }

                if (availableTexturePaths != null && availableTexturePaths.Length > 0)
                {
                    string[] displayNames = availableTexturePaths.Select(Path.GetFileNameWithoutExtension).ToArray();
                    selectedTextureIndex = EditorGUILayout.Popup("Preview Texture", selectedTextureIndex, displayNames);
                }
                else
                {
                    EditorGUILayout.HelpBox("No PNG files found in the selected folder.", MessageType.Warning);
                }
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

        private void DrawMultipleModePreview()
        {
            if (selectedTextureIndex < 0 || selectedTextureIndex >= availableTexturePaths.Length) return;

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
                    if (viewportMode == ViewportMode.Single && currentView == ViewAngle.Orbit)
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

            if (currentView == ViewAngle.Orbit)
            {
                EditorGUILayout.HelpBox("Left: Rotate | Middle: Pan | Scroll: Zoom | WASD/QE: Move", MessageType.Info);
            }

            Rect previewRect = GUILayoutUtility.GetRect(300, 300, GUILayout.ExpandWidth(true));
            HandlePreviewInput(previewRect);

            string currentTexturePath = availableTexturePaths[selectedTextureIndex];
            if (Event.current.type == EventType.Repaint)
            {
                DrawPreviewForTexture(previewRect, currentTexturePath);
            }
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

        private void DrawPreviewForTexture(Rect rect, string texturePath)
        {
            Texture2D previewTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
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
    
        private struct ClothingTypeInfo
        {
            public string FolderName;
            public string ImageName;

            public ClothingTypeInfo(string folderName, string imageName)
            {
                FolderName = folderName;
                ImageName = imageName;
            }
        }

        private void GenerateClothing(Texture2D selectedTexture)
        {
            if (selectedTexture == null)
                throw new ArgumentNullException(nameof(selectedTexture));

            EnsureTagExists("Item");
            EnsureLayerExists("Item");
            EnsureTagExists("Logic");
            EnsureLayerExists("Logic");

            if (specialTypesSet.Contains(clothingType))
            {
                EnsureTagExists("Enemy");
                EnsureLayerExists("Enemy");
            }

            string originalPath = AssetDatabase.GetAssetPath(selectedTexture);
            string fileName = Path.GetFileNameWithoutExtension(originalPath);
            ClothingTypeInfo typeInfo = clothingTypeInfoMap[clothingType];
            string baseFolder = $"{BASE_FOLDER_PATH}/{typeInfo.FolderName}/{fileName}";

            CreateFolderStructure(typeInfo.FolderName, fileName);
            string newImagePath = $"{baseFolder}/{typeInfo.ImageName}";
            CopyAndConfigureTexture(originalPath, newImagePath);

            Texture2D copiedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newImagePath);
            if (copiedTexture == null)
                throw new Exception($"Failed to load copied texture at path: {newImagePath}");

            Material mat = CreateMaterial(copiedTexture, $"{baseFolder}/{fileName}_Mat.mat");

            GameObject clothingObj = CreateClothingObject(fileName, LayerMask.NameToLayer("Item"), mat);
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/Item.prefab");
            PrefabUtility.SaveAsPrefabAsset(clothingObj, prefabPath);
            DestroyImmediate(clothingObj);

            if (equipAnimation != null || useAnimation != null)
            {
                CreateAnimationPrefab(baseFolder, LayerMask.NameToLayer("Logic"));
            }

            if (specialTypesSet.Contains(clothingType))
            {
                CreateSpecialTypeItem(clothingType, fileName, LayerMask.NameToLayer("Enemy"), mat, baseFolder);
            }
        }

        private void CreateFolderStructure(string typeFolder, string fileName)
        {
            if (!AssetDatabase.IsValidFolder(BASE_FOLDER_PATH))
            {
                string parentFolder = Path.GetDirectoryName(BASE_FOLDER_PATH);
                string baseFolderName = Path.GetFileName(BASE_FOLDER_PATH);
                AssetDatabase.CreateFolder(parentFolder, baseFolderName);
            }

            string typePath = $"{BASE_FOLDER_PATH}/{typeFolder}";
            if (!AssetDatabase.IsValidFolder(typePath))
            {
                AssetDatabase.CreateFolder(BASE_FOLDER_PATH, typeFolder);
            }

            string baseFolder = $"{typePath}/{fileName}";
            if (!AssetDatabase.IsValidFolder(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
                AssetDatabase.Refresh();
            }
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

        private GameObject CreateClothingObject(string name, int layer, Material material)
        {
            GameObject obj = selectedMesh != null
                ? new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer))
                : GameObject.CreatePrimitive(PrimitiveType.Quad);

            obj.name = name;
            obj.layer = layer;
            obj.tag = "Item";

            if (selectedMesh != null)
            {
                obj.GetComponent<MeshFilter>().sharedMesh = selectedMesh;
                obj.GetComponent<MeshRenderer>().sharedMaterial = material;
                obj.AddComponent<BoxCollider>();
            }
            else
            {
                obj.GetComponent<Renderer>().sharedMaterial = material;
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

            animationObject.layer = logicLayer;
            animationObject.tag = "Logic";

            string animPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/Animations.prefab");
            PrefabUtility.SaveAsPrefabAsset(animationObject, animPrefabPath);
            DestroyImmediate(animationObject);
        }

        private void CreateSpecialTypeItem(ClothingType type, string fileName, int enemyLayer, Material material, string baseFolder)
        {
            GameObject specialObj = new GameObject(type.ToString());
            specialObj.tag = "Enemy";
            specialObj.layer = enemyLayer;

            BoxCollider boxCollider = specialObj.AddComponent<BoxCollider>();
            if (selectedMesh != null)
            {
                boxCollider.center = selectedMesh.bounds.center;
                boxCollider.size = selectedMesh.bounds.size;
            }

            GameObject modelChild = new GameObject("Model_0");
            modelChild.transform.SetParent(specialObj.transform);
            modelChild.tag = "Enemy";
            modelChild.layer = enemyLayer;

            MeshFilter mf = modelChild.AddComponent<MeshFilter>();
            MeshRenderer mr = modelChild.AddComponent<MeshRenderer>();

            if (selectedMesh != null)
            {
                mf.sharedMesh = selectedMesh;
                mr.sharedMaterial = material;
            }

            string specialPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{baseFolder}/{type}.prefab");
            PrefabUtility.SaveAsPrefabAsset(specialObj, specialPrefabPath);
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

        private void RefreshAvailableTextures()
        {
            if (string.IsNullOrEmpty(textureFolderPath) || !Directory.Exists(textureFolderPath))
            {
                textureFolderPath = "Assets";
                EditorPrefs.SetString($"{PREFS_PREFIX}TextureFolder", textureFolderPath);
            }

            try
            {
                availableTexturePaths = Directory.GetFiles(textureFolderPath, "*.png", SearchOption.AllDirectories);
                if (availableTexturePaths.Length > 0 && selectedTextureIndex == -1)
                {
                    selectedTextureIndex = random.Next(0, availableTexturePaths.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading textures: {ex.Message}");
                availableTexturePaths = new string[0];
                selectedTextureIndex = -1;
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
    }

    public static class EditorCoroutine
    {
        public class Coroutine
        {
            public IEnumerator routine;
            public bool isDone = false;
            public System.Action onComplete;
        }

        private static List<Coroutine> activeCoroutines = new List<Coroutine>();

        public static Coroutine Start(IEnumerator routine)
        {
            Coroutine coroutine = new Coroutine { routine = routine };

            if (activeCoroutines.Count == 0)
            {
                EditorApplication.update += UpdateCoroutines;
            }

            activeCoroutines.Add(coroutine);
            return coroutine;
        }

        private static void UpdateCoroutines()
        {
            for (int i = activeCoroutines.Count - 1; i >= 0; i--)
            {
                Coroutine coroutine = activeCoroutines[i];

                if (coroutine.isDone || !coroutine.routine.MoveNext())
                {
                    activeCoroutines.RemoveAt(i);
                    coroutine.isDone = true;
                    coroutine.onComplete?.Invoke();
                }
            }


            if (activeCoroutines.Count == 0)
            {
                EditorApplication.update -= UpdateCoroutines;
            }
        }

        public static void StopAll()
        {
            foreach (var coroutine in activeCoroutines)
            {
                coroutine.isDone = true;
            }

            activeCoroutines.Clear();
            EditorApplication.update -= UpdateCoroutines;
        }

        public static void Stop(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                coroutine.isDone = true;
                activeCoroutines.Remove(coroutine);

                if (activeCoroutines.Count == 0)
                {
                    EditorApplication.update -= UpdateCoroutines;
                }
            }
        }
    }