# Mods App

Convenient preferences management for Schedule I mods.

**Note:** This mod **requires** Bars' fork of S1API (>=v1.7.5). You can get it [here](https://github.com/ifBars/S1API/releases/).

## Features
- New "Mods" app in the phone for easy access
- See all loaded mods, including their name, version, author and compatibility status (S1API/Mono/IL2CPP)
- Comprehensive preference management:
    - Support for multiple data types (boolean, integer, enum, vector3, string, float, keycode, color)
    - Organized categories for better preference organization
    - Real-time editing with type-appropriate input controls
    - Apply/Reset functionality for easy preference management
- Smart heuristics to automatically match preferences and configurations to their respective mods
- JSON configuration support for mods without MelonPreferences
- Manual JSON file path specification and live editing
- Clean, intuitive dark theme interface
- Open source (MIT License)

### Important Notes
**Preference Changes:** When you modify preferences through this app, other mods may not immediately reflect the changes. The app will remind you when a restart might be necessary for changes to take effect. This depends on how individual mods handle preference updates.

If you're a mod developer, check out the [preferences guide](https://github.com/k073l/s1-modsapp/blob/master/PREFERENCES.md).

**Enabling/Disabling Mods:** This app does not support enabling or disabling mods. This is by design - MelonLoader doesn't support dynamic mod loading/unloading and the restart would be required anyway. I'd recommend using a dedicated, mod platform aware mod manager, like [r2modman](https://thunderstore.io/c/schedule-i/p/ebkr/r2modman/), [Gale](https://thunderstore.io/c/schedule-i/p/Kesomannen/GaleModManager/) or [Vortex](https://www.nexusmods.com/site/mods/1) just to name a few. They can handle loading/unloading mods, updates and some even have mod profile support.

**Backup Your Configs:** Always back up your configuration files before making changes. While the app is designed to be safe, unexpected issues can arise.

## Screenshots
![Mod with preferences, first category](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/pref-1.png)
![Mod with preferences, second category](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/pref-2.png)
![Mod without preferences, JSON config](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/json-compat.png)
![Mod without preferences](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/no-pref.png)

## Preferences Types Supported
#### MelonPreferences
- Boolean - checkbox
- Integer/Float - number input
- String - text input
- Enum - dropdown
- Color - color wheel picker
- KeyCode - key binding input
- Vector3/other float structs - three number inputs

There's also a fallback for unsupported types (like List, Dictionary, custom classes) that's a text input. It may not work properly for all types.

#### JSON
Fallback to JSON configuration for mods that don't use MelonPreferences.

You can specify the path in the appropriate field. It's relative to `UserData`.
If the file exists and is a valid JSON, you can edit it directly in the app.
Changes are applied to the file on save (if valid JSON).

## Installation
1. Install MelonLoader
2. Install Bars' fork of S1API (>=v1.7.5).
3. Extract the zip file
4. Place the dll file into the `Mods` directory
5. Launch the game

## Credits
- Bars for forking S1API, continuing its development and providing support. Also for helping with making this mod compatible with IL2CPP.
- All the open source projects that made this possible (MelonLoader, S1API and many more).
- Original wrench icon from [FontAwesome](https://fontawesome.com/icons/wrench?s=solid)
