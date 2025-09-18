using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// VSync modes for frame rate synchronization.
/// </summary>
public enum VSyncMode
{
    /// <summary>
    /// VSync disabled - unlimited frame rate.
    /// </summary>
    Disabled,
    
    /// <summary>
    /// VSync enabled - synchronize to display refresh rate.
    /// </summary>
    Enabled,
    
    /// <summary>
    /// Adaptive VSync - enable VSync when frame rate is above refresh rate.
    /// </summary>
    Adaptive,
    
    /// <summary>
    /// Half refresh rate VSync.
    /// </summary>
    Half
}

/// <summary>
/// Frame rate limiting strategies.
/// </summary>
public enum FrameRateLimitStrategy
{
    /// <summary>
    /// No frame rate limiting.
    /// </summary>
    None,
    
    /// <summary>
    /// Target a specific frame rate.
    /// </summary>
    Target,
    
    /// <summary>
    /// Limit to display refresh rate.
    /// </summary>
    DisplayRefreshRate,
    
    /// <summary>
    /// Adaptive limiting based on performance.
    /// </summary>
    Adaptive,
    
    /// <summary>
    /// Power-saving mode with reduced frame rate.
    /// </summary>
    PowerSaving
}

/// <summary>
/// Frame rate statistics and timing information.
/// </summary>
public class FrameRateStatistics
{
    /// <summary>
    /// Current frames per second.
    /// </summary>
    public float CurrentFPS { get; set; }
    
    /// <summary>
    /// Average frames per second over the measurement period.
    /// </summary>
    public float AverageFPS { get; set; }
    
    /// <summary>
    /// Minimum frames per second in the measurement period.
    /// </summary>
    public float MinFPS { get; set; }
    
    /// <summary>
    /// Maximum frames per second in the measurement period.
    /// </summary>
    public float MaxFPS { get; set; }
    
    /// <summary>
    /// Current frame time in milliseconds.
    /// </summary>
    public float CurrentFrameTime { get; set; }
    
    /// <summary>
    /// Average frame time in milliseconds.
    /// </summary>
    public float AverageFrameTime { get; set; }
    
    /// <summary>
    /// Target frame rate being aimed for.
    /// </summary>
    public float TargetFPS { get; set; }
    
    /// <summary>
    /// Display refresh rate in Hz.
    /// </summary>
    public float DisplayRefreshRate { get; set; }
    
    /// <summary>
    /// Number of frames dropped due to performance issues.
    /// </summary>
    public long DroppedFrames { get; set; }
    
    /// <summary>
    /// Total number of frames rendered.
    /// </summary>
    public long TotalFrames { get; set; }
    
    /// <summary>
    /// Whether VSync is currently active.
    /// </summary>
    public bool IsVSyncActive { get; set; }
    
    /// <summary>
    /// Current VSync mode.
    /// </summary>
    public VSyncMode VSyncMode { get; set; }
}

/// <summary>
/// Arguments for frame rate events.
/// </summary>
public class FrameRateEventArgs : EventArgs
{
    /// <summary>
    /// The current frame rate statistics.
    /// </summary>
    public FrameRateStatistics Statistics { get; }

    /// <summary>
    /// Initializes a new instance of the FrameRateEventArgs class.
    /// </summary>
    /// <param name="statistics">The current frame rate statistics.</param>
    public FrameRateEventArgs(FrameRateStatistics statistics)
    {
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
    }
}

/// <summary>
/// Tier 2: Frame rate manager service interface for VSync and adaptive sync management.
/// Handles frame rate limiting, display synchronization, and performance optimization
/// to ensure consistent frame rates across different refresh rates and hardware.
/// </summary>
public interface IFrameRateManager : IService
{
    /// <summary>
    /// Event raised when frame rate statistics are updated.
    /// </summary>
    event EventHandler<FrameRateEventArgs>? FrameRateUpdated;
    
    /// <summary>
    /// Event raised when the target frame rate changes.
    /// </summary>
    event EventHandler<FrameRateEventArgs>? TargetFrameRateChanged;
    
    /// <summary>
    /// Event raised when VSync mode changes.
    /// </summary>
    event EventHandler<FrameRateEventArgs>? VSyncModeChanged;
    
    /// <summary>
    /// Event raised when frame drops are detected.
    /// </summary>
    event EventHandler<FrameRateEventArgs>? FrameDropDetected;

    /// <summary>
    /// Gets the current frame rate statistics.
    /// </summary>
    FrameRateStatistics CurrentStatistics { get; }
    
    /// <summary>
    /// Gets the current target frame rate.
    /// </summary>
    float TargetFrameRate { get; }
    
    /// <summary>
    /// Gets the current VSync mode.
    /// </summary>
    VSyncMode VSyncMode { get; }
    
    /// <summary>
    /// Gets the current frame rate limiting strategy.
    /// </summary>
    FrameRateLimitStrategy LimitStrategy { get; }
    
    /// <summary>
    /// Gets whether frame rate limiting is currently active.
    /// </summary>
    bool IsFrameRateLimited { get; }

    /// <summary>
    /// Sets the target frame rate.
    /// </summary>
    /// <param name="targetFPS">The target frames per second.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetTargetFrameRateAsync(float targetFPS, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the VSync mode.
    /// </summary>
    /// <param name="vSyncMode">The VSync mode to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetVSyncModeAsync(VSyncMode vSyncMode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the frame rate limiting strategy.
    /// </summary>
    /// <param name="strategy">The frame rate limiting strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetFrameRateLimitStrategyAsync(FrameRateLimitStrategy strategy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables frame rate limiting.
    /// </summary>
    /// <param name="enabled">Whether frame rate limiting should be enabled.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetFrameRateLimitingEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the display refresh rate for the primary display.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the display refresh rate in Hz.</returns>
    Task<float> GetDisplayRefreshRateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available display refresh rates for the specified display.
    /// </summary>
    /// <param name="displayIndex">The index of the display, or null for primary display.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns available refresh rates.</returns>
    Task<IEnumerable<float>> GetAvailableRefreshRatesAsync(int? displayIndex = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for the next frame to be ready for rendering.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task WaitForNextFrameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces an immediate frame rate statistics update.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns updated statistics.</returns>
    Task<FrameRateStatistics> UpdateStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets frame rate statistics counters.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ResetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the statistics update interval.
    /// </summary>
    /// <param name="intervalSeconds">The interval in seconds between statistics updates.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetStatisticsUpdateIntervalAsync(float intervalSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables adaptive sync features if supported by the hardware.
    /// </summary>
    /// <param name="enabled">Whether adaptive sync should be enabled.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetAdaptiveSyncEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if adaptive sync is supported by the current hardware and driver.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if adaptive sync is supported.</returns>
    Task<bool> IsAdaptiveSyncSupportedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets frame pacing parameters for more consistent frame timing.
    /// </summary>
    /// <param name="enabled">Whether frame pacing should be enabled.</param>
    /// <param name="targetFrameTime">The target frame time in milliseconds, or null for automatic.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetFramePacingAsync(bool enabled, float? targetFrameTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets power saving mode parameters for battery-powered devices.
    /// </summary>
    /// <param name="enabled">Whether power saving mode should be enabled.</param>
    /// <param name="powerSavingFPS">The frame rate to use in power saving mode.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetPowerSavingModeAsync(bool enabled, float powerSavingFPS = 30.0f, CancellationToken cancellationToken = default);
}