# Mods App
[![MLVScan](https://mlvscan-api.ifbars.workers.dev/public/attestations/att_YC2y40nx5ANzToFL43isMgWp/badge.svg?style=split-pill)](https://mlvscan.com/attestations/att_YC2y40nx5ANzToFL43isMgWp)

Convenient preferences management for Schedule I mods.

**Note:** This mod **requires** Bars' fork of S1API (>=v3.0.1). You can get it on [GitHub](https://github.com/ifBars/S1API/releases/), [Thunderstore](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/) or [NexusMods](https://www.nexusmods.com/schedule1/mods/1194).

## Features
- New "Mods" app in the phone for easy access
- See all loaded mods, including their name, version, author, compatibility status (S1API/Mono/IL2CPP) and if they're enabled or not
- Powerful Search capabilities
  - Search mods by name or author
  - Search through all preferences using their names or descriptions (appearing in a separate "All Mods" section)
  - Fuzzy search for better results even with typos or partial matches
- Comprehensive preference management:
    - Support for multiple data types (boolean, integer, enum, vector3, string, float, keycode, color, lists, dictionaries)
    - Organized categories for better preference organization
    - Real-time editing with type-appropriate input controls
    - Apply/Reset functionality for easy preference management
    - Reset to Default option for individual entries to quickly revert changes
- Smart heuristics to automatically match preferences and configurations to their respective mods
- JSON configuration support for mods without MelonPreferences
- Manual JSON file path specification and live editing
- Clean, intuitive interface
  - Text size scaling options for accessibility
  - Collapsible categories for better organization
  - Theme presets and custom theming options for personalization
- Mod changelog/readme viewer for easy access to information
- Dependency tracking to see which mods require others to function properly
- In-app log explorer to view mod logs and errors without leaving the app
  - Issues are visible at-a-glance with a notification badge on the app icon
- Open source (MIT License)

### Important Notes
**Preference Changes:** When you modify preferences through this app, other mods may not immediately reflect the changes. The app will remind you when a restart might be necessary for changes to take effect. This depends on how individual mods handle preference updates.

If you're a mod developer, check out the [preferences guide](https://github.com/k073l/s1-modsapp/blob/master/PREFERENCES.md).

**Enabling/Disabling Mods:** This app supports enabling or disabling mods from version 1.2.2. The restart is required for the change to apply. Still, I'd recommend using a dedicated, mod platform aware mod manager, like [Schedule I Mod Manager (SIMM)](https://www.nexusmods.com/schedule1/mods/1750), [r2modman](https://thunderstore.io/c/schedule-i/p/ebkr/r2modman/), [Gale](https://thunderstore.io/c/schedule-i/p/Kesomannen/GaleModManager/) or [Vortex](https://www.nexusmods.com/site/mods/1) just to name a few. They can handle loading/unloading mods, updates and some even have mod profile support.

**Backup Your Configs:** Always back up your configuration files before making changes. While the app is designed to be safe, unexpected issues can arise.

## Screenshots

![Mod with preferences, various input tupes](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/screenshots/melonpref-1.png)

![Mod with preferences, various input types](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/screenshots/melonpref-2.png)

![Unassigned preferences, numeric input type fallback](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/screenshots/unassigned.png)

![Mod with JSON preferences, alerts for missing dependecies and errors in log](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/screenshots/json-deps.png)

![Pop-up panel for mod's changelog and readme](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/screenshots/docs-panel.png)

![In-app log explorer showing mod logs and errors](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/screenshots/logs-panel.png)

![Phone view with notifications badge](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/screenshots/phone.png)

Select different themes:

![Different theming options - Forest theme](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/screenshots/theme-2.png)

![Different themind options - Twilight theme](https://raw.githubusercontent.com/k073l/s1-modsapp/refs/heads/master/assets/screenshots/theme-1.png)

## Preferences Types Supported
#### MelonPreferences
- Boolean - toggle switch
- Integer/Float - number input, with optional slider if the entry uses a [Validator with a specified range](https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader/Preferences/ValueValidator.cs)
- String - text input
- Enum - dropdown
- Color - color wheel picker
- KeyCode - key binding input
- Vector3/other float structs - three number inputs
- Lists - list editor that allows adding/removing/reordering/editing items in the list. Supports both simple types and complex types (some may use fallback handler instead of dedicated one).
- Dictionaries - dictionary editor similar to the list editor that allows adding/removing/editing key-value pairs.

There's also a fallback for unsupported types (like custom classes or structs) that is a text input. It may not work properly for all types.

#### JSON
Fallback to JSON configuration for mods that don't use MelonPreferences.

You can specify the path in the appropriate field. It's relative to `UserData`.
If the file exists and is a valid JSON, you can edit it directly in the app.
Changes are applied to the file on save (if valid JSON).

## Installation
1. Install MelonLoader
2. Install Bars' fork of S1API (>=v3.0.1).
3. Extract the zip file
4. Place the dll file into the `Mods` directory
5. Launch the game

## Credits
- Bars for forking S1API, continuing its development and providing support. Also for helping with making this mod compatible with IL2CPP.
- All the open source projects that made this possible (MelonLoader, S1API and many more).
- Original wrench icon from [FontAwesome](https://fontawesome.com/icons/wrench?s=solid)
- Original [warning](https://lucide.dev/icons/triangle-alert), [scroll](https://lucide.dev/icons/scroll-text), [maximize](https://lucide.dev/icons/maximize-2) and [undo](https://lucide.dev/icons/undo-2) icon from [Lucide](https://lucide.dev/license), ISC License
