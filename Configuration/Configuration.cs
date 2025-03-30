namespace Xenium;

/// <summary>
/// Current execution configuration for Xenium.
/// Modified by arguments passed into the commandline.
/// </summary>
public class Configuration
{
    /// <summary>
    /// Whether or not we want to be verbose in logging.
    /// </summary>
    public static bool IsVerbose { get; set; }
}