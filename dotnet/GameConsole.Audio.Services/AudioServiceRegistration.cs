using GameConsole.Audio.Services;
using GameConsole.Audio.Services.Implementation;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;

namespace GameConsole.Audio.Services;

/// <summary>
/// Extension methods for registering audio services with the service registry.
/// </summary>
public static class AudioServiceRegistration
{
    /// <summary>
    /// Registers all audio services with the service registry.
    /// </summary>
    /// <param name="registry">The service registry to register with.</param>
    /// <param name="loggerFactory">Optional logger factory for service logging.</param>
    public static void RegisterAudioServices(this IServiceRegistry registry, ILoggerFactory? loggerFactory = null)
    {
        if (registry == null)
            throw new ArgumentNullException(nameof(registry));

        // Register main audio service
        registry.RegisterScoped<IAudioService>(provider => 
            new AudioPlaybackService(loggerFactory?.CreateLogger<AudioPlaybackService>()));

        // Register mixer service
        registry.RegisterScoped<AudioMixerService>(provider => 
            new AudioMixerService(loggerFactory?.CreateLogger<AudioMixerService>()));

        // Register 3D audio service (with spatial audio capability)
        registry.RegisterScoped<Audio3DService>(provider => 
            new Audio3DService(loggerFactory?.CreateLogger<Audio3DService>()));

        // Register device service (with device management capability)
        registry.RegisterScoped<AudioDeviceService>(provider => 
            new AudioDeviceService(loggerFactory?.CreateLogger<AudioDeviceService>()));
    }

    /// <summary>
    /// Registers audio services from assembly attributes.
    /// </summary>
    /// <param name="registry">The service registry to register with.</param>
    public static void RegisterAudioServicesFromAttributes(this IServiceRegistry registry)
    {
        if (registry == null)
            throw new ArgumentNullException(nameof(registry));

        var assembly = typeof(AudioServiceRegistration).Assembly;
        registry.RegisterFromAttributes(assembly, "Audio");
    }
}

/// <summary>
/// Factory for creating audio service instances with proper dependencies.
/// </summary>
public static class AudioServiceFactory
{
    /// <summary>
    /// Creates a configured audio playback service.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>Configured audio playback service.</returns>
    public static AudioPlaybackService CreateAudioPlaybackService(ILogger<AudioPlaybackService>? logger = null)
    {
        return new AudioPlaybackService(logger);
    }

    /// <summary>
    /// Creates a configured audio mixer service.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>Configured audio mixer service.</returns>
    public static AudioMixerService CreateAudioMixerService(ILogger<AudioMixerService>? logger = null)
    {
        return new AudioMixerService(logger);
    }

    /// <summary>
    /// Creates a configured 3D audio service.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>Configured 3D audio service.</returns>
    public static Audio3DService CreateAudio3DService(ILogger<Audio3DService>? logger = null)
    {
        return new Audio3DService(logger);
    }

    /// <summary>
    /// Creates a configured audio device service.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>Configured audio device service.</returns>
    public static AudioDeviceService CreateAudioDeviceService(ILogger<AudioDeviceService>? logger = null)
    {
        return new AudioDeviceService(logger);
    }
}