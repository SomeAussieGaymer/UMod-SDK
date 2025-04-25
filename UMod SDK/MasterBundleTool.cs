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

		[MenuItem("Window/UMod SDK/Master Bundle Tool")]
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
				EditorGUILayout.LabelField(assetBundleName, GUILayout.Width(200));

				string currentPath = GetMasterBundleExportPath(assetBundleName);
				bool hasPath = !string.IsNullOrEmpty(currentPath);

				if (GUILayout.Button(new GUIContent("...", hasPath ? currentPath : "Select Export Path"), GUILayout.Width(30)))
				{
					string newPath = EditorUtility.OpenFolderPanel("Master Bundle Export Location", currentPath, "");
					if (!string.IsNullOrEmpty(newPath))
					{
						EditorPrefs.SetString(assetBundleName + "_ExportPath", newPath);
					}
				}

				bool wasEnabled = GUI.enabled;
				GUI.enabled = hasPath;
				if (GUILayout.Button("Export", GUILayout.Width(80)))
				{
					ExportMasterBundle(assetBundleName);
				}
				GUI.enabled = wasEnabled;
				EditorGUILayout.EndHorizontal();
			}
		}

		private void ExportMasterBundle(string assetBundleName)
		{
			string exportPath = GetMasterBundleExportPath(assetBundleName);
			if (string.IsNullOrEmpty(exportPath))
			{
				Debug.LogError($"Export path not set for {assetBundleName}");
				EditorUtility.DisplayDialog("Export Error", "Please set an export path first.", "OK");
				return;
			}

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
				EditorCoroutineUtility.StartCoroutine(ExportMasterBundleAsync(assetBundleName, exportPath), this);
			}
			else
			{
				Debug.LogWarning("Editor Coroutines package not found. Running export synchronously. For a better experience, please install the Editor Coroutines package.");
				ExportMasterBundleSynchronously(assetBundleName, exportPath);
			}
		}

		private System.Collections.IEnumerator ExportMasterBundleAsync(string assetBundleName, string outputPath)
		{
			Debug.Log("Exporting master bundle: " + assetBundleName + " to " + outputPath);
			
			float progress = 0f;
			int totalSteps = 7;
			
			Debug.Log("Step 1: Create folder structure");
			CreateFolderStructure(assetBundleName, outputPath);
			EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Creating folder structure...", 1f / totalSteps);
			progress = 1f / totalSteps;
			yield return null;
			
			Debug.Log("Step 2: Export assets directly using ExporterUtility");
			BruteForceCopyAllFiles(assetBundleName, outputPath);
			EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Exporting assets with ExporterUtility...", 2f / totalSteps);
			progress = 2f / totalSteps;
			yield return null;
			
			Debug.Log("Step 3: Exporting config files");
			ExportConfigFiles(assetBundleName, outputPath);
			EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Exporting config files...", 3f / totalSteps);
			progress = 3f / totalSteps;
			yield return null;
			
			Debug.Log("Step 4: Copying other asset files");
			string assetPrefix = DetermineAssetPrefix(assetBundleName);
			CopyOtherAssetFiles(assetBundleName, outputPath, assetPrefix);
			EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Copying other asset files...", 4f / totalSteps);
			progress = 4f / totalSteps;
			yield return null;
			
			Debug.Log("Step 5: Copying important assets");
			CopyImportantAssets(assetBundleName, outputPath, assetPrefix);
			EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Copying important assets...", 5f / totalSteps);
			progress = 5f / totalSteps;
			yield return null;
			
			Debug.Log("Step 6: Writing master bundle file");
			WriteMasterBundleFile(outputPath, assetBundleName, assetPrefix);
			EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Writing master bundle file...", 6f / totalSteps);
			progress = 6f / totalSteps;
			yield return null;
			
			Debug.Log("Step 7: Building asset bundle");
			AssetBundleDebugOptions bundleOptions = new AssetBundleDebugOptions();
			if (bundleOptions.buildAssetBundle)
			{
				BuildAssetBundle(assetBundleName, outputPath, multiplatform);
			}
			else
			{
				Debug.Log("Asset bundle build is disabled in debug options.");
			}
			EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Building asset bundle...", 7f / totalSteps);
			progress = 7f / totalSteps;
			yield return null;
			
			EditorUtility.ClearProgressBar();
			Debug.Log("Export complete for master bundle: " + assetBundleName);
			yield return null;
		}

		private void ExportMasterBundleSynchronously(string assetBundleName, string exportPath)
		{
			try
			{
				EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Step 1: Creating folder structure...", 0.1f);
				Debug.Log($"Step 1: Creating folder structure for {assetBundleName}...");
				try
				{
					CreateFolderStructure(assetBundleName, exportPath);
					Debug.Log("Folder structure created successfully");
				}
				catch (Exception e)
				{
					Debug.LogError($"Error creating folder structure: {e.Message}\n{e.StackTrace}");
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("Export Error", $"Failed to create folder structure: {e.Message}", "OK");
					return;
				}

				EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Step 2: Writing MasterBundle.dat...", 0.3f);
				Debug.Log("Step 2: Writing MasterBundle.dat...");
				try
				{
					string assetPrefix = DetermineAssetPrefix(assetBundleName);
					WriteMasterBundleFile(exportPath, assetBundleName, assetPrefix);
					Debug.Log("MasterBundle.dat written successfully");
				}
				catch (Exception e)
				{
					Debug.LogError($"Error writing MasterBundle.dat: {e.Message}\n{e.StackTrace}");
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("Export Error", $"Failed to write MasterBundle.dat: {e.Message}", "OK");
					return;
				}

				EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Step 3: Exporting config files...", 0.5f);
				Debug.Log("Step 3: Exporting config files...");
				try
				{
					ExportConfigFiles(assetBundleName, exportPath);
					Debug.Log("Config files exported successfully");
				}
				catch (Exception e)
				{
					Debug.LogError($"Error exporting config files: {e.Message}\n{e.StackTrace}");
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("Export Error", $"Failed to export config files: {e.Message}", "OK");
					return;
				}

				EditorUtility.DisplayProgressBar("Exporting Master Bundle", "Step 4: Building asset bundle...", 0.8f);
				Debug.Log("Step 4: Building asset bundle...");
				try
				{
					BuildAssetBundle(assetBundleName, exportPath, multiplatform);
					Debug.Log("Asset bundle built successfully");
				}
				catch (Exception e)
				{
					Debug.LogError($"Error building asset bundle: {e.Message}\n{e.StackTrace}");
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("Export Error", $"Failed to build asset bundle: {e.Message}", "OK");
					return;
				}

				EditorUtility.ClearProgressBar();
				Debug.Log($"Master bundle export completed successfully for {assetBundleName}");
				EditorUtility.DisplayDialog("Export Complete", $"Master bundle {assetBundleName} exported successfully to {exportPath}", "OK");
			}
			catch (Exception e)
			{
				Debug.LogError($"Unexpected error during export: {e.Message}\n{e.StackTrace}");
				EditorUtility.ClearProgressBar();
				EditorUtility.DisplayDialog("Export Error", $"An unexpected error occurred: {e.Message}", "OK");
			}
		}

		private void CreateFolderStructure(string assetBundleName, string outputPath)
		{
			string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
			HashSet<string> createdDirectories = new HashSet<string>();
			
			Debug.Log($"Found {assetPaths.Length} assets in bundle");

			string topLevelFolder = null;
			foreach (string assetPath in assetPaths)
			{
				string relativePath = assetPath;
				if (relativePath.StartsWith("Assets/"))
				{
					relativePath = relativePath.Substring(7);
					
					int firstSlash = relativePath.IndexOf('/');
					if (firstSlash > 0)
					{
						topLevelFolder = relativePath.Substring(0, firstSlash);
						break;
					}
					else if (!string.IsNullOrEmpty(relativePath))
					{
						topLevelFolder = relativePath;
						break;
					}
				}
			}
			
			if (string.IsNullOrEmpty(topLevelFolder))
			{
				Debug.LogWarning("Could not determine top-level folder from assets. Using asset bundle name as default.");
				topLevelFolder = assetBundleName;
			}
			
			Debug.Log($"Using top-level folder: {topLevelFolder}");

			foreach (string assetPath in assetPaths)
			{
				string relativePath = assetPath;
				if (relativePath.StartsWith("Assets/"))
				{
					relativePath = relativePath.Substring(7);
				}
				
				if (!relativePath.StartsWith(topLevelFolder + "/") && relativePath != topLevelFolder)
					continue;
					
				if (relativePath == topLevelFolder)
					continue;
					
				string pathWithoutTopLevel = relativePath.Substring(topLevelFolder.Length + 1);

				string directoryPath = Path.GetDirectoryName(pathWithoutTopLevel);
				if (string.IsNullOrEmpty(directoryPath))
					continue;

				string[] pathParts = directoryPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					.Where(p => !string.IsNullOrEmpty(p)).ToArray();
				
				if (pathParts.Length == 0)
					continue;

				string currentPath = "";
				for (int i = 0; i < pathParts.Length; i++)
				{
					currentPath = Path.Combine(currentPath, pathParts[i]);
					string fullPath = Path.Combine(outputPath, currentPath);

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
			}
			
			Debug.Log($"Created {createdDirectories.Count} directories for folder structure");
		}

		private string DetermineAssetPrefix(string assetBundleName)
		{
			string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
			if (assetPaths.Length == 0)
				return "Assets";
				
			string firstPath = assetPaths[0];
			if (!firstPath.StartsWith("Assets/"))
				return "Assets";
				
			string pathWithoutAssets = firstPath.Substring(7);
			int firstSlash = pathWithoutAssets.IndexOf('/');
			if (firstSlash < 0)
				return $"Assets\\{pathWithoutAssets}";
			
			string topLevelFolder = pathWithoutAssets.Substring(0, firstSlash);
			return $"Assets\\{topLevelFolder}";
		}

		private void WriteMasterBundleFile(string exportPath, string assetBundleName, string assetPrefix)
		{
			CreateFolderStructure(assetBundleName, exportPath);
			
			string masterBundlePath = Path.Combine(exportPath, "MasterBundle.dat");
			StringBuilder content = new StringBuilder();
			
			content.AppendLine($"Asset_Bundle_Name {assetBundleName}");
			content.AppendLine($"Asset_Prefix {assetPrefix}");
			content.AppendLine($"Asset_Bundle_Version {assetBundleVersion}");
			
			File.WriteAllText(masterBundlePath, content.ToString());
			Debug.Log($"Wrote MasterBundle.dat to {masterBundlePath} with Asset_Prefix {assetPrefix}");
			
			ExportConfigFiles(assetBundleName, exportPath);
			
			Debug.Log("Skipping copy of important assets - only exporting .dat files");
		}

		private void ExportConfigFiles(string assetBundleName, string outputPath)
		{
			int exportedCount = 0;
			
			try
			{
				string[] itemAssetPaths = AssetDatabase.FindAssets("t:ItemDefinitionAsset");
				string[] vehicleAssetPaths = AssetDatabase.FindAssets("t:VehicleAsset");
				string[] objectAssetPaths = AssetDatabase.FindAssets("t:ObjectDefinitionAsset");
				
				Debug.Log($"Found {itemAssetPaths.Length} items, {vehicleAssetPaths.Length} vehicles, {objectAssetPaths.Length} objects to process");
				
				string topLevelFolder = DetermineTopLevelFolder(assetBundleName);
				
				foreach (string guid in itemAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
							
							if (asset is ItemDefinitionAsset)
							{
								ItemDefinitionAsset item = asset as ItemDefinitionAsset;
								
								string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
								string targetDir = !string.IsNullOrEmpty(subfolder) ? 
									Path.Combine(outputPath, subfolder) : outputPath;
								
								if (!Directory.Exists(targetDir))
								{
									Directory.CreateDirectory(targetDir);
								}
								
								ExporterUtility.ExportItemToExistingFolder(item, targetDir);
								exportedCount++;
								Debug.Log($"Exported item definition: {item.name} to {targetDir}");
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
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
							
							if (asset is VehicleAsset)
							{
								VehicleAsset vehicle = asset as VehicleAsset;
								
								string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
								string targetDir = !string.IsNullOrEmpty(subfolder) ? 
									Path.Combine(outputPath, subfolder) : outputPath;
								
								if (!Directory.Exists(targetDir))
								{
									Directory.CreateDirectory(targetDir);
								}
								
								ExporterUtility.ExportVehicleToExistingFolder(vehicle, targetDir);
								exportedCount++;
								Debug.Log($"Exported vehicle definition: {vehicle.name} to {targetDir}");
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
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
							
							if (asset is ObjectDefinitionAsset)
							{
								ObjectDefinitionAsset obj = asset as ObjectDefinitionAsset;
								
								string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
								string targetDir = !string.IsNullOrEmpty(subfolder) ? 
									Path.Combine(outputPath, subfolder) : outputPath;
								
								if (!Directory.Exists(targetDir))
								{
									Directory.CreateDirectory(targetDir);
								}
								
								ExporterUtility.ExportObjectToExistingFolder(obj, targetDir);
								exportedCount++;
								Debug.Log($"Exported object definition: {obj.name} to {targetDir}");
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Error processing object asset: {e.Message}");
					}
				}
				
				ExportEditorOnlyAssets(outputPath, topLevelFolder);
				
				Debug.Log($"Exported {exportedCount} config files");
			}
			catch (Exception e)
			{
				Debug.LogError($"Error in ExportConfigFiles: {e.Message}\n{e.StackTrace}");
			}
		}

		private void ExportEditorOnlyAssets(string outputPath, string topLevelFolder)
		{
			string[] stereoSongAssetPaths = AssetDatabase.FindAssets("t:StereoSongAsset");
			Debug.Log($"Found {stereoSongAssetPaths.Length} stereo songs to export");
			
			foreach (string guid in stereoSongAssetPaths)
			{
				try
				{
					string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					
					if (!assetPath.Contains(topLevelFolder))
						continue;
						
					StereoSongAsset asset = AssetDatabase.LoadAssetAtPath<StereoSongAsset>(assetPath);
					
					if (asset != null)
					{
						string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
						string targetDir = !string.IsNullOrEmpty(subfolder) ? 
							Path.Combine(outputPath, subfolder) : outputPath;
						
						if (!Directory.Exists(targetDir))
						{
							Directory.CreateDirectory(targetDir);
						}
						
						ExporterUtility.ExportStereoSongToExistingFolder(asset, targetDir);
						Debug.Log($"Exporting StereoSong: {asset.name} to {targetDir}");
						
						string expectedFileName = asset.name;
						string expectedPath = Path.Combine(targetDir, $"{expectedFileName}.asset");
						
						if (File.Exists(expectedPath))
						{
							Debug.Log($"Successfully exported StereoSong: {expectedPath}");
						}
						else
						{
							Debug.LogError($"Failed to export StereoSong to expected path: {expectedPath}");
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning($"Error processing stereo song asset {guid}: {e.Message}");
				}
			}
			
			string[] materialPaletteAssetPaths = AssetDatabase.FindAssets("t:MaterialPaletteAsset");
			Debug.Log($"Found {materialPaletteAssetPaths.Length} material palettes to export");
			
			foreach (string guid in materialPaletteAssetPaths)
			{
				try
				{
					string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					
					if (!assetPath.Contains(topLevelFolder))
						continue;
						
					MaterialPaletteAsset asset = AssetDatabase.LoadAssetAtPath<MaterialPaletteAsset>(assetPath);
					
					if (asset != null)
					{
						string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
						string targetDir = !string.IsNullOrEmpty(subfolder) ? 
							Path.Combine(outputPath, subfolder) : outputPath;
						
						if (!Directory.Exists(targetDir))
						{
							Directory.CreateDirectory(targetDir);
						}
						
						ExporterUtility.ExportMaterialPaletteToExistingFolder(asset, targetDir);
						Debug.Log($"Exported material palette: {asset.name} to {targetDir}");
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning($"Error processing material palette asset {guid}: {e.Message}");
				}
			}
			
			string[] foliageResourceAssetPaths = AssetDatabase.FindAssets("t:FoliageResourceAsset");
			Debug.Log($"Found {foliageResourceAssetPaths.Length} foliage resources to export");
			
			foreach (string guid in foliageResourceAssetPaths)
			{
				try
				{
					string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					
					if (!assetPath.Contains(topLevelFolder))
						continue;
						
					FoliageResourceAsset asset = AssetDatabase.LoadAssetAtPath<FoliageResourceAsset>(assetPath);
					
					if (asset != null)
					{
						string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
						string targetDir = !string.IsNullOrEmpty(subfolder) ? 
							Path.Combine(outputPath, subfolder) : outputPath;
						
						if (!Directory.Exists(targetDir))
						{
							Directory.CreateDirectory(targetDir);
						}
						
						ExporterUtility.ExportFoliageResourceToExistingFolder(asset, targetDir);
						Debug.Log($"Exported foliage resource: {asset.name} to {targetDir}");
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning($"Error processing foliage resource asset {guid}: {e.Message}");
				}
			}
		}

		private string DetermineTopLevelFolder(string assetBundleName)
		{
			string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
			if (assetPaths.Length == 0)
				return assetBundleName;
			
			foreach (string assetPath in assetPaths)
			{
				string relativePath = assetPath;
				if (relativePath.StartsWith("Assets/"))
				{
					relativePath = relativePath.Substring(7); 
					
			
					int firstSlash = relativePath.IndexOf('/');
					if (firstSlash > 0)
					{
						return relativePath.Substring(0, firstSlash);
					}
					else if (!string.IsNullOrEmpty(relativePath))
					{
						return relativePath;
					}
				}
			}
			
			return assetBundleName;
		}

		private void CopyOtherAssetFiles(string assetBundleName, string outputPath, string topLevelFolder)
		{
			Debug.Log("Skipping copy of other assets - only exporting .dat files");
		}

		private void CopyImportantAssets(string assetBundleName, string outputPath, string topLevelFolder)
		{
			int exportedCount = 0;
			
			try
			{
				string[] itemAssetPaths = AssetDatabase.FindAssets("t:ItemDefinitionAsset");
				string[] vehicleAssetPaths = AssetDatabase.FindAssets("t:VehicleAsset");
				string[] objectAssetPaths = AssetDatabase.FindAssets("t:ObjectDefinitionAsset");
				string[] stereoSongAssetPaths = AssetDatabase.FindAssets("t:StereoSongAsset");
				string[] materialPaletteAssetPaths = AssetDatabase.FindAssets("t:MaterialPaletteAsset");
				string[] foliageResourceAssetPaths = AssetDatabase.FindAssets("t:FoliageResourceAsset");
				
				Debug.Log($"Found {itemAssetPaths.Length} items, {vehicleAssetPaths.Length} vehicles, {objectAssetPaths.Length} objects, " +
					$"{stereoSongAssetPaths.Length} stereo songs, {materialPaletteAssetPaths.Length} material palettes, " +
					$"{foliageResourceAssetPaths.Length} foliage resources to process");
				
				foreach (string guid in itemAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							ItemDefinitionAsset item = AssetDatabase.LoadAssetAtPath<ItemDefinitionAsset>(assetPath);
							if (item == null)
								continue;
							
							string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
							string targetDir = !string.IsNullOrEmpty(subfolder) ? 
								Path.Combine(outputPath, subfolder) : outputPath;
							
							if (!Directory.Exists(targetDir))
							{
								Directory.CreateDirectory(targetDir);
							}
							
							ExporterUtility.ExportItemToExistingFolder(item, targetDir);
							exportedCount++;
							Debug.Log($"Exported item: {item.name} to {targetDir}");
						}
					}
					catch (Exception e)
					{
						Debug.LogError($"Error exporting item asset: {e.Message}");
					}
				}
				
				foreach (string guid in vehicleAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							VehicleAsset vehicle = AssetDatabase.LoadAssetAtPath<VehicleAsset>(assetPath);
							if (vehicle == null)
								continue;
							
							string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
							string targetDir = !string.IsNullOrEmpty(subfolder) ? 
								Path.Combine(outputPath, subfolder) : outputPath;
							
							if (!Directory.Exists(targetDir))
							{
								Directory.CreateDirectory(targetDir);
							}
							
							ExporterUtility.ExportVehicleToExistingFolder(vehicle, targetDir);
							exportedCount++;
							Debug.Log($"Exported vehicle: {vehicle.name} to {targetDir}");
						}
					}
					catch (Exception e)
					{
						Debug.LogError($"Error exporting vehicle asset: {e.Message}");
					}
				}
				
				foreach (string guid in objectAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							ObjectDefinitionAsset obj = AssetDatabase.LoadAssetAtPath<ObjectDefinitionAsset>(assetPath);
							if (obj == null)
								continue;
							
							string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
							string targetDir = !string.IsNullOrEmpty(subfolder) ? 
								Path.Combine(outputPath, subfolder) : outputPath;
							
							if (!Directory.Exists(targetDir))
							{
								Directory.CreateDirectory(targetDir);
							}
							
							ExporterUtility.ExportObjectToExistingFolder(obj, targetDir);
							exportedCount++;
							Debug.Log($"Exported object: {obj.name} to {targetDir}");
						}
					}
					catch (Exception e)
					{
						Debug.LogError($"Error exporting object asset: {e.Message}");
					}
				}
				
				foreach (string guid in stereoSongAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							StereoSongAsset stereoSong = AssetDatabase.LoadAssetAtPath<StereoSongAsset>(assetPath);
							if (stereoSong == null)
								continue;
								
							string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
							string targetDir = !string.IsNullOrEmpty(subfolder) ? 
								Path.Combine(outputPath, subfolder) : outputPath;
								
							if (!Directory.Exists(targetDir))
							{
								Directory.CreateDirectory(targetDir);
							}
							
							Debug.Log($"Exporting StereoSong: {stereoSong.name} to {targetDir}");
							ExporterUtility.ExportStereoSongToExistingFolder(stereoSong, targetDir);
							
							string expectedFileName = stereoSong.name;
							string expectedPath = Path.Combine(targetDir, $"{expectedFileName}.asset");
							
							if (File.Exists(expectedPath))
							{
								Debug.Log($"Successfully exported StereoSong: {expectedPath}");
							}
							else
							{
								Debug.LogError($"Failed to export StereoSong to expected path: {expectedPath}");
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogError($"Error exporting stereo song: {e.Message}\n{e.StackTrace}");
					}
				}
				
				foreach (string guid in materialPaletteAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							MaterialPaletteAsset materialPalette = AssetDatabase.LoadAssetAtPath<MaterialPaletteAsset>(assetPath);
							if (materialPalette == null)
								continue;
								
							string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
							string targetDir = !string.IsNullOrEmpty(subfolder) ? 
								Path.Combine(outputPath, subfolder) : outputPath;
								
							if (!Directory.Exists(targetDir))
							{
								Directory.CreateDirectory(targetDir);
							}
							
							ExporterUtility.ExportMaterialPaletteToExistingFolder(materialPalette, targetDir);
							exportedCount++;
							Debug.Log($"Exported material palette: {materialPalette.name} to {targetDir}");
						}
					}
					catch (Exception e)
					{
						Debug.LogError($"Error exporting material palette: {e.Message}\n{e.StackTrace}");
					}
				}
				
				foreach (string guid in foliageResourceAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							FoliageResourceAsset foliageResource = AssetDatabase.LoadAssetAtPath<FoliageResourceAsset>(assetPath);
							if (foliageResource == null)
								continue;
								
							string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
							string targetDir = !string.IsNullOrEmpty(subfolder) ? 
								Path.Combine(outputPath, subfolder) : outputPath;
								
							if (!Directory.Exists(targetDir))
							{
								Directory.CreateDirectory(targetDir);
							}
							
							ExporterUtility.ExportFoliageResourceToExistingFolder(foliageResource, targetDir);
							exportedCount++;
							Debug.Log($"Exported foliage resource: {foliageResource.name} to {targetDir}");
						}
					}
					catch (Exception e)
					{
						Debug.LogError($"Error exporting foliage resource: {e.Message}\n{e.StackTrace}");
					}
				}
				
				Debug.Log($"Exported {exportedCount} config files");
			}
			catch (Exception e)
			{
				Debug.LogError($"Error in CopyImportantAssets: {e.Message}\n{e.StackTrace}");
			}
		}

		private string GetAssetSubfolderPath(string assetPath, string topLevelFolder)
		{
			string subfolder = "";
			if (assetPath.StartsWith("Assets/"))
			{
				string withoutAssets = assetPath.Substring(7);
				if (withoutAssets.StartsWith(topLevelFolder + "/"))
				{
					int index = withoutAssets.IndexOf('/');
					if (index >= 0 && index + 1 < withoutAssets.Length)
					{
						string afterTopFolder = withoutAssets.Substring(index + 1);
						subfolder = Path.GetDirectoryName(afterTopFolder);
					}
				}
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

		private void BruteForceCopyAllFiles(string assetBundleName, string outputPath)
		{
			string topLevelFolder = DetermineTopLevelFolder(assetBundleName);
			Debug.Log($"Using top-level folder: {topLevelFolder}");
			
			int exportedCount = 0;
			
			try
			{
				string[] itemAssetPaths = AssetDatabase.FindAssets("t:ItemDefinitionAsset");
				string[] vehicleAssetPaths = AssetDatabase.FindAssets("t:VehicleAsset");
				string[] objectAssetPaths = AssetDatabase.FindAssets("t:ObjectDefinitionAsset");
				
				Debug.Log($"Found {itemAssetPaths.Length} items, {vehicleAssetPaths.Length} vehicles, {objectAssetPaths.Length} objects to process");
				
				foreach (string guid in itemAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							ItemDefinitionAsset item = AssetDatabase.LoadAssetAtPath<ItemDefinitionAsset>(assetPath);
							if (item == null)
								continue;
							
							string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
							string targetDir = !string.IsNullOrEmpty(subfolder) ? 
								Path.Combine(outputPath, subfolder) : outputPath;
							
							if (!Directory.Exists(targetDir))
							{
								Directory.CreateDirectory(targetDir);
							}
							
							ExporterUtility.ExportItemToExistingFolder(item, targetDir);
							exportedCount++;
							Debug.Log($"Exported item: {item.name} to {targetDir}");
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Error exporting item: {e.Message}");
					}
				}
				
				foreach (string guid in vehicleAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							VehicleAsset vehicle = AssetDatabase.LoadAssetAtPath<VehicleAsset>(assetPath);
							if (vehicle == null)
								continue;
							
							string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
							string targetDir = !string.IsNullOrEmpty(subfolder) ? 
								Path.Combine(outputPath, subfolder) : outputPath;
							
							if (!Directory.Exists(targetDir))
							{
								Directory.CreateDirectory(targetDir);
							}
							
							ExporterUtility.ExportVehicleToExistingFolder(vehicle, targetDir);
							exportedCount++;
							Debug.Log($"Exported vehicle: {vehicle.name} to {targetDir}");
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Error exporting vehicle: {e.Message}");
					}
				}
				
				foreach (string guid in objectAssetPaths)
				{
					try
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!string.IsNullOrEmpty(assetPath) && assetPath.Contains(topLevelFolder))
						{
							ObjectDefinitionAsset obj = AssetDatabase.LoadAssetAtPath<ObjectDefinitionAsset>(assetPath);
							if (obj == null)
								continue;
							
							string subfolder = GetAssetSubfolderPath(assetPath, topLevelFolder);
							string targetDir = !string.IsNullOrEmpty(subfolder) ? 
								Path.Combine(outputPath, subfolder) : outputPath;
							
							if (!Directory.Exists(targetDir))
							{
								Directory.CreateDirectory(targetDir);
							}
							
							ExporterUtility.ExportObjectToExistingFolder(obj, targetDir);
							exportedCount++;
							Debug.Log($"Exported object: {obj.name} to {targetDir}");
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"Error exporting object: {e.Message}");
					}
				}
				
				Debug.Log($"Exported {exportedCount} .dat files");
			}
			catch (Exception e)
			{
				Debug.LogError($"Error during direct export: {e.Message}\n{e.StackTrace}");
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
