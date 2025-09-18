using GameConsole.Audio.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Collections.Concurrent;

namespace GameConsole.Audio.Services.Implementation;

/// <summary>
/// Service for audio playback and management.
/// Handles music and sound effects with support for multiple audio formats.
/// </summary>
[Service("Audio", Categories = new[] { "Audio" })]
public sealed class AudioPlaybackService : BaseAudioService, IAudioService
{
    private readonly ConcurrentDictionary<string, AudioTrack> _playingTracks = new();
    private readonly ConcurrentDictionary<string, float> _categoryVolumes = new();
    private float _masterVolume = 1.0f;

    public AudioPlaybackService(ILogger<AudioPlaybackService>? logger = null) 
        : base(logger)
    {
        // Initialize default category volumes
        _categoryVolumes["SFX"] = 1.0f;
        _categoryVolumes["Music"] = 1.0f;
        _categoryVolumes["Voice"] = 1.0f;
        _categoryVolumes["Ambient"] = 1.0f;
    }

    /// <summary>
    /// Plays an audio file with the specified category.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <param name="category">Audio category for volume management.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if playback started successfully.</returns>
    public async Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
        {
            Logger?.LogWarning("Audio file not found: {Path}", path);
            return false;
        }

        try
        {
            // Stop existing track with same path if playing
            if (_playingTracks.ContainsKey(path))
            {
                await StopAsync(path, ct);
            }

            var audioFileReader = new AudioFileReader(path);
            var waveOutDevice = new WaveOutEvent();
            
            // Set volume based on master and category volumes
            var categoryVolume = await GetCategoryVolumeAsync(category, ct);
            audioFileReader.Volume = _masterVolume * categoryVolume;

            var track = new AudioTrack(path, category, audioFileReader, waveOutDevice);
            
            // Handle playback stopped event
            waveOutDevice.PlaybackStopped += (sender, e) =>
            {
                _playingTracks.TryRemove(path, out var removedTrack);
                removedTrack?.Dispose();
            };

            waveOutDevice.Init(audioFileReader);
            _playingTracks[path] = track;
            
            waveOutDevice.Play();
            
            Logger?.LogDebug("Started playback of {Path} in category {Category}", path, category);
            return true;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to play audio file: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Stops playback of the specified audio file.
    /// </summary>
    /// <param name="path">Path to the audio file to stop.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task StopAsync(string path, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (_playingTracks.TryRemove(path, out var track))
        {
            track.Dispose();
            Logger?.LogDebug("Stopped playback of {Path}", path);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops all currently playing audio.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task StopAllAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var tracks = _playingTracks.Values.ToArray();
        _playingTracks.Clear();
        
        foreach (var track in tracks)
        {
            track.Dispose();
        }
        
        Logger?.LogDebug("Stopped all audio playback");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Sets the master volume for all audio.
    /// </summary>
    /// <param name="volume">Volume level between 0.0 and 1.0.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SetMasterVolumeAsync(float volume, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (volume < 0.0f || volume > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");

        _masterVolume = volume;
        
        // Update all playing tracks
        foreach (var track in _playingTracks.Values)
        {
            var categoryVolume = await GetCategoryVolumeAsync(track.Category, ct);
            track.AudioFileReader.Volume = _masterVolume * categoryVolume;
        }
        
        Logger?.LogDebug("Set master volume to {Volume}", volume);
    }

    /// <summary>
    /// Sets the volume for a specific audio category.
    /// </summary>
    /// <param name="category">Audio category name.</param>
    /// <param name="volume">Volume level between 0.0 and 1.0.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SetCategoryVolumeAsync(string category, float volume, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(category))
            throw new ArgumentException("Category cannot be null or empty", nameof(category));
        
        if (volume < 0.0f || volume > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");

        _categoryVolumes[category] = volume;
        
        // Update all playing tracks in this category
        foreach (var track in _playingTracks.Values.Where(t => t.Category == category))
        {
            track.AudioFileReader.Volume = _masterVolume * volume;
        }
        
        Logger?.LogDebug("Set category {Category} volume to {Volume}", category, volume);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the volume for a specific audio category.
    /// </summary>
    /// <param name="category">Audio category name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Volume level between 0.0 and 1.0.</returns>
    public async Task<float> GetCategoryVolumeAsync(string category, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(category))
            throw new ArgumentException("Category cannot be null or empty", nameof(category));

        await Task.CompletedTask;
        return _categoryVolumes.GetValueOrDefault(category, 1.0f);
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken)
    {
        await StopAllAsync(cancellationToken);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        await StopAllAsync();
    }

    /// <summary>
    /// Represents an active audio track.
    /// </summary>
    private sealed class AudioTrack : IDisposable
    {
        public string Path { get; }
        public string Category { get; }
        public AudioFileReader AudioFileReader { get; }
        public WaveOutEvent WaveOutDevice { get; }

        public AudioTrack(string path, string category, AudioFileReader audioFileReader, WaveOutEvent waveOutDevice)
        {
            Path = path;
            Category = category;
            AudioFileReader = audioFileReader;
            WaveOutDevice = waveOutDevice;
        }

        public void Dispose()
        {
            WaveOutDevice?.Stop();
            WaveOutDevice?.Dispose();
            AudioFileReader?.Dispose();
        }
    }
}