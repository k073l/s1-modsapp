# Changelog

## 1.2.2
- Removed the width limit on slider input field
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
