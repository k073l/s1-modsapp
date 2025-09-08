# Preferences – Mod Dev Guide

The most popular ways to allow users to configure your mod are **MelonPreferences** and **JSON configuration files**.

This guide will help you set them up and give you some tips on how to make your mod compatible with **ModsApp**.

Additional resources:
* [MelonPreferences Documentation](https://melonwiki.xyz/#/modders/preferences)
* [S1 Modding Wiki](https://s1modding.github.io/docs/moddevs/melonloader_utilities/)
* [Discord: Unofficial Schedule One Modding Server](https://discord.gg/9Z5RKEYSzq)

---

## Table of Contents

1. [MelonPreferences](#melonpreferences)  
   1.1 [Setting Up MelonPreferences](#setting-up-melonpreferences)  
   1.2 [MelonPreferences_Category](#melonpreferences_category)  
   1.3 [MelonPreferences_Entry<T>](#melonpreferences_entryt)  
   1.4 [Supported Types](#supported-types)  
   1.5 [Supporting Hot-Reloading of Preferences](#supporting-hot-reloading-of-preferences)  

2. [JSON Configuration](#json-configuration)  
   2.1 [Basic Setup](#basic-setup)  
   2.2 [Supporting Hot-Reloading of JSON Config](#supporting-hot-reloading-of-json-config)  

---

## MelonPreferences

**MelonPreferences** is the recommended way to manage mod preferences. It's easy to use and well-integrated with **MelonLoader**.

---

### Setting Up MelonPreferences

MelonPreferences consists of `Categories` and `Entries`.

- **Categories** group related preferences.
- **Entries** are the actual preferences users can modify. Each has a name, type, and default value.

Categories are saved (by default) in a file called `MelonPreferences.cfg` located in the `UserData` folder. You can override this per category.

#### Basic Example:

```csharp
using MelonLoader;

public class MyMod : MelonMod
{
    private MelonPreferences_Category myCategory;
    private MelonPreferences_Entry<bool> myBoolEntry;
    private MelonPreferences_Entry<int> myIntEntry;
    private MelonPreferences_Entry<string> myStringEntry;

    public override void OnApplicationStart()
    {
        // Create a category
        myCategory = MelonPreferences.CreateCategory("MyModCategory", "My Mod Preferences");

        // Create entries
        myBoolEntry = myCategory.CreateEntry("MyBool", true);
        myIntEntry = myCategory.CreateEntry("MyInt", 10);
        myStringEntry = myCategory.CreateEntry("MyString", "default");
    }
}
````

---

### MelonPreferences\_Category

```csharp
MelonPreferences.CreateCategory(string identifier, string display_name = null)
```

* `identifier`: Unique internal name for the category.
* `display_name`: Optional user-facing name.

**Tip:** Use your mod's name in the identifier and display name for clarity:

```csharp
myCategory = MelonPreferences.CreateCategory("MyMod_Preferences", "My Mod's Preferences");
```

You can also use `SetFilePath` to change the config file:

```csharp
myCategory.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "MyModConfig.cfg"));
```

---

### MelonPreferences\_Entry<T>

```csharp
MelonPreferences_Category.CreateEntry<T>(
    string identifier,
    T default_value,
    string display_name = null,
    string description = null,
    bool is_hidden = false,
    bool dont_save_default = false,
    ValueValidator validator = null,
    string oldIdentifier = null)
```

* `identifier`: Internal name.
* `default_value`: Default value.
* `display_name`: Shown in UI (optional).
* `description`: Tooltip/help text (optional).
* `validator`: (Optional) Validates values.

**Tip:** Always add a display name and description for user clarity:

```csharp
myBoolEntry = myCategory.CreateEntry("EnableFeatureX", true, "Enable Feature X", "If enabled, Feature X will be activated.");
```

#### Example of Validator:

```csharp
entry = myCategory.CreateEntry("floatValue", 0f, validator: new ValueRange<float>(0f, 1f));
```

---

### Supported Types

MelonPreferences supports any type serializable by **Tomlet**, including:

* `bool`
* `int`
* `float`
* `string`
* `enum`
* `Color`
* `KeyCode`
* Complex types (e.g., `List<List<string>>`)

> ModsApp supports all these types, but complex types may not have ideal UI controls. Consider using simpler types for better user experience (also outside of ModsApp).

---

### Supporting Hot-Reloading of Preferences

MelonPreferences automatically reloads when preferences are saved using `MelonPreferences.Save()` — including saves from ModsApp.

However, **your mod is responsible for reacting to changes**.

If you check those values constantly (e.g., in `OnUpdate()`, `OnGUI()`), you don't need to do anything special.

If you only read values once (e.g., in `OnInitializeMelon()`), you may miss changes. You can handle updates using:

#### `OnEntryValueChanged`

```csharp
colorEntry.OnEntryValueChanged.Subscribe((oldValue, newValue) =>
    Logger.Msg($"Color changed from {oldValue} to {newValue}")
);
```

---

#### Handling All Entry Changes

You can track multiple entries using a helper:

```csharp
public class PrefTracker
{
    private readonly List<MelonPreferences_Entry> _entries = new();

    public event Action<MelonPreferences_Entry, object?, object?>? OnAnyChanged;

    public PrefTracker(params MelonPreferences_Entry[] entries)
    {
        foreach (var entry in entries)
        {
            _entries.Add(entry);
            entry.OnEntryValueChangedUntyped.Subscribe((oldVal, newVal) => OnAnyChanged?.Invoke(entry, oldVal, newVal));
        }
    }
}
```

**Usage:**

```csharp
var tracker = new PrefTracker(myBoolEntry, myIntEntry, myStringEntry);
tracker.OnAnyChanged += (entry, oldVal, newVal) =>
    Logger.Msg($"[{entry.Category.DisplayName}] {entry.DisplayName} changed from {oldVal} to {newVal}");
```

> Not every mod needs hot-reload. If restarting the game is required (e.g., for patched `Awake` methods), that's acceptable. If you do support hot-reload, mention it in your mod's description — it's a nice quality-of-life feature, but not mandatory.

---

## JSON Configuration

Some mods avoid MelonPreferences due to custom requirements. In that case, JSON files are a simple fallback supported by ModsApp.

---

### Basic Setup

Example using `System.Text.Json`:

```csharp
using System.IO;
using System.Text.Json;
using MelonLoader;

public class MyMod : MelonMod
{
    private class Config
    {
        public bool EnableFeatureX { get; set; } = true;
        public int SomeValue { get; set; } = 10;
        public string SomeString { get; set; } = "default";
    }

    private Config _config;
    private string _configPath;

    public override void OnInitializeMelon()
    {
        _configPath = Path.Combine(MelonEnvironment.UserDataDirectory, "MyModConfig.json");
        LoadConfig();
    }

    private void LoadConfig()
    {
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            _config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
        }
        else
        {
            _config = new Config();
            SaveConfig();
        }
    }

    private void SaveConfig()
    {
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
```

> Use `MelonEnvironment.UserDataDirectory` and include your mod name in the filename or path for clarity.

ModsApp allows users to point to a JSON file relative to `UserData`. If it's valid JSON, it can be edited via the app.

---

### Supporting Hot-Reloading of JSON Config

ModsApp will save the file when users click **Apply**, but your mod must respond to changes.

Use `FileSystemWatcher`:

```csharp
private FileSystemWatcher _watcher;

private void SetupWatcher()
{
    _watcher = new FileSystemWatcher
    {
        Path = Path.GetDirectoryName(_configPath) ?? "",
        Filter = Path.GetFileName(_configPath),
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
    };

    _watcher.Changed += OnConfigFileChanged;
    _watcher.EnableRaisingEvents = true;
}

private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
{
    LoadConfig();
    Logger.Msg("Config reloaded due to external change.");
}
```

