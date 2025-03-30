using System;
using System.Text.RegularExpressions;

namespace Xenium;

/// <summary>
/// Contains utility functions.
/// </summary>
public static class Utils
{
    // Regular expression for paths.
    public static readonly Regex AlphanumericRegex = new Regex("^[a-zA-Z0-9_-]*$");
    
    /// <summary>
    /// Throws if the given configuration is invalid.
    /// </summary>
    /// <param name="config"> The project configuration to validate. </param>
    public static void AssertConfigurationIsValid(XeniumConfiguration? config)
    {
        if (config == null)
        {
            throw new InvalidOperationException("Project configuration is null.");
        }

        if (string.IsNullOrWhiteSpace(config.ProjectName) || !AlphanumericRegex.IsMatch(config.ProjectName))
        {
            throw new InvalidOperationException($"Project name '{config.ProjectName}' is invalid, must be an alphanumeric name (A-Z, 0-9, - or _).");
        }
        
        if (string.IsNullOrWhiteSpace(config.FolderName) || !AlphanumericRegex.IsMatch(config.FolderName))
        {
            throw new InvalidOperationException($"Project module folder name '{config.FolderName}' is invalid, must be an alphanumeric name (A-Z, 0-9, - or _).");
        }

        if (config.ProjectType != "addon" && config.ProjectType != "gamemode")
        {
            throw new InvalidOperationException($"Project type '{config.ProjectType}' is invalid, must be 'addon' or 'gamemode'.");
        }

        foreach (var moduleName in config.LoadOrder)
        {
            if (string.IsNullOrWhiteSpace(moduleName) || !AlphanumericRegex.IsMatch(moduleName))
            {
                throw new InvalidOperationException($"Module '{moduleName}' in load order is invalid, must be an alphanumeric name (A-Z, 0-9, - or _).");
            }
        }
        
        // All good, throw nothing.
    }

    /// <summary>
    /// Log information to the console using the given color.
    /// </summary>
    /// <param name="line"> The information to log. </param>
    /// <param name="color"> The color to use, defaults to white. </param>
    public static void LogInformation(string line = "", ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(line);
        Console.ResetColor();
    }

    /// <summary>
    /// Log information to the console using the given color, if --verbose is set.
    /// </summary>
    /// <param name="line"> The information to log. </param>
    /// <param name="color"> The color to use, defaults to white. </param>
    public static void LogVerbose(string line, ConsoleColor color = ConsoleColor.White)
    {
        if (!Configuration.IsVerbose)
        {
            return;
        }
        
        Console.ForegroundColor = color;
        Console.WriteLine(line);
        Console.ResetColor();
    }
}