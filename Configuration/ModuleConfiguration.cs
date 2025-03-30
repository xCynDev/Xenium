using System.Text.Json.Serialization;

namespace Xenium;

/// <summary>
/// Contains information about a module's configuration, such as its description and load order.
/// </summary>
public class ModuleConfiguration
{
    /// <summary>
    /// The name of the module.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// The description of the module.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = "No description provided";

    /// <summary>
    /// If any, the order in which to load the module's scripts.
    /// Accepts files and folders.
    /// Any remaining files and folders not in the load order will be loaded via the order they were found in.
    /// This order for these is not guaranteed and is likely OS dependent.
    /// </summary>
    [JsonPropertyName("loadOrder")]
    public string[] LoadOrder { get; set; } = [];
}