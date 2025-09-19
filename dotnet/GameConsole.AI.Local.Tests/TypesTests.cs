using Xunit;

namespace GameConsole.AI.Local.Tests;

public class TypesTests
{
    [Fact]
    public void InferenceMetrics_Constructor_ShouldSetDefaults()
    {
        // Act
        var metrics = new InferenceMetrics();

        // Assert
        Assert.Equal(0, metrics.LoadTimeMs);
        Assert.Equal(0, metrics.InferenceTimeMs);
        Assert.Equal(0, metrics.MemoryUsageBytes);
        Assert.Equal(0, metrics.OperationsPerSecond);
        Assert.Equal(0, metrics.GpuUtilizationPercent);
        Assert.Equal(0, metrics.CpuUtilizationPercent);
        Assert.True(metrics.RecordedAt <= DateTime.UtcNow);
        Assert.True(metrics.RecordedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void InferenceMetrics_Properties_ShouldBeSettable()
    {
        // Arrange
        var metrics = new InferenceMetrics();
        var testTime = DateTime.UtcNow.AddMinutes(-10);

        // Act
        metrics.LoadTimeMs = 100.5;
        metrics.InferenceTimeMs = 50.2;
        metrics.MemoryUsageBytes = 1024000;
        metrics.OperationsPerSecond = 150.7;
        metrics.GpuUtilizationPercent = 85.3;
        metrics.CpuUtilizationPercent = 45.2;
        metrics.RecordedAt = testTime;

        // Assert
        Assert.Equal(100.5, metrics.LoadTimeMs);
        Assert.Equal(50.2, metrics.InferenceTimeMs);
        Assert.Equal(1024000, metrics.MemoryUsageBytes);
        Assert.Equal(150.7, metrics.OperationsPerSecond);
        Assert.Equal(85.3, metrics.GpuUtilizationPercent);
        Assert.Equal(45.2, metrics.CpuUtilizationPercent);
        Assert.Equal(testTime, metrics.RecordedAt);
    }

    [Fact]
    public void ResourceConstraints_Constructor_ShouldSetDefaults()
    {
        // Act
        var constraints = new ResourceConstraints();

        // Assert
        Assert.Equal(2L * 1024 * 1024 * 1024, constraints.MaxMemoryBytes); // 2GB
        Assert.Equal(80.0, constraints.MaxCpuUtilizationPercent);
        Assert.Equal(90.0, constraints.MaxGpuUtilizationPercent);
        Assert.Equal(4, constraints.MaxConcurrentOperations);
        Assert.Equal(TimeSpan.FromSeconds(30), constraints.InferenceTimeout);
    }

    [Fact]
    public void ResourceConstraints_Properties_ShouldBeSettable()
    {
        // Arrange
        var constraints = new ResourceConstraints();

        // Act
        constraints.MaxMemoryBytes = 1024L * 1024 * 1024; // 1GB
        constraints.MaxCpuUtilizationPercent = 70.0;
        constraints.MaxGpuUtilizationPercent = 80.0;
        constraints.MaxConcurrentOperations = 2;
        constraints.InferenceTimeout = TimeSpan.FromSeconds(15);

        // Assert
        Assert.Equal(1024L * 1024 * 1024, constraints.MaxMemoryBytes);
        Assert.Equal(70.0, constraints.MaxCpuUtilizationPercent);
        Assert.Equal(80.0, constraints.MaxGpuUtilizationPercent);
        Assert.Equal(2, constraints.MaxConcurrentOperations);
        Assert.Equal(TimeSpan.FromSeconds(15), constraints.InferenceTimeout);
    }

    [Fact]
    public void QuantizationConfig_Constructor_ShouldSetDefaults()
    {
        // Act
        var config = new QuantizationConfig();

        // Assert
        Assert.Equal(QuantizationLevel.Dynamic, config.Level);
        Assert.True(config.UseGpuAcceleration);
        Assert.Equal(100, config.CalibrationDatasetSize);
    }

    [Fact]
    public void QuantizationLevel_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(QuantizationLevel), QuantizationLevel.None));
        Assert.True(Enum.IsDefined(typeof(QuantizationLevel), QuantizationLevel.Dynamic));
        Assert.True(Enum.IsDefined(typeof(QuantizationLevel), QuantizationLevel.Static));
        Assert.True(Enum.IsDefined(typeof(QuantizationLevel), QuantizationLevel.Aggressive));
    }

    [Fact]
    public void ExecutionProvider_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ExecutionProvider), ExecutionProvider.Cpu));
        Assert.True(Enum.IsDefined(typeof(ExecutionProvider), ExecutionProvider.Cuda));
        Assert.True(Enum.IsDefined(typeof(ExecutionProvider), ExecutionProvider.DirectMl));
        Assert.True(Enum.IsDefined(typeof(ExecutionProvider), ExecutionProvider.OpenVino));
        Assert.True(Enum.IsDefined(typeof(ExecutionProvider), ExecutionProvider.Auto));
    }

    [Fact]
    public void ResourceMetrics_Constructor_ShouldSetDefaults()
    {
        // Act
        var metrics = new ResourceMetrics();

        // Assert
        Assert.Equal(0, metrics.CpuUtilizationPercent);
        Assert.Equal(0, metrics.GpuUtilizationPercent);
        Assert.Equal(0, metrics.MemoryUsageBytes);
        Assert.Equal(0, metrics.AvailableMemoryBytes);
        Assert.True(metrics.RecordedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void ResourceAllocation_Constructor_ShouldSetDefaults()
    {
        // Act
        var allocation = new ResourceAllocation();

        // Assert
        Assert.NotNull(allocation.Id);
        Assert.NotEqual(Guid.Empty.ToString(), allocation.Id);
        Assert.Equal(0, allocation.AllocatedMemoryBytes);
        Assert.True(allocation.IsActive);
        Assert.True(allocation.AllocatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void BatchConfiguration_Constructor_ShouldSetDefaults()
    {
        // Act
        var config = new BatchConfiguration();

        // Assert
        Assert.Equal(32, config.MaxBatchSize);
        Assert.Equal(TimeSpan.FromMilliseconds(100), config.BatchTimeout);
        Assert.True(config.EnableDynamicBatching);
        Assert.Equal(8, config.OptimalBatchSize);
    }

    [Fact]
    public void BatchConfiguration_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new BatchConfiguration();

        // Act
        config.MaxBatchSize = 16;
        config.BatchTimeout = TimeSpan.FromMilliseconds(50);
        config.EnableDynamicBatching = false;
        config.OptimalBatchSize = 4;

        // Assert
        Assert.Equal(16, config.MaxBatchSize);
        Assert.Equal(TimeSpan.FromMilliseconds(50), config.BatchTimeout);
        Assert.False(config.EnableDynamicBatching);
        Assert.Equal(4, config.OptimalBatchSize);
    }
}