# Changelog

## 1.2.3
- Fixed an issue where long entry labels/hints would shrink input fields too much
- Added a "Reset to Default" button for each entry that resets it to the default value
- Added a "Maximize" button to ModsApp that expands the phone to near fullscreen size for easier navigation
- Add brightness slider (V) to Color Picker
- Enhanced the Search functionality by adding support for searching within preference categories, entries and their descriptions. Results for those appear in a "All Mods" section.
- Added a new Handler for List types - a dedicated List Editor that allows adding/removing/reordering items in the list. Supports both simple types and complex types (some may use fallback handler instead of dedicated one).
## 1.2.2
- Removed the width limit on slider input field
- Improved Color Picker by adding RGB sliders and hex input
- Added a notification badge with number of mods that report errors or dot if there are new/updated mods
- Changed default category collapse - categories now start collapse if they'd take up too much vertical space
- Add README to CHANGELOG viewer - together as docs button (if either is found)
- Added options for input field alignment, entry spacing and copy to custom theme
- Fixed an issue with dropdowns where they didn't close on mod switch/scroll
- Improved the JSON editor by adding syntax highlighting (new editor, experimental)
- Added mod toggle support (enable/disable mods) (requires restart)
- Changed bool entries to use toggles instead of checkboxes
## 1.2.1
- Fixed a bug where the phone wasn't closing with Tab, only Esc after using a slider
- Fixed compatibility bug where LogManager would crash the Melon init on ML 0.7.2
- Improved robustness of matching heuristics by checking the assembly name as well
- Improved section matching in Log Explorer by checking assembly names and allowing partial matches
## 1.2.0
- Improved UI visually by implementing panel corners
- Added theme presets and custom theming options
- Added sliders for numeric inputs validated via IValueRange
- Implemented collapsible categories that are remembered across sessions
- Added a Dependencies line to mod headers that shows which mods are required for it to work
- Fixed scroll sensitivity being too low (thanks HODL!)
- Added an in-app log explorer that shows logs and errors for the current mod
## 1.1.1
- Added more cases to check when determining category ownership
## 1.1.0
- Added a search box for searching through the mod list
- Added unassigned preferences handling for preferences that are loaded by a mod, but weren't matched to it
- Added text scaling options as presets via MelonPreferences
## 1.0.1
- Manifest update (dependency)
## 1.0.0
- initial
