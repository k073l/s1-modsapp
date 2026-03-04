# Mods App

Convenient preferences management for Schedule I mods.

**Note:** This mod **requires** Bars' fork of S1API (>=v2.8.8). You can get it [here](https://github.com/ifBars/S1API/releases/).

## Features
- New "Mods" app in the phone for easy access
- See all loaded mods, including their name, version, author and compatibility status (S1API/Mono/IL2CPP)
- Search through the mod list
- Comprehensive preference management:
    - Support for multiple data types (boolean, integer, enum, vector3, string, float, keycode, color)
    - Organized categories for better preference organization
    - Real-time editing with type-appropriate input controls
    - Apply/Reset functionality for easy preference management
- Smart heuristics to automatically match preferences and configurations to their respective mods
- JSON configuration support for mods without MelonPreferences
- Manual JSON file path specification and live editing
- Clean, intuitive interface
  - Text size scaling options for accessibility
  - Collapsible categories for better organization
  - Theme presets and custom theming options for personalization
- Mod changelog viewer for easy access to update information
- Dependency tracking to see which mods require others to function properly
- In-app log explorer to view mod logs and errors without leaving the app
- Open source (MIT License)

### Important Notes
**Preference Changes:** When you modify preferences through this app, other mods may not immediately reflect the changes. The app will remind you when a restart might be necessary for changes to take effect. This depends on how individual mods handle preference updates.

If you're a mod developer, check out the [preferences guide](https://github.com/k073l/s1-modsapp/blob/master/PREFERENCES.md).

**Enabling/Disabling Mods:** This app does not support enabling or disabling mods. This is by design - MelonLoader doesn't support dynamic mod loading/unloading and the restart would be required anyway. I'd recommend using a dedicated, mod platform aware mod manager, like [r2modman](https://thunderstore.io/c/schedule-i/p/ebkr/r2modman/), [Gale](https://thunderstore.io/c/schedule-i/p/Kesomannen/GaleModManager/) or [Vortex](https://www.nexusmods.com/site/mods/1) just to name a few. They can handle loading/unloading mods, updates and some even have mod profile support.

**Backup Your Configs:** Always back up your configuration files before making changes. While the app is designed to be safe, unexpected issues can arise.

## Screenshots

![ModsApp preferences, collapsible categories and customization](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/categories_customization.png)

![Mod with preferences, various types of input types](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/controls.png)

![Unassigned preferences, numeric input type fallback](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/unassigned.png)

![Mod with JSON preferences, alerts for missing dependecies and errors in log](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/json-alerts.png)

![Pop-up panel for mod's changelog](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/changelog.png)

![In-app log explorer showing mod logs and errors](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/logexplorer.png)

![Different theming options - Forest theme](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/forest_theme.png)

![Different themind options - Twilight theme](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/twilight_theme.png)

## Preferences Types Supported
#### MelonPreferences
- Boolean - checkbox
- Integer/Float - number input, with optional slider if the entry uses a [Validator with a specified range](https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader/Preferences/ValueValidator.cs)
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
2. Install Bars' fork of S1API (>=v2.8.8).
3. Extract the zip file
4. Place the dll file into the `Mods` directory
5. Launch the game

## Credits
- Bars for forking S1API, continuing its development and providing support. Also for helping with making this mod compatible with IL2CPP.
- All the open source projects that made this possible (MelonLoader, S1API and many more).
- Original wrench icon from [FontAwesome](https://fontawesome.com/icons/wrench?s=solid)
- Original [warning](https://lucide.dev/icons/triangle-alert) and [scroll](https://lucide.dev/icons/scroll-text) icon from [Lucide](https://lucide.dev/license), ISC License
