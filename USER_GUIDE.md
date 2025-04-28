**UMod SDK: The Complete Guide**

This guide provides a comprehensive overview of the UMod SDK, a set of Unity Editor tools designed to streamline the creation and exporting of custom assets for Unturned.

**1. Introduction**

*   **Purpose**: Simplifies creating and configuring Unturned assets (Items, Vehicles, Objects, etc.) within the Unity Editor.
*   **Core Features**:
    *   **Custom Inspectors**: Provides user-friendly interfaces for editing Unturned asset properties directly in the Unity Inspector.
    *   **`.dat` File Generation**: Automatically generates the necessary `.dat` configuration files required by Unturned based on the settings in your Unity assets.
    *   **Master Bundle Tool**: Packages your assets (models, textures, materials, configuration) into Unturned-compatible asset bundles (`.masterbundle`).
    *   **Multi-platform Support**: Can build asset bundles for Windows, Mac, and Linux.

**2. Requirements**

*   **Unity Editor**: Version `2021.3.29f1` is specified. Ensure you are using this exact version for compatibility.
*   **Unturned Game**: Required for testing the exported assets.
*   **Unity Package Manager**: The SDK requires the `com.unity.editorcoroutines` package. The Master Bundle Tool will prompt for installation if it's missing.

**3. Installation & Setup**

1.  **Import Unturned Examples**: Start by importing the official Unturned example project into Unity. This is usually found in `Your_Unturned_Install_Directory\Extras\Sources\Project.unitypackage`.
2.  **Remove Conflicting Files**: **Crucially**, delete the following files from the imported Unturned project *before* installing the UMod SDK to prevent conflicts:
    *   `Assets/Editor/Assembly-CSharp-Editor/Tools/MasterBundleTool.cs`
    *   `Assets/Runtime/Assembly-CSharp/Unturned/Bundles/MasterBundleHelper.cs`
3.  **Download UMod SDK**: Get the latest `UMod.SDK.unitypackage` from the project's Releases page (refer to the `README.md` for the link, typically GitHub).
4.  **Import UMod SDK**: In Unity, go to `Assets > Import Package > Custom Package...` and select the downloaded `UMod.SDK.unitypackage`.
5.  **Verification**: After import, you should see a new `UMod SDK` menu item in the Unity top menu bar. Custom inspectors will automatically apply when you select compatible Unturned asset types.

**4. Core Workflow**

The process for creating custom content generally follows these steps:

1.  **Create Definition Asset**:
    *   In the Unity Project window, right-click -> `Create` -> `UMod SDK` -> `[Asset Type]` (e.g., `Item Dats`, `Vehicle Dats`, `Object Dats`).
    *   This creates a Scriptable Object (`.asset` file) that holds the configuration for your custom Unturned asset.
2.  **Configure Properties**:
    *   Select the created Definition Asset.
    *   Use the custom inspector panel (provided by the UMod SDK) to set all necessary properties (ID, Name, Type, Stats, Size, etc.).
    *   Remember to generate a unique `Guid` and assign an appropriate `Item ID` (or Vehicle ID, etc.). Follow Unturned's ID conventions (e.g., >28000 for custom items).
3.  **Assign to Asset Bundle**:
    *   Place your Definition Asset and all its dependencies (3D models, prefabs, materials, textures) within a dedicated folder structure in your Unity project.
    *   Select the **root folder** containing these assets.
    *   In the Inspector, locate the `AssetBundle` dropdown at the bottom.
    *   Assign a new, unique asset bundle name (e.g., `mycoolmod.masterbundle`). This name *must* end with `.masterbundle`.
4.  **Export with Master Bundle Tool**:
    *   Open the tool: `UMod SDK` -> `Master Bundle Tool`.
    *   In the `Asset Bundles` section, find and check the box next to the asset bundle name you created.
    *   In the `Master Bundles` section:
        *   Click the `...` button next to your bundle name to choose an **Export Path**. This is where the final, game-ready files will be saved.
        *   (Optional) Configure `Multi-platform` builds or `Asset Bundle Version` under `Global Settings`.
    *   Click the `Export` button next to your specific bundle name.
    *   The tool builds the `.masterbundle` file and generates all necessary `.dat and .asset` files (`MasterBundle.dat`, localization files like `English.dat`, and the specific asset `.dat` file) into the chosen export path, organized in the correct folder structure for Unturned.

**5. Testing in Unturned**

1.  **Copy Files**: Copy the entire exported folder (containing the `.masterbundle` and `.dat` files) from your **Export Path** into your Unturned `Sandbox` directory (`Steam\steamapps\common\Unturned\Sandbox\`) or into the appropriate folder for workshop content.
2.  **Launch & Test**: Start Unturned and use console commands (e.g., `/give YourItemID`) or the in-game editor to spawn and test your custom asset.

**6. Key SDK Components (Files)**

Located primarily in `Assets/UMod SDK/Editor/`:

*   **`ItemDefinitionAsset.cs`, `VehicleAsset.cs`, `ObjectDefinitionAsset.cs`, etc.**: Define the structure and properties for different Unturned asset types (Scriptable Objects).
*   **`UnturnedAssetEditor.cs`, `ItemGenerator.cs`, `VehicleEditor.cs`, etc.**: Implement the custom inspectors seen in Unity when selecting the Definition Assets.
*   **`MasterBundleTool.cs`**: The core logic for the export tool window, handling asset bundle building and `.dat` file generation.
*   **`ExporterUtility.cs`**: Contains helper functions used by the Master Bundle Tool for exporting and file generation.
*   **`*PropertyDefinitions.cs`**: Likely define the available properties shown in the dropdowns within the custom inspectors (e.g., `ItemPropertyDefinitions.cs`).
*   **`UnturnedTypeDefinitions.cs`, `EObjectType.cs`**: Enums and type definitions used throughout the SDK, likely mapping to Unturned's internal types.
*   **`USER_GUIDE.md`, `README.md`**: Source documentation files.

**7. Advanced Topics & Considerations**

*   **Dependencies**: Ensure all models, textures, materials, and prefabs used by your asset are included in the *same* asset bundle as the Definition Asset.
*   **Localization**: The custom inspectors provide fields for `Name` and `Description` (e.g., `Name English`). The Master Bundle Tool generates the corresponding `English.dat` (or other language files if configured).
*   **GUIDs**: Always generate a unique GUID for each new asset definition to prevent conflicts.
*   **Item/Vehicle IDs**: Choose IDs carefully to avoid conflicts with official content or other mods. Consult Unturned community best practices for ID ranges.
*   **Troubleshooting**:
    *   Ensure the correct Unity version is used.
    *   Double-check that conflicting files were deleted *before* importing the SDK.
    *   Verify all dependencies are included in the asset bundle.
    *   Check the Unity console for error messages during export.

This guide covers the essential aspects of the UMod SDK based on the provided files. For highly specific details about individual properties for items, vehicles, etc., you will likely need to consult the official Unturned modding documentation or experiment within the editor.

**8. Detailed Asset Guides**

This section provides more specific details on creating various asset types using the UMod SDK.

**8.1 Creating Stereo Songs (`StereoSongAsset`)**

Stereo songs define custom music tracks that can be played in-game (e.g., via boomboxes).

*   **Creation**: `Right-click -> Create -> UMod SDK -> Stereo Song`
*   **Key Properties**:
    *   `Guid`: Unique identifier for the song asset. Use `Regenerate` if needed.
    *   `Title`: Display name of the song.
    *   `Title Namespace` / `Title Token`: Alternative localization method (less common).
    *   `Link URL`: Optional URL displayed for the song.
    *   `Is Loop`: Whether the song should loop when played.
    *   **Song Reference**:
        *   `Bundle Name`: The name of the asset bundle containing the actual audio file (e.g., `YourAudio.masterbundle`).
        *   `Asset Path`: The path to the `AudioClip` asset *within* the specified bundle.
*   **Exporting**: Stereo Song assets are typically *not* included directly in Master Bundles (`DONOTMASTERBUNDLE` is assigned automatically). Instead, you use the `Save Stereo Song` button in the inspector to export the `.asset` definition file manually. The actual `AudioClip` must be placed in a separate asset bundle specified in the `Song Reference` fields.

**8.2 Creating Vehicles (`VehicleAsset`)**

Defines custom vehicles.

*   **Creation**: `Right-click -> Create -> UMod SDK -> Vehicle Dats`
*   **Key Properties (Organized by Tabs in Inspector)**:
    *   **Top Section**:
        *   `Guid`: Unique identifier.
        *   `Export Options`: Buttons to export `.dat` and `english.dat` files directly.
    *   **Basic**:
        *   `Engine`: Type (Car, Plane, Boat, etc.).
        *   `Rarity`: Common, Uncommon, etc.
        *   `Title`: In-game name.
        *   `ID`: Numerical ID.
        *   `Vehicle Features`: Toggles for locking, horn, paintability, etc.
    *   **Health**:
        *   `Health`, `Health Min/Max`: Durability values.
        *   `Armor Settings`: Invulnerability toggles (environment, explosions, etc.).
        *   `Damage Multipliers`: Bumper strength, passenger armor, etc.
    *   **Power**:
        *   `Fuel Settings`: Capacity, min/max spawn fuel, burn rate.
        *   `Battery Settings`: If battery-powered, charge/burn rates, default battery type, etc.
    *   **Storage**:
        *   `Trunk Storage X/Y`: Dimensions of the trunk inventory.
    *   **Physics**:
        *   `Movement Settings`: Brake force, lift (for planes/helis), min/max speed, steering angles.
        *   `Air Control`: Steering responsiveness while airborne.
        *   `Physics Properties`: Traction, carjacking force, engine force multiplier.
        *   `Train Settings`: Track/wheel offsets (if `Engine` is Train).
        *   `Center of Mass`: Option to override the default center of mass Y-position.
        *   `Wheel Configurations`: Defines colliders and models for each wheel, including powered/steered status.
    *   **Paint**:
        *   `Paintable Sections`: List defining which mesh renderers/materials can be painted.
        *   `Default Paint Color Mode`: How default colors are chosen (None, List, Random).
        *   `Default Paint Colors`: List of specific default colors (if Mode is List).
        *   `Default Paint Color Configuration`: Settings for random color generation (saturation, value ranges, grayscale chance).
    *   **Explosion**:
        *   `Explosion Guid`: GUID of the effect asset to play on destruction.
        *   `Explosion Force/Damage`: Settings for the explosion physics/damage.
        *   `Explosion Burn`: Materials to apply burn effects to.
*   **Exporting**: Configure properties, assign the `VehicleAsset` and its dependencies (models, prefabs) to a `.masterbundle`, and use the `Master Bundle Tool` for export.

**8.3 Creating Objects (`ObjectDefinitionAsset`)**

Defines world objects, ranging from small props to large structures and decals.

*   **Creation**: `Right-click -> Create -> UMod SDK -> Object Dats`
*   **Key Properties**:
    *   `Guid`: Unique identifier.
    *   `Object Type`: Category (Small, Medium, Large, NPC, Decal). This influences available properties.
    *   `ID`: Numerical ID.
    *   **Localization**:
        *   `Name English`: Display name (if applicable).
        *   `Interact English`: Text shown when interacting (if applicable).
    *   **Object Properties**: A list where you add specific Unturned properties based on the `Object Type` (e.g., `Health`, `Is_Gore`, `Decal_X`, `Interactability`, `Rubble_Health`). Use the `Add Property` dropdown.
    *   **Custom Key-Value Pairs**: For properties not listed in the dropdown, you can add them manually here.
*   **Available Properties**: The `Add Property` dropdown is populated based on the selected `Object Type`. Key categories include:
    *   *Base*: Common properties like `Allow_Structures`, `Material_Palette`, `Fuel`, `Refill`.
    *   *Decal*: `Decal_Alpha`, `Decal_X`, `Decal_Y`.
    *   *Interior Culling*: `Exclude_From_Culling_Volumes`, LOD settings.
    *   *Interactive*: `Interactability` (type like Door, Switch, NPC), `Interactability_Hint`, `Interactability_Health`, reward settings.
    *   *Rubble*: `Rubble` (mode), `Rubble_Health`, `Rubble_Effect`, reward settings.
*   **Exporting**: Configure properties, assign the `ObjectDefinitionAsset` and dependencies to a `.masterbundle`, and use the `Master Bundle Tool`.

**8.4 Creating Items (`ItemDefinitionAsset`)**

Defines all types of items players can interact with or carry.

*   **Creation**: `Right-click -> Create -> UMod SDK -> Item Dats`
*   **Key Properties**:
    *   `Guid`: Unique identifier.
    *   `Item Type`: Primary category (Hat, Shirt, Gun, Melee, Food, Barricade, etc.).
    *   `Rarity`: Common, Uncommon, etc.
    *   `Useable Type`: Secondary category defining behavior (Melee, Gun, Medical, Food, etc.).
    *   `Slot Type`: Where the item is equipped (Primary, Secondary, Hat, etc.).
    *   `Barricade Build Type`: Specific type if `Item Type` is Barricade (Door, Storage, Bed, etc.).
    *   `ID`: Numerical ID.
    *   `Size X/Y/Z`: Inventory dimensions.
    *   **Localization**:
        *   `Name English`: Display name.
        *   `Description English`: Item description.
    *   **Properties**: List for adding specific Unturned properties relevant to the `Item Type` and `Useable Type` (e.g., `Damage`, `Armor`, `Health`, `Recoil_X`, `Storage_X`). Use the `Add Property` dropdown.
    *   **Custom Key-Value Pairs**: For properties not in the dropdown.
    *   **Crafting Recipes**: Define blueprints where this item is an output.
        *   Add/Remove recipes.
        *   Specify Ingredients (Item ID, Amount).
        *   Specify Outputs (typically just this item, ID 0, Amount 1).
        *   Set Skill Level required.
        *   Add Extra Directives (e.g., `Tool`, `Map`).
*   **Available Properties**: The `Add Property` dropdown changes based on `Item Type`. Examples:
    *   *Clothing*: `Armor`, `Movement_Speed_Multiplier`, `Hair_Visible`.
    *   *Gear (Hats, Masks)*: `Hair`, `Beard`, `Hair_Override`.
    *   *Bags*: `Storage_X`, `Storage_Y`.
    *   *Weapons*: `Range`, damage multipliers (`Player_Damage`, `Zombie_Damage`, etc.), `Durability`.
    *   *Consumables*: `Health`, `Food`, `Water`, `Virus`, status modifiers.
    *   *Guns*: `Calibers`, `Recoil_X/Y`, `Spread`, `Firerate`.
    *   *Attachments*: Stat modifiers (`Zoom`, `Shake`, `Muzzle_Flash_Visible`).
    *   *Magazines*: `Pellets`, `Stuck`, `Tracer`.
    *   *Barricades*: `Health`, `Range`, `Offset`, `Explosion`.
*   **Exporting**: Configure properties, assign the `ItemDefinitionAsset` and dependencies to a `.masterbundle`, and use the `Master Bundle Tool`. Blueprints are exported as separate `Blueprints_X.dat` files alongside.

**8.5 Creating Material Palettes (`MaterialPaletteAsset`)**

Defines a collection of materials that can be referenced by objects for randomization or specific looks.

*   **Creation**: `Right-click -> Create -> UMod SDK -> Material Palette`
*   **Key Properties**:
    *   `Guid`: Unique identifier.
    *   `Materials`: A list of material entries.
        *   `Bundle Name`: The asset bundle containing the material (often `core.masterbundle` for vanilla materials, or your custom bundle).
        *   `Material Path`: The path to the material asset *within* the specified bundle.
*   **Usage**: Objects can reference a `MaterialPaletteAsset` using the `Material_Palette` property. The game will then potentially use materials from this palette for the object.
*   **Exporting**: Like Stereo Songs, these are typically *not* included in Master Bundles. Use the `Save Material Palette` button in the inspector to export the `.asset` definition. Ensure the referenced materials exist in their specified bundles.

**8.6 Creating Foliage Resources (`FoliageResourceAsset`)**

Defines how a specific foliage asset (like grass, small rocks, or flowers defined in a `FoliageCollectionAsset` - not directly handled by this SDK) should spawn and behave when placed via the level editor's foliage tool.

*   **Creation**: `Right-click -> Create -> UMod SDK -> Foliage Resource`
*   **Key Properties**:
    *   `Guid`: Unique identifier.
    *   `ID`: Numerical ID.
    *   `Resource Guid`: The GUID of the *actual foliage asset* (often an object) that this resource definition controls the placement of.
    *   `Obstruction Radius`: How much space this foliage takes up, preventing other foliage from spawning too close.
    *   **Position**: Min/Max offset from the surface normal.
    *   **Rotation**: Alignment to surface normal, min/max random rotation offsets, angle constraints.
    *   **Scale**: Min/Max random scale multiplier (X, Y, Z).
    *   **Weight**: Min/Max weighting factor influencing spawn probability relative to other foliage.
    *   `Density`: Overall spawn density multiplier.
*   **Exporting**: Use the `Generate Foliage File` button in the inspector to save the `.dat` file for this definition. The `.asset` file itself isn't typically bundled.

**9. Generator Tools**

The UMod SDK includes several editor windows designed to automate parts of the asset creation process, especially for items, clothing, and objects that follow common patterns.

**9.1 Using the Item Generator (`UMod SDK -> Item Generator`)**

This tool assists in creating basic item prefabs and folder structures, primarily for simple items that might have an equip/use animation or sound.

*   **Purpose**: Quickly generate a basic `_Item.prefab`, `_LOD1.prefab` (if applicable, though LOD settings aren't in this specific tool), material, and folder structure for a simple mesh-based item.
*   **Workflow**:
    1.  Select `Item Type` (Item or Barricade) and `Item Sub Type` (influences default folder structure).
    2.  Assign the main `Mesh` for the item.
    3.  Optionally assign `Equip Animation`, `Use Animation`, and `Use Sound`.
    4.  Choose an `Export Path`.
    5.  Optionally enable `Use Custom Folder Name`.
    6.  Optionally add `Additional Meshes` and `Materials` for more complex items (though this is less common for simple items this tool targets).
    7.  Click `Generate Item`.
*   **Output**: Creates a folder (e.g., `Assets/Items/Supply/MyItem/`) containing:
    *   `MyItem_Item.prefab`: Base prefab with MeshFilter, MeshRenderer, and potentially Collider/Animator.
    *   `Materials/MyItem_Material.mat`: A basic material.
    *   (Optional) Copies of animations/sounds.
*   **Note**: This tool *does not* create the `ItemDefinitionAsset` (`.dat` file configuration). You still need to create that separately and link this generated prefab.

**9.2 Using the Clothing Generator (`UMod SDK -> Clothing Generator`)**

Streamlines the process of creating clothing items based on a base mesh and multiple texture variations.

*   **Purpose**: Generate prefabs, materials, and folder structures for multiple clothing items that share the same mesh but have different textures.
*   **Workflow**:
    1.  Select `Clothing Type` (Shirt, Pants, Hat, etc.).
    2.  Choose `Selection Mode`:
        *   `Multiple`: Processes all compatible textures found in the `Texture Folder Path`.
        *   `Single`: Processes only the texture specified in `Single Texture Path`.
    3.  Assign the base `Mesh` for the clothing item.
    4.  Optionally assign `Equip Animation` and `Use Animation`.
    5.  Set the `Export Path`.
    6.  Optionally enable `Use Custom Folder Name` (creates one subfolder for all generated items).
    7.  Click `Generate Items`.
*   **Output**: For each texture processed, creates a folder (e.g., `Assets/Clothing/Shirts/MyShirt_Blue/`) containing:
    *   `MyShirt_Blue_Item.prefab`: Prefab with MeshFilter, MeshRenderer, configured layer/tag.
    *   `Materials/MyShirt_Blue_Material.mat`: Material using the specific texture.
    *   `Textures/MyShirt_Blue_Texture.png`: A copy of the processed texture.
*   **Texture Import Settings**: Ensure source textures are marked as `Readable` in their import settings.
The tool sets generated textures to `Default` type, `2D`, disables sRGB for masks/non-color maps, sets `Alpha Source` to `Input Texture Alpha`, and enables `Alpha Is Transparency`.
*   **Note**: Like the Item Generator, this *does not* create the `ItemDefinitionAsset` files. You need to create those for each generated clothing variant.

**9.3 Using the Hue Shifter (`UMod SDK -> Hue Shifter`)**

Generates color variations of a source clothing texture.

*   **Purpose**: Create multiple texture variants from a single source texture by shifting hue, saturation, and value.
*   **Workflow**:
    1.  Assign the `Source Texture`.
    2.  Select `Variation Mode`:
        *   `Natural`: Attempts to shift colors based on the texture's dominant color.
        *   `Family`: Uses predefined color ranges (Denim, Earth, Pastel, etc.). Select the desired `Color Family`.
        *   `Complement`: Shifts colors towards the complement of the dominant color.
        *   `Custom`: Define ranges manually using `Base Color`, `Hue Range`, `Saturation Range`, `Value Range`.
    3.  Set `Number of Variations`.
    4.  Adjust `Variation Strength`.
    5.  Configure `Preserve Details` (uses threshold to avoid washing out details) and `Preserve Transparency`.
    6.  Set the `Output Path`.
    7.  Click `Generate Variants`.
*   **Output**: Saves the generated texture variants as PNG files in the specified output path.
*   **Note**: Requires the source texture to be `Readable`.

**9.4 Using the Object & Resource Generator (`UMod SDK -> Object & Resource Generator`)**

Helps create prefabs and basic components for various types of world objects, including standard props, resources, trees, and bushes.

*   **Purpose**: Automate prefab creation for objects, applying correct layers, tags, colliders, and LOD groups based on the selected type.
*   **Workflow (Varies by Type)**:
    *   **Standard Objects (Small, Medium, Large)**:
        1.  Select Type (Small, Medium, Large).
        2.  Enter `Object Name` (used for folder/prefab names).
        3.  Assign `Main Mesh` and `Main Material`.
        4.  Assign `Collider Material` (PhysicMaterial).
        5.  Optionally add `Child Meshes` (for objects with multiple parts needing colliders).
        6.  Optionally configure `LOD` (Level of Detail) settings or use presets.
        7.  Set `Export Path`.
        8.  Click `Generate Prefabs`.
    *   **Resources**:
        1.  Select Type `Resource`.
        2.  Enter `Object Name`.
        3.  Assign `Main Mesh` and `Main Material`.
        4.  Assign `Metal Mesh` (the mesh shown when the resource is depleted).
        5.  Assign `Stump Mesh` (optional, shown after destruction).
        6.  Set `Export Path`.
        7.  Click `Generate Prefabs`.
    *   **Trees**:
        1.  Select Type `Trees`.
        2.  Enter `Object Name`.
        3.  Assign `Trunk Mesh` and `Leaves Mesh`.
        4.  Assign `Trunk Texture` and `Leaves Texture`.
        5.  Assign `Tree Physics Material`.
        6.  Optionally add `Additional Trunk/Leaves Meshes` and `Stump Meshes`.
        7.  Configure `LOD` settings.
        8.  Set `Export Path`.
        9.  Click `Generate Prefabs`.
    *   **Bushes**:
        1.  Select Type `Bushes`.
        2.  Enter `Object Name`.
        3.  Add `Bush Models` (meshes).
        4.  Assign `Bush Texture`.
        5.  Assign `Bush Physics Material`.
        6.  Optionally add a `Bush Forage Mesh` if it's harvestable.
        7.  Set `Export Path`.
        8.  Click `Generate Prefabs`.
*   **Output**: Creates a folder structure (e.g., `Assets/Objects/MyObject/`) containing:
    *   `MyObject_LOD0.prefab`, `MyObject_LOD1.prefab`, etc. (depending on LOD settings).
    *   `MyObject_Nav.prefab` (for collision/navmesh baking).
    *   `MyObject_Skybox.prefab` (optional low-poly version for distant rendering).
    *   `MyObject_Clip.prefab` (basic collider version).
    *   Materials/Textures folders with copies/generated assets.
*   **Components Added**: Automatically adds `MeshCollider`, sets appropriate layers (`Small`, `Medium`, `Large`, `Resource`, `Environment`, `Navmesh`), tags (`Small`, `Medium`, `Large`, `Resource`, `Environment`, `Navmesh`), and potentially `LODGroup`, `Resource` (script), or `FoliageEffect` components.
*   **Note**: This tool focuses on prefab setup. You still need to create the corresponding `ObjectDefinitionAsset` to define the object's properties and link these generated prefabs.
