using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModsApp.Managers;

public class JsonConfigManager
{
    private readonly MelonLogger.Instance _logger;
    private Dictionary<string, string> _modJsonFiles = new Dictionary<string, string>(); // modName -> filename
    private const string CONFIG_DIRECTORY = "ModsApp";
    private const string CONFIG_FILENAME = "JsonFiles.json";

    public JsonConfigManager(MelonLogger.Instance logger)
    {
        _logger = logger;
        LoadSavedJsonFiles();
    }

    public string GetSavedFilename(string modName)
    {
        return _modJsonFiles.ContainsKey(modName) ? _modJsonFiles[modName] : "";
    }

    public void SaveFilename(string modName, string filename)
    {
        _modJsonFiles[modName] = filename;
        SaveJsonFileMapping();
    }

    public string FindFileForMod(string modName, string searchPath)
    {
        if (string.IsNullOrEmpty(modName) || string.IsNullOrEmpty(searchPath) || !Directory.Exists(searchPath))
            return "";

        var files = Directory.GetFiles(searchPath, "*.json", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.IndexOf(modName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                DirectoryContainsModName(file, modName))
            {
                // Always return relative to UserData
                return Path.GetRelativePath(MelonEnvironment.UserDataDirectory, file);
            }
        }

        return "";
    }

    private bool DirectoryContainsModName(string filePath, string modName)
    {
        var currentDir = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrEmpty(currentDir))
        {
            var directoryName = Path.GetFileName(currentDir);
            if (!string.IsNullOrEmpty(directoryName) &&
                directoryName.IndexOf(modName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            currentDir = Path.GetDirectoryName(currentDir);
        }
        return false;
    }

    public JsonLoadResult LoadJsonFile(string filename)
    {
        var result = new JsonLoadResult();

        if (string.IsNullOrEmpty(filename))
        {
            result.ErrorMessage = "Please enter a filename";
            return result;
        }

        var fullPath = Path.Combine(MelonEnvironment.UserDataDirectory, filename);

        try
        {
            if (!File.Exists(fullPath))
            {
                result.ErrorMessage = $"File not found: {fullPath}";
                return result;
            }

            var content = File.ReadAllText(fullPath);

            // Validate JSON
            try
            {
                JToken.Parse(content);
            }
            catch (JsonReaderException ex)
            {
                result.ErrorMessage = $"Invalid JSON in file {filename}: {ex.Message}";
                return result;
            }

            result.Success = true;
            result.Content = FormatJson(content);
            result.OriginalContent = result.Content;

            _logger.Msg($"Successfully loaded JSON file: {filename}");
        }
        catch (System.Exception ex)
        {
            result.ErrorMessage = $"Failed to load JSON file {filename}: {ex.Message}";
        }

        return result;
    }

    public JsonSaveResult SaveJsonFile(string filename, string content)
    {
        var result = new JsonSaveResult();

        if (string.IsNullOrEmpty(filename))
        {
            result.ErrorMessage = "No filename specified";
            return result;
        }

        var fullPath = Path.Combine(MelonEnvironment.UserDataDirectory, filename);

        try
        {
            // Validate JSON before saving
            JToken.Parse(content);

            File.WriteAllText(fullPath, content);

            result.Success = true;
            _logger.Msg($"Successfully saved JSON file: {filename}");
        }
        catch (JsonReaderException ex)
        {
            result.ErrorMessage = $"Cannot save invalid JSON: {ex.Message}";
        }
        catch (System.Exception ex)
        {
            result.ErrorMessage = $"Failed to save JSON file: {ex.Message}";
        }

        return result;
    }

    public string FormatJson(string json)
    {
        try
        {
            var parsed = JToken.Parse(json);
            return parsed.ToString(Formatting.Indented);
        }
        catch
        {
            return json; // Return as-is if parsing fails
        }
    }

    public bool IsValidJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return false;

        try
        {
            JToken.Parse(json);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }

    private void LoadSavedJsonFiles()
    {
        try
        {
            var configPath = Path.Combine(MelonEnvironment.UserDataDirectory, CONFIG_DIRECTORY, CONFIG_FILENAME);
            if (File.Exists(configPath))
            {
                var content = File.ReadAllText(configPath);
                _modJsonFiles = JsonConvert.DeserializeObject<Dictionary<string, string>>(content) ??
                                new Dictionary<string, string>();
            }
        }
        catch (System.Exception ex)
        {
            _logger.Error($"Failed to load saved JSON file mappings: {ex.Message}");
            _modJsonFiles = new Dictionary<string, string>();
        }
    }

    private void SaveJsonFileMapping()
    {
        try
        {
            var configDir = Path.Combine(MelonEnvironment.UserDataDirectory, CONFIG_DIRECTORY);
            Directory.CreateDirectory(configDir);
            var configPath = Path.Combine(configDir, CONFIG_FILENAME);
            var content = JsonConvert.SerializeObject(_modJsonFiles, Formatting.Indented);
            File.WriteAllText(configPath, content);
        }
        catch (System.Exception ex)
        {
            _logger.Error($"Failed to save JSON file mappings: {ex.Message}");
        }
    }
}

public class JsonLoadResult
{
    public bool Success { get; set; } = false;
    public string Content { get; set; } = "";
    public string OriginalContent { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
}

public class JsonSaveResult
{
    public bool Success { get; set; } = false;
    public string ErrorMessage { get; set; } = "";
}