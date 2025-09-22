using GameConsole.UI.Console;

namespace GameConsole.UI.Console.Tests;

public class ConsoleBufferTests
{
    [Fact]
    public void Constructor_WithValidDimensions_SetsProperties()
    {
        var buffer = new ConsoleBuffer(80, 25);
        
        Assert.Equal(80, buffer.Width);
        Assert.Equal(25, buffer.Height);
        Assert.Equal(new ConsoleSize(80, 25), buffer.Size);
    }
    
    [Theory]
    [InlineData(0, 25)]
    [InlineData(-1, 25)]
    [InlineData(80, 0)]
    [InlineData(80, -1)]
    public void Constructor_WithInvalidDimensions_ThrowsArgumentOutOfRangeException(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleBuffer(width, height));
    }
    
    [Fact]
    public void Indexer_WithValidCoordinates_GetsAndSetsCell()
    {
        var buffer = new ConsoleBuffer(10, 10);
        var cell = ConsoleCell.Create('A', ANSIEscapeSequences.FgRed);
        
        buffer[5, 5] = cell;
        
        Assert.Equal(cell, buffer[5, 5]);
    }
    
    [Fact]
    public void Indexer_WithInvalidCoordinates_ReturnsEmptyCell()
    {
        var buffer = new ConsoleBuffer(10, 10);
        
        var result = buffer[-1, 5];
        
        Assert.Equal(ConsoleCell.Empty, result);
    }
    
    [Fact]
    public void SetText_WithValidPosition_SetsTextCorrectly()
    {
        var buffer = new ConsoleBuffer(20, 5);
        var text = "Hello";
        
        buffer.SetText(2, 1, text, ANSIEscapeSequences.FgGreen);
        
        for (int i = 0; i < text.Length; i++)
        {
            var cell = buffer[2 + i, 1];
            Assert.Equal(text[i], cell.Character);
            Assert.Equal(ANSIEscapeSequences.FgGreen, cell.ForegroundColor);
        }
    }
    
    [Fact]
    public void Clear_WhenCalled_ClearsAllCells()
    {
        var buffer = new ConsoleBuffer(5, 5);
        buffer.SetText(0, 0, "Test");
        
        buffer.Clear();
        
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                Assert.Equal(ConsoleCell.Empty, buffer[x, y]);
            }
        }
    }
    
    [Fact]
    public void FillRect_WithValidRect_FillsCorrectly()
    {
        var buffer = new ConsoleBuffer(10, 10);
        var rect = new ConsoleRect(2, 2, 4, 3);
        
        buffer.FillRect(rect, '#', ANSIEscapeSequences.FgBlue);
        
        for (int y = rect.Top; y < rect.Bottom; y++)
        {
            for (int x = rect.Left; x < rect.Right; x++)
            {
                var cell = buffer[x, y];
                Assert.Equal('#', cell.Character);
                Assert.Equal(ANSIEscapeSequences.FgBlue, cell.ForegroundColor);
            }
        }
    }
    
    [Fact]
    public void DrawBorder_WithValidRect_DrawsBorderCorrectly()
    {
        var buffer = new ConsoleBuffer(10, 10);
        var rect = new ConsoleRect(1, 1, 6, 4);
        
        buffer.DrawBorder(rect, ANSIEscapeSequences.FgWhite);
        
        // Check corners
        Assert.Equal('┌', buffer[rect.Left, rect.Top].Character);
        Assert.Equal('┐', buffer[rect.Right - 1, rect.Top].Character);
        Assert.Equal('└', buffer[rect.Left, rect.Bottom - 1].Character);
        Assert.Equal('┘', buffer[rect.Right - 1, rect.Bottom - 1].Character);
        
        // Check top/bottom borders
        Assert.Equal('─', buffer[rect.Left + 1, rect.Top].Character);
        Assert.Equal('─', buffer[rect.Left + 1, rect.Bottom - 1].Character);
        
        // Check left/right borders
        Assert.Equal('│', buffer[rect.Left, rect.Top + 1].Character);
        Assert.Equal('│', buffer[rect.Right - 1, rect.Top + 1].Character);
    }
    
    [Fact]
    public void Render_WithFormattedCells_ReturnsCorrectANSIString()
    {
        var buffer = new ConsoleBuffer(3, 2);
        buffer.SetText(0, 0, "Hi", ANSIEscapeSequences.FgRed);
        buffer.SetText(0, 1, "Yo", ANSIEscapeSequences.FgBlue);
        
        var result = buffer.Render();
        
        Assert.Contains(ANSIEscapeSequences.FgRed, result);
        Assert.Contains(ANSIEscapeSequences.FgBlue, result);
        Assert.Contains("Hi", result);
        Assert.Contains("Yo", result);
        Assert.EndsWith(ANSIEscapeSequences.Reset, result);
    }
    
    [Fact]
    public void CopyFrom_WithValidSource_CopiesCorrectly()
    {
        var source = new ConsoleBuffer(5, 5);
        var dest = new ConsoleBuffer(10, 10);
        
        source.SetText(1, 1, "Test", ANSIEscapeSequences.FgGreen);
        
        dest.CopyFrom(source, new ConsoleRect(0, 0, 5, 5), new ConsolePoint(2, 3));
        
        // Check that text was copied to destination
        var cell = dest[3, 4]; // 1,1 from source should be at 3,4 in dest (2+1, 3+1)
        Assert.Equal('T', cell.Character);
        Assert.Equal(ANSIEscapeSequences.FgGreen, cell.ForegroundColor);
    }
}