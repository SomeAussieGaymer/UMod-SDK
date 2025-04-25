using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System;

namespace Tools
{
    public class ExporterUtility : EditorWindow
    {
        private VehicleAsset selectedVehicle;
        private ItemDefinitionAsset selectedItem;
        private ObjectDefinitionAsset selectedObject;
        private MaterialPaletteAsset selectedMaterialPalette;
        private StereoSongAsset selectedStereoSong;
        private FoliageResourceAsset selectedFoliageResource;
        
        private List<VehicleAsset> selectedVehicles = new List<VehicleAsset>();
        private List<ItemDefinitionAsset> selectedItems = new List<ItemDefinitionAsset>();
        private List<ObjectDefinitionAsset> selectedObjects = new List<ObjectDefinitionAsset>();
        private List<MaterialPaletteAsset> selectedMaterialPalettes = new List<MaterialPaletteAsset>();
        private List<StereoSongAsset> selectedStereoSongs = new List<StereoSongAsset>();
        private List<FoliageResourceAsset> selectedFoliageResources = new List<FoliageResourceAsset>();
        
        private bool useMultiSelectionVehicles = false;
        private bool useMultiSelectionItems = false;
        private bool useMultiSelectionObjects = false;
        private bool useMultiSelectionMaterialPalettes = false;
        private bool useMultiSelectionStereoSongs = false;
        private bool useMultiSelectionFoliageResources = false;
        
        private bool showVehicleExporter = true;
        private bool showItemExporter = true;
        private bool showObjectExporter = true;
        private bool showMaterialPaletteExporter = true;
        private bool showStereoSongExporter = true;
        private bool showFoliageResourceExporter = true;
        private Vector2 scrollPosition;

        [MenuItem("UMod SDK/Export Utility")]
        public static void ShowWindow()
        {
            GetWindow<ExporterUtility>("UMod Exporter");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            showVehicleExporter = EditorGUILayout.Foldout(showVehicleExporter, "Vehicle Exporter");
            if (showVehicleExporter)
            {
                EditorGUI.indentLevel++;
                
                if (GUILayout.Button("Export All Vehicles"))
                {
                    ExportAllVehicles();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Export Vehicles", EditorStyles.boldLabel);
                
                useMultiSelectionVehicles = EditorGUILayout.Toggle("Multi-Selection Mode", useMultiSelectionVehicles);
                
                if (useMultiSelectionVehicles)
                {
                    
                EditorGUILayout.LabelField("Select Multiple Vehicles (hold Ctrl/Cmd to select multiple)");
                    EditorGUILayout.BeginVertical("box");
                    
                    for (int i = 0; i < selectedVehicles.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        selectedVehicles[i] = EditorGUILayout.ObjectField($"Vehicle {i+1}", selectedVehicles[i], typeof(VehicleAsset), false) as VehicleAsset;
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            selectedVehicles.RemoveAt(i);
                            i--; 
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (GUILayout.Button("Add Vehicle"))
                    {
                        selectedVehicles.Add(null);
                    }
                    
                    EditorGUILayout.EndVertical();
                    
                    if (selectedVehicles.Count > 0 && !selectedVehicles.Contains(null))
                    {
                        if (GUILayout.Button("Export Selected Vehicles"))
                        {
                            ExportSelectedVehicles();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select at least one Vehicle Asset to export.", MessageType.Info);
                    }
                }
                else
                {
                    selectedVehicle = EditorGUILayout.ObjectField("Vehicle Asset", selectedVehicle, typeof(VehicleAsset), false) as VehicleAsset;

                    if (selectedVehicle != null)
                    {
                        if (GUILayout.Button("Export Selected Vehicle"))
                        {
                            ExportVehicleData();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select a Vehicle Asset to export.", MessageType.Info);
                    }
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            showItemExporter = EditorGUILayout.Foldout(showItemExporter, "Item Exporter");
            if (showItemExporter)
            {
                EditorGUI.indentLevel++;

                if (GUILayout.Button("Export All Items"))
                {
                    ExportAllItems();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Export Items", EditorStyles.boldLabel);

                useMultiSelectionItems = EditorGUILayout.Toggle("Multi-Selection Mode", useMultiSelectionItems);
                
                if (useMultiSelectionItems)
                {
                    
                    EditorGUILayout.LabelField("Select Multiple Items (hold Ctrl/Cmd to select multiple)");
                    EditorGUILayout.BeginVertical("box");
                    
                    for (int i = 0; i < selectedItems.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        selectedItems[i] = EditorGUILayout.ObjectField($"Item {i+1}", selectedItems[i], typeof(ItemDefinitionAsset), false) as ItemDefinitionAsset;
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            selectedItems.RemoveAt(i);
                            i--; 
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (GUILayout.Button("Add Item"))
                    {
                        selectedItems.Add(null);
                    }
                    
                    EditorGUILayout.EndVertical();
                    
                    if (selectedItems.Count > 0 && !selectedItems.Contains(null))
                    {
                        if (GUILayout.Button("Export Selected Items"))
                        {
                            ExportSelectedItems();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select at least one Item Definition Asset to export.", MessageType.Info);
                    }
                }
                else
                {
                    selectedItem = EditorGUILayout.ObjectField("Item Definition Asset", selectedItem, typeof(ItemDefinitionAsset), false) as ItemDefinitionAsset;

                    if (selectedItem != null)
                    {
                        if (GUILayout.Button("Export Selected Item"))
                        {
                            ExportItemData();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select an Item Definition Asset to export.", MessageType.Info);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            showObjectExporter = EditorGUILayout.Foldout(showObjectExporter, "Object Exporter");
            if (showObjectExporter)
            {
                EditorGUI.indentLevel++;

                if (GUILayout.Button("Export All Objects"))
                {
                    ExportAllObjects();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Export Objects", EditorStyles.boldLabel);

                useMultiSelectionObjects = EditorGUILayout.Toggle("Multi-Selection Mode", useMultiSelectionObjects);
                
                if (useMultiSelectionObjects)
                {
                    
                    EditorGUILayout.LabelField("Select Multiple Objects (hold Ctrl/Cmd to select multiple)");
                    EditorGUILayout.BeginVertical("box");
                    
                    for (int i = 0; i < selectedObjects.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        selectedObjects[i] = EditorGUILayout.ObjectField($"Object {i+1}", selectedObjects[i], typeof(ObjectDefinitionAsset), false) as ObjectDefinitionAsset;
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            selectedObjects.RemoveAt(i);
                            i--; 
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (GUILayout.Button("Add Object"))
                    {
                        selectedObjects.Add(null);
                    }
                    
                    EditorGUILayout.EndVertical();
                    
                    if (selectedObjects.Count > 0 && !selectedObjects.Contains(null))
                    {
                        if (GUILayout.Button("Export Selected Objects"))
                        {
                            ExportSelectedObjects();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select at least one Object Definition Asset to export.", MessageType.Info);
                    }
                }
                else
                {
                    selectedObject = EditorGUILayout.ObjectField("Object Definition Asset", selectedObject, typeof(ObjectDefinitionAsset), false) as ObjectDefinitionAsset;

                    if (selectedObject != null)
                    {
                        if (GUILayout.Button("Export Selected Object"))
                        {
                            ExportObjectData();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select an Object Definition Asset to export.", MessageType.Info);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            showMaterialPaletteExporter = EditorGUILayout.Foldout(showMaterialPaletteExporter, "Material Palette Exporter");
            if (showMaterialPaletteExporter)
            {
                EditorGUI.indentLevel++;

                if (GUILayout.Button("Export All Material Palettes"))
                {
                    ExportAllMaterialPalettes();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Export Material Palettes", EditorStyles.boldLabel);

                useMultiSelectionMaterialPalettes = EditorGUILayout.Toggle("Multi-Selection Mode", useMultiSelectionMaterialPalettes);
                
                if (useMultiSelectionMaterialPalettes)
                {
                    
                    EditorGUILayout.LabelField("Select Multiple Material Palettes (hold Ctrl/Cmd to select multiple)");
                    EditorGUILayout.BeginVertical("box");
                    
                    for (int i = 0; i < selectedMaterialPalettes.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        selectedMaterialPalettes[i] = EditorGUILayout.ObjectField($"Material Palette {i+1}", selectedMaterialPalettes[i], typeof(MaterialPaletteAsset), false) as MaterialPaletteAsset;
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            selectedMaterialPalettes.RemoveAt(i);
                            i--; 
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (GUILayout.Button("Add Material Palette"))
                    {
                        selectedMaterialPalettes.Add(null);
                    }
                    
                    EditorGUILayout.EndVertical();
                    
                    if (selectedMaterialPalettes.Count > 0 && !selectedMaterialPalettes.Contains(null))
                    {
                        if (GUILayout.Button("Export Selected Material Palettes"))
                        {
                            ExportSelectedMaterialPalettes();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select at least one Material Palette Asset to export.", MessageType.Info);
                    }
                }
                else
                {
                    selectedMaterialPalette = EditorGUILayout.ObjectField("Material Palette Asset", selectedMaterialPalette, typeof(MaterialPaletteAsset), false) as MaterialPaletteAsset;

                    if (selectedMaterialPalette != null)
                    {
                        if (GUILayout.Button("Export Selected Material Palette"))
                        {
                            ExportMaterialPaletteData();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select a Material Palette Asset to export.", MessageType.Info);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            showStereoSongExporter = EditorGUILayout.Foldout(showStereoSongExporter, "Stereo Song Exporter");
            if (showStereoSongExporter)
            {
                EditorGUI.indentLevel++;

                if (GUILayout.Button("Export All Stereo Songs"))
                {
                    ExportAllStereoSongs();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Export Stereo Songs", EditorStyles.boldLabel);

     
                useMultiSelectionStereoSongs = EditorGUILayout.Toggle("Multi-Selection Mode", useMultiSelectionStereoSongs);
                
                if (useMultiSelectionStereoSongs)
                {
                    EditorGUILayout.LabelField("Select Multiple Stereo Songs (hold Ctrl/Cmd to select multiple)");
                    EditorGUILayout.BeginVertical("box");
                    
                    for (int i = 0; i < selectedStereoSongs.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        selectedStereoSongs[i] = EditorGUILayout.ObjectField($"Stereo Song {i+1}", selectedStereoSongs[i], typeof(StereoSongAsset), false) as StereoSongAsset;
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            selectedStereoSongs.RemoveAt(i);
                            i--; 
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (GUILayout.Button("Add Stereo Song"))
                    {
                        selectedStereoSongs.Add(null);
                    }
                    
                    EditorGUILayout.EndVertical();
                    
                    if (selectedStereoSongs.Count > 0 && !selectedStereoSongs.Contains(null))
                    {
                        if (GUILayout.Button("Export Selected Stereo Songs"))
                        {
                            ExportSelectedStereoSongs();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select at least one Stereo Song Asset to export.", MessageType.Info);
                    }
                }
                else
                {
                    selectedStereoSong = EditorGUILayout.ObjectField("Stereo Song Asset", selectedStereoSong, typeof(StereoSongAsset), false) as StereoSongAsset;

                    if (selectedStereoSong != null)
                    {
                        if (GUILayout.Button("Export Selected Stereo Song"))
                        {
                            ExportStereoSongData();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select a Stereo Song Asset to export.", MessageType.Info);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            showFoliageResourceExporter = EditorGUILayout.Foldout(showFoliageResourceExporter, "Foliage Resource Exporter");
            if (showFoliageResourceExporter)
            {
                EditorGUI.indentLevel++;

                if (GUILayout.Button("Export All Foliage Resources"))
                {
                    ExportAllFoliageResources();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Export Foliage Resources", EditorStyles.boldLabel);

                useMultiSelectionFoliageResources = EditorGUILayout.Toggle("Multi-Selection Mode", useMultiSelectionFoliageResources);
                
                if (useMultiSelectionFoliageResources)
                {
                    EditorGUILayout.LabelField("Select Multiple Foliage Resources (hold Ctrl/Cmd to select multiple)");
                    EditorGUILayout.BeginVertical("box");
                    
                    for (int i = 0; i < selectedFoliageResources.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        selectedFoliageResources[i] = EditorGUILayout.ObjectField($"Foliage Resource {i+1}", selectedFoliageResources[i], typeof(FoliageResourceAsset), false) as FoliageResourceAsset;
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            selectedFoliageResources.RemoveAt(i);
                            i--; 
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (GUILayout.Button("Add Foliage Resource"))
                    {
                        selectedFoliageResources.Add(null);
                    }
                    
                    EditorGUILayout.EndVertical();
                    
                    if (selectedFoliageResources.Count > 0 && !selectedFoliageResources.Contains(null))
                    {
                        if (GUILayout.Button("Export Selected Foliage Resources"))
                        {
                            ExportSelectedFoliageResources();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select at least one Foliage Resource Asset to export.", MessageType.Info);
                    }
                }
                else
                {
                    selectedFoliageResource = EditorGUILayout.ObjectField("Foliage Resource Asset", selectedFoliageResource, typeof(FoliageResourceAsset), false) as FoliageResourceAsset;

                    if (selectedFoliageResource != null)
                    {
                        if (GUILayout.Button("Export Selected Foliage Resource"))
                        {
                            ExportFoliageResourceData();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please select a Foliage Resource Asset to export.", MessageType.Info);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();
        }

        private void ExportAllVehicles()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for All Vehicles",
                "",
                "Vehicles"
            );

            if (string.IsNullOrEmpty(folderPath)) return;
            ExportAllVehiclesToFolder(folderPath);
        }

        private void ExportAllItems()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for All Items",
                "",
                "Items"
            );

            if (string.IsNullOrEmpty(folderPath)) return;
            ExportAllItemsToFolder(folderPath);
        }

        private void ExportAllObjects()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for All Objects",
                "",
                "Objects"
            );

            if (string.IsNullOrEmpty(folderPath)) return;

            var allObjects = AssetDatabase.FindAssets("t:ObjectDefinitionAsset")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ObjectDefinitionAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(obj => obj != null);

            foreach (var obj in allObjects)
            {
                string objFolderPath = Path.Combine(folderPath, obj.name);
                ExportObject(obj, objFolderPath);
            }

            EditorUtility.DisplayDialog("Export Complete", "All objects have been exported successfully!", "OK");
        }

        private void ExportAllMaterialPalettes()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for All Material Palettes",
                "",
                "MaterialPalettes"
            );

            if (string.IsNullOrEmpty(folderPath)) return;
            ExportAllMaterialPalettesToFolder(folderPath);
        }

        private void ExportAllStereoSongs()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for All Stereo Songs",
                "",
                "StereoSongs"
            );

            if (string.IsNullOrEmpty(folderPath)) return;
            ExportAllStereoSongsToFolder(folderPath);
        }

        private void ExportAllFoliageResources()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for All Foliage Resources",
                "",
                "FoliageResources"
            );

            if (string.IsNullOrEmpty(folderPath)) return;
            ExportAllFoliageResourcesToFolder(folderPath);
        }

        private void ExportVehicleData()
        {
            if (selectedVehicle == null) return;

            string path = EditorUtility.SaveFilePanel(
                "Export Vehicle Data",
                "",
                selectedVehicle.name + ".dat",
                "dat"
            );

            if (!string.IsNullOrEmpty(path))
            {
                ExportVehicle(selectedVehicle, path);
            }
        }

        private void ExportItemData()
        {
            if (selectedItem == null) return;

            string folderPath = EditorUtility.SaveFolderPanel(
                "Export Item Data",
                "",
                selectedItem.name
            );

            if (!string.IsNullOrEmpty(folderPath))
            {
                ExportItem(selectedItem, folderPath);
            }
        }

        private void ExportObjectData()
        {
            if (selectedObject == null) return;

            string folderPath = EditorUtility.SaveFolderPanel(
                "Export Object Data",
                "",
                selectedObject.name
            );

            if (!string.IsNullOrEmpty(folderPath))
            {
                ExportObject(selectedObject, folderPath);
            }
        }

        private void ExportMaterialPaletteData()
        {
            if (selectedMaterialPalette == null) return;

            string path = EditorUtility.SaveFilePanel(
                "Export Material Palette Data",
                "",
                selectedMaterialPalette.name + ".asset",
                "asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                ExportMaterialPalette(selectedMaterialPalette, path);
            }
        }

        private void ExportStereoSongData()
        {
            if (selectedStereoSong == null) return;

            string path = EditorUtility.SaveFilePanel(
                "Export Stereo Song Data",
                "",
                selectedStereoSong.name + ".asset",
                "asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                ExportStereoSong(selectedStereoSong, path);
            }
        }

        private void ExportFoliageResourceData()
        {
            if (selectedFoliageResource == null) return;

            string path = EditorUtility.SaveFilePanel(
                "Export Foliage Resource Data",
                "",
                selectedFoliageResource.assetName + ".asset",
                "asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                ExportFoliageResource(selectedFoliageResource, path);
            }
        }

        public static void ExportVehicle(VehicleAsset vehicle, string outputPath)
        {
            if (vehicle == null)
            {
                Debug.LogError("Vehicle asset is null!");
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                string vehicleData = vehicle.GetText();
                
                if (Directory.Exists(outputPath))
                {
                    string fileName = string.IsNullOrEmpty(vehicle.title) ? vehicle.name : vehicle.title;
                    
                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        fileName = fileName.Replace(c, '_');
                    }
                    fileName = fileName.Replace(' ', '_');
                    
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        fileName = "Vehicle_" + vehicle.GetInstanceID();
                        Debug.LogWarning($"Using fallback filename for vehicle: {fileName}");
                    }
                    
                    outputPath = Path.Combine(outputPath, $"{fileName}.dat");
                }
                
                File.WriteAllText(outputPath, vehicleData);
                Debug.Log($"Vehicle data exported to: {outputPath}");
                
                string englishText = vehicle.GetEnglishText();
                
                if (!string.IsNullOrEmpty(englishText))
                {
                    string dir = Path.GetDirectoryName(outputPath);
                    string englishPath = Path.Combine(dir, "English.dat");
                    File.WriteAllText(englishPath, englishText);
                    Debug.Log($"Vehicle English text exported to: {englishPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting vehicle {vehicle.name}: {e.Message}\n{e.StackTrace}");
            }
        }

        public static string GetVehicleData(VehicleAsset vehicle)
        {
            if (vehicle == null)
            {
                Debug.LogError("Vehicle asset is null!");
                return null;
            }

            return vehicle.GetText();
        }

        public static void ExportItem(ItemDefinitionAsset item, string outputFolderPath)
        {
            if (item == null)
            {
                Debug.LogError("Item Definition asset is null!");
                return;
            }

            try
            {
                if (!Directory.Exists(outputFolderPath))
                {
                    Directory.CreateDirectory(outputFolderPath);
                }
                
                string itemData = item.GetText();
                string englishData = item.GetEnglishLanguageText();
                
                string fileName = string.IsNullOrEmpty(item.nameEnglish) ? item.name : item.nameEnglish;
                
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                fileName = fileName.Replace(' ', '_');
                
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "Item_" + item.GetInstanceID();
                    Debug.LogWarning($"Using fallback filename for item: {fileName}");
                }
                
                string itemPath = Path.Combine(outputFolderPath, $"{fileName}.dat");
                File.WriteAllText(itemPath, itemData);
                Debug.Log($"Exported item file: {itemPath}");
                
                if (!string.IsNullOrEmpty(englishData))
                {
                    string englishPath = Path.Combine(outputFolderPath, "English.dat");
                    File.WriteAllText(englishPath, englishData);
                    Debug.Log($"Exported English file: {englishPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting item {item.name}: {e.Message}\n{e.StackTrace}");
            }
        }

        public static string GetItemData(ItemDefinitionAsset item)
        {
            if (item == null)
            {
                Debug.LogError("Item Definition asset is null!");
                return null;
            }

            return item.GetText();
        }

        public static string GetItemEnglishData(ItemDefinitionAsset item)
        {
            if (item == null)
            {
                Debug.LogError("Item Definition asset is null!");
                return null;
            }

            return item.GetEnglishLanguageText();
        }

        public static void ExportObject(ObjectDefinitionAsset obj, string outputFolderPath)
        {
            if (obj == null)
            {
                Debug.LogError("Object Definition asset is null!");
                return;
            }

            try
            {
                if (!Directory.Exists(outputFolderPath))
                {
                    Directory.CreateDirectory(outputFolderPath);
                }
                
                string objectData = obj.GetText();
                
                string fileName = string.IsNullOrEmpty(obj.nameEnglish) ? obj.name : obj.nameEnglish;
                
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                fileName = fileName.Replace(' ', '_');
                
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "Object_" + obj.GetInstanceID();
                    Debug.LogWarning($"Using fallback filename for object: {fileName}");
                }
                
                string objectPath = Path.Combine(outputFolderPath, $"{fileName}.dat");
                File.WriteAllText(objectPath, objectData);
                Debug.Log($"Exported object file: {objectPath}");
                
                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrEmpty(obj.nameEnglish))
                    sb.AppendLine("Name " + obj.nameEnglish);
                if (!string.IsNullOrEmpty(obj.interactEnglish))
                    sb.AppendLine("Interact " + obj.interactEnglish);
                string englishText = sb.ToString();
                
                if (!string.IsNullOrEmpty(englishText))
                {
                    string englishPath = Path.Combine(outputFolderPath, "English.dat");
                    File.WriteAllText(englishPath, englishText);
                    Debug.Log($"Exported English file: {englishPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting object {obj.name}: {e.Message}\n{e.StackTrace}");
            }
        }

        public static string GetObjectData(ObjectDefinitionAsset obj)
        {
            if (obj == null)
            {
                Debug.LogError("Object Definition asset is null!");
                return null;
            }

            return obj.GetText();
        }

        public static string GetObjectEnglishData(ObjectDefinitionAsset obj)
        {
            if (obj == null)
            {
                Debug.LogError("Object Definition asset is null!");
                return null;
            }

            return obj.GetEnglishText();
        }

        public static void ExportMaterialPalette(MaterialPaletteAsset materialPalette, string outputPath)
        {
            if (materialPalette == null)
            {
                Debug.LogError("Material Palette asset is null!");
                return;
            }

            string materialPaletteData = GetMaterialPaletteData(materialPalette);
            File.WriteAllText(outputPath, materialPaletteData);
            Debug.Log($"Material Palette data exported to: {outputPath}");
        }

        public static string GetMaterialPaletteData(MaterialPaletteAsset materialPalette)
        {
            if (materialPalette == null)
            {
                Debug.LogError("Material Palette asset is null!");
                return null;
            }

            MaterialPaletteEditor editor = (MaterialPaletteEditor)Editor.CreateEditor(materialPalette);
            string text = editor.GetText(materialPalette);
            UnityEngine.Object.DestroyImmediate(editor);
            return text;
        }

        public static void ExportAllVehiclesToFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("Export folder path is null or empty!");
                return;
            }

            var allVehicles = GetAllVehicleAssets();
            foreach (var vehicle in allVehicles)
            {
                string outputPath = Path.Combine(folderPath, $"{vehicle.name}.dat");
                ExportVehicle(vehicle, outputPath);
            }
            Debug.Log($"All vehicles exported to: {folderPath}");
        }

        public static void ExportAllItemsToFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("Export folder path is null or empty!");
                return;
            }

            var allItems = GetAllItemAssets();
            foreach (var item in allItems)
            {
                string itemFolderPath = Path.Combine(folderPath, item.name);
                ExportItem(item, itemFolderPath);
            }
            Debug.Log($"All items exported to: {folderPath}");
        }

        public static void ExportAllMaterialPalettesToFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("Export folder path is null or empty!");
                return;
            }

            var allMaterialPalettes = GetAllMaterialPaletteAssets();
            foreach (var materialPalette in allMaterialPalettes)
            {
                string outputPath = Path.Combine(folderPath, $"{materialPalette.name}.asset");
                ExportMaterialPalette(materialPalette, outputPath);
            }
            Debug.Log($"All material palettes exported to: {folderPath}");
        }

        public static VehicleAsset[] GetAllVehicleAssets()
        {
            return AssetDatabase.FindAssets("t:VehicleAsset")
                .Select(guid => AssetDatabase.LoadAssetAtPath<VehicleAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(vehicle => vehicle != null)
                .ToArray();
        }

        public static ItemDefinitionAsset[] GetAllItemAssets()
        {
            return AssetDatabase.FindAssets("t:ItemDefinitionAsset")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ItemDefinitionAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(item => item != null)
                .ToArray();
        }

        public static MaterialPaletteAsset[] GetAllMaterialPaletteAssets()
        {
            return AssetDatabase.FindAssets("t:MaterialPaletteAsset")
                .Select(guid => AssetDatabase.LoadAssetAtPath<MaterialPaletteAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(materialPalette => materialPalette != null)
                .ToArray();
        }

        public static Dictionary<string, string> ExportVehicleWithData(VehicleAsset vehicle, string outputPath)
        {
            var result = new Dictionary<string, string>();
            if (vehicle == null)
            {
                result["error"] = "Vehicle asset is null!";
                return result;
            }

            try
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                string vehicleData = vehicle.GetText();
                
                if (Directory.Exists(outputPath))
                {
                    string fileName = string.IsNullOrEmpty(vehicle.title) ? vehicle.name : vehicle.title;
                    
                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        fileName = fileName.Replace(c, '_');
                    }
                    fileName = fileName.Replace(' ', '_');
                    
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        fileName = "Vehicle_" + vehicle.GetInstanceID();
                        Debug.LogWarning($"Using fallback filename for vehicle: {fileName}");
                    }
                    
                    outputPath = Path.Combine(outputPath, $"{fileName}.dat");
                }
                
                File.WriteAllText(outputPath, vehicleData);
                
                string englishText = vehicle.GetEnglishText();
                string englishPath = null;
                
                if (!string.IsNullOrEmpty(englishText))
                {
                    string parentDir = Path.GetDirectoryName(outputPath);
                    englishPath = Path.Combine(parentDir, "English.dat");
                    File.WriteAllText(englishPath, englishText);
                    result["english_path"] = englishPath;
                    result["english_data"] = englishText;
                }
                
                result["success"] = $"Vehicle exported successfully to: {outputPath}";
                result["data"] = vehicleData;
                result["path"] = outputPath;
            }
            catch (System.Exception ex)
            {
                result["error"] = $"Failed to export vehicle: {ex.Message}";
            }

            return result;
        }

        public static Dictionary<string, string> ExportItemWithData(ItemDefinitionAsset item, string outputFolderPath)
        {
            var result = new Dictionary<string, string>();
            if (item == null)
            {
                result["error"] = "Item Definition asset is null!";
                return result;
            }

            try
            {
                Directory.CreateDirectory(outputFolderPath);
                
                string itemData = item.GetText();
                string englishData = item.GetEnglishLanguageText();
                
                string fileName = string.IsNullOrEmpty(item.nameEnglish) ? item.name : item.nameEnglish;
                
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                fileName = fileName.Replace(' ', '_');
                
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "Item_" + item.GetInstanceID();
                    Debug.LogWarning($"Using fallback filename for item: {fileName}");
                }
                
                string itemPath = Path.Combine(outputFolderPath, $"{fileName}.dat");
                string englishPath = Path.Combine(outputFolderPath, "English.dat");
                
                File.WriteAllText(itemPath, itemData);
                if (!string.IsNullOrEmpty(englishData))
                {
                    File.WriteAllText(englishPath, englishData);
                }
                
                result["success"] = $"Item exported successfully to: {outputFolderPath}";
                result["data"] = itemData;
                result["english_data"] = englishData;
                result["item_path"] = itemPath;
                result["english_path"] = englishPath;
            }
            catch (System.Exception ex)
            {
                result["error"] = $"Failed to export item: {ex.Message}";
            }

            return result;
        }

        public static Dictionary<string, string> ExportMaterialPaletteWithData(MaterialPaletteAsset materialPalette, string outputPath)
        {
            var result = new Dictionary<string, string>();
            if (materialPalette == null)
            {
                result["error"] = "Material Palette asset is null!";
                return result;
            }

            try
            {
                string materialPaletteData = GetMaterialPaletteData(materialPalette);
                File.WriteAllText(outputPath, materialPaletteData);
                result["success"] = $"Material Palette exported successfully to: {outputPath}";
                result["data"] = materialPaletteData;
                result["path"] = outputPath;
            }
            catch (System.Exception ex)
            {
                result["error"] = $"Failed to export material palette: {ex.Message}";
            }

            return result;
        }

        public static void ExportItemToExistingFolder(ItemDefinitionAsset item, string existingFolderPath)
        {
            if (item == null)
            {
                Debug.LogError("ItemDefinitionAsset is null - cannot export");
                return;
            }
            
            if (!Directory.Exists(existingFolderPath))
            {
                Debug.LogWarning($"Directory does not exist, creating: {existingFolderPath}");
                Directory.CreateDirectory(existingFolderPath);
            }
            
            try
            {
                string itemData = item.GetText();
                string englishData = item.GetEnglishLanguageText();
                
                string fileName = string.IsNullOrEmpty(item.nameEnglish) ? item.name : item.nameEnglish;
                
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                fileName = fileName.Replace(' ', '_');
                
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "Item_" + item.GetInstanceID();
                    Debug.LogWarning($"Using fallback filename for item: {fileName}");
                }
                
                string itemPath = Path.Combine(existingFolderPath, $"{fileName}.dat");
                File.WriteAllText(itemPath, itemData);
                Debug.Log($"Exported item file: {itemPath}");
                
                if (!string.IsNullOrEmpty(englishData))
                {
                    string englishPath = Path.Combine(existingFolderPath, "English.dat");
                    File.WriteAllText(englishPath, englishData);
                    Debug.Log($"Exported English file: {englishPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting item {item.name}: {e.Message}\n{e.StackTrace}");
            }
        }

        public static void ExportVehicleToExistingFolder(VehicleAsset vehicle, string existingFolderPath)
        {
            if (vehicle == null)
            {
                Debug.LogError("VehicleAsset is null - cannot export");
                return;
            }
            
            if (!Directory.Exists(existingFolderPath))
            {
                Debug.LogWarning($"Directory does not exist, creating: {existingFolderPath}");
                Directory.CreateDirectory(existingFolderPath);
            }
            
            try
            {
                string vehicleData = vehicle.GetText();
                
                string fileName = string.IsNullOrEmpty(vehicle.title) ? vehicle.name : vehicle.title;
                
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                fileName = fileName.Replace(' ', '_');
                
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "Vehicle_" + vehicle.GetInstanceID();
                    Debug.LogWarning($"Using fallback filename for vehicle: {fileName}");
                }
                
                string vehiclePath = Path.Combine(existingFolderPath, $"{fileName}.dat");
                File.WriteAllText(vehiclePath, vehicleData);
                Debug.Log($"Exported vehicle file: {vehiclePath}");
                
                string englishText = vehicle.GetEnglishText();
                
                if (!string.IsNullOrEmpty(englishText))
                {
                    string englishPath = Path.Combine(existingFolderPath, "English.dat");
                    File.WriteAllText(englishPath, englishText);
                    Debug.Log($"Exported English file: {englishPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting vehicle {vehicle.name}: {e.Message}\n{e.StackTrace}");
            }
        }

        public static void ExportObjectToExistingFolder(ObjectDefinitionAsset obj, string existingFolderPath)
        {
            if (obj == null)
            {
                Debug.LogError("ObjectDefinitionAsset is null - cannot export");
                return;
            }
            
            if (!Directory.Exists(existingFolderPath))
            {
                Debug.LogWarning($"Directory does not exist, creating: {existingFolderPath}");
                Directory.CreateDirectory(existingFolderPath);
            }
            
            try
            {
                string objectData = obj.GetText();
                
                string fileName = string.IsNullOrEmpty(obj.nameEnglish) ? obj.name : obj.nameEnglish;
                
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                fileName = fileName.Replace(' ', '_');
                
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "Object_" + obj.GetInstanceID();
                    Debug.LogWarning($"Using fallback filename for object: {fileName}");
                }
                
                string objectPath = Path.Combine(existingFolderPath, $"{fileName}.dat");
                File.WriteAllText(objectPath, objectData);
                Debug.Log($"Exported object file: {objectPath}");
                
                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrEmpty(obj.nameEnglish))
                    sb.AppendLine("Name " + obj.nameEnglish);
                if (!string.IsNullOrEmpty(obj.interactEnglish))
                    sb.AppendLine("Interact " + obj.interactEnglish);
                string englishText = sb.ToString();
                
                if (!string.IsNullOrEmpty(englishText))
                {
                    string englishPath = Path.Combine(existingFolderPath, "English.dat");
                    File.WriteAllText(englishPath, englishText);
                    Debug.Log($"Exported English file: {englishPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting object {obj.name}: {e.Message}\n{e.StackTrace}");
            }
        }

        public static void ExportMaterialPaletteToExistingFolder(MaterialPaletteAsset materialPalette, string existingFolderPath)
        {
            if (materialPalette == null)
            {
                Debug.LogError("MaterialPaletteAsset is null - cannot export");
                return;
            }
            
            if (!Directory.Exists(existingFolderPath))
            {
                Debug.LogWarning($"Directory does not exist, creating: {existingFolderPath}");
                Directory.CreateDirectory(existingFolderPath);
            }
            
            try
            {
                string materialPaletteData = GetMaterialPaletteData(materialPalette);
                
                string fileName = materialPalette.name;
                
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                fileName = fileName.Replace(' ', '_');
                
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "MaterialPalette_" + materialPalette.GetInstanceID();
                    Debug.LogWarning($"Using fallback filename for material palette: {fileName}");
                }
                
                string materialPalettePath = Path.Combine(existingFolderPath, $"{fileName}.dat");
                File.WriteAllText(materialPalettePath, materialPaletteData);
                Debug.Log($"Exported material palette file: {materialPalettePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting material palette {materialPalette.name}: {e.Message}\n{e.StackTrace}");
            }
        }

        public static void ExportStereoSong(StereoSongAsset stereoSong, string outputPath)
        {
            if (stereoSong == null)
            {
                Debug.LogError("Stereo Song asset is null!");
                return;
            }

            try 
            {
                Editor editor = Editor.CreateEditor(stereoSong);
                if (editor is StereoSongEditor stereoSongEditor)
                {
                    string text = stereoSongEditor.GetText(stereoSong);
                    File.WriteAllText(outputPath, text);
                    UnityEngine.Object.DestroyImmediate(editor);
                    Debug.Log($"Stereo Song data exported to: {outputPath}");
                }
                else
                {
                    Debug.LogError($"Failed to create StereoSongEditor. Created editor type: {editor?.GetType().FullName}");
                    UnityEngine.Object.DestroyImmediate(editor);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error when exporting stereo song. Asset type: {stereoSong.GetType().FullName}. Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static string GetStereoSongData(StereoSongAsset stereoSong)
        {
            if (stereoSong == null)
            {
                Debug.LogError("Stereo Song asset is null!");
                return null;
            }

            try
            {
                Editor editor = Editor.CreateEditor(stereoSong);
                if (editor is StereoSongEditor stereoSongEditor)
                {
                    string text = stereoSongEditor.GetText(stereoSong);
                    UnityEngine.Object.DestroyImmediate(editor);
                    return text;
                }
                else
                {
                    Debug.LogError($"Failed to create StereoSongEditor. Created editor type: {editor?.GetType().FullName}");
                    UnityEngine.Object.DestroyImmediate(editor);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error when getting stereo song data. Asset type: {stereoSong.GetType().FullName}. Error: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public static void ExportAllStereoSongsToFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("Export folder path is null or empty!");
                return;
            }

            var allStereoSongs = GetAllStereoSongAssets();
            foreach (var stereoSong in allStereoSongs)
            {
                string outputPath = Path.Combine(folderPath, $"{stereoSong.name}.asset");
                ExportStereoSong(stereoSong, outputPath);
            }
            Debug.Log($"All stereo songs exported to: {folderPath}");
        }

        public static StereoSongAsset[] GetAllStereoSongAssets()
        {
            return AssetDatabase.FindAssets("t:StereoSongAsset")
                .Select(guid => AssetDatabase.LoadAssetAtPath<StereoSongAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(stereoSong => stereoSong != null)
                .ToArray();
        }

        public static Dictionary<string, string> ExportStereoSongWithData(StereoSongAsset stereoSong, string outputPath)
        {
            var result = new Dictionary<string, string>();
            if (stereoSong == null)
            {
                result["error"] = "Stereo Song asset is null!";
                return result;
            }

            try
            {
                Editor editor = Editor.CreateEditor(stereoSong);
                if (editor is StereoSongEditor stereoSongEditor)
                {
                    string stereoSongData = stereoSongEditor.GetText(stereoSong);
                    File.WriteAllText(outputPath, stereoSongData);
                    result["success"] = $"Stereo Song exported successfully to: {outputPath}";
                    result["data"] = stereoSongData;
                    result["path"] = outputPath;
                }
                else
                {
                    result["error"] = $"Failed to create StereoSongEditor. Created editor type: {editor?.GetType().FullName}";
                }
            }
            catch (System.Exception ex)
            {
                result["error"] = $"Failed to export stereo song: {ex.Message}";
            }

            return result;
        }

        public static void ExportStereoSongToExistingFolder(StereoSongAsset stereoSong, string existingFolderPath)
        {
            if (stereoSong == null)
            {
                Debug.LogError("StereoSongAsset is null - cannot export");
                return;
            }
            
            if (!Directory.Exists(existingFolderPath))
            {
                Debug.LogWarning($"Directory does not exist, creating: {existingFolderPath}");
                Directory.CreateDirectory(existingFolderPath);
            }
            
            try
            {       
                string stereoSongData = null;
                
                try 
                {
                    Editor editor = Editor.CreateEditor(stereoSong);
                    if (editor is StereoSongEditor stereoSongEditor)
                    {
                        stereoSongData = stereoSongEditor.GetText(stereoSong);
                        UnityEngine.Object.DestroyImmediate(editor);
                    }
                    else
                    {
                        Debug.LogError($"Failed to create StereoSongEditor. Created editor type: {editor?.GetType().FullName}");
                        UnityEngine.Object.DestroyImmediate(editor);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error when getting stereo song data. Asset type: {stereoSong.GetType().FullName}. Error: {ex.Message}\n{ex.StackTrace}");
                    return;
                }
                
                if (string.IsNullOrEmpty(stereoSongData))
                {
                    Debug.LogError($"Failed to get data for stereo song {stereoSong.name}");
                    return;
                }
                
                string stereoSongPath = Path.Combine(existingFolderPath, $"{stereoSong.name}.asset");
                File.WriteAllText(stereoSongPath, stereoSongData);
                Debug.Log($"Exported stereo song file: {stereoSongPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting stereo song {stereoSong.name}: {e.Message}\n{e.StackTrace}");
            }
        }

        public static void ExportFoliageResource(FoliageResourceAsset foliageResource, string outputPath)
        {
            if (foliageResource == null)
            {
                Debug.LogError("Foliage Resource asset is null!");
                return;
            }

            string foliageData = foliageResource.GetText();
            File.WriteAllText(outputPath, foliageData);
            Debug.Log($"Foliage Resource data exported to: {outputPath}");
        }

        public static string GetFoliageResourceData(FoliageResourceAsset foliageResource)
        {
            if (foliageResource == null)
            {
                Debug.LogError("Foliage Resource asset is null!");
                return null;
            }

            return foliageResource.GetText();
        }

        public static void ExportAllFoliageResourcesToFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("Export folder path is null or empty!");
                return;
            }

            var allFoliageResources = GetAllFoliageResourceAssets();
            foreach (var foliageResource in allFoliageResources)
            {
                string outputPath = Path.Combine(folderPath, $"{foliageResource.assetName}.asset");
                ExportFoliageResource(foliageResource, outputPath);
            }
            Debug.Log($"All foliage resources exported to: {folderPath}");
        }

        public static FoliageResourceAsset[] GetAllFoliageResourceAssets()
        {
            return AssetDatabase.FindAssets("t:FoliageResourceAsset")
                .Select(guid => AssetDatabase.LoadAssetAtPath<FoliageResourceAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(foliageResource => foliageResource != null)
                .ToArray();
        }

        public static Dictionary<string, string> ExportFoliageResourceWithData(FoliageResourceAsset foliageResource, string outputPath)
        {
            var result = new Dictionary<string, string>();
            if (foliageResource == null)
            {
                result["error"] = "Foliage Resource asset is null!";
                return result;
            }

            try
            {
                string foliageResourceData = foliageResource.GetText();
                File.WriteAllText(outputPath, foliageResourceData);
                result["success"] = $"Foliage Resource exported successfully to: {outputPath}";
                result["data"] = foliageResourceData;
                result["path"] = outputPath;
            }
            catch (System.Exception ex)
            {
                result["error"] = $"Failed to export foliage resource: {ex.Message}";
            }

            return result;
        }

        public static void ExportFoliageResourceToExistingFolder(FoliageResourceAsset foliageResource, string existingFolderPath)
        {
            if (foliageResource == null)
            {
                Debug.LogError("FoliageResourceAsset is null - cannot export");
                return;
            }
            
            if (!Directory.Exists(existingFolderPath))
            {
                Debug.LogWarning($"Directory does not exist, creating: {existingFolderPath}");
                Directory.CreateDirectory(existingFolderPath);
            }
            
            try
            {
                string foliageResourceData = GetFoliageResourceData(foliageResource);
                
                string fileName = foliageResource.assetName;

                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                fileName = fileName.Replace(' ', '_');
                
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "FoliageResource_" + foliageResource.GetInstanceID();
                    Debug.LogWarning($"Using fallback filename for foliage resource: {fileName}");
                }
                
                string foliageResourcePath = Path.Combine(existingFolderPath, $"{fileName}.dat");
                File.WriteAllText(foliageResourcePath, foliageResourceData);
                Debug.Log($"Exported foliage resource file: {foliageResourcePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting foliage resource {foliageResource.name}: {e.Message}\n{e.StackTrace}");
            }
        }

        private void ExportSelectedVehicles()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for Selected Vehicles",
                "",
                "SelectedVehicles"
            );

            if (string.IsNullOrEmpty(folderPath)) return;

            foreach (var vehicle in selectedVehicles)
            {
                if (vehicle != null)
                {
                    string outputPath = Path.Combine(folderPath, $"{vehicle.name}.dat");
                    ExportVehicle(vehicle, outputPath);
                }
            }

            EditorUtility.DisplayDialog("Export Complete", "Selected vehicles have been exported successfully!", "OK");
        }

        private void ExportSelectedItems()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for Selected Items",
                "",
                "SelectedItems"
            );

            if (string.IsNullOrEmpty(folderPath)) return;

            foreach (var item in selectedItems)
            {
                if (item != null)
                {
                    string itemFolderPath = Path.Combine(folderPath, item.name);
                    ExportItem(item, itemFolderPath);
                }
            }

            EditorUtility.DisplayDialog("Export Complete", "Selected items have been exported successfully!", "OK");
        }

        private void ExportSelectedObjects()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for Selected Objects",
                "",
                "SelectedObjects"
            );

            if (string.IsNullOrEmpty(folderPath)) return;

            foreach (var obj in selectedObjects)
            {
                if (obj != null)
                {
                    string objFolderPath = Path.Combine(folderPath, obj.name);
                    ExportObject(obj, objFolderPath);
                }
            }

            EditorUtility.DisplayDialog("Export Complete", "Selected objects have been exported successfully!", "OK");
        }

        private void ExportSelectedMaterialPalettes()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for Selected Material Palettes",
                "",
                "SelectedMaterialPalettes"
            );

            if (string.IsNullOrEmpty(folderPath)) return;

            foreach (var materialPalette in selectedMaterialPalettes)
            {
                if (materialPalette != null)
                {
                    string outputPath = Path.Combine(folderPath, $"{materialPalette.name}.asset");
                    ExportMaterialPalette(materialPalette, outputPath);
                }
            }

            EditorUtility.DisplayDialog("Export Complete", "Selected material palettes have been exported successfully!", "OK");
        }

        private void ExportSelectedStereoSongs()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for Selected Stereo Songs",
                "",
                "SelectedStereoSongs"
            );

            if (string.IsNullOrEmpty(folderPath)) return;

            foreach (var stereoSong in selectedStereoSongs)
            {
                if (stereoSong != null)
                {
                    string outputPath = Path.Combine(folderPath, $"{stereoSong.name}.asset");
                    ExportStereoSong(stereoSong, outputPath);
                }
            }

            EditorUtility.DisplayDialog("Export Complete", "Selected stereo songs have been exported successfully!", "OK");
        }

        private void ExportSelectedFoliageResources()
        {
            string folderPath = EditorUtility.SaveFolderPanel(
                "Select Export Folder for Selected Foliage Resources",
                "",
                "SelectedFoliageResources"
            );

            if (string.IsNullOrEmpty(folderPath)) return;

            foreach (var foliageResource in selectedFoliageResources)
            {
                if (foliageResource != null)
                {
                    string outputPath = Path.Combine(folderPath, $"{foliageResource.assetName}.asset");
                    ExportFoliageResource(foliageResource, outputPath);
                }
            }

            EditorUtility.DisplayDialog("Export Complete", "Selected foliage resources have been exported successfully!", "OK");
        }
    }

#if UNITY_EDITOR
    public class UModAssetPostProcessor : AssetPostprocessor
    {
        private static HashSet<string> processedAssets = new HashSet<string>();
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string importedAsset in importedAssets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(importedAsset) == typeof(StereoSongAsset))
                {
                    StereoSongAsset asset = AssetDatabase.LoadAssetAtPath<StereoSongAsset>(importedAsset);
                    if (asset != null)
                    {
                        AssetImporter importer = AssetImporter.GetAtPath(importedAsset);
                        if (importer != null && importer.assetBundleName != "DONOTMASTERBUNDLE")
                        {
                            processedAssets.Add(importedAsset);
                            importer.assetBundleName = "DONOTMASTERBUNDLE";
                            Debug.Log($"Assigned DONOTMASTERBUNDLE to {importedAsset}");
                        }
                    }
                }

                if (AssetDatabase.GetMainAssetTypeAtPath(importedAsset) == typeof(MaterialPaletteAsset))
                {
                    MaterialPaletteAsset asset = AssetDatabase.LoadAssetAtPath<MaterialPaletteAsset>(importedAsset);
                    if (asset != null)
                    {
                        AssetImporter importer = AssetImporter.GetAtPath(importedAsset);
                        if (importer != null && importer.assetBundleName != "DONOTMASTERBUNDLE")
                        {
                            processedAssets.Add(importedAsset);
                            importer.assetBundleName = "DONOTMASTERBUNDLE";
                            Debug.Log($"Assigned DONOTMASTERBUNDLE to {importedAsset}");
                        }
                    }
                }

                if (AssetDatabase.GetMainAssetTypeAtPath(importedAsset) == typeof(FoliageResourceAsset))
                {
                    FoliageResourceAsset asset = AssetDatabase.LoadAssetAtPath<FoliageResourceAsset>(importedAsset);
                    if (asset != null)
                    {
                        AssetImporter importer = AssetImporter.GetAtPath(importedAsset);
                        if (importer != null && importer.assetBundleName != "DONOTMASTERBUNDLE")
                        {
                            processedAssets.Add(importedAsset);
                            importer.assetBundleName = "DONOTMASTERBUNDLE";
                            Debug.Log($"Assigned DONOTMASTERBUNDLE to {importedAsset}");
                        }
                    }
                }
            }
        }
    }
#endif
} 