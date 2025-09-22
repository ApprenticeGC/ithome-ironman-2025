using GameConsole.UI.Console;

namespace GameConsole.UI.Console.Tests;

public class ConsoleProgressBarTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsDefaultValues()
    {
        var progressBar = new ConsoleProgressBar();
        
        Assert.Equal(0.0, progressBar.Value);
        Assert.Equal(0.0, progressBar.MinValue);
        Assert.Equal(100.0, progressBar.MaxValue);
        Assert.Equal(0.0, progressBar.Percentage);
    }
    
    [Fact]
    public void Value_WithValidValue_SetsCorrectly()
    {
        var progressBar = new ConsoleProgressBar();
        
        progressBar.Value = 0.5;
        
        Assert.Equal(0.5, progressBar.Value);
        Assert.Equal(50.0, progressBar.Percentage);
    }
    
    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(1.5, 1.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 1.0)]
    public void Value_WithClampedValues_ClampsCorrectly(double input, double expected)
    {
        var progressBar = new ConsoleProgressBar();
        
        progressBar.Value = input;
        
        Assert.Equal(expected, progressBar.Value);
    }
    
    [Fact]
    public void NumericValue_WithCustomRange_CalculatesCorrectly()
    {
        var progressBar = new ConsoleProgressBar();
        progressBar.MinValue = 10;
        progressBar.MaxValue = 90;
        progressBar.Value = 0.5; // 50%
        
        var numericValue = progressBar.NumericValue;
        
        Assert.Equal(50.0, numericValue); // 10 + 0.5 * (90 - 10) = 50
    }
    
    [Fact]
    public void SetNumericValue_WithCustomRange_SetsValueCorrectly()
    {
        var progressBar = new ConsoleProgressBar();
        progressBar.MinValue = 0;
        progressBar.MaxValue = 200;
        
        progressBar.NumericValue = 100;
        
        Assert.Equal(0.5, progressBar.Value); // (100 - 0) / (200 - 0) = 0.5
        Assert.Equal(100.0, progressBar.NumericValue);
    }
    
    [Fact]
    public void SetPercentage_WithValidPercentage_SetsValueCorrectly()
    {
        var progressBar = new ConsoleProgressBar();
        
        progressBar.SetPercentage(75);
        
        Assert.Equal(0.75, progressBar.Value);
        Assert.Equal(75.0, progressBar.Percentage);
    }
    
    [Fact]
    public void Increment_WithDefaultAmount_IncrementsCorrectly()
    {
        var progressBar = new ConsoleProgressBar();
        progressBar.Value = 0.3;
        
        progressBar.Increment();
        
        Assert.Equal(0.31, progressBar.Value, precision: 2);
    }
    
    [Fact]
    public void Increment_WithCustomAmount_IncrementsCorrectly()
    {
        var progressBar = new ConsoleProgressBar();
        progressBar.Value = 0.2;
        
        progressBar.Increment(0.3);
        
        Assert.Equal(0.5, progressBar.Value);
    }
    
    [Fact]
    public void Increment_BeyondMax_ClampsToMax()
    {
        var progressBar = new ConsoleProgressBar();
        progressBar.Value = 0.8;
        
        progressBar.Increment(0.5); // Would go to 1.3
        
        Assert.Equal(1.0, progressBar.Value);
    }
    
    [Fact]
    public void Reset_WhenCalled_SetsValueToZero()
    {
        var progressBar = new ConsoleProgressBar();
        progressBar.Value = 0.7;
        
        progressBar.Reset();
        
        Assert.Equal(0.0, progressBar.Value);
    }
    
    [Fact]
    public void Complete_WhenCalled_SetsValueToOne()
    {
        var progressBar = new ConsoleProgressBar();
        progressBar.Value = 0.3;
        
        progressBar.Complete();
        
        Assert.Equal(1.0, progressBar.Value);
    }
    
    [Fact]
    public void GetDesiredSize_WithLabel_ReturnsCorrectHeight()
    {
        var progressBarWithLabel = new ConsoleProgressBar("Loading...");
        var progressBarWithoutLabel = new ConsoleProgressBar("");
        
        var sizeWithLabel = progressBarWithLabel.GetDesiredSize(new ConsoleSize(100, 100));
        var sizeWithoutLabel = progressBarWithoutLabel.GetDesiredSize(new ConsoleSize(100, 100));
        
        Assert.Equal(2, sizeWithLabel.Height); // Label + progress bar
        Assert.Equal(1, sizeWithoutLabel.Height); // Just progress bar
    }
    
    [Fact]
    public void Render_WithValidBuffer_RendersProgressBar()
    {
        var progressBar = new ConsoleProgressBar("Test", showPercentage: false);
        progressBar.Value = 0.5; // 50%
        var buffer = new ConsoleBuffer(20, 3);
        
        progressBar.Render(buffer, new ConsoleRect(0, 0, 20, 3));
        
        // Should render label on first line
        Assert.Equal('T', buffer[0, 0].Character);
        Assert.Equal('e', buffer[1, 0].Character);
        Assert.Equal('s', buffer[2, 0].Character);
        Assert.Equal('t', buffer[3, 0].Character);
        
        // Should render progress bar on second line with approximately half filled
        var progressLineHasFilled = false;
        var progressLineHasEmpty = false;
        
        for (int x = 0; x < 20; x++)
        {
            var cell = buffer[x, 1];
            if (cell.Character == '█') progressLineHasFilled = true;
            if (cell.Character == '░') progressLineHasEmpty = true;
        }
        
        Assert.True(progressLineHasFilled, "Progress bar should have filled characters");
        Assert.True(progressLineHasEmpty, "Progress bar should have empty characters");
    }
}

public class ConsoleMultiProgressBarTests
{
    [Fact]
    public void Constructor_WithDefaults_InitializesCorrectly()
    {
        var multiProgressBar = new ConsoleMultiProgressBar("Loading");
        
        Assert.Empty(multiProgressBar.ProgressBars);
    }
    
    [Fact]
    public void AddProgressBar_WithLabelAndBar_AddsCorrectly()
    {
        var multiProgressBar = new ConsoleMultiProgressBar();
        var progressBar = new ConsoleProgressBar("Task");
        
        multiProgressBar.AddProgressBar("Task 1", progressBar);
        
        Assert.Single(multiProgressBar.ProgressBars);
        Assert.Equal("Task 1", multiProgressBar.ProgressBars[0].label);
        Assert.Equal(progressBar, multiProgressBar.ProgressBars[0].progressBar);
    }
    
    [Fact]
    public void AddProgressBar_WithCreateMethod_CreatesAndAddsBar()
    {
        var multiProgressBar = new ConsoleMultiProgressBar();
        
        var createdBar = multiProgressBar.AddProgressBar("Download", ProgressBarStyle.Hash);
        
        Assert.Single(multiProgressBar.ProgressBars);
        Assert.Equal("Download", multiProgressBar.ProgressBars[0].label);
        Assert.Equal(createdBar, multiProgressBar.ProgressBars[0].progressBar);
    }
    
    [Fact]
    public void GetProgressBar_WithExistingLabel_ReturnsBar()
    {
        var multiProgressBar = new ConsoleMultiProgressBar();
        var progressBar = multiProgressBar.AddProgressBar("Test Task");
        
        var retrieved = multiProgressBar.GetProgressBar("Test Task");
        
        Assert.Equal(progressBar, retrieved);
    }
    
    [Fact]
    public void GetProgressBar_WithNonexistentLabel_ReturnsNull()
    {
        var multiProgressBar = new ConsoleMultiProgressBar();
        
        var retrieved = multiProgressBar.GetProgressBar("Nonexistent");
        
        Assert.Null(retrieved);
    }
    
    [Fact]
    public void RemoveProgressBar_WithExistingLabel_RemovesAndReturnsTrue()
    {
        var multiProgressBar = new ConsoleMultiProgressBar();
        multiProgressBar.AddProgressBar("Task 1");
        multiProgressBar.AddProgressBar("Task 2");
        
        var removed = multiProgressBar.RemoveProgressBar("Task 1");
        
        Assert.True(removed);
        Assert.Single(multiProgressBar.ProgressBars);
        Assert.Equal("Task 2", multiProgressBar.ProgressBars[0].label);
    }
    
    [Fact]
    public void RemoveProgressBar_WithNonexistentLabel_ReturnsFalse()
    {
        var multiProgressBar = new ConsoleMultiProgressBar();
        
        var removed = multiProgressBar.RemoveProgressBar("Nonexistent");
        
        Assert.False(removed);
    }
    
    [Fact]
    public void ClearProgressBars_WhenCalled_RemovesAllBars()
    {
        var multiProgressBar = new ConsoleMultiProgressBar();
        multiProgressBar.AddProgressBar("Task 1");
        multiProgressBar.AddProgressBar("Task 2");
        
        multiProgressBar.ClearProgressBars();
        
        Assert.Empty(multiProgressBar.ProgressBars);
    }
    
    [Fact]
    public void GetDesiredSize_WithTitle_IncludesTitleInHeight()
    {
        var multiProgressBarWithTitle = new ConsoleMultiProgressBar("Progress");
        var multiProgressBarWithoutTitle = new ConsoleMultiProgressBar("");
        
        multiProgressBarWithTitle.AddProgressBar("Task 1");
        multiProgressBarWithoutTitle.AddProgressBar("Task 1");
        
        var sizeWithTitle = multiProgressBarWithTitle.GetDesiredSize(new ConsoleSize(100, 100));
        var sizeWithoutTitle = multiProgressBarWithoutTitle.GetDesiredSize(new ConsoleSize(100, 100));
        
        Assert.True(sizeWithTitle.Height > sizeWithoutTitle.Height);
    }
}