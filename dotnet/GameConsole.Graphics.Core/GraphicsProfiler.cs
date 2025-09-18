namespace GameConsole.Graphics.Core;

/// <summary>
/// Interface for graphics performance monitoring and debugging.
/// </summary>
public interface IGraphicsProfiler
{
    /// <summary>
    /// Gets a value indicating whether profiling is currently enabled.
    /// </summary>
    bool IsProfilingEnabled { get; }

    /// <summary>
    /// Gets the current frame statistics.
    /// </summary>
    FrameStatistics CurrentFrameStats { get; }

    /// <summary>
    /// Gets the average frame statistics over a specified number of frames.
    /// </summary>
    FrameStatistics AverageFrameStats { get; }

    /// <summary>
    /// Gets the performance counters for detailed metrics.
    /// </summary>
    IReadOnlyDictionary<string, PerformanceCounter> PerformanceCounters { get; }

    /// <summary>
    /// Enables profiling and performance monitoring.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EnableProfilingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables profiling and performance monitoring.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DisableProfilingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new frame for profiling.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BeginFrameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the current frame and updates statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EndFrameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins profiling a specific operation or section.
    /// </summary>
    /// <param name="sectionName">The name of the section to profile.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a profiling session.</returns>
    Task<IProfilingSession> BeginSectionAsync(string sectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a custom performance metric.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RecordMetricAsync(string name, double value, string unit = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes a snapshot of current performance metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the performance snapshot.</returns>
    Task<PerformanceSnapshot> TakeSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports profiling data to a file.
    /// </summary>
    /// <param name="filePath">The path to export the data to.</param>
    /// <param name="format">The export format.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ExportDataAsync(string filePath, ExportFormat format = ExportFormat.Json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all performance counters and statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ResetCountersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets GPU memory usage statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns memory usage information.</returns>
    Task<MemoryUsageInfo> GetMemoryUsageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed timing information for the last frame.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns timing information.</returns>
    Task<TimingInfo> GetTimingInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for performance bottlenecks and returns recommendations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns performance recommendations.</returns>
    Task<PerformanceRecommendations> AnalyzePerformanceAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for a profiling session that tracks a specific operation.
/// </summary>
public interface IProfilingSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the section being profiled.
    /// </summary>
    string SectionName { get; }

    /// <summary>
    /// Gets the start time of the profiling session.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets the elapsed time since the session started.
    /// </summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Gets a value indicating whether the session is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Adds a custom marker to the profiling session.
    /// </summary>
    /// <param name="markerName">The name of the marker.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddMarkerAsync(string markerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a custom metric for this profiling session.
    /// </summary>
    /// <param name="metricName">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RecordMetricAsync(string metricName, double value, string unit = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the profiling session and records the results.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the session results.</returns>
    Task<ProfilingResults> EndAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Contains performance counter information.
/// </summary>
public record struct PerformanceCounter
{
    /// <summary>
    /// Gets or sets the name of the counter.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the current value of the counter.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the unit of measurement.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// Gets or sets the minimum value recorded.
    /// </summary>
    public double MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value recorded.
    /// </summary>
    public double MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the average value over time.
    /// </summary>
    public double AverageValue { get; set; }

    /// <summary>
    /// Gets or sets the number of samples recorded.
    /// </summary>
    public ulong SampleCount { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }
}

/// <summary>
/// Contains a snapshot of performance metrics at a specific point in time.
/// </summary>
public record struct PerformanceSnapshot
{
    /// <summary>
    /// Gets or sets the timestamp when the snapshot was taken.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the frame statistics at the time of the snapshot.
    /// </summary>
    public FrameStatistics FrameStats { get; set; }

    /// <summary>
    /// Gets or sets the memory usage information.
    /// </summary>
    public MemoryUsageInfo MemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the timing information.
    /// </summary>
    public TimingInfo Timing { get; set; }

    /// <summary>
    /// Gets or sets custom performance counters.
    /// </summary>
    public Dictionary<string, PerformanceCounter> Counters { get; set; }

    /// <summary>
    /// Gets or sets any active profiling sessions.
    /// </summary>
    public string[] ActiveSections { get; set; }
}

/// <summary>
/// Contains memory usage information for graphics resources.
/// </summary>
public record struct MemoryUsageInfo
{
    /// <summary>
    /// Gets or sets the total allocated GPU memory in bytes.
    /// </summary>
    public ulong TotalAllocated { get; set; }

    /// <summary>
    /// Gets or sets the currently used GPU memory in bytes.
    /// </summary>
    public ulong CurrentUsage { get; set; }

    /// <summary>
    /// Gets or sets the peak GPU memory usage in bytes.
    /// </summary>
    public ulong PeakUsage { get; set; }

    /// <summary>
    /// Gets or sets the number of active allocations.
    /// </summary>
    public uint AllocationCount { get; set; }

    /// <summary>
    /// Gets or sets the memory usage by resource type.
    /// </summary>
    public Dictionary<string, ulong> UsageByType { get; set; }

    /// <summary>
    /// Gets or sets the memory fragmentation percentage.
    /// </summary>
    public float FragmentationPercentage { get; set; }
}

/// <summary>
/// Contains detailed timing information for graphics operations.
/// </summary>
public record struct TimingInfo
{
    /// <summary>
    /// Gets or sets the total CPU time spent on graphics in milliseconds.
    /// </summary>
    public double CpuTime { get; set; }

    /// <summary>
    /// Gets or sets the total GPU time spent on graphics in milliseconds.
    /// </summary>
    public double GpuTime { get; set; }

    /// <summary>
    /// Gets or sets the time spent waiting for GPU in milliseconds.
    /// </summary>
    public double WaitTime { get; set; }

    /// <summary>
    /// Gets or sets the time spent on vertex processing in milliseconds.
    /// </summary>
    public double VertexTime { get; set; }

    /// <summary>
    /// Gets or sets the time spent on fragment processing in milliseconds.
    /// </summary>
    public double FragmentTime { get; set; }

    /// <summary>
    /// Gets or sets the time spent on compute operations in milliseconds.
    /// </summary>
    public double ComputeTime { get; set; }

    /// <summary>
    /// Gets or sets the time spent on resource transfers in milliseconds.
    /// </summary>
    public double TransferTime { get; set; }

    /// <summary>
    /// Gets or sets timing for individual rendering sections.
    /// </summary>
    public Dictionary<string, double> SectionTimings { get; set; }
}

/// <summary>
/// Contains performance analysis results and recommendations.
/// </summary>
public record struct PerformanceRecommendations
{
    /// <summary>
    /// Gets or sets the overall performance score (0-100).
    /// </summary>
    public float PerformanceScore { get; set; }

    /// <summary>
    /// Gets or sets identified performance bottlenecks.
    /// </summary>
    public PerformanceBottleneck[] Bottlenecks { get; set; }

    /// <summary>
    /// Gets or sets optimization recommendations.
    /// </summary>
    public string[] Recommendations { get; set; }

    /// <summary>
    /// Gets or sets potential performance improvements.
    /// </summary>
    public PerformanceImprovement[] PotentialImprovements { get; set; }

    /// <summary>
    /// Gets or sets the analysis timestamp.
    /// </summary>
    public DateTimeOffset AnalysisTime { get; set; }
}

/// <summary>
/// Describes a performance bottleneck.
/// </summary>
public record struct PerformanceBottleneck
{
    /// <summary>
    /// Gets or sets the type of bottleneck.
    /// </summary>
    public BottleneckType Type { get; set; }

    /// <summary>
    /// Gets or sets the severity of the bottleneck (0-100).
    /// </summary>
    public float Severity { get; set; }

    /// <summary>
    /// Gets or sets the description of the bottleneck.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the affected component or operation.
    /// </summary>
    public string AffectedComponent { get; set; }

    /// <summary>
    /// Gets or sets the measured impact on performance.
    /// </summary>
    public double PerformanceImpact { get; set; }
}

/// <summary>
/// Describes a potential performance improvement.
/// </summary>
public record struct PerformanceImprovement
{
    /// <summary>
    /// Gets or sets the name of the improvement.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the improvement.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the estimated performance gain percentage.
    /// </summary>
    public float EstimatedGain { get; set; }

    /// <summary>
    /// Gets or sets the implementation difficulty (1-10).
    /// </summary>
    public int Difficulty { get; set; }

    /// <summary>
    /// Gets or sets the category of the improvement.
    /// </summary>
    public string Category { get; set; }
}

/// <summary>
/// Contains results from a profiling session.
/// </summary>
public record struct ProfilingResults
{
    /// <summary>
    /// Gets or sets the name of the profiled section.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the total elapsed time.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// Gets or sets custom metrics recorded during the session.
    /// </summary>
    public Dictionary<string, double> Metrics { get; set; }

    /// <summary>
    /// Gets or sets markers added during the session.
    /// </summary>
    public string[] Markers { get; set; }

    /// <summary>
    /// Gets or sets the session start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the session end time.
    /// </summary>
    public DateTimeOffset EndTime { get; set; }
}

/// <summary>
/// Defines export formats for profiling data.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// JSON format.
    /// </summary>
    Json,

    /// <summary>
    /// CSV format.
    /// </summary>
    Csv,

    /// <summary>
    /// XML format.
    /// </summary>
    Xml,

    /// <summary>
    /// Binary format.
    /// </summary>
    Binary
}

/// <summary>
/// Defines types of performance bottlenecks.
/// </summary>
public enum BottleneckType
{
    /// <summary>
    /// CPU bottleneck.
    /// </summary>
    Cpu,

    /// <summary>
    /// GPU bottleneck.
    /// </summary>
    Gpu,

    /// <summary>
    /// Memory bandwidth bottleneck.
    /// </summary>
    Memory,

    /// <summary>
    /// Vertex processing bottleneck.
    /// </summary>
    Vertex,

    /// <summary>
    /// Fragment processing bottleneck.
    /// </summary>
    Fragment,

    /// <summary>
    /// Texture sampling bottleneck.
    /// </summary>
    Texture,

    /// <summary>
    /// Draw call overhead bottleneck.
    /// </summary>
    DrawCalls,

    /// <summary>
    /// State change overhead bottleneck.
    /// </summary>
    StateChanges,

    /// <summary>
    /// Resource transfer bottleneck.
    /// </summary>
    Transfer,

    /// <summary>
    /// Synchronization bottleneck.
    /// </summary>
    Synchronization
}