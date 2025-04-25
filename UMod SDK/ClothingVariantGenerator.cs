using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace HueShifter
{
    public class ClothingVariantGenerator : EditorWindow
    {
        private Texture2D sourceTexture;
        private Vector2 scrollPosition;
        private bool isProcessing = false;
        private string outputPath = "Assets/Variants";
        private Texture2D[] previewTextures;
        private bool showPreviews = true;
        private int previewSize = 128;

        private enum ColorMode
        {
            Natural,     
            Family,      
            Complement,  
            Custom      
        }

        private enum ColorFamily
        {
            Denim,      
            Earth,      
            Pastel,     
            Warm,       
            Cool,       
            Neutral,    
            Vibrant     
        }

        private ColorMode colorMode = ColorMode.Natural;
        private ColorFamily colorFamily = ColorFamily.Denim;
        private int variationCount = 5;
        private float variationStrength = 0.3f;
        private bool preserveDetails = true;
        private float detailThreshold = 0.2f;
        private bool preserveAlpha = true;

        private Color customBaseColor = Color.white;
        private float customHueRange = 0.1f;
        private Vector2 customSaturationRange = new Vector2(0.7f, 1.0f);
        private Vector2 customValueRange = new Vector2(0.7f, 1.0f);

        private readonly Dictionary<ColorFamily, ColorRange> colorFamilyRanges = new Dictionary<ColorFamily, ColorRange>()
        {
            { ColorFamily.Denim, new ColorRange(
                new Vector2(0.55f, 0.65f),   
                new Vector2(0.4f, 0.8f),     
                new Vector2(0.3f, 0.8f))     
            },
            { ColorFamily.Earth, new ColorRange(
                new Vector2(0.05f, 0.15f),   
                new Vector2(0.3f, 0.7f),     
                new Vector2(0.3f, 0.8f))     
            },
            { ColorFamily.Pastel, new ColorRange(
                new Vector2(0f, 1f),         
                new Vector2(0.15f, 0.4f),    
                new Vector2(0.8f, 1.0f))     
            },
            { ColorFamily.Warm, new ColorRange(
                new Vector2(0.95f, 0.15f),   
                new Vector2(0.4f, 0.9f),     
                new Vector2(0.4f, 0.9f))     
            },
            { ColorFamily.Cool, new ColorRange(
                new Vector2(0.45f, 0.65f),   
                new Vector2(0.4f, 0.9f),     
                new Vector2(0.4f, 0.9f))     
            },
            { ColorFamily.Neutral, new ColorRange(
                new Vector2(0.05f, 0.15f),   
                new Vector2(0.1f, 0.3f),     
                new Vector2(0.5f, 0.9f))     
            },
            { ColorFamily.Vibrant, new ColorRange(
                new Vector2(0f, 1f),         
                new Vector2(0.7f, 1.0f),     
                new Vector2(0.7f, 1.0f))     
            }
        };

        [MenuItem("UMod SDK/Hue Shifter")]
        public static void ShowWindow()
        {
            GetWindow<ClothingVariantGenerator>("Hue Shifter");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            DrawSourceTextureSection();
            EditorGUILayout.Space(10);
            DrawColorSettingsSection();
            EditorGUILayout.Space(10);
            DrawOutputSettingsSection();
            EditorGUILayout.Space(10);
            DrawPreviewSection();
            EditorGUILayout.Space(10);
            DrawGenerateButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSourceTextureSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Source Texture", EditorStyles.boldLabel);
                
                sourceTexture = EditorGUILayout.ObjectField("Clothing Texture", sourceTexture, typeof(Texture2D), false) as Texture2D;
                
                if (sourceTexture != null)
                {
                    string path = AssetDatabase.GetAssetPath(sourceTexture);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null && !importer.isReadable)
                    {
                        EditorGUILayout.HelpBox("Texture needs to be marked as readable.", MessageType.Warning);
                        if (GUILayout.Button("Fix Import Settings"))
                        {
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                        }
                    }
                }
            }
        }

        private void DrawColorSettingsSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Color Settings", EditorStyles.boldLabel);
                
                colorMode = (ColorMode)EditorGUILayout.EnumPopup("Variation Mode", colorMode);
                
                if (colorMode == ColorMode.Family)
                {
                    colorFamily = (ColorFamily)EditorGUILayout.EnumPopup("Color Family", colorFamily);
                    EditorGUILayout.HelpBox(GetColorFamilyDescription(colorFamily), MessageType.Info);
                }
                else if (colorMode == ColorMode.Custom)
                {
                    customBaseColor = EditorGUILayout.ColorField("Base Color", customBaseColor);
                    customHueRange = EditorGUILayout.Slider("Hue Range", customHueRange, 0f, 0.5f);
                    EditorGUILayout.MinMaxSlider("Saturation Range", ref customSaturationRange.x, ref customSaturationRange.y, 0f, 1f);
                    EditorGUILayout.MinMaxSlider("Value Range", ref customValueRange.x, ref customValueRange.y, 0f, 1f);
                }

                variationCount = EditorGUILayout.IntField("Number of Variations", variationCount);
                if (variationCount < 1) variationCount = 1;
                
                variationStrength = EditorGUILayout.Slider("Variation Strength", variationStrength, 0f, 1f);
                
                EditorGUILayout.Space(5);
                preserveDetails = EditorGUILayout.Toggle("Preserve Details", preserveDetails);
                if (preserveDetails)
                {
                    EditorGUI.indentLevel++;
                    detailThreshold = EditorGUILayout.Slider("Detail Threshold", detailThreshold, 0f, 1f);
                    EditorGUI.indentLevel--;
                }
                
                preserveAlpha = EditorGUILayout.Toggle("Preserve Transparency", preserveAlpha);
            }
        }

        private void DrawOutputSettingsSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                outputPath = EditorGUILayout.TextField("Output Path", outputPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.StartsWith(Application.dataPath))
                        {
                            path = "Assets" + path.Substring(Application.dataPath.Length);
                        }
                        outputPath = path;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPreviewSection()
        {
            if (previewTextures != null && previewTextures.Length > 0)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    showPreviews = EditorGUILayout.Foldout(showPreviews, "Previews", true);
                    if (showPreviews)
                    {
                        previewSize = EditorGUILayout.IntSlider("Preview Size", previewSize, 64, 256);
                        EditorGUILayout.Space(5);

                        int columns = Mathf.Max(1, (int)(position.width / (previewSize + 10)));
                        for (int i = 0; i < previewTextures.Length; i += columns)
                        {
                            EditorGUILayout.BeginHorizontal();
                            for (int j = 0; j < columns && i + j < previewTextures.Length; j++)
                            {
                                if (previewTextures[i + j] != null)
                                {
                                    GUILayout.Box(previewTextures[i + j], GUILayout.Width(previewSize), GUILayout.Height(previewSize));
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }
        }

        private void DrawGenerateButton()
        {
            EditorGUI.BeginDisabledGroup(isProcessing || sourceTexture == null);
            if (GUILayout.Button("Generate Variants", GUILayout.Height(30)))
            {
                GenerateVariationsAsync();
            }
            EditorGUI.EndDisabledGroup();
        }

        private string GetColorFamilyDescription(ColorFamily family)
        {
            switch (family)
            {
                case ColorFamily.Denim:
                    return "Various shades of blue, perfect for jeans and denim clothing.";
                case ColorFamily.Earth:
                    return "Natural earth tones including browns, tans, and khakis.";
                case ColorFamily.Pastel:
                    return "Soft, light colors with low saturation and high brightness.";
                case ColorFamily.Warm:
                    return "Warm colors including reds, oranges, and yellows.";
                case ColorFamily.Cool:
                    return "Cool colors including blues, greens, and purples.";
                case ColorFamily.Neutral:
                    return "Neutral colors like grays, tans, and subtle browns.";
                case ColorFamily.Vibrant:
                    return "Bright, saturated colors across the spectrum.";
                default:
                    return "";
            }
        }

        private async void GenerateVariationsAsync()
        {
            if (sourceTexture == null || string.IsNullOrEmpty(outputPath))
                return;

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            isProcessing = true;

            try
            {
                await GenerateVariants();
                EditorUtility.DisplayDialog("Success", "Generated variants successfully!", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating variants: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", "Failed to generate variants. Check console for details.", "OK");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }

        private async Task GenerateVariants()
        {
            Color[] sourcePixels = sourceTexture.GetPixels();
            int width = sourceTexture.width;
            int height = sourceTexture.height;

            Color baseColor = CalculateBaseColor(sourcePixels);
            float baseHue, baseSat, baseVal;
            Color.RGBToHSV(baseColor, out baseHue, out baseSat, out baseVal);

            List<Color[]> variants = new List<Color[]>();
            
            float[] hueShifts = GenerateDistributedHueShifts(variationCount);
            
            for (int i = 0; i < variationCount; i++)
            {
                Color[] newPixels = new Color[sourcePixels.Length];
                Array.Copy(sourcePixels, newPixels, sourcePixels.Length);

                ColorRange range = GetColorRange(baseHue, baseSat, baseVal);
                float hueShift = hueShifts[i];
                await Task.Run(() => 
                {
                    ProcessVariant(newPixels, range, i, width, height, hueShift);
                });
                variants.Add(newPixels);
            }

            EditorApplication.delayCall += () =>
            {
                previewTextures = new Texture2D[variants.Count];
                string baseName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(sourceTexture));

                for (int i = 0; i < variants.Count; i++)
                {
                    Texture2D newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    newTexture.SetPixels(variants[i]);
                    newTexture.Apply();

                    previewTextures[i] = CreatePreviewTexture(newTexture);
                    
                    string fileName = $"{baseName}_variant_{i + 1}.png";
                    string filePath = Path.Combine(outputPath, fileName);
                    File.WriteAllBytes(filePath, newTexture.EncodeToPNG());
                    
                    DestroyImmediate(newTexture);
                }

                AssetDatabase.Refresh();
                showPreviews = true;
            };
        }

        private float[] GenerateDistributedHueShifts(int count)
        {
            List<float> shifts = new List<float>();
            
            if (count == 1)
            {
                shifts.Add(UnityEngine.Random.Range(0f, 1f));
                return shifts.ToArray();
            }

            shifts.Add(UnityEngine.Random.Range(0f, 1f));

            for (int i = 1; i < count; i++)
            {
                float bestShift = 0f;
                float maxMinDistance = 0f;

                for (int attempt = 0; attempt < 20; attempt++)
                {
                    float candidateShift = UnityEngine.Random.Range(0f, 1f);
                    float minDistance = 1f;

                    foreach (float existing in shifts)
                    {
                        float distance = Mathf.Min(
                            Mathf.Abs(candidateShift - existing),
                            1f - Mathf.Abs(candidateShift - existing)  
                        );
                        minDistance = Mathf.Min(minDistance, distance);
                    }

                    if (minDistance > maxMinDistance)
                    {
                        maxMinDistance = minDistance;
                        bestShift = candidateShift;
                    }
                }

                shifts.Add(bestShift);
            }

            for (int i = shifts.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                float temp = shifts[i];
                shifts[i] = shifts[swapIndex];
                shifts[swapIndex] = temp;
            }

            return shifts.ToArray();
        }

        private Color CalculateBaseColor(Color[] pixels)
        {
            var colorCounts = new Dictionary<Color32, int>();
            
            foreach (Color pixel in pixels)
            {
                if (pixel.a < 0.1f) continue;
                
                Color32 key = new Color32(
                    (byte)(pixel.r * 255 / 32 * 32),
                    (byte)(pixel.g * 255 / 32 * 32),
                    (byte)(pixel.b * 255 / 32 * 32),
                    255
                );

                if (!colorCounts.ContainsKey(key))
                    colorCounts[key] = 0;
                colorCounts[key]++;
            }

            if (colorCounts.Count == 0)
                return Color.white;

            var mostCommon = colorCounts.OrderByDescending(kv => kv.Value).First().Key;
            return new Color(mostCommon.r / 255f, mostCommon.g / 255f, mostCommon.b / 255f, 1f);
        }

        private ColorRange GetColorRange(float baseHue, float baseSat, float baseVal)
        {
            switch (colorMode)
            {
                case ColorMode.Natural:
                    return new ColorRange(
                        new Vector2(baseHue - 0.03f, baseHue + 0.03f),
                        new Vector2(baseSat * 0.95f, baseSat * 1.05f),
                        new Vector2(baseVal * 0.95f, baseVal * 1.05f)
                    );

                case ColorMode.Family:
                    var familyRange = colorFamilyRanges[colorFamily];
                    return new ColorRange(
                        familyRange.HueRange,
                        new Vector2(
                            Mathf.Lerp(baseSat, familyRange.SaturationRange.x, variationStrength),
                            Mathf.Lerp(baseSat, familyRange.SaturationRange.y, variationStrength)
                        ),
                        new Vector2(
                            Mathf.Lerp(baseVal, familyRange.ValueRange.x, variationStrength),
                            Mathf.Lerp(baseVal, familyRange.ValueRange.y, variationStrength)
                        )
                    );

                case ColorMode.Complement:
                    float complementHue = (baseHue + 0.5f) % 1f;
                    return new ColorRange(
                        new Vector2(complementHue - 0.05f, complementHue + 0.05f),
                        new Vector2(
                            Mathf.Lerp(baseSat, 0.7f, variationStrength),
                            Mathf.Lerp(baseSat, 1f, variationStrength)
                        ),
                        new Vector2(
                            Mathf.Lerp(baseVal, 0.7f, variationStrength),
                            Mathf.Lerp(baseVal, 1f, variationStrength)
                        )
                    );

                case ColorMode.Custom:
                    return new ColorRange(
                        new Vector2(
                            (customBaseColor.r - customHueRange + 1f) % 1f,
                            (customBaseColor.r + customHueRange) % 1f
                        ),
                        customSaturationRange,
                        customValueRange
                    );

                default:
                    return new ColorRange(
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f)
                    );
            }
        }

        private void ProcessVariant(Color[] pixels, ColorRange range, int index, int width, int height, float randomHueShift)
        {
            Dictionary<Color32, List<int>> colorRegions = new Dictionary<Color32, List<int>>();
            
            for (int i = 0; i < pixels.Length; i++)
            {
                if (preserveAlpha && pixels[i].a < 0.1f)
                    continue;

                if (preserveDetails && CalculateDetailValue(pixels, i, width, height) > detailThreshold)
                    continue;

                Color32 key = new Color32(
                    (byte)(pixels[i].r * 255 / 16 * 16),
                    (byte)(pixels[i].g * 255 / 16 * 16),
                    (byte)(pixels[i].b * 255 / 16 * 16),
                    255
                );

                if (!colorRegions.ContainsKey(key))
                    colorRegions[key] = new List<int>();
                colorRegions[key].Add(i);
            }

            foreach (var region in colorRegions)
            {
                Color baseColor = new Color(
                    region.Key.r / 255f,
                    region.Key.g / 255f,
                    region.Key.b / 255f
                );

                float h, s, v;
                Color.RGBToHSV(baseColor, out h, out s, out v);

                h = (h + randomHueShift) % 1f;
                Color newColor = Color.HSVToRGB(h, s, v);

                foreach (int pixelIndex in region.Value)
                {
                    newColor.a = pixels[pixelIndex].a;
                    pixels[pixelIndex] = newColor;
                }
            }
        }

        private float CalculateDetailValue(Color[] pixels, int index, int width, int height)
        {
            Color pixel = pixels[index];
            float maxDiff = 0f;

            int[] offsets = { -1, 1, -width, width };
            foreach (int offset in offsets)
            {
                int neighborIndex = index + offset;
                if (neighborIndex >= 0 && neighborIndex < pixels.Length)
                {
                    if (offset == -1 && (index % width) == 0) continue;
                    if (offset == 1 && (index % width) == width - 1) continue;

                    float diff = ColorDifference(pixel, pixels[neighborIndex]);
                    maxDiff = Mathf.Max(maxDiff, diff);
                }
            }

            return maxDiff;
        }

        private float ColorDifference(Color a, Color b)
        {
            return Mathf.Max(
                Mathf.Abs(a.r - b.r),
                Mathf.Max(
                    Mathf.Abs(a.g - b.g),
                    Mathf.Abs(a.b - b.b)
                )
            );
        }

        private Texture2D CreatePreviewTexture(Texture2D source)
        {
            Texture2D preview = new Texture2D(previewSize, previewSize, TextureFormat.RGBA32, false);
            float scale = Mathf.Min((float)previewSize / source.width, (float)previewSize / source.height);
            int targetWidth = Mathf.RoundToInt(source.width * scale);
            int targetHeight = Mathf.RoundToInt(source.height * scale);
            int offsetX = (previewSize - targetWidth) / 2;
            int offsetY = (previewSize - targetHeight) / 2;

            Color[] clearPixels = new Color[previewSize * previewSize];
            for (int i = 0; i < clearPixels.Length; i++)
                clearPixels[i] = Color.clear;
            preview.SetPixels(clearPixels);

            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = Mathf.FloorToInt(x / scale);
                    int sourceY = Mathf.FloorToInt(y / scale);
                    Color pixel = source.GetPixel(sourceX, sourceY);
                    preview.SetPixel(x + offsetX, y + offsetY, pixel);
                }
            }

            preview.Apply();
            return preview;
        }
    }

    public class ColorRange
    {
        public Vector2 HueRange { get; private set; }
        public Vector2 SaturationRange { get; private set; }
        public Vector2 ValueRange { get; private set; }
        private System.Random random;

        public ColorRange(Vector2 hueRange, Vector2 saturationRange, Vector2 valueRange)
        {
            HueRange = hueRange;
            SaturationRange = saturationRange;
            ValueRange = valueRange;
            random = new System.Random();
        }

        public float GetRandomHue()
        {
            float value = (float)(random.NextDouble() * (HueRange.y - HueRange.x) + HueRange.x);
            return (value + 1f) % 1f;
        }

        public float GetRandomSaturation()
        {
            return (float)(random.NextDouble() * (SaturationRange.y - SaturationRange.x) + SaturationRange.x);
        }

        public float GetRandomValue()
        {
            return (float)(random.NextDouble() * (ValueRange.y - ValueRange.x) + ValueRange.x);
        }
    }
}