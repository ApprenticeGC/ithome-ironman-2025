namespace GameConsole.Providers.Registry;

/// <summary>
/// Represents supported platforms for provider compatibility.
/// </summary>
[Flags]
public enum Platform
{
    /// <summary>
    /// No specific platform requirement.
    /// </summary>
    None = 0,

    /// <summary>
    /// Windows platform.
    /// </summary>
    Windows = 1 << 0,

    /// <summary>
    /// Linux platform.
    /// </summary>
    Linux = 1 << 1,

    /// <summary>
    /// macOS platform.
    /// </summary>
    MacOS = 1 << 2,

    /// <summary>
    /// Android platform.
    /// </summary>
    Android = 1 << 3,

    /// <summary>
    /// iOS platform.
    /// </summary>
    iOS = 1 << 4,

    /// <summary>
    /// WebAssembly platform.
    /// </summary>
    WebAssembly = 1 << 5,

    /// <summary>
    /// All platforms.
    /// </summary>
    All = Windows | Linux | MacOS | Android | iOS | WebAssembly
}