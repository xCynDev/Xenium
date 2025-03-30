using System.Text.Json.Serialization;

namespace Xenium.Json;

/// <summary>
/// Handles source generation for our models, since Native AOT breaks reflection.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(XeniumConfiguration))]
[JsonSerializable(typeof(ModuleConfiguration))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
        
}