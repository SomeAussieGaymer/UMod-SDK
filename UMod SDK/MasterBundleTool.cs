using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using Tools;
using System.Text;
using SDG.Unturned;
using SDG.Framework.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor.Build.Reporting;

namespace Tools
{
	[Serializable]
	public class AssetBundleDebugOptions 
	{
		public bool DisableWriteTypeTree = false;
		public bool DeterministicAssetBundle = true;
		public bool ForceRebuildAssetBundle = true;
		public bool IgnoreTypeTreeChanges = false;
		public bool AppendHashToAssetBundleName = true;
		public bool buildAssetBundle = true;
	}

	public class MasterBundleTool : EditorWindow
	{
		private bool foldoutState_AllAssetBundles = true;
		private bool foldoutState_MasterBundles = true;
		private Vector2 assetScrollPosition;
		private Vector2 masterScrollPosition;
		private bool multiplatform = false;
		private int assetBundleVersion = 5;

		[MenuItem("UMod SDK/Master Bundle Tool")]
		public static void ShowWindow()
		{
			CheckRequiredPackages();
			
			GetWindow(typeof(MasterBundleTool));
		}

		private static void CheckRequiredPackages()
		{
			bool hasEditorCoroutines = false;
			
			try
			{
				Type editorCoroutineType = Type.GetType("Unity.EditorCoroutines.Editor.EditorCoroutineUtility, Unity.EditorCoroutines.Editor");
				hasEditorCoroutines = editorCoroutineType != null;
			}
			catch
			{
				hasEditorCoroutines = false;
			}
			
			if (!hasEditorCoroutines)
			{
				if (EditorUtility.DisplayDialog("Missing Required Package", 
					"The Editor Coroutines package is required for the Master Bundle Tool to function properly.\n\n" +
					"Would you like to add it through the Package Manager now?", 
					"Yes, add package", "No, I'll add it manually"))
				{
					UnityEditor.PackageManager.Client.Add("com.unity.editorcoroutines");
					Debug.Log("Added Editor Coroutines package. You may need to restart Unity for changes to take effect.");
				}
			}
		}

		private void OnEnable()
		{
			titleContent = new GUIContent("UMod SDK - Master Bundle Tool");
			multiplatform = EditorPrefs.GetBool("MasterBundleTool_Multiplatform", false);
			assetBundleVersion = EditorPrefs.GetInt("MasterBundleTool_AssetBundleVersion", 5);
		}

		private bool IsMasterBundle(string assetBundleName)
		{
			return EditorPrefs.GetBool(assetBundleName + "_IsMasterBundle", false);
		}

		private void SetMasterBundle(string assetBundleName, bool isMasterBundle)
		{
			EditorPrefs.SetBool(assetBundleName + "_IsMasterBundle", isMasterBundle);
		}

		private string GetMasterBundleExportPath(string assetBundleName)
		{
			return EditorPrefs.GetString(assetBundleName + "_ExportPath", "");
		}

		private void OnGUI()
		{
			foldoutState_AllAssetBundles = EditorGUILayout.Foldout(foldoutState_AllAssetBundles, "Asset Bundles");
			if (foldoutState_AllAssetBundles)
			{
				assetScrollPosition = GUILayout.BeginScrollView(assetScrollPosition);
				DisplayAssetBundleList();
				GUILayout.EndScrollView();
			}

			foldoutState_MasterBundles = EditorGUILayout.Foldout(foldoutState_MasterBundles, "Master Bundles");
			if (foldoutState_MasterBundles)
			{
				masterScrollPosition = GUILayout.BeginScrollView(masterScrollPosition);
				DisplayMasterBundleOptions();
				GUILayout.EndScrollView();
			}
		}

		private void DisplayAssetBundleList()
		{
			string[] assetBundles = AssetDatabase.GetAllAssetBundleNames();
			foreach (string assetBundleName in assetBundles)
			{
				bool currentlyMasterBundle = IsMasterBundle(assetBundleName);
				bool checkState = GUILayout.Toggle(currentlyMasterBundle, assetBundleName);
				if (checkState != currentlyMasterBundle)
				{
					SetMasterBundle(assetBundleName, checkState);
				}
			}
		}

		private void DisplayMasterBundleOptions()
		{
			EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
			
			bool newMultiplatform = EditorGUILayout.Toggle(new GUIContent("Multi-platform", "Build for Windows, Mac, and Linux"), multiplatform);
			if (newMultiplatform != multiplatform)
			{
				multiplatform = newMultiplatform;
				EditorPrefs.SetBool("MasterBundleTool_Multiplatform", multiplatform);
			}
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Asset Bundle Version:", GUILayout.Width(150));
			string versionString = EditorGUILayout.TextField(assetBundleVersion.ToString(), GUILayout.Width(50));
			if (int.TryParse(versionString, out int newVersion) && newVersion > 0 && newVersion != assetBundleVersion)
			{
				assetBundleVersion = newVersion;
				EditorPrefs.SetInt("MasterBundleTool_AssetBundleVersion", assetBundleVersion);
			}
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Master Bundles", EditorStyles.boldLabel);

			string[] assetBundles = AssetDatabase.GetAllAssetBundleNames();
			foreach (string assetBundleName in assetBundles)
			{
				if (!IsMasterBundle(assetBundleName))
					continue;

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(assetBundleName, GUILayout.MinWidth(150));

			
				string currentExportPath = GetMasterBundleExportPath(assetBundleName);
				bool hasExportPath = !string.IsNullOrEmpty(currentExportPath);
				if (GUILayout.Button(new GUIContent("...", hasExportPath ? currentExportPath : "Select Export Path"), GUILayout.Width(30)))
				{
					string newExportPath = EditorUtility.OpenFolderPanel("Master Bundle Export Location", currentExportPath, "");
					if (!string.IsNullOrEmpty(newExportPath))
					{
						EditorPrefs.SetString(assetBundleName + "_ExportPath", newExportPath);
					}
				}

				bool canExport = hasExportPath; 
				bool wasEnabled = GUI.enabled;
				GUI.enabled = canExport;
				if (GUILayout.Button("Export", GUILayout.Width(80)))
				{
					ExportMasterBundle(assetBundleName);
				}
				GUI.enabled = wasEnabled;
				EditorGUILayout.EndHorizontal();
			}
		}

		private string FindAssignedRootFolder(string assetBundleName)
		{
			List<string> candidateFolders = new List<string>();
			string[] folderGUIDs = AssetDatabase.FindAssets("t:DefaultAsset");

			foreach (string guid in folderGUIDs)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (AssetDatabase.IsValidFolder(path))
				{
					AssetImporter importer = AssetImporter.GetAtPath(path);
					if (importer != null && importer.assetBundleName == assetBundleName)
					{
						candidateFolders.Add(path);
					}
				}
			}

			if (candidateFolders.Count == 0)
			{
				Debug.LogError($"Asset bundle '{assetBundleName}' is not assigned to any folder. Please select the root folder for your bundle in the Project view and assign the bundle name in the Inspector.");
				return null;
			}

			if (candidateFolders.Count > 1)
			{
				Debug.LogError($"Asset bundle '{assetBundleName}' is assigned to multiple folders ({string.Join(", ", candidateFolders)}). Please assign the bundle name to only one root folder.");
				return null;
			}

			return candidateFolders[0]; 
		}

		private void ExportMasterBundle(string assetBundleName)
		{
			string exportPath = GetMasterBundleExportPath(assetBundleName);

			string absoluteRootPath = FindAssignedRootFolder(assetBundleName);

			if (string.IsNullOrEmpty(exportPath))
			{
				Debug.LogError($"Export path not set for {assetBundleName}");
				EditorUtility.DisplayDialog("Export Error", "Please set an export path first.", "OK");
				return;
			}
	
			if (string.IsNullOrEmpty(absoluteRootPath))
			{
				EditorUtility.DisplayDialog("Export Error", $"Could not automatically determine the root folder for bundle '{assetBundleName}'. Check console for details.", "OK");
				return;
			}

	
			string rootBundleFolderPath = absoluteRootPath.StartsWith("Assets/") ? absoluteRootPath.Substring(7) : absoluteRootPath;
			rootBundleFolderPath = rootBundleFolderPath.Trim('/', '\\');
			Debug.Log($"Automatically detected root folder: '{absoluteRootPath}" + (absoluteRootPath == rootBundleFolderPath ? "" : $"' -> Using relative path: '{rootBundleFolderPath}'"));

			bool hasEditorCoroutines = false;
			try
			{
				Type editorCoroutineType = Type.GetType("Unity.EditorCoroutines.Editor.EditorCoroutineUtility, Unity.EditorCoroutines.Editor");
				hasEditorCoroutines = editorCoroutineType != null;
			}
			catch
			{
				hasEditorCoroutines = false;
			}

			if (hasEditorCoroutines)
			{
				EditorCoroutineUtility.StartCoroutine(ExportMasterBundleAsync(assetBundleName, exportPath, rootBundleFolderPath), this);
			}
			else
			{
				Debug.LogWarning("Editor Coroutines package not found. Running export synchronously. For a better experience, please install the Editor Coroutines package.");
				ExportMasterBundleSynchronously(assetBundleName, exportPath, rootBundleFolderPath);
			}
		}

		private System.Collections.IEnumerator ExportMasterBundleAsync(string assetBundleName, string outputPath, string rootBundleFolderPath)
		{
			EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Preparing...", 0.0f);
			
			try
			{
				if (!Directory.Exists(outputPath))
				{
					Directory.CreateDirectory(outputPath);
				}
				
				Debug.Log($"Using automatically detected root folder path (relative to Assets/): {rootBundleFolderPath}");
				string assetPrefix = $"Assets\\{rootBundleFolderPath}"; 
				Debug.Log($"Using Asset prefix: {assetPrefix}");
			}
			catch (Exception e)
			{
				EditorUtility.ClearProgressBar();
				Debug.LogError($"Error preparing export: {e.Message}");
				EditorUtility.DisplayDialog("Export Error", $"An error occurred during preparation: {e.Message}", "OK");
				yield break;
			}
			
			yield return null;
			
			EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Writing MasterBundle.dat and creating folders...", 0.2f);
			try
			{
				string lastFolder = rootBundleFolderPath.Contains('/') ? rootBundleFolderPath.Substring(rootBundleFolderPath.LastIndexOf('/') + 1) :
											rootBundleFolderPath.Contains('\\') ? rootBundleFolderPath.Substring(rootBundleFolderPath.LastIndexOf('\\') + 1) :
											rootBundleFolderPath;
				string assetPrefix = $"Assets\\{lastFolder}"; 
				WriteMasterBundleFile(outputPath, assetBundleName, assetPrefix, rootBundleFolderPath);
			}
			catch (Exception e)
			{
				EditorUtility.ClearProgressBar();
				Debug.LogError($"Error writing master bundle file: {e.Message}");
				EditorUtility.DisplayDialog("Export Error", $"An error occurred writing MasterBundle.dat: {e.Message}", "OK");
				yield break;
			}
			
			yield return null;
			
			EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Building asset bundle...", 0.7f);
			try
			{
				BuildAssetBundle(assetBundleName, outputPath, multiplatform);
			}
			catch (Exception e)
			{
				EditorUtility.ClearProgressBar();
				Debug.LogError($"Error building asset bundle: {e.Message}");
				EditorUtility.DisplayDialog("Export Error", $"An error occurred building asset bundle: {e.Message}", "OK");
				yield break;
			}
			
			yield return null;
			
			EditorUtility.ClearProgressBar();
			Debug.Log("Export completed successfully!");
			EditorUtility.DisplayDialog("Export Complete", $"Master Bundle {assetBundleName} has been exported to {outputPath}", "OK");
		}

		private void ExportMasterBundleSynchronously(string assetBundleName, string exportPath, string rootBundleFolderPath)
		{
			try
			{
				Debug.Log($"Starting export of {assetBundleName} to {exportPath} with automatically detected root folder Assets/{rootBundleFolderPath}");
				
			
				if (!Directory.Exists(exportPath))
				{
					Directory.CreateDirectory(exportPath);
				}

				string lastFolder = rootBundleFolderPath.Contains('/') ? rootBundleFolderPath.Substring(rootBundleFolderPath.LastIndexOf('/') + 1) :
											rootBundleFolderPath.Contains('\\') ? rootBundleFolderPath.Substring(rootBundleFolderPath.LastIndexOf('\\') + 1) :
											rootBundleFolderPath;
				string assetPrefix = $"Assets\\{lastFolder}"; 
				Debug.Log($"Determined asset prefix: {assetPrefix}");
				
		
				WriteMasterBundleFile(exportPath, assetBundleName, assetPrefix, rootBundleFolderPath);
				Debug.Log("Created MasterBundle.dat file and exported configs");
			
				BuildAssetBundle(assetBundleName, exportPath, multiplatform);
				Debug.Log("Built asset bundle successfully");
				
				Debug.Log("Export completed successfully!");
				EditorUtility.DisplayDialog("Export Complete", $"Master Bundle {assetBundleName} has been exported to {exportPath}", "OK");
			}
			catch (Exception e)
			{
				Debug.LogError($"Error exporting master bundle: {e.Message}");
				EditorUtility.DisplayDialog("Export Error", $"An error occurred: {e.Message}", "OK");
			}
		}

		private void CreateFolderStructure(string assetBundleName, string outputPath, string rootBundleFolderPath)
		{
			string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
			HashSet<string> createdDirectories = new HashSet<string>();
			string fullRootPrefix = "Assets/" + rootBundleFolderPath.Trim('/', '\\') + "/";

			Debug.Log($"Creating folder structure based on root: {fullRootPrefix}");
			Debug.Log($"Found {assetPaths.Length} assets in bundle '{assetBundleName}'");

		
			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
				Debug.Log($"Created base directory: {outputPath}");
			}

			foreach (string assetPath in assetPaths)
			{
			
				if (!assetPath.StartsWith(fullRootPrefix))
				{
					continue;
				}

				
				string pathRelativeToRoot = assetPath.Substring(fullRootPrefix.Length);

			
				string directoryPath = Path.GetDirectoryName(pathRelativeToRoot);

			
				if (string.IsNullOrEmpty(directoryPath))
				{
					continue;
				}

				
				string fullPath = Path.Combine(outputPath, directoryPath);
				
				if (!createdDirectories.Contains(fullPath))
				{
					if (!Directory.Exists(fullPath))
					{
						Directory.CreateDirectory(fullPath);
						Debug.Log($"Created directory: {fullPath}");
					}
					createdDirectories.Add(fullPath);
				}
			}
			
			Debug.Log($"Created {createdDirectories.Count} directories for folder structure");
		}

		private void WriteMasterBundleFile(string exportPath, string assetBundleName, string assetPrefix, string rootBundleFolderPath)
		{
		
			CreateFolderStructure(assetBundleName, exportPath, rootBundleFolderPath);
			
			string masterBundlePath = Path.Combine(exportPath, "MasterBundle.dat");
			StringBuilder content = new StringBuilder();
			
			content.AppendLine($"Asset_Bundle_Name {assetBundleName}");
			content.AppendLine($"Asset_Prefix {assetPrefix}");
			content.AppendLine($"Asset_Bundle_Version {assetBundleVersion}");
			
			File.WriteAllText(masterBundlePath, content.ToString());
			Debug.Log($"Wrote MasterBundle.dat to {masterBundlePath} with Asset_Prefix {assetPrefix} based on root folder '{rootBundleFolderPath}'");
			
			ExportConfigFiles(assetBundleName, exportPath, rootBundleFolderPath);
		}

		private void ExportConfigFiles(string assetBundleName, string outputPath, string rootBundleFolderPath)
		{
			int exportedCount = 0;
			string fullRootPrefix = "Assets/" + rootBundleFolderPath.Trim('/', '\\') + "/";
			
			try
			{
				string[] itemAssetPaths = AssetDatabase.FindAssets("t:ItemDefinitionAsset");
				string[] vehicleAssetPaths = AssetDatabase.FindAssets("t:VehicleAsset");
				string[] objectAssetPaths = AssetDatabase.FindAssets("t:ObjectDefinitionAsset");
				
				Debug.Log($"Found {itemAssetPaths.Length} items, {vehicleAssetPaths.Length} vehicles, {objectAssetPaths.Length} objects to process");
				
			
				Debug.Log($"Exporting configs based on root folder: {fullRootPrefix}");
				
			
				if (!Directory.Exists(outputPath))
				{
					Directory.CreateDirectory(outputPath);
				}
				
				foreach (string guid in itemAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					
						if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith(fullRootPrefix))
						{
							UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
							
							if (asset is ItemDefinitionAsset)
							{
								ItemDefinitionAsset item = asset as ItemDefinitionAsset;
								
								string subfolder = GetAssetSubfolderPath(assetPath, rootBundleFolderPath);
								
								if (string.IsNullOrEmpty(subfolder))
								{
									Debug.Log($"Skipping .dat export for item '{item.name}' (Asset path: {assetPath}) because it is directly in the specified root folder '{rootBundleFolderPath}'.");
									continue;
								}
								
								string targetDir = Path.Combine(outputPath, subfolder);
								
								if (!Directory.Exists(targetDir))
								{
									Directory.CreateDirectory(targetDir);
								}
								
								ExporterUtility.ExportItemToExistingFolder(item, targetDir);
								exportedCount++;
								Debug.Log($"Exported item definition: {item.name} to {targetDir}");
							}
							else if (!string.IsNullOrEmpty(assetPath))
							{
						
						
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Error processing item asset: {e.Message}");
					}
				}
				
				foreach (string guid in vehicleAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					
						if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith(fullRootPrefix))
						{
							UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
							
							if (asset is VehicleAsset)
							{
								VehicleAsset vehicle = asset as VehicleAsset;
								
								string subfolder = GetAssetSubfolderPath(assetPath, rootBundleFolderPath);
								
								if (string.IsNullOrEmpty(subfolder))
								{
									Debug.Log($"Skipping .dat export for vehicle '{vehicle.name}' (Asset path: {assetPath}) because it is directly in the specified root folder '{rootBundleFolderPath}'.");
									continue;
								}
									
								string targetDir = Path.Combine(outputPath, subfolder);
								
								if (!Directory.Exists(targetDir))
								{
									Directory.CreateDirectory(targetDir);
								}
								
								ExporterUtility.ExportVehicleToExistingFolder(vehicle, targetDir);
								exportedCount++;
								Debug.Log($"Exported vehicle definition: {vehicle.name} to {targetDir}");
							}
							else if (!string.IsNullOrEmpty(assetPath))
							{
							
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Error processing vehicle asset: {e.Message}");
					}
				}
				
				foreach (string guid in objectAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					
						if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith(fullRootPrefix))
						{
							UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
							
							if (asset is ObjectDefinitionAsset)
							{
								ObjectDefinitionAsset obj = asset as ObjectDefinitionAsset;
								
								string subfolder = GetAssetSubfolderPath(assetPath, rootBundleFolderPath);
								
								if (string.IsNullOrEmpty(subfolder))
								{
									Debug.Log($"Skipping .dat export for object '{obj.name}' (Asset path: {assetPath}) because it is directly in the specified root folder '{rootBundleFolderPath}'.");
									continue;
								}
									
								string targetDir = Path.Combine(outputPath, subfolder);
								
								if (!Directory.Exists(targetDir))
								{
									Directory.CreateDirectory(targetDir);
								}
								
								ExporterUtility.ExportObjectToExistingFolder(obj, targetDir);
								exportedCount++;
								Debug.Log($"Exported object definition: {obj.name} to {targetDir}");
							}
							else if (!string.IsNullOrEmpty(assetPath))
							{
							
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Error processing object asset: {e.Message}");
					}
				}
				
						
				string[] materialPaletteAssetPaths = AssetDatabase.FindAssets("t:MaterialPaletteAsset");
				Debug.Log($"Found {materialPaletteAssetPaths.Length} Material Palettes to process for config export");
				foreach (string guid in materialPaletteAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith(fullRootPrefix))
						{
							MaterialPaletteAsset asset = AssetDatabase.LoadAssetAtPath<MaterialPaletteAsset>(assetPath);
							if (asset != null)
							{
								string subfolder = GetAssetSubfolderPath(assetPath, rootBundleFolderPath);
								if (string.IsNullOrEmpty(subfolder))
								{
									Debug.Log($"Skipping config export for Material Palette '{asset.name}' (Asset path: {assetPath}) because it is directly in the specified root folder '{rootBundleFolderPath}'.");
									continue;
								}
								string targetDir = Path.Combine(outputPath, subfolder);
								if (!Directory.Exists(targetDir))
								{
									Directory.CreateDirectory(targetDir);
								}
								ExporterUtility.ExportMaterialPaletteToExistingFolder(asset, targetDir);
								exportedCount++;
								Debug.Log($"Exported material palette config: {asset.name} to {targetDir}");
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Error processing material palette asset {guid}: {e.Message}");
					}
				}

				string[] stereoSongAssetPaths = AssetDatabase.FindAssets("t:StereoSongAsset");
				Debug.Log($"Found {stereoSongAssetPaths.Length} Stereo Songs to process for config export");
				foreach (string guid in stereoSongAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith(fullRootPrefix))
						{
							StereoSongAsset asset = AssetDatabase.LoadAssetAtPath<StereoSongAsset>(assetPath);
							if (asset != null)
							{
								string subfolder = GetAssetSubfolderPath(assetPath, rootBundleFolderPath);
								if (string.IsNullOrEmpty(subfolder))
								{
									Debug.Log($"Skipping config export for Stereo Song '{asset.name}' (Asset path: {assetPath}) because it is directly in the specified root folder '{rootBundleFolderPath}'.");
									continue;
								}
								string targetDir = Path.Combine(outputPath, subfolder);
								if (!Directory.Exists(targetDir))
								{
									Directory.CreateDirectory(targetDir);
								}
								ExporterUtility.ExportStereoSongToExistingFolder(asset, targetDir);
								exportedCount++;
								Debug.Log($"Exported stereo song config: {asset.name} to {targetDir}");
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Error processing stereo song asset {guid}: {e.Message}");
					}
				}

				string[] foliageResourceAssetPaths = AssetDatabase.FindAssets("t:FoliageResourceAsset");
				Debug.Log($"Found {foliageResourceAssetPaths.Length} Foliage Resources to process for config export");
				foreach (string guid in foliageResourceAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith(fullRootPrefix))
						{
							FoliageResourceAsset asset = AssetDatabase.LoadAssetAtPath<FoliageResourceAsset>(assetPath);
							if (asset != null)
							{
								string subfolder = GetAssetSubfolderPath(assetPath, rootBundleFolderPath);
								if (string.IsNullOrEmpty(subfolder))
								{
									Debug.Log($"Skipping config export for Foliage Resource '{asset.name}' (Asset path: {assetPath}) because it is directly in the specified root folder '{rootBundleFolderPath}'.");
									continue;
								}
								string targetDir = Path.Combine(outputPath, subfolder);
								if (!Directory.Exists(targetDir))
								{
									Directory.CreateDirectory(targetDir);
								}
								ExporterUtility.ExportFoliageResourceToExistingFolder(asset, targetDir);
								exportedCount++;
								Debug.Log($"Exported foliage resource config: {asset.name} to {targetDir}");
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Error processing foliage resource asset {guid}: {e.Message}");
					}
				}

				Debug.Log($"Exported {exportedCount} config files total (including .dat and .asset types)");
			}
			catch (Exception e)
			{
				Debug.LogError($"Error exporting config files: {e.Message}");
			}
		}

		private string GetAssetSubfolderPath(string assetPath, string rootBundleFolderPath)
		{
			string subfolder = "";
			string fullRootPrefix = "Assets/" + rootBundleFolderPath.Trim('/', '\\') + "/";

			if (assetPath.StartsWith(fullRootPrefix))
			{
			
				string afterRootFolder = assetPath.Substring(fullRootPrefix.Length);
				subfolder = Path.GetDirectoryName(afterRootFolder);
			}
			else
			{
				Debug.LogWarning($"GetAssetSubfolderPath called with asset path '{assetPath}' which is not under the specified root '{fullRootPrefix}'. Returning empty subfolder.");
			}
			return subfolder; 
		}

		private void BuildAssetBundle(string assetBundleName, string outputPath, bool multiplatform)
		{
			string[] problemAssets = AssetDatabase.FindAssets("t:StereoSongAsset t:MaterialPaletteAsset t:FoliageResourceAsset");
			foreach (string guid in problemAssets)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(assetPath))
				{
					AssetImporter importer = AssetImporter.GetAtPath(assetPath);
					if (importer != null)
					{
						if (importer.assetBundleName != "DONOTMASTERBUNDLE")
						{
							importer.assetBundleName = "DONOTMASTERBUNDLE";
							Debug.Log($"Excluded editor-only asset from bundle: {assetPath}");
						}
					}
				}
			}
			
			AssetDatabase.SaveAssets();

			string[] allAssetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
			if (allAssetPaths.Length == 0)
			{
				Debug.LogError($"No assets found in bundle {assetBundleName}. Make sure assets are properly assigned to this bundle.");
				throw new Exception("No assets found in bundle");
			}

			List<string> validAssetPaths = new List<string>();
			foreach (string assetPath in allAssetPaths)
			{
				bool isEditorOnly = false;
				
				UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
				if (asset is ItemDefinitionAsset || asset is VehicleAsset || asset is ObjectDefinitionAsset ||
					asset is StereoSongAsset || asset is MaterialPaletteAsset || asset is FoliageResourceAsset)
				{
					Debug.LogWarning($"Skipping editor-only asset from bundle: {assetPath}");
					isEditorOnly = true;
				}
				
				if (assetPath.EndsWith(".meta"))
				{
					isEditorOnly = true;
				}
				
				if (assetPath.Contains("/Editor/") || assetPath.StartsWith("Assets/Editor/"))
				{
					Debug.LogWarning($"Skipping editor folder asset from bundle: {assetPath}");
					isEditorOnly = true;
				}
				
				if (!isEditorOnly)
				{
					validAssetPaths.Add(assetPath);
				}
			}
			
			Debug.Log($"Building asset bundle with {validAssetPaths.Count} assets (filtered from {allAssetPaths.Length})");
			
			if (validAssetPaths.Count == 0)
			{
				Debug.LogWarning($"All assets in bundle {assetBundleName} are editor-only. Creating empty bundle.");
				string dummyPath = "Assets/dummy.txt";
				if (!File.Exists(dummyPath))
				{
					File.WriteAllText(dummyPath, "Dummy asset for bundle creation");
					AssetDatabase.Refresh();
					AssetImporter importer = AssetImporter.GetAtPath(dummyPath);
					importer.assetBundleName = assetBundleName;
				}
				validAssetPaths.Add(dummyPath);
			}
			
			AssetBundleBuild[] buildMap = new AssetBundleBuild[]
			{
				new AssetBundleBuild
				{
					assetBundleName = assetBundleName,
					assetNames = validAssetPaths.ToArray()
				}
			};
			
			string tempBuildPath = Path.Combine(Path.GetTempPath(), "UModSDK_TempBuild");
			if (!Directory.Exists(tempBuildPath))
			{
				Directory.CreateDirectory(tempBuildPath);
			}
			
			try
			{
				BuildTarget windowsTarget = BuildTarget.StandaloneWindows64;
				Debug.Log($"Building asset bundle for Windows (Target: {windowsTarget})");
				BuildPipeline.BuildAssetBundles(tempBuildPath, buildMap, BuildAssetBundleOptions.None, windowsTarget);
				
				string builtBundlePath = Path.Combine(tempBuildPath, assetBundleName);
				string targetBundlePath = Path.Combine(outputPath, Path.GetFileName(assetBundleName));
				if (File.Exists(builtBundlePath))
				{
					File.Copy(builtBundlePath, targetBundlePath, true);
					Debug.Log($"Copied Windows bundle to: {targetBundlePath}");
				}
				else
				{
					Debug.LogError($"Failed to find built bundle at: {builtBundlePath}");
				}
				
				if (multiplatform)
				{
					string linuxName = MasterBundleHelper.getLinuxAssetBundleName(assetBundleName);
					Debug.Log($"Building Linux bundle: {linuxName}");
					buildMap[0].assetBundleName = linuxName;
					BuildPipeline.BuildAssetBundles(tempBuildPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneLinux64);
					
					string builtLinuxPath = Path.Combine(tempBuildPath, linuxName);
					string targetLinuxPath = Path.Combine(outputPath, Path.GetFileName(linuxName));
					if (File.Exists(builtLinuxPath))
					{
						File.Copy(builtLinuxPath, targetLinuxPath, true);
						Debug.Log($"Copied Linux bundle to: {targetLinuxPath}");
					}
					
					string macName = MasterBundleHelper.getMacAssetBundleName(assetBundleName);
					Debug.Log($"Building Mac bundle: {macName}");
					buildMap[0].assetBundleName = macName;
					BuildPipeline.BuildAssetBundles(tempBuildPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);
					
					string builtMacPath = Path.Combine(tempBuildPath, macName);
					string targetMacPath = Path.Combine(outputPath, Path.GetFileName(macName));
					if (File.Exists(builtMacPath))
					{
						File.Copy(builtMacPath, targetMacPath, true);
						Debug.Log($"Copied Mac bundle to: {targetMacPath}");
					}
				}
				
				Debug.Log("Asset bundle building completed successfully");
				
				if (validAssetPaths.Contains("Assets/dummy.txt"))
				{
					AssetDatabase.DeleteAsset("Assets/dummy.txt");
					AssetDatabase.Refresh();
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"Error building asset bundle: {e.Message}\n{e.StackTrace}");
				
				if (validAssetPaths.Contains("Assets/dummy.txt"))
				{
					AssetDatabase.DeleteAsset("Assets/dummy.txt");
					AssetDatabase.Refresh();
				}
				
				throw;
			}
			finally
			{
				try
				{
					if (Directory.Exists(tempBuildPath))
					{
						Directory.Delete(tempBuildPath, true);
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning($"Failed to clean up temp directory: {e.Message}");
				}
			}
		}
	}
}

namespace SDG.Unturned
{
	public static class MasterBundleHelper
	{
		public static string getConfigPath(string absoluteDirectory)
		{
			return absoluteDirectory + "/MasterBundle.dat";
		}

		public static bool containsMasterBundle(string absoluteDirectory)
		{
			return File.Exists(getConfigPath(absoluteDirectory));
		}

		public static string insertAssetBundleNameSuffix(string name, string suffix)
		{
			int index = name.IndexOf('.');
			if (index < 0)
			{
				return name + suffix;
			}
			else
			{
				return name.Insert(index, suffix);
			}
		}

		public static string getLinuxAssetBundleName(string name)
		{
			return insertAssetBundleNameSuffix(name, "_linux");
		}

		public static string getMacAssetBundleName(string name)
		{
			return insertAssetBundleNameSuffix(name, "_mac");
		}

		public static string getHashFileName(string name)
		{
			return name + ".hash";
		}
	}
}

namespace SDG.Unturned.Tools
{
	public static class AssetExtensions
	{
		public static void SaveAllFiles(this VehicleAsset vehicleAsset, string targetDirectory)
		{
			if (!Directory.Exists(targetDirectory))
			{
				Directory.CreateDirectory(targetDirectory);
			}

			string vehicleText = vehicleAsset.GetText();
			string fileName = vehicleAsset.title;
			string filePath = Path.Combine(targetDirectory, fileName + ".dat");
			File.WriteAllText(filePath, vehicleText);

			StringBuilder sb = new StringBuilder();
			if (!string.IsNullOrEmpty(vehicleAsset.title))
				sb.AppendLine("Name " + vehicleAsset.title);
			string englishText = sb.ToString();
			
			if (!string.IsNullOrEmpty(englishText))
			{
				string englishFilePath = Path.Combine(targetDirectory, "English.dat");
				File.WriteAllText(englishFilePath, englishText);
			}

			Debug.Log($"Vehicle files saved to: {targetDirectory}");
		}

		public static void SaveAllFiles(this ItemDefinitionAsset itemAsset, string targetDirectory)
		{
			if (!Directory.Exists(targetDirectory))
			{
				Directory.CreateDirectory(targetDirectory);
			}

			string itemText = itemAsset.GetText();
			string fileName = itemAsset.nameEnglish;
			if (string.IsNullOrEmpty(fileName))
				fileName = itemAsset.name;

			foreach (char c in Path.GetInvalidFileNameChars())
			{
				fileName = fileName.Replace(c, '_');
			}
			string filePath = Path.Combine(targetDirectory, fileName + ".dat");
			File.WriteAllText(filePath, itemText);

			StringBuilder sb = new StringBuilder();
			if (!string.IsNullOrEmpty(itemAsset.nameEnglish))
				sb.AppendLine("Name " + itemAsset.nameEnglish);
			if (!string.IsNullOrEmpty(itemAsset.descriptionEnglish))
				sb.AppendLine("Description " + itemAsset.descriptionEnglish);
			string englishText = sb.ToString();
			
			if (!string.IsNullOrEmpty(englishText))
			{
				string englishFilePath = Path.Combine(targetDirectory, "English.dat");
				File.WriteAllText(englishFilePath, englishText);
			}

			Debug.Log($"Item files saved to: {targetDirectory}");
		}

		public static void SaveAllFiles(this ObjectDefinitionAsset objectAsset, string targetDirectory)
		{
			if (!Directory.Exists(targetDirectory))
			{
				Directory.CreateDirectory(targetDirectory);
			}

			string objectText = objectAsset.GetText();
			string fileName = objectAsset.nameEnglish;
			if (string.IsNullOrEmpty(fileName))
				fileName = objectAsset.name;

			foreach (char c in Path.GetInvalidFileNameChars())
			{
				fileName = fileName.Replace(c, '_');
			}
			string filePath = Path.Combine(targetDirectory, fileName + ".dat");
			File.WriteAllText(filePath, objectText);

			StringBuilder sb = new StringBuilder();
			if (!string.IsNullOrEmpty(objectAsset.nameEnglish))
				sb.AppendLine("Name " + objectAsset.nameEnglish);
			if (!string.IsNullOrEmpty(objectAsset.interactEnglish))
				sb.AppendLine("Interact " + objectAsset.interactEnglish);
			string englishText = sb.ToString();
			
			if (!string.IsNullOrEmpty(englishText))
			{
				string englishFilePath = Path.Combine(targetDirectory, "English.dat");
				File.WriteAllText(englishFilePath, englishText);
			}

			Debug.Log($"Object files saved to: {targetDirectory}");
		}
	}
}

namespace SDG.Framework.IO
{
	public static class EditorAssetBundleHelper
	{
		public static void Build(string assetBundleName, string outputPath, bool multiplatform)
		{
			BuildPipeline.BuildAssetBundles(outputPath, 
				new[] { new AssetBundleBuild 
				{ 
					assetBundleName = assetBundleName,
					assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName)
				} }, 
				BuildAssetBundleOptions.None,
				multiplatform ? BuildTarget.StandaloneWindows : EditorUserBuildSettings.activeBuildTarget);
		}
	}
}
