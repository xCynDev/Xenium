using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xenium.Json;

namespace Xenium;

/// <summary>
/// Handles the Xenium generate command, generates the init scripts for a project.
/// </summary>
internal static class XeniumGenerate
{
    /// <summary>
    /// The currently loaded project configuration.
    /// </summary>
    private static XeniumConfiguration? ProjectConfiguration = null;
    
    /// <summary>
    /// The string builder for the server's init file.
    /// </summary>
    private static StringBuilder ServerInit = new StringBuilder();

    /// <summary>
    /// The string builder for the client's init file.
    /// </summary>
    private static StringBuilder ClientInit = new StringBuilder();

    /// <summary>
    /// Set of modules that have been generated.
    /// </summary>
    private static HashSet<string> GeneratedModules = new();

    /// <summary>
    /// Set of file paths that have been generateed.
    /// Used to avoid duplicating generated code for these.
    /// </summary>
    private static HashSet<string> GeneratedPaths = new();

    /// <summary>
    /// The path to the modules folder.
    /// </summary>
    private static string ModuleFolderPath = string.Empty;
    
    /// <summary>
    /// Generates the initialization scripts for the given project.
    /// </summary>
    /// <param name="path"> The path to the root directory of the project. </param>
    public static async Task GenerateScriptsAsync(DirectoryInfo path)
    {
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

        ModuleFolderPath = Path.Combine(path.FullName, scriptFolder, ProjectConfiguration.FolderName);
        if (!Directory.Exists(ModuleFolderPath))
        {
            throw new InvalidOperationException($"Could not find modules folder at '{ModuleFolderPath}'.");
        }
        
        Utils.LogInformation($"Generating scripts for {ProjectConfiguration.ProjectType} '{ProjectConfiguration.ProjectName}'.");
        Utils.LogInformation($"Module folder: {ProjectConfiguration.FolderName}");
        
        if (ProjectConfiguration.LoadOrder.Length == 0)
        {
            Utils.LogInformation("Load Order: None");
        }
        else
        {
            Utils.LogInformation("Load Order:");
            foreach (var moduleName in ProjectConfiguration.LoadOrder)
            {
                Utils.LogInformation($" - {moduleName}");
            }
        }

        Utils.LogInformation();
        
        // Setup the init string builders.
        SetupInitFileBuilders();
        
        // Generate all of the modules in the load order.
        foreach (var moduleName in ProjectConfiguration.LoadOrder)
        {
            if (!GeneratedModules.Contains(moduleName))
            {
                await GenerateModuleAsync(moduleName);
            }        
        }
        
        // Generate the remaining modules that aren't in the load order, and haven't been generated.
        foreach (var modulePath in Directory.EnumerateDirectories(ModuleFolderPath))
        {
            // We have to append a \ or the following will return synergy-modules, always.
            var fullPath = Path.GetFullPath(modulePath + @"/");
            var directoryName = new DirectoryInfo(Path.GetDirectoryName(fullPath)!).Name;
            if (!GeneratedModules.Contains(directoryName))
            {
                await GenerateModuleAsync(directoryName);
            }
        }

        FinalizeFileBuilders();
        
        Utils.LogInformation("Writing generated scripts to files.");
        
        // Determine the location of the init scripts.
        // For addons, it'll be lua/autorun/client/xenium_init_projectname.lua and lua/autorun/server/xenium_init_projectname.lua
        // For gamemodes, it'll be gamemode/cl_init.lua and gamemode/init.lua
        var serverPath = ProjectConfiguration.ProjectType == "addon"
            ? $"lua/autorun/server/xenium_init_{ProjectConfiguration.ProjectName.ToLower()}.lua"
            : $"gamemode/init.lua";
        
        var clientPath = ProjectConfiguration.ProjectType == "addon"
            ? $"lua/autorun/client/xenium_init_{ProjectConfiguration.ProjectName.ToLower()}.lua"
            : $"gamemode/cl_init.lua";
        
        // Write the files.
        var fullServerPath = Path.Combine(path.FullName, serverPath);
        var fullClientPath = Path.Combine(path.FullName, clientPath);
        
        try
        {
            new FileInfo(fullServerPath).Directory?.Create();
            await File.WriteAllTextAsync(fullServerPath, ServerInit.ToString());
        }
        catch (Exception)
        {
            if (Configuration.IsVerbose)
            {
                throw;
            }

            throw new InvalidOperationException($"Failed to write serverside init script at path '{fullServerPath}'");
        }

        try
        {
            new FileInfo(fullClientPath).Directory?.Create();
            await File.WriteAllTextAsync(fullClientPath, ClientInit.ToString());
        }
        catch (Exception)
        {
            if (Configuration.IsVerbose)
            {
                throw;
            }

            throw new InvalidOperationException($"Failed to write clientside init script at path '{fullClientPath}'");
        }
        
        Utils.LogInformation($"Successfully generated init scripts for {ProjectConfiguration.ProjectType} '{ProjectConfiguration.ProjectName}'", ConsoleColor.Green);
        Utils.LogInformation($"Server: '{Path.GetFullPath(fullServerPath)}'");
        Utils.LogInformation($"Client: '{Path.GetFullPath(fullClientPath)}'");
    }

    /// <summary>
    /// Resets the init file builders and adds a header.
    /// </summary>
    private static void SetupInitFileBuilders()
    {
        ServerInit = new StringBuilder();
        ServerInit.AppendLine($"-- Generated by Xenium @ https://github.com/xCynDev/Xenium");
        ServerInit.AppendLine($"-- Serverside init script for {ProjectConfiguration!.ProjectType} '{ProjectConfiguration.ProjectName}'");
        ServerInit.AppendLine($"-- DO NOT MODIFY MANUALLY, AS ANY CHANGES WILL BE OVERWRITTEN BY XENIUM ON NEXT GENERATION");
        ServerInit.AppendLine();
        ServerInit.AppendLine($"MsgC(Color(243, 124, 27), '[Xenium]', Color(255, 255, 255), ' Initializing {ProjectConfiguration.ProjectType} {ProjectConfiguration.ProjectName}\\n')");
        ServerInit.AppendLine();
        
        ClientInit = new StringBuilder();
        ClientInit.AppendLine($"-- Generated by Xenium @ https://github.com/xCynDev/Xenium");
        ClientInit.AppendLine($"-- Clientside init script for {ProjectConfiguration!.ProjectType} '{ProjectConfiguration.ProjectName}'");
        ClientInit.AppendLine($"-- DO NOT MODIFY MANUALLY, AS ANY CHANGES WILL BE OVERWRITTEN BY XENIUM ON NEXT GENERATION");
        ClientInit.AppendLine();
        ClientInit.AppendLine($"MsgC(Color(243, 124, 27), '[Xenium]', Color(255, 255, 255), ' Initializing {ProjectConfiguration.ProjectType} {ProjectConfiguration.ProjectName}\\n')");
        ClientInit.AppendLine();
        
    }

    /// <summary>
    /// Adds any finishing touches to the files.
    /// </summary>
    private static void FinalizeFileBuilders()
    {
        ServerInit.AppendLine();
        ServerInit.AppendLine($"MsgC(Color(243, 124, 27), '[Xenium]', Color(255, 255, 255), ' Done!\\n')");
        ServerInit.AppendLine();
        ServerInit.AppendLine($"-- Generated by Xenium @ https://github.com/xCynDev/Xenium");
        ServerInit.AppendLine($"-- Serverside init script for {ProjectConfiguration!.ProjectType} '{ProjectConfiguration.ProjectName}'");
        ServerInit.AppendLine($"-- DO NOT MODIFY MANUALLY, AS ANY CHANGES WILL BE OVERWRITTEN BY XENIUM ON NEXT GENERATION");
        
        ClientInit.AppendLine();
        ClientInit.AppendLine($"MsgC(Color(243, 124, 27), '[Xenium]', Color(255, 255, 255), ' Done!\\n')");
        ClientInit.AppendLine();
        ClientInit.AppendLine($"-- Generated by Xenium @ https://github.com/xCynDev/Xenium");
        ClientInit.AppendLine($"-- Clientside init script for {ProjectConfiguration!.ProjectType} '{ProjectConfiguration.ProjectName}'");
        ClientInit.AppendLine($"-- DO NOT MODIFY MANUALLY, AS ANY CHANGES WILL BE OVERWRITTEN BY XENIUM ON NEXT GENERATION");
    }

    /// <summary>
    /// Generates the init statements for a module.
    /// </summary>
    /// <param name="moduleName"> The name of the module to generate code for. </param>
    private static async Task GenerateModuleAsync(string moduleName)
    {
        if (GeneratedModules.Contains(moduleName))
        {
            Utils.LogVerbose($"Attempted to generate module '{moduleName}' but it was already generated, skipping.");
            return;
        }

        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new InvalidOperationException($"Failed to generate module '{moduleName}'");
        }

        GeneratedModules.Add(moduleName);
        
        Utils.LogInformation($"Generating module '{moduleName}'.");

        // Get the path to the module.
        var modulePath = Path.Combine(ModuleFolderPath, moduleName);
        if (!Directory.Exists(modulePath))
        {
            Utils.LogInformation($"Could not find module '{moduleName}' at '{modulePath}', skipping.", ConsoleColor.Yellow);
            return;
        }

        // Get the configuration for the module.
        var configPath = Path.Combine(modulePath, "config.json");
        if (!File.Exists(configPath))
        {
            throw new InvalidOperationException($"Module '{moduleName}' is missing a configuration file at '{configPath}'.");
        }
        
        // Read the configuration.
        ModuleConfiguration? moduleConfig = null;
        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            moduleConfig = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ModuleConfiguration);
            if (moduleConfig == null)
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
            
            throw new InvalidOperationException($"Failed to read module configuration file at '{configPath}'. Run with --verbose for more information.");
        }
        
        // Go through the load order and start collecting all of the files we can find. 
        var filePaths = new List<string>();
        foreach (var path in moduleConfig.LoadOrder)
        {
            // If the path contains '..', mark it as invalid.
            // We don't allow relative paths.
            if (path.Contains(".."))
            {
                Utils.LogInformation($"Path '{path}' is relative, and as such illegal. Ignoring.");
                continue;
            }
            
            // If the path is a directory, add the files to the file paths.
            var fullPath = Path.Combine(modulePath, path);
            if (Directory.Exists(fullPath))
            {
                // Get all files that haven't currently been generated.
                var files = Directory.GetFiles(fullPath, "*.lua", SearchOption.AllDirectories);
                // Add them to the file paths.
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(Directory.GetParent(ModuleFolderPath)!.FullName, file);
                    if (!filePaths.Contains(relativePath))
                    {
                        filePaths.Add(relativePath);
                    }
                }
            }
            else if (File.Exists(fullPath))
            {
                if (!fullPath.EndsWith(".lua", StringComparison.CurrentCultureIgnoreCase))
                {
                    Utils.LogInformation($"File '{fullPath}' is in the load order for the module, but it isn't a Lua file. Ignoring.", ConsoleColor.Yellow);
                    continue;
                }
                
                // Add it to the file path if it isn't already in there.
                var relativePath = Path.GetRelativePath(Directory.GetParent(ModuleFolderPath)!.FullName, fullPath);
                if (!filePaths.Contains(relativePath))
                {
                    filePaths.Add(relativePath);
                }
            }
            else
            {
                Utils.LogInformation($"Path '{fullPath}' is in the load order, but doesn't exist. Skipping.");
            }
        }
        
        // Go through all the lua files in the module folder, and add any remaining ones that weren't in the load order.
        var remainingFiles = Directory.GetFiles(modulePath, "*.lua", SearchOption.AllDirectories);
        foreach (var file in remainingFiles)
        {
            var relativePath = Path.GetRelativePath(Directory.GetParent(ModuleFolderPath)!.FullName, file);
            if (!filePaths.Contains(relativePath))
            {
                filePaths.Add(relativePath);
            }
        }
        
        // Go through the file paths, and generate the AddCSLuaFile()/include() statements for each of them.
        foreach (var file in filePaths)
        {
            // Check if the file has any clientside prefix.
            var lowerFile = Path.GetFileName(file).ToLower();
            var fixedPath = file.Replace(@"\", "/")
                .Replace("//", "/");
            if (ProjectConfiguration!.ClientPrefixes.Any(cl => lowerFile.StartsWith(cl)))
            {
                Utils.LogVerbose($"[CL] {fixedPath}");
                // File is clientside, append AddCSLuaFile() to server init.
                ServerInit.AppendLine($"AddCSLuaFile('{fixedPath}')");
                // Include it on the client.
                ClientInit.AppendLine($"include('{fixedPath}')");
            }
            else if (ProjectConfiguration!.SharedPrefixes.Any(sh => lowerFile.StartsWith(sh)))
            {
                Utils.LogVerbose($"[SH] {fixedPath}");
                // File is shared, append AddCSLuaFile() to server init, and include on both realms.
                ServerInit.AppendLine($"AddCSLuaFile('{fixedPath}')");
                ServerInit.AppendLine($"include('{fixedPath}')");
                ClientInit.AppendLine($"include('{fixedPath}')");
            }
            else if (ProjectConfiguration!.ServerPrefixes.Any(sv => lowerFile.StartsWith(sv)))
            {
                Utils.LogVerbose($"[SV] {fixedPath}");
                // File is server only, include on server.
                ServerInit.AppendLine($"include('{fixedPath}')");
            }
            else
            {
                // Matches no known prefix, log warning and continue.
                Utils.LogInformation($"File '{file}' contains no configured clientside, shared or serverside prefix. Ignoring file!", ConsoleColor.Yellow);
                continue;
            }
        }
    }
}