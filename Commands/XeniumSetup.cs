using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xenium.Json;

namespace Xenium;

/// <summary>
/// Handles the Xenium setup command, sets up an addon or gamemode with a Xenium configuration and modules folder.
/// </summary>
internal static class XeniumSetup
{
    /// <summary>
    /// Sets up a project in the given directory using the provided name.
    /// </summary>
    /// <param name="name"> The name of the project to setup. </param>
    /// <param name="path"> The path to the root directory of the project. </param>
    /// <param name="type"> The type of project (addon or gamemode). </param>
    public static async Task SetupProjectAsync(string name, DirectoryInfo path, string type)
    {
        Console.WriteLine($"Generating configuration and folder structure for {type} '{name}'");
        // Check that there is no configuration at the path already.
        var configFilePath = Path.Combine(path.FullName, "xenium-config.json");
        if (File.Exists(configFilePath))
        {
            throw new InvalidOperationException($"Cannot setup project: xenium-config.json file already exists at '{configFilePath}'.");
        }
        
        // Check that the folder structure of the project matches the given type.
        // For instance, addons are expected to have a 'lua' folder in the root directory.
        // For gamemodes, we expect a 'gamemode' folder to put the modules folder in.
        var expectedFolder = type == "addon"
            ? "lua"
            : "gamemode";

        var scriptFolderPath = Path.Combine(path.FullName, expectedFolder);

        if (!Directory.Exists(scriptFolderPath))
        {
            throw new InvalidOperationException($"Cannot setup project: missing expected '{expectedFolder}' folder for project of type '{type}' in project directory.");
        }
                    
        // Generate a new configuration file and write it.
        var config = new XeniumConfiguration
        {
            ProjectName = name,
            ProjectType = type,
            FolderName = $"{name.ToLower()}-modules",
        };

        try
        {
            var json = JsonSerializer.Serialize(config, SourceGenerationContext.Default.XeniumConfiguration);
            await File.WriteAllTextAsync(configFilePath, json);
        }
        catch (Exception)
        {
            if (Configuration.IsVerbose)
            {
                throw;
            }

            throw new InvalidOperationException($"Failed to generate configuration for project '{name}'. Run with --verbose for more information.");
        }
        
        Console.WriteLine($"Created configuration file at '{configFilePath}'.");
        
        // Create the modules folder for the project, if it doesn't already exist.
        var moduleFolderPath = Path.Combine(scriptFolderPath, config.FolderName);
        if (Directory.Exists(moduleFolderPath))
        {
            Utils.LogInformation($"Module folder already exists at '{moduleFolderPath}', skipping.", ConsoleColor.Yellow);
        }
        else
        {
            try
            {
                Directory.CreateDirectory(moduleFolderPath);
            }
            catch (Exception)
            {
                if (Configuration.IsVerbose)
                {
                    throw;
                }
                
                throw new InvalidOperationException($"Failed to create modules folder at '{moduleFolderPath}'. Run with --verbose for more information.");
            }
        }

        Utils.LogInformation($"Successfully setup project '{name}'.", ConsoleColor.Green);
    }
}