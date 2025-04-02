using System.Text.Json.Serialization;

namespace Xenium;

/// <summary>
/// The configuration for Xenium to use while generating init files for the project.
/// </summary>
public class XeniumConfiguration
{
    /// <summary>
    /// The name of the project. 
    /// </summary>
    [JsonPropertyName("projectName")]
    public required string ProjectName { get; set; }
    
    /// <summary>
    /// The type of project.
    /// Can either be "addon" or "gamemode".
    /// </summary>
    [JsonPropertyName("projectType")]
    public required string ProjectType { get; set; }
    
    /// <summary>
    /// The name of the folder containing the modules for the project.
    /// The convention is generally "projectname-modules".
    /// </summary>
    [JsonPropertyName("folderName")]
    public required string FolderName { get; set; }

    /// <summary>
    /// List of prefixes in files that we consider clientside only.
    /// These will be included in the client realm only.
    /// The server will send them via AddCSLuaFile().
    /// </summary>
    [JsonPropertyName("clientPrefixes")]
    public string[] ClientPrefixes { get; set; } = ["cl_"];

    /// <summary>
    /// List of prefixes in files that we consider shared.
    /// These will be included on the client & server realms.
    /// The server will send them via AddCSLuaFile().
    /// </summary>
    [JsonPropertyName("sharedPrefixes")]
    public string[] SharedPrefixes { get; set; } = ["sh_"];

    /// <summary>
    /// List of prefixes in files that we consider serverside only.
    /// These will be included on the server only.
    /// They will not be sent to clients.
    /// </summary>
    [JsonPropertyName("serverPrefixes")]
    public string[] ServerPrefixes { get; set; } = ["sv_"];

    /// <summary>
    /// The load order of the modules.
    /// Modules will be loaded using the order specified here.
    /// Any remaining modules not included in the load order will then be loaded.
    /// This order for these is not guaranteed and is likely OS dependent.
    /// </summary>
    [JsonPropertyName("loadOrder")]
    public string[] LoadOrder { get; set; } = [];
}