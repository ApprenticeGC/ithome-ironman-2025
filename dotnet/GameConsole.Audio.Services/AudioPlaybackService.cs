using GameConsole.Core.Abstractions;
using GameConsole.Audio.Core;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace GameConsole.Audio.Services;

/// <summary>
/// Audio playback service for music and sound effects using NAudio.
/// </summary>
[Service("AudioPlaybackService", "1.0.0", "Core audio playback service for music and sound effects")]
public class AudioPlaybackService : BaseAudioService
{
    private readonly Dictionary<string, IWavePlayer> _activePlayers;
    private readonly Dictionary<string, AudioFileReader> _activeReaders;
    private readonly object _lockObject = new();

    public AudioPlaybackService(ILogger<AudioPlaybackService> logger) : base(logger)
    {
        _activePlayers = new Dictionary<string, IWavePlayer>();
        _activeReaders = new Dictionary<string, AudioFileReader>();
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Initializing audio playback system");
        
        // Verify NAudio is available
        try
        {
            using var waveOut = new WaveOutEvent();
            _logger.LogDebug("NAudio initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize NAudio");
            throw;
        }

        await base.OnInitializeAsync(cancellationToken);
    }

    protected override Task OnStartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting audio playback monitoring");
        return Task.CompletedTask;
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping all audio playback");
        await StopAllAsync(cancellationToken);
        await base.OnStopAsync(cancellationToken);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        await StopAllAsync();
        
        lock (_lockObject)
        {
            foreach (var player in _activePlayers.Values)
            {
                player.Dispose();
            }
            
            foreach (var reader in _activeReaders.Values)
            {
                reader.Dispose();
            }

            _activePlayers.Clear();
            _activeReaders.Clear();
        }

        await base.OnDisposeAsync();
    }

    public override Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default)
    {
        if (!ValidateAudioPath(path))
        {
            return Task.FromResult(false);
        }

        try
        {
            lock (_lockObject)
            {
                // Stop any existing playback for this file
                if (_activePlayers.ContainsKey(path))
                {
                    StopInternalLocked(path);
                }

                // Create audio reader
                var reader = new AudioFileReader(path);
                var effectiveVolume = GetEffectiveVolume(category);
                reader.Volume = effectiveVolume;

                // Create wave player
                var waveOut = new WaveOutEvent();
                waveOut.Init(reader);

                // Set up cleanup when playback finishes
                waveOut.PlaybackStopped += (sender, e) =>
                {
                    lock (_lockObject)
                    {
                        if (_activePlayers.ContainsKey(path))
                        {
                            StopInternalLocked(path);
                        }
                    }
                };

                // Store references
                _activePlayers[path] = waveOut;
                _activeReaders[path] = reader;

                // Start playback
                waveOut.Play();

                _logger.LogDebug("Started playback of {Path} in category {Category} at volume {Volume}", 
                    path, category, effectiveVolume);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play audio file {Path}", path);
            return Task.FromResult(false);
        }
    }

    public override Task StopAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.CompletedTask;
        }

        lock (_lockObject)
        {
            StopInternalLocked(path);
        }

        _logger.LogDebug("Stopped playback of {Path}", path);
        return Task.CompletedTask;
    }

    public override Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            var paths = _activePlayers.Keys.ToList();
            foreach (var path in paths)
            {
                StopInternalLocked(path);
            }
        }

        _logger.LogDebug("Stopped all audio playback");
        return Task.CompletedTask;
    }

    public override Task SetCategoryVolumeAsync(string category, float volume, CancellationToken cancellationToken = default)
    {
        var result = base.SetCategoryVolumeAsync(category, volume, cancellationToken);

        // Update volume for all active players in this category
        // Note: This is a simplified approach - in a full implementation,
        // we'd need to track which files belong to which categories
        lock (_lockObject)
        {
            foreach (var reader in _activeReaders.Values)
            {
                reader.Volume = GetEffectiveVolume(category);
            }
        }

        return result;
    }

    public override Task SetMasterVolumeAsync(float volume, CancellationToken cancellationToken = default)
    {
        var result = base.SetMasterVolumeAsync(volume, cancellationToken);

        // Update volume for all active players
        lock (_lockObject)
        {
            foreach (var reader in _activeReaders.Values)
            {
                reader.Volume = _masterVolume * reader.Volume;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets information about the currently playing audio files.
    /// </summary>
    /// <returns>Dictionary of file paths and their playback states.</returns>
    public Dictionary<string, GameConsole.Audio.Core.PlaybackState> GetPlaybackStates()
    {
        lock (_lockObject)
        {
            var states = new Dictionary<string, GameConsole.Audio.Core.PlaybackState>();
            foreach (var kvp in _activePlayers)
            {
                var state = kvp.Value.PlaybackState switch
                {
                    NAudio.Wave.PlaybackState.Playing => GameConsole.Audio.Core.PlaybackState.Playing,
                    NAudio.Wave.PlaybackState.Paused => GameConsole.Audio.Core.PlaybackState.Paused,
                    NAudio.Wave.PlaybackState.Stopped => GameConsole.Audio.Core.PlaybackState.Stopped,
                    _ => GameConsole.Audio.Core.PlaybackState.Stopped
                };
                states[kvp.Key] = state;
            }
            return states;
        }
    }

    /// <summary>
    /// Gets supported audio formats.
    /// </summary>
    /// <returns>Array of supported audio format extensions.</returns>
    public static string[] GetSupportedFormats()
    {
        return new[] { ".wav", ".mp3", ".aiff", ".flac" };
    }

    private void StopInternalLocked(string path)
    {
        if (_activePlayers.TryGetValue(path, out var player))
        {
            player.Stop();
            player.Dispose();
            _activePlayers.Remove(path);
        }

        if (_activeReaders.TryGetValue(path, out var reader))
        {
            reader.Dispose();
            _activeReaders.Remove(path);
        }
    }
}