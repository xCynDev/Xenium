using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Tasks;
using Xenium.Json;

namespace Xenium;

/// <summary>
/// Handles the Xenium create-module command, creates a module with the given name in the modules folder of the desired project.
/// </summary>
internal static class XeniumCreateModule
{
    /// <summary>
    /// The currently loaded project configuration.
    /// </summary>
    private static XeniumConfiguration? ProjectConfiguration = null;

    public static async Task CreateModuleAsync(DirectoryInfo path, string moduleName)
    {
        Utils.LogInformation($"Reading configuration for project at '{path.FullName}'.");
        
        // Grab the configuration file for the project.
        var configFilePath = Path.Combine(path.FullName, "xenium-config.json");
        if (!File.Exists(configFilePath))
        {
            throw new InvalidOperationException($"Could not find configuration file at '{configFilePath}'. Run Xenium setup first!");
        }
        
        try
        {
            var json = await File.ReadAllTextAsync(configFilePath);
            ProjectConfiguration = JsonSerializer.Deserialize<XeniumConfiguration>(json, SourceGenerationContext.Default.XeniumConfiguration);
            if (ProjectConfiguration == null)
            {
                throw new InvalidOperationException();
            }
        }
        catch (Exception)
        {
            if (Configuration.IsVerbose)
            {
                throw;
            }
            
            throw new InvalidOperationException($"Failed to read configuration file at '{configFilePath}'. Run with --verbose for more information.");
        }

        // Check that the configuration is valid before proceeding.
        Utils.AssertConfigurationIsValid(ProjectConfiguration);
        
        // Check that the folder structure is valid before proceeding.
        var scriptFolder = ProjectConfiguration.ProjectType == "addon"
            ? "lua"
            : "gamemode";

        var parentFolderPath = Path.Combine(path.FullName, scriptFolder, ProjectConfiguration.FolderName);
        if (!Directory.Exists(parentFolderPath))
        {
            throw new InvalidOperationException($"Could not find modules folder at '{parentFolderPath}'.");
        }
        
        // Check that there is no existing folder with that name.
        var moduleFolderPath = Path.Combine(parentFolderPath, moduleName.ToLower());
        if (Directory.Exists(moduleFolderPath))
        {
            throw new InvalidOperationException($"Could not create module '{moduleName}' as a folder already exists at '{moduleFolderPath}");
        }
        
        // Create the folder for the module.
        Utils.LogInformation($"Creating module '{moduleName}'");
        Directory.CreateDirectory(moduleFolderPath);
        
        // Create the configuration for the module.
        var moduleConfig = new ModuleConfiguration
        {
            Name = moduleName,
            Description = "No description specified."
        };
        
        Utils.LogInformation($"Writing configuration for module.");
        var configPath = Path.Combine(moduleFolderPath, "config.json");

        try
        {
            var json = JsonSerializer.Serialize<ModuleConfiguration>(moduleConfig, SourceGenerationContext.Default.ModuleConfiguration);
            await File.WriteAllTextAsync(configPath, json);
        }
        catch (Exception)
        {
            if (Configuration.IsVerbose)
            {
                throw;
            }

            throw new InvalidOperationException($"Failed to write module configuration file at '{configPath}'. Run with --verbose for more information.");
        }
        
        Utils.LogInformation($"Successfully created module '{moduleName}' for {ProjectConfiguration.ProjectType} '{ProjectConfiguration.ProjectName}'.", ConsoleColor.Green);
    }
}