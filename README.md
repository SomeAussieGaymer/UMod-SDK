# Unturned Modding SDK (UMod SDK) 

This repository contains a set of Unity Editor tools designed to streamline the creation and exporting of custom assets for the game Unturned. It provides custom inspectors for Unturned asset types and a tool for bundling and exporting them in the required format.

## Features

*   Custom editors for various Unturned asset types (Items, Vehicles, Objects, etc.) simplifying configuration.
*   Automatic generation of `.dat and .asset` files based on asset properties.
*   Master Bundle Tool for packaging assets into Unturned-compatible asset bundles.
*   Support for multi-platform builds (Windows, Mac, Linux).

## Requirements

*   Unity Editor version `2021.3.29f1`
*   Unturned Game (For testing the exported assets)
*   Unity Package: `com.unity.editorcoroutines` (The Master Bundle Tool will prompt to install if missing)

## Getting Started

1.  Import the official Unturned example project into Unity from `Your_Unturned_Install_Directory\Extras\Sources\Project.unitypackage`.
2.  **Delete** the following files from the imported project to avoid conflicts:
    *   `Assets\Editor\Assembly-CSharp-Editor\Tools\MasterBundleTool.cs`
    *   `Assets\Runtime\Assembly-CSharp\Unturned\Bundles\MasterBundleHelper.cs`
3.  Download the latest `UMod.SDK.unitypackage` from the [Releases page](https://github.com/SomeAussieGaymer/UMod-SDK/releases).
4.  Import the downloaded `UMod.SDK.unitypackage` into your Unity project (`Assets > Import Package > Custom Package...`).
5.  The tools should now appear under the `UMod SDK` menu in Unity, and custom inspectors will be active for relevant asset types.

## User Guide

For detailed instructions on creating and exporting assets, please see the [User Guide](USER_GUIDE.md).

## Contributing

At the moment, there are one or two updates I would still like to complete for this project, and then I will be out of ideas. Bug reports and suggestions are very welcome! Please open an issue on the GitHub repository or contact `@SomeAussieGamer` on Discord.

## License

This project uses a modified MIT license. The full license text can be found [here](https://github.com/SomeAussieGaymer/UMod-SDK/blob/main/LICENSE). 
