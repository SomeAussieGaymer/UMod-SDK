# UMod SDK - User Guide

This guide provides step-by-step instructions on how to use the UMod SDK tools within Unity to create and export custom assets for Unturned.

## Workflow Overview

The general process involves:

1.  **Creating Asset Definitions**: Using Unity's Scriptable Objects to define the properties of your custom asset (e.g., an item, vehicle).
2.  **Configuring Assets**: Using the custom inspectors provided by the SDK to easily set asset properties.
3.  **Preparing Asset Bundles**: Assigning your assets and their dependencies (models, textures, materials) to a Unity Asset Bundle.
4.  **Exporting with Master Bundle Tool**: Using the SDK's Master Bundle Tool to package your asset bundle and generate the necessary `.dat` files for Unturned.

## Example: Creating a Custom Item

Let's walk through creating a simple custom item.

### 1. Create the Item Definition Asset

*   In your Unity Project window, navigate to the folder where you want to store your item definition.
*   Right-click in the folder -> `Create` -> `UMod SDK` -> `Item Dats`.
*   Give the newly created asset a descriptive name (e.g., `MyCustomSword`).

### 2. Configure Item Properties

*   Select the `MyCustomSword` asset you just created.
*   The Inspector window will now show the custom editor provided by the UMod SDK.
*   **General Settings**:
    *   `Guid`: Click the `Generate` button to create a unique ID for your asset.
    *   `Item ID`: Assign a unique numerical ID (The Highest you can do is 65535, and anything less than 2000 is reserved for offical items) (e.g., >28000 is recommended for custom items).
*   **Item Properties**:
    *   `Item Type`: Select the appropriate type (e.g., `Melee`).
    *   `Rarity`: Choose the rarity (e.g., `Epic`).
    *   `Useable`: Select the corresponding useable type (e.g., `Melee`).
    *   `Slot`: Define where the item can be equipped (e.g., `Primary`).
    *   `Size X`, `Size Y`, `Size Z`: Set the inventory size.
    *   **Properties**: Add properties specific to the chosen `Item Type` and `Useable` type. Use the `Add Property` dropdown and `Add` button. For a melee weapon, you might add `Damage`, `Player_Damage`, `Range`, etc., and set their values.
*   **Localization**:
    *   `Name English`: Enter the display name (e.g., "My Custom Sword") This will be the name of the item in-game.
    *   `Description English`: Enter the item description.
*   **(Optional) Export Options**: You can use the buttons here (`Generate Item.dat File`, `Generate English.dat File`) to generate individual `.dat` files for testing or manual setup, but the Master Bundle Tool (covered next) usually handles this.

### 3. Prepare the Asset Bundle

*   Ensure your `ItemDefinitionAsset` (`MyCustomSword`) and any associated assets (like 3D models, materials, textures it might reference - **TODO: Explain how to link models/prefabs if needed**) are located within a single folder structure in your Project.
*   Select the **root folder** containing your item asset and its dependencies.
*   In the Inspector window, at the bottom, find the `AssetBundle` section.
*   Assign a unique asset bundle name (e.g., `mycustomstuff.masterbundle`). You can type a new name and press Enter.

### 4. Export using the Master Bundle Tool

*   Open the tool via the Unity menu: `UMod SDK` -> `Master Bundle Tool`.
*   In the tool window:
    *   **Asset Bundles**: Find the asset bundle name you assigned (e.g., `mycustomstuff.masterbundle`) in the list and check the box next to it. This marks it as a Master Bundle to be exported.
    *   **Master Bundles**: This section now shows options for your selected bundle.
        *   Click the `...` button next to your bundle name to select an **Export Path**. This should be the directory where you want the final Unturned-ready files to be placed (e.g., create a `Bundles` folder somewhere).
        *   **(Optional) Global Settings**: You can enable `Multi-platform` if needed and adjust the `Asset Bundle Version`.
    *   Click the `Export` button next to your bundle name.
*   The tool will build the asset bundle(s) and generate the necessary `.dat` files (including `MasterBundle.dat`, `English.dat`, and `YourItemName.dat`) into the specified export path, organized into the correct folder structure for Unturned.

### 5. Testing in Unturned

*   Copy the entire exported folder (e.g., the `MyCustomStuff` folder created within your chosen `Export Path`) into your Unturned `Sandbox` directory (`Steam\steamapps\common\Unturned\Sandbox\`).
*   Alternatively, copy it into the appropriate location within an Unturned workshop content folder.
*   Launch Unturned and use commands (e.g., `/give YourItemID`) or find the item in the editor to test it.

## Next Steps

This guide covered a basic item. The process is similar for other asset types (Vehicles, Objects, etc.), using their respective `Definition Asset` types found under `Create -> Unturned`.

*   Explore the different Asset Definition types.
*   Refer to Unturned modding documentation for details on specific properties and asset requirements. 
